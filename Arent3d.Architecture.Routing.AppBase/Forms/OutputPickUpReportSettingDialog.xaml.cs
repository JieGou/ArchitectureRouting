using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class OutputPickUpReportSettingDialog
  {
    public OutputPickUpReportSettingDialog(PickUpReportDatFileViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
}