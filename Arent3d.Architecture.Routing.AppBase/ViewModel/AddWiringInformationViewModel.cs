using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Data ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class AddWiringInformationViewModel: NotifyPropertyChanged
  {

    private readonly Document _document ;
    private readonly List<WiresAndCablesModel> _wiresAndCablesData;
    public RelayCommand<Window> SaveCommand => new( Save ) ; 
    public RelayCommand<Window> CancelCommand => new(Cancel) ; 
    public RelayCommand WireTypeChangedCommand => new(WireTypeChanged) ;
    public RelayCommand WireSizeChangedCommand => new(WireSizeChanged) ;
    public RelayCommand EarthTypeChangedCommand => new(EarthTypeChanged) ; 

    public DetailTableModel DetailTableModel { get ; set ; }
    public ObservableCollection<string> ConduitTypes { get ; set ; } 
    public ObservableCollection<string> ConstructionItems { get ; set ; }
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
    public ObservableCollection<string> PlumbingSizes { get ; set ; }
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
   
    
    public AddWiringInformationViewModel( Document document, DetailTableModel detailTableModel, ObservableCollection<string> conduitTypes, ObservableCollection<string> constructionItems, ObservableCollection<string> levels, ObservableCollection<string> wireTypes, ObservableCollection<string> earthTypes, ObservableCollection<string> numbers, ObservableCollection<string> constructionClassificationTypes, ObservableCollection<string> signalTypes  )
    {
      _document = document ;
      var csvStorable = _document.GetCsvStorable() ;
      _wiresAndCablesData = csvStorable.WiresAndCablesModelData ;
      DetailTableModel = detailTableModel ;
      ConduitTypes = conduitTypes ;
      ConstructionItems = constructionItems ;
      Levels = levels ;
      WireTypes = wireTypes ;
      EarthTypes = earthTypes ;
      Numbers = numbers ;
      ConstructionClassificationTypes = constructionClassificationTypes ;
      SignalTypes = signalTypes ;
 
      WireStrips = new ObservableCollection<string>(DetailTableModel.WireStrips.Select( x=>x.Name ).ToList() ) ;
      WireSizes = new ObservableCollection<string>( DetailTableModel.WireSizes.Select( x=>x.Name ).ToList() ) ;
      EarthSizes = new ObservableCollection<string>(DetailTableModel.EarthSizes.Select( x=>x.Name ).ToList()) ;
      PlumbingSizes = new ObservableCollection<string>(DetailTableModel.PlumbingSizes.Select( x=>x.Name ).ToList()) ;
      PlumbingItemTypes = new ObservableCollection<string>(DetailTableModel.PlumbingItemTypes.Select( x=>x.Name ).ToList()) ;
      
    }
     
    private void Save(Window window)
    {
      try {
        DetailTableViewModel.SaveData( _document, new ObservableCollection<DetailTableModel>(){DetailTableModel} ) ;
        window.DialogResult = true ; 
        window.Close();
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        throw ;
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
      DetailTableModel.WireSize = String.Empty;
      
      var wireStripsOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableModel.WireType && w.DiameterOrNominal == DetailTableModel.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      DetailTableModel.WireStrip = String.Empty;
      
      WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
      CollectionViewSource.GetDefaultView( WireStrips ).Refresh() ;
      
      WireSizes = new ObservableCollection<string>( wireSizesOfWireType ) ;
      CollectionViewSource.GetDefaultView( WireSizes ).Refresh() ;
    }
    
    private void WireSizeChanged()
    { 
      var wireStripsOfWireType = _wiresAndCablesData.Where( w => w.WireType == DetailTableModel.WireType && w.DiameterOrNominal == DetailTableModel.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
        
      WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
      CollectionViewSource.GetDefaultView( WireStrips ).Refresh() ;
       
    }
    
    private void EarthTypeChanged()
    { 
      var earthSizes = _wiresAndCablesData.Where( c => c.WireType == DetailTableModel.EarthType.ToString() ).Select( c => c.DiameterOrNominal ).ToList() ;
      EarthSizes = new ObservableCollection<string>(earthSizes) ;
      CollectionViewSource.GetDefaultView( EarthSizes ).Refresh() ;
       
    }
      
  }
}