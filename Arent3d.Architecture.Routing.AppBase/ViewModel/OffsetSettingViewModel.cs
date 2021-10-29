using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.Collections.Generic ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class OffsetSettingViewModel : ViewModelBase
  {
    private List<OffsetSettingModel> OffsetSettingModels { get ; }

    public OffsetSettingStorable SettingStorable { get ; }

    public OffsetSettingViewModel( OffsetSettingStorable settingStorable )
    {
      SettingStorable = settingStorable ;
      OffsetSettingModels = settingStorable.OffsetSettingsData.Values.ToList() ;
    }
  }
}