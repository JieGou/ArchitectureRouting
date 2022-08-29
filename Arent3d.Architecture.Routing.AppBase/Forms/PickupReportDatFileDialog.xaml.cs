using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickupReportDatFileDialog
  {
    public PickupReportDatFileDialog()
    {
      InitializeComponent() ;
    }
    public PickupReportDatFileDialog(PickUpReportDatFileViewModel datFileViewModel)
    {
      InitializeComponent() ;
      DataContext = datFileViewModel ;
    }
  }
}