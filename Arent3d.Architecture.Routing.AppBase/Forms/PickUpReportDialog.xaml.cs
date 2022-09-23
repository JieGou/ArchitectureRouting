using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpReportDialog
  {
    public PickUpReportDialog(PickUpReportViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
}