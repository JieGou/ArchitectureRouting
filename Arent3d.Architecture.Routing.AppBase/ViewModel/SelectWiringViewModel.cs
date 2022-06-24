using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Data ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SelectWiringViewModel : NotifyPropertyChanged
  {
    public WiringModel? SelectedWiring { get ; set ; }
    public RelayCommand<Window> SaveWiringCommand => new(SaveWiring) ;
    public RelayCommand<Window> ChangeWiringInfoCommand => new(ChangeWiringInfo) ;
    public RelayCommand<Window> CancelCommand => new(Cancel) ;

    public ObservableCollection<WiringModel> WiringList { get ; set ; }
    public ObservableCollection<WiringModel> ConduitList { get ; set ; }
    public List<WiringModel> WiringChangedList { get ; set ; }

    private List<ElectricalCategoryModel> _electricalCategoriesEco ;
    private List<ElectricalCategoryModel> _electricalCategoriesNormal ;
    private Dictionary<string, string> _dictElectricalCategoriesEcoKey ;
    private Dictionary<string, string> _dictElectricalCategoriesNormalKey ;

    private List<HiroiMasterModel> HiroiMasterModels ;
    private List<HiroiSetMasterModel>? HiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel>? HiroiSetMasterEcoModels ;

    private Document _document ;

    public SelectWiringViewModel( Document document, List<WiringModel> wiringList )
    {
      _document = document ;
      WiringList = new ObservableCollection<WiringModel>( wiringList.OrderBy( x => x.RouteName ).ToList() ) ;
      ConduitList = new ObservableCollection<WiringModel>() ;
      foreach ( var wiring in WiringList.Where( wiring => ConduitList.FirstOrDefault( x => x.RouteName == wiring.RouteName && x.IdOfToConnector == wiring.IdOfToConnector ) == null ) ) {
        ConduitList.Add( wiring ) ;
      }

      _dictElectricalCategoriesEcoKey = new Dictionary<string, string>() ;
      _dictElectricalCategoriesNormalKey = new Dictionary<string, string>() ;
      _electricalCategoriesEco = CommandUtils.LoadElectricalCategories( "Eco", ref _dictElectricalCategoriesEcoKey ) ;
      _electricalCategoriesNormal = CommandUtils.LoadElectricalCategories( "Normal", ref _dictElectricalCategoriesNormalKey ) ;

      var csvStorable = document.GetCsvStorable() ;
      HiroiMasterModels = csvStorable.HiroiMasterModelData ;
      HiroiSetMasterEcoModels = csvStorable.HiroiSetMasterEcoModelData ;
      HiroiSetMasterNormalModels = csvStorable.HiroiSetMasterNormalModelData ;

      WiringChangedList = new List<WiringModel>() ;
    }

    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    private void ChangeWiringInfo( Window window )
    {
      ElectricalCategoryViewModel electricalCategoryViewModel = new(_document, _electricalCategoriesEco, _electricalCategoriesNormal, _dictElectricalCategoriesEcoKey, _dictElectricalCategoriesNormalKey, HiroiMasterModels, HiroiSetMasterNormalModels, HiroiSetMasterEcoModels, 100, "m", "") ;
      ElectricalCategoryDialog dialog = new(electricalCategoryViewModel) ;
      if ( true != dialog.ShowDialog() ) return ;
      if ( null == electricalCategoryViewModel.CeedDetailSelected ) return ;

      var exitedItem = ConduitList.FirstOrDefault( x => x.RouteName == SelectedWiring?.RouteName ) ;
      if(exitedItem == null) return;
      
      exitedItem.WireType = electricalCategoryViewModel.CeedDetailSelected.Type ;
      exitedItem.WireSize = electricalCategoryViewModel.CeedDetailSelected.Size1 ;
      exitedItem.WireStrip = electricalCategoryViewModel.CeedDetailSelected.Size2 ;
      CollectionViewSource.GetDefaultView( ConduitList ).Refresh() ;
      
      foreach ( var wiring in WiringList ) {
        if ( wiring.RouteName == SelectedWiring?.RouteName ) {
          wiring.WireType = electricalCategoryViewModel.CeedDetailSelected.Type ;
          wiring.WireSize = electricalCategoryViewModel.CeedDetailSelected.Size1 ;
          wiring.WireStrip = electricalCategoryViewModel.CeedDetailSelected.Size2 ;
          wiring.ParentPartMode = electricalCategoryViewModel.CeedDetailSelected.ParentPartModel ;
          var exitedInWiringChangedList = WiringChangedList.FirstOrDefault( x => x.Id == wiring.Id ) ;
          if ( null != exitedInWiringChangedList )
            WiringChangedList.Remove( exitedInWiringChangedList ) ;
          WiringChangedList.Add( wiring );
        }
      }

      
    }

    private void SaveWiring( Window window )
    {
      var wiringStorable = _document.GetWiringStorable() ;
      foreach ( var wiring in WiringChangedList ) {
        var oldWiring = wiringStorable.WiringData.FirstOrDefault( x => x.Id == wiring.Id ) ;
        if ( null != oldWiring )
          wiringStorable.WiringData.Remove( oldWiring ) ;
        wiringStorable.WiringData.Add( wiring ) ;
      }

      if ( WiringChangedList.Count > 0 ) {
        using var trans = new Transaction( _document, "Save wiring storable" ) ;
        trans.Start() ;
        wiringStorable.Save();
        trans.Commit() ;  
      } 
       
      window.DialogResult = true ;
      window.Close() ;
    }
  }
}