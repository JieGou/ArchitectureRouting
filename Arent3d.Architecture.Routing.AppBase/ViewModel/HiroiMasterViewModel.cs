using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class HiroiMasterViewModel : NotifyPropertyChanged
  {
    private Document? _document ;
    public RelayCommand<Window> AddCeedDetailCommand => new(AddCeedDetail) ;

    public HiroiMasterModel? HiroiMasterSelected { get ; set ; }
    private ObservableCollection<HiroiMasterModel> _hiroiMasterList = new() ;

    private ObservableCollection<HiroiMasterModel> _hiroiMasterListDisplay = new() ;

    public ObservableCollection<HiroiMasterModel> HiroiMasterListDisplay
    {
      get => _hiroiMasterListDisplay ;
      set
      {
        _hiroiMasterListDisplay = value ;
        OnPropertyChanged( "HiroiMasterListDisplay" ) ;
      }
    }

    public ObservableCollection<HiroiMasterModel> HiroiMasterList
    {
      get => _hiroiMasterList ;
      set
      {
        _hiroiMasterList = value ;
        OnPropertyChanged( "HiroiMasterList" ) ;
      }
    }
  
    private string _searchText = String.Empty ; 

    public string SearchText
    {
      get => _searchText ;
      set
      {
        _searchText = value ;
        OnPropertyChanged( "SearchText" ) ; 
        var list = HiroiMasterList.ToList() ;
        foreach ( var textSearch in SearchText.Split( new char[] { ' ', ';', ',' } ) ) {
          if ( string.IsNullOrEmpty( textSearch ) )
            continue ;

          list = list.FindAll( x => CheckContainSearchText( x, textSearch ) ) ;
        }

        HiroiMasterListDisplay = new ObservableCollection<HiroiMasterModel>( list ) ; 
      }
    }
 
    private bool CheckContainSearchText( HiroiMasterModel x, string textSearch )
    {
      if ( string.IsNullOrEmpty( textSearch ) )
        return true ;

      return CheckContainSearchText( x.Buzaicd, textSearch ) || CheckContainSearchText( x.Buzaisyu, textSearch ) || CheckContainSearchText( x.Hinmei, textSearch ) || CheckContainSearchText( x.Hinmeicd, textSearch ) || CheckContainSearchText( x.Kikaku, textSearch ) ||
             CheckContainSearchText( x.Ryakumeicd, textSearch ) || CheckContainSearchText( x.Setubisyu, textSearch ) || CheckContainSearchText( x.Syurui, textSearch ) || CheckContainSearchText( x.Type, textSearch ) || CheckContainSearchText( x.Tani, textSearch ) ;
    }

    private bool CheckContainSearchText( string textContainer, string textSearch )
    {
      return textContainer?.IndexOf( textSearch.Trim(), StringComparison.OrdinalIgnoreCase ) >= 0 ;
    }

    #region Command

    private void AddCeedDetail( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    #endregion

    public HiroiMasterViewModel( Document? document, List<HiroiMasterModel> hiroiMasterList )
    {
      _document = document ;
      HiroiMasterList = new ObservableCollection<HiroiMasterModel>( hiroiMasterList ) ;
      HiroiMasterListDisplay = HiroiMasterList ;  
    }
  } 
}