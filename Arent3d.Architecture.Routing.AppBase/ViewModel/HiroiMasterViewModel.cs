using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Data ;
using System.Windows.Forms ;
using System.Windows.Forms.VisualStyles ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class HiroiMasterViewModel : NotifyPropertyChanged
  {
    private Document? _document ;
    public RelayCommand<Window> AddCeedDetailCommand => new ( AddCeedDetail ) ;

    public HiroiMasterModel? HiroiMasterSelected { get ; set ; }
    private ObservableCollection<HiroiMasterModel> _hiroiMasterList = new() ;

    public ObservableCollection<HiroiMasterModel> HiroiMasterList
    {
      get => _hiroiMasterList ;
      set
      {
        _hiroiMasterList = value ;
        OnPropertyChanged( "HiroiMasterList" ) ;
      }
    }

    private ListCollectionView _view ;
    public ICollectionView View => this._view ;

    private string _searchText = String.Empty ;

    public string SearchText
    {
      get => _searchText ;
      set
      {   
        _searchText = value;
        OnPropertyChanged( "SearchText" ) ;
        View.Refresh() ;
      }
    }

    private bool Filter( object item )
    {
      if ( String.IsNullOrEmpty( SearchText ) )
        return true ;
      else {
        var x = item as HiroiMasterModel ;
        if ( x == null )
          return true ;

        return CheckContainSearchText( x.Buzaicd ) || CheckContainSearchText( x.Buzaisyu ) || CheckContainSearchText( x.Hinmei ) || CheckContainSearchText( x.Hinmeicd ) || CheckContainSearchText( x.Kikaku ) || CheckContainSearchText( x.Ryakumeicd ) || CheckContainSearchText( x.Setubisyu ) ||
               CheckContainSearchText( x.Syurui ) || CheckContainSearchText( x.Type ) || CheckContainSearchText( x.Tani ) ;
      }
    }

    private bool CheckContainSearchText( string text )
    {
      return text?.IndexOf( _searchText.Trim(), StringComparison.OrdinalIgnoreCase ) >= 0 ;
    }

    #region Command

    private void AddCeedDetail(Window window)
    {
      window.DialogResult = true ;
      window.Close() ;
    }
  
    #endregion

    public HiroiMasterViewModel( Document? document, List<HiroiMasterModel> hiroiMasterList )
    {
      _document = document ;
      HiroiMasterList = new ObservableCollection<HiroiMasterModel>( hiroiMasterList ) ;

      this._view = new ListCollectionView( HiroiMasterList ) { Filter = Filter } ;
    }
  }
}