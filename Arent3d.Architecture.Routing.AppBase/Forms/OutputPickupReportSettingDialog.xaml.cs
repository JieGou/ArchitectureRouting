using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class OutputPickupReportSettingDialog
  {
    public OutputPickupReportSettingDialog(PickupReportDatFileViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
}