using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.Collections.Generic ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class HeightSettingViewModel : ViewModelBase
  {
    public List<HeightSettingModel> HeightSettingModels { get ; }

    public HeightSettingStorable SettingStorable { get ; }

    public HeightSettingViewModel( HeightSettingStorable settingStorables )
    {
      SettingStorable = settingStorables ;
      HeightSettingModels = settingStorables.HeightSettingsData.Values.ToList() ;
    }
  }
}