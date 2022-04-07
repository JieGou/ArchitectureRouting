using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using System.Windows.Interop ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Microsoft.WindowsAPICodePack.Shell ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class RegisterSymbolViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly RegisterSymbolStorable _settingStorable ;
    private readonly bool _isExistBrowseFolderPath ;
    private readonly bool _isExistFolderSelectedPath ;

    public const string DwgExtension = ".dwg" ;
    public const string PngExtension = ".png" ;
    public readonly string[] PatternSearchings = { $"*{DwgExtension}", $"*{PngExtension}" } ;

    private ObservableCollection<FolderModel>? _folders ;

    public ObservableCollection<FolderModel> Folders
    {
      get
      {
        if ( ! _isExistBrowseFolderPath )
          return _folders ??= new ObservableCollection<FolderModel>() ;

        if ( null != _folders )
          return _folders ;

        var folderModel = GetFolderModel( _settingStorable.BrowseFolderPath ) ;
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

    public ExternalEventHandler? ExternalEventHandler { get ; set ; }

    public RegisterSymbolViewModel( UIDocument uiDocument )
    {
      _uiDocument = uiDocument ;
      _settingStorable = _uiDocument.Document.GetRegisterSymbolStorable() ;
      _isExistBrowseFolderPath = Directory.Exists( _settingStorable.BrowseFolderPath ) ;
      _isExistFolderSelectedPath = Directory.Exists( _settingStorable.FolderSelectedPath ) ;
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
              _settingStorable.BrowseFolderPath = folderBrowserDialog.SelectedPath ;
              var folderModel = GetFolderModel( _settingStorable.BrowseFolderPath ) ;
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
          ExternalEventHandler?.AddAction( Import )?.Raise() ;
          wd.Close() ;
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

      if ( directoryInfo.FullName.Length > _settingStorable.FolderSelectedPath.Length || ! _settingStorable.FolderSelectedPath.StartsWith( directoryInfo.FullName ) )
        return ( false, false ) ;

      return directoryInfo.FullName.Length < _settingStorable.FolderSelectedPath.Length ? ( true, false ) : ( true, true ) ;
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
      _settingStorable.FolderSelectedPath = FolderSelected?.Path ?? string.Empty ;
      _settingStorable.Save() ;
      transaction.Commit() ;
    }

    private void Import()
    {
      SaveSettingData() ;
      var previewSelected = Previews.SingleOrDefault( x => x.IsSelected ) ;
      if ( null != previewSelected ) {
        switch ( Path.GetExtension( previewSelected.FileName ) ) {
          case DwgExtension :
            ImportDwgFile( previewSelected ) ;
            break ;
          case PngExtension :
            ImportImageFile( previewSelected ) ;
            break ;
        }
      }
      else {
        System.Windows.MessageBox.Show( "Please, select a file at the preview!", "Arent Notification" ) ;
      }
    }

    private void ImportDwgFile( PreviewModel previewFile )
    {
      using var transaction = new Transaction( _uiDocument.Document ) ;
      transaction.Start( "Import DWG File" ) ;

      var filter = new FilteredElementCollector( _uiDocument.Document ) ;
      var cadLinkType = filter.OfClass( typeof( CADLinkType ) ).OfType<CADLinkType>().SingleOrDefault( x => x.Name == previewFile.FileName ) ;

      var pickPoint = _uiDocument.Selection.PickPoint( ObjectSnapTypes.Points, "図形の配置位置を指定してください。" ) ;
      if ( null != cadLinkType ) {
        var importInstance = ImportInstance.Create( _uiDocument.Document, cadLinkType.Id, _uiDocument.ActiveView ) ;
        if ( importInstance.Pinned ) importInstance.Pinned = false ;
        var boundingBox = importInstance.get_BoundingBox( _uiDocument.ActiveView ) ;
        var centerPoint = ( boundingBox.Min + boundingBox.Max ) * 0.5 ;
        ElementTransformUtils.MoveElement( _uiDocument.Document, importInstance.Id, pickPoint - centerPoint ) ;
      }
      else {
        var options = new DWGImportOptions { ReferencePoint = pickPoint, ThisViewOnly = true, Placement = ImportPlacement.Centered, Unit = ImportUnit.Default } ;
        var result = _uiDocument.Document.Import( previewFile.Path, options, _uiDocument.ActiveView, out _ ) ;
        if ( ! result ) {
          System.Windows.MessageBox.Show( "図面ファイルが無効です。", "Arent Notification" ) ;
        }
      }

      transaction.Commit() ;
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
      _uiDocument.Document.Import( previewFile.Path, new ImageImportOptions { RefPoint = pickPoint, Placement =
 BoxPlacement.Center }, _uiDocument.ActiveView, out _ ) ;
#endif

      transaction.Commit() ;
    }

    #endregion
  }
}