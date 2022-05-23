using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class LayerNameSettingDialog 
  {
    private LayerNameSettingViewModel ViewModel => (LayerNameSettingViewModel)DataContext ;
    public LayerNameSettingDialog(LayerNameSettingViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  
  public abstract class DesignExportDwgViewModel : LayerNameSettingViewModel
  {
    protected DesignExportDwgViewModel() : base(default!)
    {
    }
  }
}