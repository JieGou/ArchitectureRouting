using System ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PressureGuidingTubeSettingViewModel : NotifyPropertyChanged
  {
    public readonly Array CreationList = Enum.GetValues( typeof( CreationModeEnum ) ) ;
    public readonly Array TubeTypeList = Enum.GetValues( typeof( TubeTypeEnum ) ) ;

    public TubeTypeEnum SelectedTubeType
    {
      get => (TubeTypeEnum)Enum.Parse( typeof( TubeTypeEnum ), PressureGuidingTube.TubeType ) ;
      set => PressureGuidingTube.TubeType = value.GetFieldName() ;
    }

    public CreationModeEnum SelectedCreationMode
    {
      get => (CreationModeEnum)Enum.Parse( typeof( CreationModeEnum ), PressureGuidingTube.CreationMode ) ;
      set => PressureGuidingTube.CreationMode = value.GetFieldName() ;
    }
    public RelayCommand<Window> CreateCommand => new ( CreatePressureGuidingTube ) ;

    private void CreatePressureGuidingTube( Window window )
    {
      window.DialogResult = true ;
      window.Close();
    }

    public PressureGuidingTubeModel PressureGuidingTube { get ; set ; }

    public PressureGuidingTubeSettingViewModel( PressureGuidingTubeModel pressureGuidingTube )
    {
      PressureGuidingTube = pressureGuidingTube ;
    }
  }
}