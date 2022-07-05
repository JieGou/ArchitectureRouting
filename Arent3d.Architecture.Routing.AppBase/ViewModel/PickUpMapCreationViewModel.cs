using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;
using System.Collections.ObjectModel ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable ;
namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpMapCreationViewModel : NotifyPropertyChanged
  {
    private readonly Document _document ;
    private const string On = "ON" ;
    private const string Off = "OFF" ;
    private const string Represent = "表示" ;
    private const string NonRepresent = "非表示" ;
    
    public ObservableCollection<ListBoxItem> DoconTypes { get ; set ; }
    public ObservableCollection<ListBoxItem> RepresentTypes { get ; set ; }
    public RelayCommand<Window> CancelCommand => new( Cancel ) ;
    public PickUpMapCreationViewModel( Document document )
    {
      _document = document ;
      DoconTypes = new ObservableCollection<ListBoxItem>() ;
      RepresentTypes = new ObservableCollection<ListBoxItem>() ;
      
      CreateCheckBoxList() ;
    }
    
    private void Cancel( Window window)
    {
      window.DialogResult = false ;
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

    private void RepresentChanged()
    {
      RepresentTypes.Add( new ListBoxItem { TheText = NonRepresent, TheValue = false } ) ;
    }
  }
  
  public class ListBoxItem
  {
    public string? TheText { get ; set ; }
    public bool TheValue { get ; set ; }
  }
}
