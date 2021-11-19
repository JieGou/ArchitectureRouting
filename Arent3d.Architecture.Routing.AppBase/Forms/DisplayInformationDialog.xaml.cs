using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DisplayInformationDialog : Window
  {
    public DisplayInformationDialog( DisplayInformationViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel;
    }
  }
}