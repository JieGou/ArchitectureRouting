using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using System.Windows.Interop ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Extensions ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
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
    private readonly RegisterSymbolStorable _registerSymbolStorable ;
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

        var folderModel = GetFolderModel( _registerSymbolStorable.BrowseFolderPath ) ;
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
      _registerSymbolStorable = _uiDocument.Document.GetRegisterSymbolStorable() ;
      _setupPrintStorable = _uiDocument.Document.GetSetupPrintStorable() ;
      _isExistBrowseFolderPath = Directory.Exists( _registerSymbolStorable.BrowseFolderPath ) ;
      _isExistFolderSelectedPath = Directory.Exists( _registerSymbolStorable.FolderSelectedPath ) ;
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
            folderBrowserDialog.Reset() ;
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer ;
            folderBrowserDialog.Description = $"Select folder contains the {string.Join( ",", PatternSearchings )} file extension." ;
            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK && ! string.IsNullOrWhiteSpace( folderBrowserDialog.SelectedPath ) ) {
              _registerSymbolStorable.BrowseFolderPath = folderBrowserDialog.SelectedPath ;
              var folderModel = GetFolderModel( _registerSymbolStorable.BrowseFolderPath ) ;
              var folderModelList = new List<FolderModel>() ;
              if ( null != folderModel ) folderModelList.Add( folderModel ) ;
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

      if ( directoryInfo.FullName.Length > _registerSymbolStorable.FolderSelectedPath.Length || ! _registerSymbolStorable.FolderSelectedPath.StartsWith( directoryInfo.FullName ) )
        return ( false, false ) ;

      return directoryInfo.FullName.Length < _registerSymbolStorable.FolderSelectedPath.Length ? ( true, false ) : ( true, true ) ;
    }

    private FolderModel? FindSelectedFolder( IEnumerable<FolderModel> folders )
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
      _registerSymbolStorable.FolderSelectedPath = FolderSelected?.Path ?? string.Empty ;
      _registerSymbolStorable.Save() ;
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
      var materialFreeFormParameter = freeFormElement.get_Parameter( BuiltInParameter.MATERIAL_ID_PARAM ) ;
      var materialFamilyParameter = electricalFixtureDocument.FamilyManager.AddParameter( "Material", GroupTypeId.Materials, SpecTypeId.Reference.Material, false ) ;
      if(electricalFixtureDocument.FamilyManager.CanElementParameterBeAssociated(materialFreeFormParameter))
        electricalFixtureDocument.FamilyManager.AssociateElementParameterToFamilyParameter( materialFreeFormParameter, materialFamilyParameter ) ;
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

    private string? GetFamilyPath(Assembly assembly, string familyName )
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
    
    private Solid CreateCubeSolid()
    {
      var halfLength = UnitUtils.ConvertToInternalUnits(100, UnitTypeId.Millimeters) * 0.5;
      var lineOne = Line.CreateBound(XYZ.BasisX * halfLength + XYZ.BasisY.Negate() * halfLength, XYZ.BasisX * halfLength + XYZ.BasisY * halfLength);
      var lineTwo = Line.CreateBound(XYZ.BasisX * halfLength + XYZ.BasisY * halfLength, XYZ.BasisX.Negate() * halfLength + XYZ.BasisY * halfLength);
      var lineThree = Line.CreateBound(XYZ.BasisX.Negate() * halfLength + XYZ.BasisY * halfLength, XYZ.BasisX.Negate() * halfLength + XYZ.BasisY.Negate() * halfLength);
      var lineFour = Line.CreateBound(XYZ.BasisX.Negate() * halfLength + XYZ.BasisY.Negate() * halfLength, XYZ.BasisX * halfLength + XYZ.BasisY.Negate() * halfLength);
      var curveLoop = CurveLoop.Create(new List<Curve>() { lineOne, lineTwo, lineThree, lineFour });
      return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ.Negate(), halfLength * 2);
    }

    private PlanarFace GetPlanarFaceTop(Element freeFormElement)
    {
      var option = new Options { ComputeReferences = true };
      return freeFormElement.get_Geometry(option).OfType<Solid>().Select(x => x.Faces.OfType<PlanarFace>()).SelectMany(x => x).MaxBy(x => x.Origin.Z)!;
    }

    #endregion
  }
}