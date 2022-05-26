using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Data ;
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

    private readonly Document _document ;
    private readonly List<WiresAndCablesModel> _wiresAndCablesData;
    public RelayCommand<Window> SaveCommand => new( Save ) ; 
    public RelayCommand<Window> CancelCommand => new(Cancel) ;
    public RelayCommand<object> DataGridChangedCommand => new(DataGridChanged) ;
    public RelayCommand<object> DataGridComboBoxChangeCommand => new(DataGridComboBoxChange) ;

    public ObservableCollection<DetailTableModel> DetailTableModels { get ; set ; }
    public ObservableCollection<string> ConduitTypes { get ; set ; } 
    public ObservableCollection<string> ConstructionItems { get ; set ; }
    public ObservableCollection<string> Levels { get ; set ; }  
    public ObservableCollection<string> WireTypes { get ; set ; }
    public ObservableCollection<string> EarthTypes { get ; set ; } 
    public ObservableCollection<string> Numbers { get ; set ; } 
    public ObservableCollection<string> ConstructionClassificationTypes { get ; set ; } 
    public ObservableCollection<string> SignalTypes { get ; set ; }

    public string _selectedWireType = string.Empty;
    public string SelectedWireType
    {
      get => _selectedWireType ;
      set
      {
        _selectedWireType = value ;
        var wireSizesOfWireType = _wiresAndCablesData.Where( w => w.WireType == _selectedWireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
        WireSizes = new ObservableCollection<string>(wireSizesOfWireType) ;
        OnPropertyChanged( "WireSizes" ) ;
        
      }
    }

    private DetailTableModel? _selectedDetailTableModel ;
    public DetailTableModel? SelectedDetailTableModel
    {
      get => _selectedDetailTableModel ;
      set
      {
        if(null == value) return;
        _selectedDetailTableModel = value ;
        // var csvStorable = _document.GetCsvStorable() ;
        // var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
        // if(null == wiresAndCablesModelData) return;
        //  
        // var wireSizesOfWireType = wiresAndCablesModelData.Where( w => w.WireType == _selectedDetailTableModel.WireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
        // WireSizes = new ObservableCollection<string>(wireSizesOfWireType) ;
        // OnPropertyChanged( "WireSizes" ) ;
        //
        // var wireStripsOfWireType = wiresAndCablesModelData.Where( w => w.WireType == _selectedDetailTableModel.WireType && w.DiameterOrNominal == _selectedDetailTableModel.WireSize.ToString() ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
        // WireStrips = new ObservableCollection<string>(wireStripsOfWireType) ;
        // OnPropertyChanged( "WireStrips" ) ;

      }
    }
    
    public ObservableCollection<string> EarthSizes { get ; set ; }
    public ObservableCollection<string> PlumbingSizes { get ; set ; }
    public ObservableCollection<string> PlumbingItemTypes { get ; set ; }
    public ObservableCollection<string> WireSizes { get ; set ; }
    public ObservableCollection<string> WireStrips { get ; set ; }
   
    
    public AddWiringInformationViewModel( Document document, ObservableCollection<DetailTableModel> detailTableModels, ObservableCollection<string> conduitTypes, ObservableCollection<string> constructionItems, ObservableCollection<string> levels, ObservableCollection<string> wireTypes, ObservableCollection<string> earthTypes, ObservableCollection<string> numbers, ObservableCollection<string> constructionClassificationTypes, ObservableCollection<string> signalTypes  )
    {
      _document = document ;
      var csvStorable = _document.GetCsvStorable() ;
      _wiresAndCablesData = csvStorable.WiresAndCablesModelData ;
      DetailTableModels = detailTableModels ;
      ConduitTypes = conduitTypes ;
      ConstructionItems = constructionItems ;
      Levels = levels ;
      WireTypes = wireTypes ;
      EarthTypes = earthTypes ;
      Numbers = numbers ;
      ConstructionClassificationTypes = constructionClassificationTypes ;
      SignalTypes = signalTypes ;

      EarthSizes = new ObservableCollection<string>() ;
      PlumbingSizes = new ObservableCollection<string>() ;
      PlumbingItemTypes = new ObservableCollection<string>() ;
      WireSizes = new ObservableCollection<string>() ;
      WireStrips = new ObservableCollection<string>() ; 
      
      if(detailTableModels.Count > 0 )
        SelectedDetailTableModel = detailTableModels[0];

      
    }
     
    private void Save(Window window)
    {
      try {
        using Transaction t = new Transaction( _document, "Save wiring data" ) ;
        t.Start() ;
        // _pickUpStorable.AllPickUpModelData = _pickUpModels ;
        // _pickUpStorable.Save() ;
        t.Commit() ;
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
    
    private void DataGridChanged(object window)
    {
      var x = window ;
    }
    
    private void DataGridComboBoxChange(object window)
    {
      var x = window ;
    }
    
    
    
  }
}