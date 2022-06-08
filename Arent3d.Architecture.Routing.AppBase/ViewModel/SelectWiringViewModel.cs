using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SelectWiringViewModel : NotifyPropertyChanged
  {
    public SelectWiringModel? SelectedWiring { get ; set ; }
    public RelayCommand<Window> SelectWiringCommand => new(SelectWiring) ;
    public RelayCommand<Window> ChangeWiringInfoCommand => new(ChangeWiringInfo) ;
    public RelayCommand<Window> CancelCommand => new(Cancel) ;

    public List<SelectWiringModel> SelectWiringList { get ; set ; }

    public SelectWiringViewModel( List<SelectWiringModel> selectWiringList )
    {
      SelectWiringList = selectWiringList.OrderBy( x=>x.RouteName ).ToList() ;
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