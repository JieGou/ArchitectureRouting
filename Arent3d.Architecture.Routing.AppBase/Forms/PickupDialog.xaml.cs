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
      DataContext = pickUpViewModel ;
      InitializeComponent() ;
    }

    private void DataGrid_LoadingRow( object sender, DataGridRowEventArgs e )
    {
      e.Row.Header = ( e.Row.GetIndex() + 1 ).ToString() ;
    }

    private void ExportType_Checked( object sender, RoutedEventArgs e )
    {
      PickUpViewModel vm = (PickUpViewModel)DataContext ;
      var radioButton = sender as RadioButton ;
      string exportType = radioButton!.Content?.ToString() ?? string.Empty ;
      vm.ExportType = exportType switch
      {
        "dat" => "dat", 
        "拾い書" => "csv", 
        _ => string.Empty
      } ;
    }
  }
 
  public abstract class DesignPickUpViewModel : PickUpViewModel
  {
    protected DesignPickUpViewModel( Document document ) : base( document )
    {
    }
  }
}