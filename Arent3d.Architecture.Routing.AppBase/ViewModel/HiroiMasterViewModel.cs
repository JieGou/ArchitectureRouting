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
    
    private List<HiroiSetMasterModel>? _hiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel>? _hiroiSetMasterEcoModels ;
    private bool _isEcoModel ;

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

    // public string SearchText
    // {
    //   get => _searchText ;
    //   set
    //   {
    //     _searchText = value ;
    //     OnPropertyChanged( "SearchText" ) ; 
    //     var list = HiroiMasterList.ToList() ;
    //     foreach ( var textSearch in SearchText.Split( new char[] { ' ', ';', ',' } ) ) {
    //       if ( string.IsNullOrEmpty( textSearch ) )
    //         continue ;
    //
    //       list = list.FindAll( x => CheckContainSearchText( x, textSearch ) ) ;
    //     }
    //
    //     HiroiMasterListDisplay = new ObservableCollection<HiroiMasterModel>( list ) ; 
    //   }
    // }
    
    public string SearchText
    {
      get => _searchText ;
      set
      {
        _searchText = value ;
        OnPropertyChanged( "SearchText" ) ; 
        var listHiroiMaster = HiroiMasterList.ToList() ;
        var listHiroiSetMaster = _isEcoModel ? _hiroiSetMasterEcoModels?.ToList() : _hiroiSetMasterNormalModels?.ToList() ;
        listHiroiSetMaster = SearchText.Split( new char[] { ' ', ';', ',' } ).Where( textSearch => ! string.IsNullOrEmpty( textSearch ) ).Aggregate( listHiroiSetMaster, ( current, textSearch ) => current!.FindAll( x => CheckContainSearchText( x, textSearch ) ) ) ;

        listHiroiMaster = listHiroiMaster.FindAll( x => listHiroiSetMaster!.FirstOrDefault( y => y.MaterialCode1 == x.Buzaicd || y.MaterialCode2 == x.Buzaicd  || y.MaterialCode3 == x.Buzaicd  || y.MaterialCode4 == x.Buzaicd  || y.MaterialCode5 == x.Buzaicd  || y.MaterialCode6 == x.Buzaicd  || y.MaterialCode7 == x.Buzaicd  || y.MaterialCode8 == x.Buzaicd ) != null ) ;
        HiroiMasterListDisplay = new ObservableCollection<HiroiMasterModel>( listHiroiMaster ) ; 
      }
    }
    
    private bool CheckContainSearchText( HiroiSetMasterModel x, string textSearch )
    {
      if ( string.IsNullOrEmpty( textSearch ) )
        return true ;

      return CheckContainSearchText( x.Name1, textSearch ) || CheckContainSearchText( x.Quantity1, textSearch ) || CheckContainSearchText( x.MaterialCode1, textSearch ) || CheckContainSearchText( x.Name2, textSearch ) || CheckContainSearchText( x.Quantity2, textSearch ) ||
             CheckContainSearchText( x.MaterialCode2, textSearch ) || CheckContainSearchText( x.Name3, textSearch ) || CheckContainSearchText( x.Quantity3, textSearch ) || CheckContainSearchText( x.MaterialCode3, textSearch ) || CheckContainSearchText( x.Name4, textSearch ) ||
             CheckContainSearchText( x.Quantity4, textSearch ) || CheckContainSearchText( x.MaterialCode4, textSearch ) || CheckContainSearchText( x.Name5, textSearch ) || CheckContainSearchText( x.Quantity5, textSearch ) || CheckContainSearchText( x.MaterialCode5, textSearch ) ||
             CheckContainSearchText( x.Name6, textSearch ) || CheckContainSearchText( x.Quantity6, textSearch ) || CheckContainSearchText( x.MaterialCode6, textSearch ) || CheckContainSearchText( x.Name7, textSearch ) || CheckContainSearchText( x.Quantity7, textSearch ) ||
             CheckContainSearchText( x.MaterialCode7, textSearch ) || CheckContainSearchText( x.Name8, textSearch ) || CheckContainSearchText( x.Quantity8, textSearch ) || CheckContainSearchText( x.MaterialCode8, textSearch ) || CheckContainSearchText( x.ParentPartName, textSearch ) ||
             CheckContainSearchText( x.ParentPartsQuantity, textSearch ) || CheckContainSearchText( x.ParentPartModelNumber, textSearch ) ;
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
      return textContainer?.IndexOf( textSearch.Trim(), StringComparison.OrdinalIgnoreCase ) >= 0 || textContainer?.Replace( " ","" ).IndexOf( textSearch.Trim(), StringComparison.OrdinalIgnoreCase ) >= 0 ;
    }

    #region Command

    private void AddCeedDetail( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }

    #endregion

    public HiroiMasterViewModel( Document? document, List<HiroiMasterModel> hiroiMasterList, List<HiroiSetMasterModel>? hiroiSetMasterEcoModels, List<HiroiSetMasterModel>? hiroiSetMasterNormalModels, bool isEcoModel )
    {
      _document = document ;
      HiroiMasterList = new ObservableCollection<HiroiMasterModel>( hiroiMasterList ) ;
      HiroiMasterListDisplay = HiroiMasterList ;

      _hiroiSetMasterEcoModels = hiroiSetMasterEcoModels ;
      _hiroiSetMasterNormalModels = hiroiSetMasterNormalModels ; 
      _isEcoModel = isEcoModel ;

      ChangeMaterialCodeForHiroiSetMasterNormal() ;
    }

    private void ChangeMaterialCodeForHiroiSetMasterNormal()
    {
      if ( null == _hiroiSetMasterNormalModels || ! _hiroiSetMasterNormalModels.Any() ) return ;
      foreach ( var item in _hiroiSetMasterNormalModels ) {
        if ( ! string.IsNullOrEmpty( item.MaterialCode1 ) ) {
          item.MaterialCode1 = item.MaterialCode1.Length switch
          {
            3 => "000" + item.MaterialCode1,
            4 => "00" + item.MaterialCode1,
            5 => "0" + item.MaterialCode1,
            _ => item.MaterialCode1
          } ;
        }
        if ( ! string.IsNullOrEmpty( item.MaterialCode2 ) ) {
          item.MaterialCode2 = item.MaterialCode2.Length switch
          {
            3 => "000" + item.MaterialCode2,
            4 => "00" + item.MaterialCode2,
            5 => "0" + item.MaterialCode2,
            _ => item.MaterialCode2
          } ;
        }
        if ( ! string.IsNullOrEmpty( item.MaterialCode3 ) ) {
          item.MaterialCode3 = item.MaterialCode3.Length switch
          {
            3 => "000" + item.MaterialCode3,
            4 => "00" + item.MaterialCode3,
            5 => "0" + item.MaterialCode3,
            _ => item.MaterialCode3
          } ;
        }   
        if ( ! string.IsNullOrEmpty( item.MaterialCode4 ) ) {
          item.MaterialCode4 = item.MaterialCode4.Length switch
          {
            3 => "000" + item.MaterialCode4,
            4 => "00" + item.MaterialCode4,
            5 => "0" + item.MaterialCode4,
            _ => item.MaterialCode4
          } ;
        }
        if ( ! string.IsNullOrEmpty( item.MaterialCode5 ) ) {
          item.MaterialCode5 = item.MaterialCode5.Length switch
          {
            3 => "000" + item.MaterialCode5,
            4 => "00" + item.MaterialCode5,
            5 => "0" + item.MaterialCode5,
            _ => item.MaterialCode5
          } ;
        }
        if ( ! string.IsNullOrEmpty( item.MaterialCode6 ) ) {
          item.MaterialCode6 = item.MaterialCode6.Length switch
          {
            3 => "000" + item.MaterialCode6,
            4 => "00" + item.MaterialCode6,
            5 => "0" + item.MaterialCode6,
            _ => item.MaterialCode6
          } ;
        }
        if ( ! string.IsNullOrEmpty( item.MaterialCode7 ) ) {
          item.MaterialCode7 = item.MaterialCode7.Length switch
          {
            3 => "000" + item.MaterialCode7,
            4 => "00" + item.MaterialCode7,
            5 => "0" + item.MaterialCode7,
            _ => item.MaterialCode7
          } ;
        }
        if ( ! string.IsNullOrEmpty( item.MaterialCode8 ) ) {
          item.MaterialCode8 = item.MaterialCode8.Length switch
          {
            3 => "000" + item.MaterialCode8,
            4 => "00" + item.MaterialCode8,
            5 => "0" + item.MaterialCode8,
            _ => item.MaterialCode8
          } ;
        }
      }
    }
  } 
}