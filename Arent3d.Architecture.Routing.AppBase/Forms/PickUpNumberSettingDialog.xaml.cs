using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PickUpNumberSettingDialog : Window
  {
    private PickUpNumberSettingViewModel ViewModel => (PickUpNumberSettingViewModel)DataContext ;
    public PickUpNumberSettingDialog(PickUpNumberSettingViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  public class DesignPickUpNumberSettingViewModel : PickUpNumberSettingViewModel
  {
    public DesignPickUpNumberSettingViewModel( Document document ) : base( default ! )
    {
    }
  }
}