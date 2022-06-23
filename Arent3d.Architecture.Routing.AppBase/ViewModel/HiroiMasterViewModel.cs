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

    private bool hasOneWordOnly = false ;
    public string SearchText
    {
      get => _searchText ;
      set
      {
        _searchText = value ;
        OnPropertyChanged( "SearchText" ) ;

        hasOneWordOnly = SearchText.Split( new char[] { ' ', ';', ',' } ).Length <= 1 ;
        
        var listHiroiMaster = HiroiMasterList.ToList() ;
        var listHiroiSetMaster = _isEcoModel ? _hiroiSetMasterEcoModels?.ToList() : _hiroiSetMasterNormalModels?.ToList() ;
        var materialCodes = new List<string>() ;
        foreach ( var textSearch in SearchText.Split( new char[] { ' ', ';', ',' } ).Where( textSearch => ! string.IsNullOrEmpty( textSearch ) ) ) {
          materialCodes.AddRange( CheckContainSearchText( listHiroiSetMaster, textSearch ) ) ;
        }

        if ( materialCodes.Any() ) {
          listHiroiMaster = listHiroiMaster.Where( x => materialCodes.Contains( int.Parse( x.Buzaicd ).ToString() ) ).ToList() ;
          HiroiMasterListDisplay = new ObservableCollection<HiroiMasterModel>( listHiroiMaster ) ; 
        }
        else {
          HiroiMasterListDisplay = new ObservableCollection<HiroiMasterModel>() ; 
        }
      }
    }
    
    private List<string> CheckContainSearchText( List<HiroiSetMasterModel>? listHiroiSetMaster, string textSearch )
    {
      var materialCodes = new List<string>() ;
      if ( string.IsNullOrEmpty( textSearch ) || listHiroiSetMaster == null )
        return materialCodes ;
      
      foreach ( var x in listHiroiSetMaster ) {
        var isHasMaterialCode = false ;
        if ( CheckContainSearchText( x.Name1, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode1 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode1 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name2, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode2 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode2 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name3, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode3 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode3 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name4, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode4 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode4 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name5, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode5 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode5 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name6, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode6 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode6 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name7, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode7 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode7 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( CheckContainSearchText( x.Name8, textSearch ) && ! string.IsNullOrEmpty( x.MaterialCode8 ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode8 ).ToString() ) ;
          isHasMaterialCode = true ;
        }
        if ( ! isHasMaterialCode && ! string.IsNullOrEmpty( x.MaterialCode1 ) && CheckContainSearchText( x.ParentPartName, textSearch ) ) {
          materialCodes.Add( int.Parse( x.MaterialCode1 ).ToString() ) ;
        }
      }

      return materialCodes ;
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
      if ( hasOneWordOnly )
        return textContainer.Length > textSearch.Length && ( textContainer.Substring( 0, textSearch.Length ).Equals( textSearch, StringComparison.OrdinalIgnoreCase ) 
                                                             || textContainer.Replace( " ","" ).Substring( 0, textSearch.Length ).Equals( textSearch, StringComparison.OrdinalIgnoreCase )
                                                             || textContainer.Replace( " ","" ).Contains( textSearch ) ) ;
         
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