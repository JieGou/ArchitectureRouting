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
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class AddWiringInformationViewModel: NotifyPropertyChanged
  {

    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;
    
    private readonly Document _document ;
    private readonly List<WiresAndCablesModel> _wiresAndCablesData;
    public RelayCommand<Window> SaveCommand => new( Save ) ; 
    public RelayCommand<Window> CancelCommand => new(Cancel) ; 
    public RelayCommand WireTypeChangedCommand => new(WireTypeChanged) ;
    public RelayCommand WireSizeChangedCommand => new(WireSizeChanged) ;
    public RelayCommand EarthTypeChangedCommand => new(EarthTypeChanged) ; 
    public RelayCommand PlumbingTypeChangedCommand => new(PlumbingTypeChanged) ; 

    public DetailTableModel DetailTableModel { get ; set ; }
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
    public AddWiringInformationViewModel( Document document, DetailTableModel detailTableModel, List<ConduitsModel> conduitsModelData ,ObservableCollection<string> conduitTypes, ObservableCollection<string> constructionItems, ObservableCollection<string> levels, ObservableCollection<string> wireTypes, ObservableCollection<string> earthTypes, ObservableCollection<string> numbers, ObservableCollection<string> constructionClassificationTypes, ObservableCollection<string> signalTypes, bool isMixConstructionItems )
    {
      _document = document ;
      var csvStorable = _document.GetCsvStorable() ;
      _wiresAndCablesData = csvStorable.WiresAndCablesModelData ;
      DetailTableModel = detailTableModel ;
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
      EarthSizes = new ObservableCollection<string>(DetailTableModel.EarthSizes.Select( x=>x.Name ).ToList()) ;
      PlumbingSizes = new ObservableCollection<string>(DetailTableModel.PlumbingSizes.Select( x=>x.Name ).ToList()) ;
      PlumbingItemTypes = new ObservableCollection<string>(DetailTableModel.PlumbingItemTypes.Select( x=>x.Name ).ToList()) ;
      
    }
     
    private void Save(Window window)
    {
      try {
        using ( Transaction t = new Transaction( _document, "Save wiring information changed transaction" ) ) {
          t.Start() ;
          //Get connector of route
          var wiringInfoChangedStoreable = _document.GetWiringInformationChangedStorable() ;
          var hiroiMasterModelData = _document.GetCsvStorable().HiroiMasterModelData ;
          string kikaku = DetailTableModel.WireType +  DetailTableModel.WireSize + "x" + DetailTableModel.WireStrip ;
          var hiroiMasterModel = hiroiMasterModelData.FirstOrDefault( x => String.Equals( x.Kikaku.Replace( " ","" ), kikaku, StringComparison.CurrentCultureIgnoreCase ) ) ;
          if ( null != hiroiMasterModel ) {
            //Save wiring info changed in store
            var toConnector = ConduitUtil.GetConnectorOfRoute( _document, DetailTableModel.RouteName, false ) ;
            if ( null != toConnector ) {
              var wiringInfoChangedModel = wiringInfoChangedStoreable.WiringInformationChangedData.FirstOrDefault( x => x.ConnectorUniqueId == toConnector.UniqueId ) ;
              if ( wiringInfoChangedModel != null ) {
                wiringInfoChangedModel.MaterialCode = hiroiMasterModel.Kikaku ; 
              }
              else {
                wiringInfoChangedStoreable.WiringInformationChangedData.Add( new WiringInformationChangedModel( toConnector.UniqueId, hiroiMasterModel.Kikaku ) );
              }
              wiringInfoChangedStoreable.Save();
            }
          
            //Update DetailTableStore
            DetailTableStorable detailTableStorable = _document.GetDetailTableStorable() ; 
            if(detailTableStorable.DetailTableModelData.FirstOrDefault(x=>x.RouteName == DetailTableModel.RouteName) == null)
              detailTableStorable.DetailTableModelData.Add( DetailTableModel );
            detailTableStorable.Save();
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
    
    private void Cancel(Window window)
    {
      window.DialogResult = false ;
      window.Close();
    }
      
    private void WireTypeChanged()
    { 
      var wireSizesOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableModel.WireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
      WireSizes = new ObservableCollection<string>( wireSizesOfWireType ) ;
      DetailTableModel.WireSizes = WireSizes.Count > 0 ? ( from wireSize in WireSizes select new DetailTableModel.ComboboxItemType( wireSize, wireSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ; 
      CollectionViewSource.GetDefaultView( WireSizes ).Refresh() ;
      
      var wireStripsOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableModel.WireType && w.DiameterOrNominal == DetailTableModel.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
      DetailTableModel.WireStrips = WireStrips.Count > 0 ? ( from wireStrip in WireStrips select new DetailTableModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
      CollectionViewSource.GetDefaultView( WireStrips ).Refresh() ;
    }
    
    private void WireSizeChanged()
    { 
      var wireStripsOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableModel.WireType && w.DiameterOrNominal == DetailTableModel.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
      DetailTableModel.WireStrips = WireStrips.Count > 0 ? ( from wireStrip in WireStrips select new DetailTableModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
      CollectionViewSource.GetDefaultView( WireStrips ).Refresh() ;
       
    }
    
    private void EarthTypeChanged()
    { 
      var earthSizes = _wiresAndCablesData.Where( c => c.WireType == DetailTableModel.EarthType.ToString() ).Select( c => c.DiameterOrNominal ).ToList() ;
      EarthSizes = new ObservableCollection<string>(earthSizes) ;
      DetailTableModel.EarthSizes = EarthSizes.Count > 0 ? ( from earthSize in EarthSizes select new DetailTableModel.ComboboxItemType( earthSize, earthSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
      CollectionViewSource.GetDefaultView( EarthSizes ).Refresh() ;  
    }
    private void PlumbingTypeChanged()
    {
      var plumbingType = DetailTableModel.PlumbingType ;
      var detailTableModel = new List<DetailTableModel>() { DetailTableModel } ;
      if ( plumbingType == NoPlumping ) {
        CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableModel, _isMixConstructionItems ) ;
      }
      else {
        CreateDetailTableCommandBase.SetPlumbingData( _conduitsModelData, ref detailTableModel, plumbingType, _isMixConstructionItems ) ;
      }
      
      if ( _isMixConstructionItems ) {
        DetailTableViewModel.SetPlumbingItemsForDetailTableRowsMixConstructionItems( detailTableModel ) ;
      }
      else {
        DetailTableViewModel.SetPlumbingItemsForDetailTableRows( detailTableModel ) ;
      }
      
      var earthSizes = _wiresAndCablesData.Where( c => c.WireType == DetailTableModel.EarthType.ToString() ).Select( c => c.DiameterOrNominal ).ToList() ;
      EarthSizes = new ObservableCollection<string>(earthSizes) ;
      CollectionViewSource.GetDefaultView( EarthSizes ).Refresh() ;  
    }
      
  }
}