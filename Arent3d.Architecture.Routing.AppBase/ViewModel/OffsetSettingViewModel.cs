using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class OffsetSettingViewModel : ViewModelBase
  {
    public OffsetSettingModel OffsetSettingModels { get ; }

    public OffsetSettingStorable SettingStorable { get ; }

    public OffsetSettingViewModel( OffsetSettingStorable settingStorables )
    {
      SettingStorable = settingStorables ;
      OffsetSettingModels = settingStorables.OffsetSettingsData ;
    }
  }
}