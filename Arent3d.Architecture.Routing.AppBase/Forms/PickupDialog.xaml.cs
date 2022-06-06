using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickupDialog : Window
  {
    public PickupDialog( PickUpViewModel pickUpViewModel )
    { 
      InitializeComponent() ;
      DataContext = pickUpViewModel ;
    }

    private void DataGrid_LoadingRow( object sender, DataGridRowEventArgs e )
    {
      e.Row.Header = ( e.Row.GetIndex() + 1 ).ToString() ;
    } 
  }
 
  public abstract class DesignPickUpViewModel : PickUpViewModel
  {
    protected DesignPickUpViewModel( Document document ) : base( document )
    {
    }
  }
}