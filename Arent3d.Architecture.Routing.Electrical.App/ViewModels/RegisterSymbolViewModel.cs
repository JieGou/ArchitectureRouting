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
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Arent3d.Utility ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class RegisterSymbolViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private string? _browseFolderPath ;

    public readonly string[] AllowedExtensions = { "*.dwg", "*.png", "*.jpg" } ;

    private ObservableCollection<FolderModel>? _folders ;

    public ObservableCollection<FolderModel> Folders
    {
      get { return _folders ??= new ObservableCollection<FolderModel>() ; }
      set
      {
        _folders = value ;
        Previews = new ObservableCollection<PreviewModel>() ;
        OnPropertyChanged() ;
      }
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

    public RegisterSymbolViewModel( UIDocument uiDocument )
    {
      _uiDocument = uiDocument ;
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
            folderBrowserDialog.Description = $"Select folder contains the {string.Join( ",", AllowedExtensions )} file extension." ;
            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK && ! string.IsNullOrWhiteSpace( folderBrowserDialog.SelectedPath ) ) {
              _browseFolderPath = folderBrowserDialog.SelectedPath ;
              Folders = new ObservableCollection<FolderModel>( GetFolders( _browseFolderPath ) ) ;
            }
          }
          catch ( Exception exception ) {
            System.Windows.MessageBox.Show( exception.Message, "Arent Notification" ) ;
          }

          wd?.Show() ;
        } ) ;
      }
    }

    public ICommand SelectedItemCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, tv =>
        {
          var folder = FindSelectedFolder( Folders ) ;
          if ( null == folder ) return ;

          Previews = new ObservableCollection<PreviewModel>( GetPreviewFiles( folder.Path ) ) ;
        } ) ;
      }
    }

    #endregion

    #region Methods

    private IEnumerable<FolderModel> GetFolders( string? browseFolderPath )
    {
      var folders = new List<FolderModel>() ;

      if ( null != browseFolderPath && Directory.Exists( browseFolderPath ) ) {
        var directoryInfo = new DirectoryInfo( browseFolderPath ) ;
        if ( ! directoryInfo.Attributes.HasFlag( FileAttributes.Hidden ) ) {
          folders.Add( new FolderModel { Name = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = false } ) ;
        }

        foreach ( var path in Directory.GetDirectories( browseFolderPath ) ) {
          directoryInfo = new DirectoryInfo( path ) ;
          if ( directoryInfo.Attributes.HasFlag( FileAttributes.Hidden ) )
            continue ;

          var folder = new FolderModel { Name = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = false } ;
          var subPaths = Directory.GetDirectories( folder.Path ) ;
          if ( subPaths.Length > 0 ) {
            RecursiveFolder( subPaths, ref folder ) ;
          }

          folders.Add( folder ) ;
        }
      }
      else {
        System.Windows.MessageBox.Show( "The folder path does not exist!", "Arent Notification" ) ;
      }

      return folders ;
    }

    private void RecursiveFolder( IEnumerable<string> paths, ref FolderModel folderModel )
    {
      foreach ( var path in paths ) {
        var directoryInfo = new DirectoryInfo( path ) ;
        if ( directoryInfo.Attributes.HasFlag( FileAttributes.Hidden ) )
          continue ;

        var subFolderModel = new FolderModel { Name = directoryInfo.Name, Path = directoryInfo.FullName, IsExpanded = false } ;
        var subPaths = Directory.GetDirectories( directoryInfo.FullName ) ;
        if ( subPaths.Length > 0 ) {
          RecursiveFolder( subPaths, ref subFolderModel ) ;
        }

        folderModel.Folders.Add( subFolderModel ) ;
      }
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

    public IEnumerable<PreviewModel> GetPreviewFiles( string? folderPath )
    {
      var previewModels = new List<PreviewModel>() ;
      if ( string.IsNullOrEmpty( folderPath ) || ! Directory.Exists( folderPath ) )
        return previewModels ;

      foreach ( var allowedExtension in AllowedExtensions ) {
        var filePaths = Directory.GetFiles( folderPath, allowedExtension, SearchOption.TopDirectoryOnly ) ;

        foreach ( var filePath in filePaths ) {
          var fileInfo = new FileInfo( filePath ) ;
          var bitmap = ThumbnailProvider.GetThumbnail( fileInfo.FullName, 75, 75, ThumbnailOptions.ThumbnailOnly ) ;

          previewModels.Add( new PreviewModel() { FileName = fileInfo.Name, Thumbnail = Imaging.CreateBitmapSourceFromHBitmap( bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions() ) } ) ;
        }
      }

      return previewModels.OrderBy( x => x.FileName ) ;
    }

    #endregion
  }
}