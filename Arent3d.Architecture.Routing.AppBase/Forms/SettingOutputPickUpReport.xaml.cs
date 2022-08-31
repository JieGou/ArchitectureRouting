using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SettingOutputPickUpReport : Window
  {
    private PickUpReportViewModel ViewModel => (PickUpReportViewModel)DataContext ;
    
    public SettingOutputPickUpReport(PickUpReportViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
    public SettingOutputPickUpReport(PickUpReportDatFileViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  public class DesignSettingPickUpReportViewModel : PickUpReportViewModel
  {
    public DesignSettingPickUpReportViewModel( Document document ) : base( default! )
    {
    }
  }
}

