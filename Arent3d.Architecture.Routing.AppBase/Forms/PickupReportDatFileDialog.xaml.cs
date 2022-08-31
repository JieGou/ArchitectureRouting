using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickupReportDatFileDialog : Window
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