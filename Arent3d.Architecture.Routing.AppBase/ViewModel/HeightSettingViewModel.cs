using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using System.Collections.Generic;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class HeightSettingViewModel : ViewModelBase
  {
    public List<HeightSettingModel> HeightSettingModels { get; set; }

    public HeightSettingStorable SettingStorable { get; set; }

    public HeightSettingViewModel( HeightSettingStorable settingStorables )
    {
      SettingStorable = settingStorables;
      HeightSettingModels = settingStorables.HeightSettingsData.Values.ToList();
      if (HeightSettingModels == null)
      {
        HeightSettingModels = new List<HeightSettingModel>();
      }
    }
  }
}
