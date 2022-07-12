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
      if ( IsDoconEnable ) {
        var pickUpViewModel = new PickUpViewModel( _document ) ;
        PickUpModels = pickUpViewModel.DataPickUpModels ;
      }

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
