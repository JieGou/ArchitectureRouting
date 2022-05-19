using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class LayerNameSettingDialog 
  {
    public LayerNameSettingDialog(LayerNameSettingViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }
  }
  
  
  public abstract class DesignExportDwgViewModel : LayerNameSettingViewModel
  {
    protected DesignExportDwgViewModel( List<Layer> layers ) : base( layers )
    {
    }
  }
}