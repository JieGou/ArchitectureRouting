using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Data ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class AddWiringInformationViewModel : NotifyPropertyChanged
  {

    private const string NoPlumping = "配管なし" ;

    private readonly Document _document ;
    private readonly List<WiresAndCablesModel> _wiresAndCablesData;
    public RelayCommand<Window> SaveCommand => new( Save ) ; 
    public RelayCommand<Window> CancelCommand => new(Cancel) ; 
    public RelayCommand WireTypeChangedCommand => new(WireTypeChanged) ;
    public RelayCommand WireSizeChangedCommand => new(WireSizeChanged) ;
    public RelayCommand EarthTypeChangedCommand => new(EarthTypeChanged) ; 
    public RelayCommand PlumbingTypeChangedCommand => new(PlumbingTypeChanged) ; 

    public DetailTableItemModel DetailTableItemModel { get ; set ; }
    public ObservableCollection<string> ConduitTypes { get ; set ; } 
    public ObservableCollection<string> ConstructionList { get ; set ; }
    public ObservableCollection<string> Levels { get ; set ; }  
    public ObservableCollection<string> WireTypes { get ; set ; }
    public ObservableCollection<string> EarthTypes { get ; set ; } 
    public ObservableCollection<string> Numbers { get ; set ; } 
    public ObservableCollection<string> ConstructionClassificationTypes { get ; set ; } 
    public ObservableCollection<string> SignalTypes { get ; set ; }
    
    private ObservableCollection<string> _earthSizes = new() ;
     
    public ObservableCollection<string> EarthSizes { 
      get => _earthSizes;
      set
      {
        _earthSizes = value ;
        OnPropertyChanged( nameof(EarthSizes) );
      } 
    }

    private ObservableCollection<string> _plumbingSizes = new() ;

    public ObservableCollection<string> PlumbingSizes
    {
      get =>_plumbingSizes;
      set
      {
        _plumbingSizes = value ;
        OnPropertyChanged( nameof(PlumbingSizes) );
      } 
      
    }
    public ObservableCollection<string> PlumbingItemTypes { get ; set ; }

    private ObservableCollection<string> _wireSizes = new() ;
    public ObservableCollection<string> WireSizes
    { 
      get => _wireSizes;
      set
      {
        _wireSizes = value ;
        OnPropertyChanged( nameof(WireSizes) );
      }  
    }

    private ObservableCollection<string> _wireStrips = new() ;
    public ObservableCollection<string> WireStrips { 
      get => _wireStrips ;
      set 
      { 
        _wireStrips = value ;
        OnPropertyChanged( nameof(WireStrips) );
        
      }
    }

    private readonly bool _isMixConstructionItems ;
    private readonly List<ConduitsModel> _conduitsModelData ;
    public AddWiringInformationViewModel( Document document, DetailTableItemModel detailTableItemModel, List<ConduitsModel> conduitsModelData ,ObservableCollection<string> conduitTypes, ObservableCollection<string> constructionItems, ObservableCollection<string> levels, ObservableCollection<string> wireTypes, ObservableCollection<string> earthTypes, ObservableCollection<string> numbers, ObservableCollection<string> constructionClassificationTypes, ObservableCollection<string> signalTypes, bool isMixConstructionItems )
    {
      _document = document ;
      var csvStorable = _document.GetCsvStorable() ;
      _wiresAndCablesData = csvStorable.WiresAndCablesModelData ;
      DetailTableItemModel = detailTableItemModel ;
      ConduitTypes = conduitTypes ;
      ConstructionList = constructionItems ;
      Levels = levels ;
      WireTypes = wireTypes ;
      EarthTypes = earthTypes ;
      Numbers = numbers ;
      ConstructionClassificationTypes = constructionClassificationTypes ;
      SignalTypes = signalTypes ;
      _isMixConstructionItems = isMixConstructionItems ;
      _conduitsModelData = conduitsModelData ;
       
      WireTypeChanged() ;
      EarthSizes = new ObservableCollection<string>(DetailTableItemModel.EarthSizes.Select( x=>x.Name ).ToList()) ;
      PlumbingSizes = new ObservableCollection<string>(DetailTableItemModel.PlumbingSizes.Select( x=>x.Name ).ToList()) ;
      PlumbingItemTypes = new ObservableCollection<string>(DetailTableItemModel.PlumbingItemTypes.Select( x=>x.Name ).ToList()) ;
      
    }
     
    private void Save(Window window)
    {
      try {
        using ( var t = new Transaction( _document, "Save wiring information changed transaction" ) ) {
          t.Start() ;
          //Get connector of route
          var wiringInfoChangedStoreable = _document.GetWiringInformationChangedStorable() ;
          var hiroiMasterModelData = _document.GetCsvStorable().HiroiMasterModelData ;

          var hiroiMasterModel = hiroiMasterModelData.FirstOrDefault( x => IsExistHiroiMasterModel(DetailTableItemModel, x) ) ;
          if ( null != hiroiMasterModel ) {
            //Save wiring info changed in store
            var toConnector = ConduitUtil.GetConnectorOfRoute( _document, DetailTableItemModel.RouteName, false ) ;
            if ( null != toConnector ) {
              var wiringInfoChangedModel = wiringInfoChangedStoreable.WiringInformationChangedData.FirstOrDefault( x => x.ConnectorUniqueId == toConnector.UniqueId ) ;
              if ( wiringInfoChangedModel != null ) {
                wiringInfoChangedModel.MaterialCode = hiroiMasterModel.Buzaicd ; 
              }
              else {
                wiringInfoChangedStoreable.WiringInformationChangedData.Add( new WiringInformationChangedModel( toConnector.UniqueId, hiroiMasterModel.Kikaku ) );
              }
              wiringInfoChangedStoreable.Save();
            }
          
            //Update DetailTableStore
            var storageService = new StorageService<Level, DetailTableModel>( ( (ViewPlan) _document.ActiveView ).GenLevel ) ;
            if ( storageService.Data.DetailTableData.FirstOrDefault( x => x.RouteName == DetailTableItemModel.RouteName ) == null ) {
              storageService.Data.DetailTableData.Add( DetailTableItemModel );
              storageService.SaveChange();
            }
            
          } 
          t.Commit() ;
        }
        
        window.DialogResult = true ; 
        window.Close();
      }
      catch ( Exception ) {
        MessageBox.Show( "Save wiring information changed failed.", "Error Message" ) ;
        window.DialogResult = false ;
      }
    }
    
    private bool IsExistHiroiMasterModel( DetailTableItemModel detailTableItemModel, HiroiMasterModel hiroiMasterModel )
    {
      var isExistWireType = hiroiMasterModel.Ryakumeicd.Contains( detailTableItemModel.WireType ) ;
      if ( ! isExistWireType )
        return false ;
      
      var isExistWireSize = hiroiMasterModel.Ryakumeicd.Contains( detailTableItemModel.WireSize ) ;
      if ( ! isExistWireSize )
        return false ;
      
      return detailTableItemModel.WireStrip.Contains( "-" ) || hiroiMasterModel.Ryakumeicd.Contains( detailTableItemModel.WireStrip ) ;
    }
    
    private void Cancel(Window window)
    {
      window.DialogResult = false ;
      window.Close();
    }
      
    private void WireTypeChanged()
    { 
      var wireSizesOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableItemModel.WireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
      WireSizes = new ObservableCollection<string>( wireSizesOfWireType ) ;
      DetailTableItemModel.WireSizes = WireSizes.Count > 0 ? ( from wireSize in WireSizes select new DetailTableItemModel.ComboboxItemType( wireSize, wireSize ) ).ToList() : new List<DetailTableItemModel.ComboboxItemType>() ; 
      CollectionViewSource.GetDefaultView( WireSizes ).Refresh() ;
      
      var wireStripsOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableItemModel.WireType && w.DiameterOrNominal == DetailTableItemModel.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
      DetailTableItemModel.WireStrips = WireStrips.Count > 0 ? ( from wireStrip in WireStrips select new DetailTableItemModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() : new List<DetailTableItemModel.ComboboxItemType>() ;
      CollectionViewSource.GetDefaultView( WireStrips ).Refresh() ;
    }
    
    private void WireSizeChanged()
    { 
      var wireStripsOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableItemModel.WireType && w.DiameterOrNominal == DetailTableItemModel.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
      DetailTableItemModel.WireStrips = WireStrips.Count > 0 ? ( from wireStrip in WireStrips select new DetailTableItemModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() : new List<DetailTableItemModel.ComboboxItemType>() ;
      CollectionViewSource.GetDefaultView( WireStrips ).Refresh() ;
       
    }
    
    private void EarthTypeChanged()
    { 
      var earthSizes = _wiresAndCablesData.Where( c => c.WireType == DetailTableItemModel.EarthType ).Select( c => c.DiameterOrNominal ).ToList() ;
      EarthSizes = new ObservableCollection<string>(earthSizes) ;
      DetailTableItemModel.EarthSizes = EarthSizes.Count > 0 ? ( from earthSize in EarthSizes select new DetailTableItemModel.ComboboxItemType( earthSize, earthSize ) ).ToList() : new List<DetailTableItemModel.ComboboxItemType>() ;
      CollectionViewSource.GetDefaultView( EarthSizes ).Refresh() ;  
    }
    private void PlumbingTypeChanged()
    {
      var plumbingType = DetailTableItemModel.PlumbingType ;
      var detailTableItemModel = new List<DetailTableItemModel>() { DetailTableItemModel } ;
      if ( plumbingType == NoPlumping ) {
        CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableItemModel, _isMixConstructionItems ) ;
      }
      else {
        CreateDetailTableCommandBase.SetPlumbingData( _conduitsModelData, ref detailTableItemModel, plumbingType, _isMixConstructionItems ) ;
      }
      
      if ( _isMixConstructionItems ) {
        DetailTableViewModel.SetPlumbingItemsForDetailTableItemRowsMixConstructionItems( detailTableItemModel ) ;
      }
      else {
        DetailTableViewModel.SetPlumbingItemsForDetailTableItemRows( detailTableItemModel ) ;
      }
      
      var earthSizes = _wiresAndCablesData.Where( c => c.WireType == DetailTableItemModel.EarthType ).Select( c => c.DiameterOrNominal ).ToList() ;
      EarthSizes = new ObservableCollection<string>(earthSizes) ;
      CollectionViewSource.GetDefaultView( EarthSizes ).Refresh() ;  
    }
      
  }
}