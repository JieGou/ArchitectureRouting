using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SettingOutputPickUpReport
  {
    public SettingOutputPickUpReport(PickUpReportViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
}

