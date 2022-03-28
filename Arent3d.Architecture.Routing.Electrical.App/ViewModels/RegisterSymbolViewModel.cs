using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class RegisterSymbolViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private string? _browseFolderPath ;
    private const string SearchPattern = "*.dwg|*.png|*.jpg" ;

    private ObservableCollection<FolderItemModel>? _folderItems ;

    public ObservableCollection<FolderItemModel> FolderItems
    {
      get { return _folderItems ??= new ObservableCollection<FolderItemModel>() ; }
      set
      {
        _folderItems = value ;
        OnPropertyChanged();
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

          using var folderBrowserDialog = new FolderBrowserDialog { ShowNewFolderButton = true } ;
          folderBrowserDialog.Reset() ;
          folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer ;
          folderBrowserDialog.Description = "Select folder contains .dwg and image file." ;
          if ( folderBrowserDialog.ShowDialog() == DialogResult.OK && ! string.IsNullOrWhiteSpace( folderBrowserDialog.SelectedPath ) )
            _browseFolderPath = folderBrowserDialog.SelectedPath ;

          wd?.Show() ;
        } ) ;
      }
    }

    #endregion

    #region Methods

    private void GetFolders()
    {
      var folderItems = new List<FolderItemModel>() ;
      if ( null != _browseFolderPath && Directory.Exists( _browseFolderPath ) ) {
        foreach ( var directory in Directory.GetDirectories(_browseFolderPath, SearchPattern, SearchOption.TopDirectoryOnly) ) {
          
        }
      }
      else {
        System.Windows.MessageBox.Show( "The folder path does not exist.", "Arent Notification" ) ;
      }
    }

    #endregion
    
  }
}