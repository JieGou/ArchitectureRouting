using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PressureGuidingTubeSettingDialog
  {
    public PressureGuidingTubeSettingDialog(PressureGuidingTubeSettingViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      CbTubeType.ItemsSource = viewModel.TubeTypeList ;
      CbCreationMode.ItemsSource = viewModel.CreationList ;
    }
  }
  
  public abstract class DesignPressureGuidingTubeSettingViewModel : PressureGuidingTubeSettingViewModel
  {
    protected DesignPressureGuidingTubeSettingViewModel( PressureGuidingTubeModel pressureGuidingTubeModel ) : base( pressureGuidingTubeModel )
    {
    }
  }
}