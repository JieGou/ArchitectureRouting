﻿using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Text ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using System.Windows.Interop ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Microsoft.WindowsAPICodePack.Shell ;
using ImageType = Autodesk.Revit.DB.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class RegisterSymbolViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly StorageService<Level, RegisterSymbolModel> _storageService ;
    private readonly SetupPrintStorable _setupPrintStorable ;
    private readonly bool _isExistBrowseFolderPath ;
    private readonly bool _isExistFolderSelectedPath ;

    private const string Prefix = "Arent_Symbol-CAD" ;

    public const string DwgExtension = ".dwg" ;
    public const string PngExtension = ".png" ;
    public const string PdfExtension = ".pdf" ;
    public const string TifExtension = ".tif" ;
    public readonly string[] PatternSearchings = { $"*{DwgExtension}", $"*{PngExtension}", $"*{PdfExtension}", $"*{TifExtension}" } ;

    private ObservableCollection<FolderModel>? _folders ;

    public ObservableCollection<FolderModel> Folders
    {
      get
      {
        if ( ! _isExistBrowseFolderPath )
          return _folders ??= new ObservableCollection<FolderModel>() ;

        if ( null != _folders )
          return _folders ;

        var folderModel = GetFolderModel( _storageService.Data.BrowseFolderPath ) ;
        var folderModelList = new List<FolderModel>() ;
        if ( null != folderModel ) folderModelList.Add( folderModel ) ;
        _folders = new ObservableCollection<FolderModel>( folderModelList ) ;

        FolderSelected = FindSelectedFolder( _folders ) ;
        Previews = new ObservableCollection<PreviewModel>( GetPreviewFiles( FolderSelected?.Path ) ) ;

        return _folders ;
      }
      set
      {
        _folders = value ;
        FolderSelected = FindSelectedFolder( _folders ) ;
        Previews = new ObservableCollection<PreviewModel>( GetPreviewFiles( FolderSelected?.Path ) ) ;
        OnPropertyChanged() ;
      }
    }

    private FolderModel? _folderSelected ;

    private FolderModel? FolderSelected
    {
      get { return _folderSelected ??= FindSelectedFolder( Folders ) ; }
      set => _folderSelected = value ;
    }

    private ObservableCollection<PreviewModel>? _previews ;

    public ObservableCollection<PreviewModel> Previews
    {
      get { return _previews ??= new ObservableCollection<PreviewModel>() ; }
      set
      {
        _previews = value ;
        OnPropertyChanged() ;
      }
    }

    private bool _isInsertConnector = true;

    public bool IsInsertConnector
    {
      get => _isInsertConnector ;
      set { _isInsertConnector = value ; OnPropertyChanged(); }
    }

    private PreviewModel? _previewSelected ;
    public ExternalEventHandler? ExternalEventHandler { get ; set ; }

    public RegisterSymbolViewModel( UIDocument uiDocument )
    {
      _uiDocument = uiDocument ;
      _storageService = new StorageService<Level, RegisterSymbolModel>(((ViewPlan)uiDocument.ActiveView).GenLevel) ;
      _setupPrintStorable = _uiDocument.Document.GetSetupPrintStorable() ;
      _isExistBrowseFolderPath = Directory.Exists( _storageService.Data.BrowseFolderPath ) ;
      _isExistFolderSelectedPath = Directory.Exists( _storageService.Data.FolderSelectedPath ) ;
    }

    #region Commands

    public ICommand BrowseCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          wd?.Hide() ;

          try {
            using var folderBrowserDialog = new FolderBrowserDialog { ShowNewFolderButton = true } ;
            var path = GetSettingPath( _uiDocument.Document ) ;
            folderBrowserDialog.Reset() ;
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer ;
            folderBrowserDialog.Description = $"Select folder contains the {string.Join( ",", PatternSearchings )} file extension." ;
            folderBrowserDialog.SelectedPath = string.IsNullOrEmpty(_storageService.Data.BrowseFolderPath) ? ReadFileTxtIncludePath(path) : _storageService.Data.BrowseFolderPath ;
            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK && ! string.IsNullOrWhiteSpace( folderBrowserDialog.SelectedPath ) ) {
              _storageService.Data.BrowseFolderPath = folderBrowserDialog.SelectedPath ;
              WriteFileTxtIncludePath(path, folderBrowserDialog.SelectedPath ) ;
              var folderModel = GetFolderModel( _storageService.Data.BrowseFolderPath ) ;
              var folderModelList = new List<FolderModel>() ;
              if ( null != folderModel ) {
                SetFolderByOldSelectedFolder( folderModel ) ;
                folderModelList.Add( folderModel ) ;
              }
              Folders = new ObservableCollection<FolderModel>( folderModelList ) ;
            }
          }
          catch ( Exception exception ) {
            System.Windows.MessageBox.Show( exception.Message, "Arent Notification" ) ;
          }

          wd?.Show() ;
        } ) ;
      }
    }

    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          _previewSelected = Previews.SingleOrDefault( x => x.IsSelected ) ;
          if ( null != _previewSelected ) {
            ExternalEventHandler?.AddAction( Import )?.Raise() ;
            wd.Close() ;
          }
          else {
            System.Windows.MessageBox.Show( "Please, select a file at the preview!", "Arent Notification" ) ;
          }
        } ) ;
      }
    }

    public ICommand SaveCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          ExternalEventHandler?.AddAction( SaveSettingData )?.Raise() ;
          wd.Close() ;
        } ) ;
      }
    }

    public ICommand SelectedItemCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, _ =>
        {
          FolderSelected = FindSelectedFolder( Folders ) ;
          Previews = new ObservableCollection<PreviewModel>( GetPreviewFiles( FolderSelected?.Path ) ) ;
        } ) ;
      }
    }

    #endregion

    #region Methods

    private FolderModel? GetFolderModel( string? browseFolderPath )
    {
      FolderModel? folderModel = null ;

      if ( null != browseFolderPath && Directory.Exists( browseFolderPath ) ) {
        var directoryInfo = new DirectoryInfo( browseFolderPath ) ;
        if ( ! directoryInfo.Attributes.HasFlag( FileAttributes.Hidden ) ) {
          var (isExpanded, isSelected) = IsNodeSelected( directoryInfo ) ;
          folderModel = new FolderModel { Name = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = isExpanded, IsSelected = isSelected } ;
        }

        foreach ( var path in Directory.GetDirectories( browseFolderPath ) ) {
          directoryInfo = new DirectoryInfo( path ) ;
          if ( directoryInfo.Attributes.HasFlag( FileAttributes.Hidden ) )
            continue ;

          var (isExpanded, isSelected) = IsNodeSelected( directoryInfo ) ;
          var subFolderModel = new FolderModel { Name = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = isExpanded, IsSelected = isSelected } ;
          var subPaths = Directory.GetDirectories( subFolderModel.Path ) ;
          if ( subPaths.Length > 0 ) {
            RecursiveFolder( subPaths, ref subFolderModel ) ;
          }

          folderModel?.Folders.Add( subFolderModel ) ;
        }
      }
      else {
        System.Windows.MessageBox.Show( "The folder path does not exist!", "Arent Notification" ) ;
      }

      return folderModel ;
    }

    private void SetFolderByOldSelectedFolder( FolderModel folder )
    {
      if ( ! Folders.Any() ) return ;
      var prevFolder = FindOldFolder( folder.Path, Folders ) ;
      if ( prevFolder != null ) {
        folder.IsExpanded = prevFolder.IsExpanded ;
        folder.IsSelected = prevFolder.IsSelected ;
      }

      if ( ! folder.Folders.Any() ) return ;
      foreach ( var subFolder in folder.Folders ) {
        SetFolderByOldSelectedFolder( subFolder ) ;
        if ( subFolder.IsSelected || subFolder.IsExpanded )
          folder.IsExpanded = true ;
      }
    }

    private FolderModel? FindOldFolder( string path, IEnumerable<FolderModel> folders )
    {
      var folderModels = folders.ToList() ;
      var prevFolder = folderModels.FirstOrDefault( f => f.Path == path ) ;
      if ( prevFolder != null ) return prevFolder ;
      foreach ( var folder in folderModels ) {
        if ( prevFolder == null && folder.Folders.Any() ) {
          prevFolder = FindOldFolder( path, folder.Folders ) ;
        }
      }

      return prevFolder ;
    }

    private void RecursiveFolder( IEnumerable<string> paths, ref FolderModel folderModel )
    {
      foreach ( var path in paths ) {
        var directoryInfo = new DirectoryInfo( path ) ;
        if ( directoryInfo.Attributes.HasFlag( FileAttributes.Hidden ) )
          continue ;

        var (isExpanded, isSelected) = IsNodeSelected( directoryInfo ) ;
        var subFolderModel = new FolderModel { Name = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = isExpanded, IsSelected = isSelected } ;
        var subPaths = Directory.GetDirectories( directoryInfo.FullName ) ;
        if ( subPaths.Length > 0 ) {
          RecursiveFolder( subPaths, ref subFolderModel ) ;
        }

        folderModel.Folders.Add( subFolderModel ) ;
      }
    }

    private (bool IsExpanded, bool IsSelected) IsNodeSelected( FileSystemInfo directoryInfo )
    {
      if ( ! _isExistFolderSelectedPath )
        return ( false, false ) ;

      if ( directoryInfo.FullName.Length > _storageService.Data.FolderSelectedPath.Length || ! _storageService.Data.FolderSelectedPath.StartsWith( directoryInfo.FullName ) )
        return ( false, false ) ;

      return directoryInfo.FullName.Length < _storageService.Data.FolderSelectedPath.Length ? ( true, false ) : ( true, true ) ;
    }

    private static FolderModel? FindSelectedFolder( IEnumerable<FolderModel> folders )
    {
      foreach ( var folder in folders ) {
        if ( folder.IsSelected )
          return folder ;

        if ( ! folder.Folders.Any() )
          continue ;

        var subFolder = FindSelectedFolder( folder.Folders ) ;
        if ( null != subFolder )
          return subFolder ;
      }

      return null ;
    }

    private IEnumerable<PreviewModel> GetPreviewFiles( string? folderPath )
    {
      var previewModels = new List<PreviewModel>() ;
      if ( string.IsNullOrEmpty( folderPath ) || ! Directory.Exists( folderPath ) )
        return previewModels ;

      foreach ( var allowedExtension in PatternSearchings ) {
        var filePaths = Directory.GetFiles( folderPath, allowedExtension, SearchOption.TopDirectoryOnly ) ;

        foreach ( var filePath in filePaths ) {
          var fileInfo = new FileInfo( filePath ) ;
          var bitmap = ShellFile.FromFilePath( fileInfo.FullName ).Thumbnail.LargeBitmap ;

          previewModels.Add( new PreviewModel { FileName = fileInfo.Name, Thumbnail = Imaging.CreateBitmapSourceFromHBitmap( bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions() ), Path = fileInfo.FullName } ) ;
        }
      }

      return previewModels.OrderBy( x => x.FileName ) ;
    }

    private void SaveSettingData()
    {
      using var transaction = new Transaction( _uiDocument.Document ) ;
      transaction.Start( "Save Setting Data" ) ;
      _storageService.Data.FolderSelectedPath = FolderSelected?.Path ?? string.Empty ;
      _storageService.SaveChange();
      transaction.Commit() ;
    }

    private void Import()
    {
      SaveSettingData() ;
      switch ( Path.GetExtension( _previewSelected!.FileName ) ) {
        case DwgExtension :
          ImportDwgFile( _previewSelected ) ;
          break ;
        case PngExtension or PdfExtension or TifExtension :
          ImportImageFile( _previewSelected ) ;
          break ;
      }
    }

    private void ImportDwgFile( PreviewModel previewFile )
    {
      var pickPoint = _uiDocument.Selection.PickPoint( ObjectSnapTypes.Points, "図形の配置位置を指定してください。" ) ;
      
      var ratio =  _setupPrintStorable.Scale * _setupPrintStorable.Ratio;
      var strRatio = $"{Math.Round( ratio )}" ;
      var name = Path.GetFileNameWithoutExtension( previewFile.FileName ) ;
      var extension = Path.GetExtension( previewFile.FileName ) ;

      using var transactionGroup = new TransactionGroup( _uiDocument.Document ) ;
      transactionGroup.Start( "Import CAD" ) ;
      
      if ( IsInsertConnector ) {
        var familySymbol = FindOrCreateConnector(previewFile.Path, name, ratio, strRatio) ;
        if ( null == familySymbol )
          return ;

        using var transaction = new Transaction( _uiDocument.Document ) ;
        transaction.Start( "Create Instance" ) ;
        if ( ! familySymbol.IsActive )
          familySymbol.Activate() ;
        
        var familyInstance = _uiDocument.Document.Create.NewFamilyInstance( pickPoint, familySymbol, _uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;
        var heightOfConnector = _uiDocument.Document.GetHeightSettingStorable()[ _uiDocument.ActiveView.GenLevel ].HeightOfConnectors.MillimetersToRevitUnits() ;
        familyInstance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( heightOfConnector ) ;
        transaction.Commit() ;
      }
      else {
        var cadLinkType = new FilteredElementCollector( _uiDocument.Document ).OfClass( typeof( CADLinkType ) ).OfType<CADLinkType>().SingleOrDefault( x => x.Name == $"{name}_{strRatio}{extension}" ) ;
      
        using var transaction = new Transaction( _uiDocument.Document ) ;
        transaction.Start( "Import DWG File" ) ;
        if ( null != cadLinkType ) {
          var importInstance = ImportInstance.Create( _uiDocument.Document, cadLinkType.Id, _uiDocument.ActiveView ) ;
          if ( importInstance.Pinned )
            importInstance.Pinned = false ;
          var boundingBox = importInstance.get_BoundingBox( _uiDocument.ActiveView ) ;
          var centerPoint = ( boundingBox.Min + boundingBox.Max ) * 0.5 ;
          ElementTransformUtils.MoveElement( _uiDocument.Document, importInstance.Id, pickPoint - centerPoint ) ;
        }
        else {
          var options = new DWGImportOptions { ReferencePoint = pickPoint, ThisViewOnly = true, Placement = ImportPlacement.Centered, Unit = ImportUnit.Default, CustomScale = _uiDocument.ActiveView.Scale } ;
          var result = _uiDocument.Document.Import( previewFile.Path, options, _uiDocument.ActiveView, out var elementId ) ;
          if ( ! result ) {
            System.Windows.MessageBox.Show( "図面ファイルが無効です。", "Arent Notification" ) ;
          }
          else {
            cadLinkType = (CADLinkType) _uiDocument.Document.GetElement( _uiDocument.Document.GetElement(elementId).GetTypeId() ) ;
            cadLinkType.Name = $"{name}_{strRatio}{extension}" ;
            cadLinkType.get_Parameter( BuiltInParameter.IMPORT_SCALE ).Set( ratio.MillimetersToRevitUnits() ) ;
          }
        }
        transaction.Commit() ;
      }

      transactionGroup.Assimilate() ;
    }

    private void ImportImageFile( PreviewModel previewFile )
    {
      using var transaction = new Transaction( _uiDocument.Document ) ;
      transaction.Start( "Import Image File" ) ;

      var filter = new FilteredElementCollector( _uiDocument.Document ) ;
      var imageType = filter.OfClass( typeof( ImageType ) ).OfType<ImageType>().SingleOrDefault( x => x.Name == previewFile.FileName ) ;

      var pickPoint = _uiDocument.Selection.PickPoint( ObjectSnapTypes.Points, "図形の配置位置を指定してください。" ) ;

#if REVIT2021_OR_GREATER
      var optionInstance = new ImagePlacementOptions { PlacementPoint = BoxPlacement.Center, Location = pickPoint } ;
      if ( null == imageType ) {
        var optionType = new ImageTypeOptions( previewFile.Path, false, ImageTypeSource.Import ) ;
        imageType = ImageType.Create( _uiDocument.Document, optionType ) ;
      }
#endif

#if REVIT2020
      var optionInstance = new ImagePlacementOptions { PlacementPoint = BoxPlacement.Center, Location = pickPoint } ;
      if ( null == imageType ) {
        var optionType = new ImageTypeOptions( previewFile.Path, false ) ;
        imageType = ImageType.Create( _uiDocument.Document, optionType ) ;
      }
#endif

#if REVIT2020_OR_GREATER
      if ( ! ImageInstance.IsValidView( _uiDocument.ActiveView ) ) return ;
      ImageInstance.Create( _uiDocument.Document, _uiDocument.ActiveView, imageType.Id, optionInstance ) ;
#elif REVIT2019
      _uiDocument.Document.Import( previewFile.Path, new ImageImportOptions { RefPoint = pickPoint, Placement = BoxPlacement.Center }, _uiDocument.ActiveView, out _ ) ;
#endif

      transaction.Commit() ;
    }

    private FamilySymbol? FindOrCreateConnector(string filePath, string fileName, double ratio, string strRatio)
    {
      var familyName = $"{Prefix}_{fileName}-{strRatio}" ;
      var electricalFixtureFamilySymbol = _uiDocument.Document.GetAllTypes<FamilySymbol>( x => x.Family.Name == familyName ).FirstOrDefault() ;
      if ( null != electricalFixtureFamilySymbol )
        return electricalFixtureFamilySymbol ;

      var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault( x => x.GetName().Name == "Arent3d.Architecture.Routing" ) ;
      if ( null == assembly )
        return null ;
      
      var electricalFixtureTemplateFamilyPath = GetFamilyPath( assembly, "Metric Electrical Fixture.rft" ) ;
      if ( string.IsNullOrEmpty( electricalFixtureTemplateFamilyPath ) )
        return null ;
      
      var electricalFixtureDocument = _uiDocument.Document.Application.NewFamilyDocument( electricalFixtureTemplateFamilyPath ) ;
      File.Delete(electricalFixtureTemplateFamilyPath);
      
      var viewPlan = electricalFixtureDocument.GetAllElements<ViewPlan>().FirstOrDefault(x => x.Name == "Ref. Level") ;
      if ( null == viewPlan )
        return null ;

      using var electricalFixtureTransaction = new Transaction( electricalFixtureDocument ) ;
      
      electricalFixtureTransaction.Start( "New Type" ) ;
      var familyType = electricalFixtureDocument.FamilyManager.NewType( familyName ) ;
      electricalFixtureDocument.FamilyManager.CurrentType = familyType ;
      electricalFixtureTransaction.Commit() ;
      
      electricalFixtureTransaction.Start( "Import CAD" ) ;
      var option = new DWGImportOptions { Placement = ImportPlacement.Origin, ThisViewOnly = true, ReferencePoint = XYZ.Zero, Unit = ImportUnit.Default } ;
      var result = electricalFixtureDocument.Import( filePath, option, viewPlan, out var importInstanceElementId ) ;
      if ( ! result ) {
        electricalFixtureTransaction.RollBack() ;
        return null ;
      }
      var cadLinkType = (CADLinkType) electricalFixtureDocument.GetElement( electricalFixtureDocument.GetElement( importInstanceElementId ).GetTypeId() ) ;
      cadLinkType.get_Parameter( BuiltInParameter.IMPORT_SCALE ).Set( ratio ) ;
      electricalFixtureTransaction.Commit() ;

      electricalFixtureTransaction.Start( "Create Solid" ) ;
      var solid = CreateCubeSolid() ;
      var freeFormElement = FreeFormElement.Create( electricalFixtureDocument, solid ) ;
      var elementVisibility = new FamilyElementVisibility( FamilyElementVisibilityType.Model ) { IsShownInFrontBack = false, IsShownInLeftRight = false, IsShownInPlanRCPCut = false, IsShownInTopBottom = false } ;
      freeFormElement.SetVisibility( elementVisibility ) ;
      electricalFixtureTransaction.Commit() ;
      
      electricalFixtureTransaction.Start( "Create Connector" ) ;
      var plannarFace = GetPlanarFaceTop( freeFormElement ) ;
      ConnectorElement.CreateConduitConnector( electricalFixtureDocument, plannarFace.Reference ) ;
      electricalFixtureTransaction.Commit() ;
      
      var electricalFixtureFamilyPath = Path.Combine( Path.GetTempPath(), $"{familyName}.rfa" ) ;
      electricalFixtureDocument.SaveAs( electricalFixtureFamilyPath, new SaveAsOptions { MaximumBackups = 1, OverwriteExistingFile = true } ) ;
      electricalFixtureDocument.Close( true ) ;

      using var transaction = new Transaction( _uiDocument.Document ) ;
      transaction.Start( "Load Family" ) ;
      _uiDocument.Document.LoadFamily( electricalFixtureFamilyPath, new FamilyLoadOptions(), out var electricalFamily ) ;
      File.Delete(electricalFixtureFamilyPath);
      transaction.Commit() ;
      
      electricalFixtureFamilySymbol =  new FilteredElementCollector( _uiDocument.Document ).WherePasses( new FamilySymbolFilter( electricalFamily.Id ) ).OfType<FamilySymbol>().FirstOrDefault() ;
      return electricalFixtureFamilySymbol ;
    }

    private static string? GetFamilyPath(Assembly assembly, string familyName )
    {
      var resourceFullName = assembly.GetManifestResourceNames().FirstOrDefault(element => element.EndsWith(familyName));
      if ( string.IsNullOrEmpty( resourceFullName ) )
        return null ;
      
      using var stream = assembly.GetManifestResourceStream(resourceFullName);
      if ( null == stream )
        return null ;
      
      var fileData = new byte[stream.Length];
      stream.Read(fileData, 0, fileData.Length);
      
      var pathFamily = Path.Combine(Path.GetTempPath(), familyName);
      File.WriteAllBytes(pathFamily, fileData);

      return pathFamily ;
    }
    
    private static Solid CreateCubeSolid()
    {
      var halfLength = 100d.MillimetersToRevitUnits() * 0.5;
      var lineOne = Line.CreateBound(XYZ.BasisX * halfLength + XYZ.BasisY.Negate() * halfLength, XYZ.BasisX * halfLength + XYZ.BasisY * halfLength);
      var lineTwo = Line.CreateBound(XYZ.BasisX * halfLength + XYZ.BasisY * halfLength, XYZ.BasisX.Negate() * halfLength + XYZ.BasisY * halfLength);
      var lineThree = Line.CreateBound(XYZ.BasisX.Negate() * halfLength + XYZ.BasisY * halfLength, XYZ.BasisX.Negate() * halfLength + XYZ.BasisY.Negate() * halfLength);
      var lineFour = Line.CreateBound(XYZ.BasisX.Negate() * halfLength + XYZ.BasisY.Negate() * halfLength, XYZ.BasisX * halfLength + XYZ.BasisY.Negate() * halfLength);
      var curveLoop = CurveLoop.Create(new List<Curve>() { lineOne, lineTwo, lineThree, lineFour });
      return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ.Negate(), halfLength * 2);
    }

    private static PlanarFace GetPlanarFaceTop(Element freeFormElement)
    {
      var option = new Options { ComputeReferences = true };
      return freeFormElement.get_Geometry(option).OfType<Solid>().Select(x => x.Faces.OfType<PlanarFace>()).SelectMany(x => x).MaxBy(x => x.Origin.Z)!;
    }

    private static void WriteFileTxtIncludePath(string path, string content)
    {
      File.WriteAllText( path, content, Encoding.UTF8 ) ;
    }

    private static string ReadFileTxtIncludePath(string path)
    {
      using var reader = File.OpenText( path ) ;
      var pathOpenedFolder = reader.ReadLine() ?? string.Empty ;
      reader.Close() ;

      return pathOpenedFolder ;
    }
    
    private static string GetSettingPath(Document document)
    {
      var resourcesPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, "resources" )  ;
      var layerSettingsFileName = "Electrical.App.Commands.Initialization.RegisterSymbolFolderPath".GetDocumentStringByKeyOrDefault( document, "RegisterSymbolFolderPath.txt" ) ;
      var filePath = Path.Combine( resourcesPath, layerSettingsFileName ) ;

      return filePath ;
    }
    
    #endregion
    
  }
}