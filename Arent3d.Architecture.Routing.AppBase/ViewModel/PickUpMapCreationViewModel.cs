using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using RadioButton = System.Windows.Controls.RadioButton ;


namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpMapCreationViewModel : NotifyPropertyChanged
  {
    private readonly Document _document ;
    private const string On = "ON" ;
    private const string Off = "OFF" ;
    private const string Represent = "表示" ;
    private const string NonRepresent = "非表示" ;
    private const string DefaultConstructionItem = "未設定" ;
    
    public List<PickUpModel> PickUpModels { get ; set ; }
    public ObservableCollection<ListBoxItem> DoconTypes { get ; set ; }
    public ObservableCollection<ListBoxItem> RepresentTypes { get ; set ; }

    private bool _isDoconEnable ;

    public bool IsDoconEnable
    {
      get => _isDoconEnable ;
      set
      {
        _isDoconEnable = value ;
        OnPropertyChanged();
      }
    }
    
    public RelayCommand<Window> CancelCommand => new( Cancel ) ;
    public RelayCommand<Window> ExecuteCommand => new( Execute ) ;
    public PickUpMapCreationViewModel( Document document )
    {
      _document = document ;
      DoconTypes = new ObservableCollection<ListBoxItem>() ;
      RepresentTypes = new ObservableCollection<ListBoxItem>() ;
      PickUpModels = new List<PickUpModel>() ;

      InitPickUpModels() ;
      CreateCheckBoxList() ;
      IsDoconEnable = RepresentTypes.Any( x => (x.TheText == Represent && x.TheValue ) ) ;
    }
    
    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }
    
    private void Execute( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }
    
    private void CreateCheckBoxList()
    {
      //DoconTypes
      DoconTypes.Add( new ListBoxItem { TheText = On, TheValue = true } ) ;
      DoconTypes.Add( new ListBoxItem { TheText = Off, TheValue = false } ) ;
      
      // RepresentTypes
      RepresentTypes.Add( new ListBoxItem { TheText = Represent, TheValue = true } ) ;
      RepresentTypes.Add( new ListBoxItem { TheText = NonRepresent, TheValue = false } ) ;
    }

    public void RepresentItemChecked( object sender )
    {
      var radioButton = sender as RadioButton ;
      var isRepresent = radioButton!.Content.ToString() == Represent ;
      IsDoconEnable = isRepresent ;
    }
    
    private void InitPickUpModels()
    {
      var pickUpStorable = _document.GetAllStorables<PickUpStorable>().FirstOrDefault() ;
      if ( pickUpStorable != null ) PickUpModels = pickUpStorable.AllPickUpModelData  ;
    }

    private void GetPickUpToShow( List<PickUpModel> pickUpModels )
    {
      if ( ! pickUpModels.Any() ) return ;
      var pickUpNumbers = GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var routeName = pickUpModel.RouteName ;
      Dictionary<string, int> trajectory = new Dictionary<string, int>() ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        double seenQuantity = 0 ;
        Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        foreach ( var item in items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ) ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          if ( ! string.IsNullOrEmpty( item.Direction ) ) {
            if ( ! notSeenQuantities.Keys.Contains( item.Direction ) ) {
              notSeenQuantities.Add( item.Direction, 0 ) ;
            }
            notSeenQuantities[ item.Direction ] += quantity ;
          }
          else
            seenQuantity += quantity ;
        }
      }

      var allConduits = new FilteredElementCollector( _document ).OfCategory( BuiltInCategory.OST_ConduitFitting ) ;
      
      var routeNames = allConduits.Where( conduit => conduit.GetRouteName() == routeName );
      
    }
    
    public List<string> GetCodeList()
    {
      var codeList = new List<string>() ;
      foreach ( var pickUpModel in PickUpModels.Where( pickUpModel => ! codeList.Contains( pickUpModel.Specification2 ) ) ) {
        codeList.Add( pickUpModel.Specification2 ) ;
      }

      return codeList ;
    }
    
    public List<string> GetPickUpNumbersList( List<PickUpModel> pickUpModels )
    {
      var pickUpNumberList = new List<string>() ;
      foreach ( var pickUpModel in pickUpModels.Where( pickUpModel => ! pickUpNumberList.Contains( pickUpModel.PickUpNumber ) ) ) {
        pickUpNumberList.Add( pickUpModel.PickUpNumber ) ;
      }

      return pickUpNumberList ;
    }
  }
  
  public class ListBoxItem
  {
    public string? TheText { get ; set ; }
    public bool TheValue { get ; set ; }
  }
}
