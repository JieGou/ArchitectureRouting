using System.Collections.Generic ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.I18n ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SwitchEcoNormalModeViewModel : NotifyPropertyChanged
  {
    private const string EcoModeKey = "Dialog.Electrical.SwitchEcoNormalModeDialog.EcoNormalMode.EcoMode" ;
    private const string EcoModeDefaultString = "Eco Mode" ;
    private const string NormalModeKey = "Dialog.Electrical.SwitchEcoNormalModeDialog.EcoNormalMode.NormalMode" ;
    private const string NormalModeDefaultString = "Normal Mode" ;

    public enum SwitchEcoNormalMode
    {
      SetDefaultMode,
      ApplyForProject,
      ApplyForARange
    }

    public enum EcoNormalMode
    {
      EcoMode,
      NormalMode
    }

    public SwitchEcoNormalMode SelectedSwitchEcoNormalMode ;
    public int SelectedEcoNormalModeIndex { get ; set ; }
    public EcoNormalMode SelectedEcoNormalModeItem => 0 == SelectedEcoNormalModeIndex ? EcoNormalMode.NormalMode : EcoNormalMode.EcoMode ;

    public ICommand SetDefaultCommand => new RelayCommand( SetDefault ) ;
    public ICommand ApplyAllProjectCommand => new RelayCommand( ApplyAllProject ) ;
    public ICommand ApplyRangeCommand => new RelayCommand( ApplyRange ) ;

    public IReadOnlyDictionary<EcoNormalMode, string> EcoNormalModes { get ; } = new Dictionary<EcoNormalMode, string> { [ EcoNormalMode.NormalMode ] = NormalModeKey.GetAppStringByKeyOrDefault( NormalModeDefaultString ), [ EcoNormalMode.EcoMode ] = EcoModeKey.GetAppStringByKeyOrDefault( EcoModeDefaultString ) } ;

    private void SetDefault()
    {
      SelectedSwitchEcoNormalMode = SwitchEcoNormalMode.SetDefaultMode ;
    }

    private void ApplyAllProject()
    {
      SelectedSwitchEcoNormalMode = SwitchEcoNormalMode.ApplyForProject ;
    }

    private void ApplyRange()
    {
      SelectedSwitchEcoNormalMode = SwitchEcoNormalMode.ApplyForARange ;
    }
  }
}