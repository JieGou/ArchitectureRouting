using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpMapCreationDialog : Window
  {
    private PickUpMapCreationViewModel ViewModel => (PickUpMapCreationViewModel)DataContext ;
    public PickUpMapCreationDialog(PickUpMapCreationViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }

    private void Represent_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.RepresentItemChecked( sender ) ;
    }
  }
  
  public class DesignPickUpMapCreationViewModel : PickUpMapCreationViewModel
  {
    public DesignPickUpMapCreationViewModel( Document document ) : base( default ! )
    {
    }
  }
}