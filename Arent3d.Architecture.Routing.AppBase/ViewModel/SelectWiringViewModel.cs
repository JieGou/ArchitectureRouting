using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SelectWiringViewModel : NotifyPropertyChanged
  {
    public WiringModel? SelectedWiring { get ; set ; }
    public RelayCommand<Window> SelectWiringCommand => new(SelectWiring) ;
    public RelayCommand<Window> ChangeWiringInfoCommand => new(ChangeWiringInfo) ;
    public RelayCommand<Window> CancelCommand => new(Cancel) ;

    public List<WiringModel> WiringList { get ; set ; }
    public ObservableCollection<WiringModel> ConduitList { get ; set ; }

    public SelectWiringViewModel( List<WiringModel> wiringList )
    {
      WiringList = wiringList.OrderBy( x=>x.RouteName ).ToList() ;
      ConduitList = new ObservableCollection<WiringModel>() ;
      foreach ( var wiring in WiringList.Where( wiring => ConduitList.FirstOrDefault(x=>x.RouteName == wiring.RouteName && x.IdOfToConnector == wiring.IdOfToConnector) == null ) ) {
        ConduitList.Add( wiring );
      }
    }
    
    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    private void ChangeWiringInfo( Window window )
    { 
      window.DialogResult = true ;
      window.Close() ;
    }
    
    private void SelectWiring( Window window )
    {
       

      window.DialogResult = true ;
      window.Close() ;
    }
  }
}