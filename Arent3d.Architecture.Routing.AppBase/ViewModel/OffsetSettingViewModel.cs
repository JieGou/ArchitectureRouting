using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.Collections.Generic ;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class OffsetSettingViewModel : ViewModelBase
  {
    public List<OffsetSettingModel> OffsetSettinggModels { get ; }

    public OffsetSettingStorable SettingStorable { get ; }

    public OffsetSettingViewModel( OffsetSettingStorable settingStorables )
    {
      SettingStorable = settingStorables ;
      OffsetSettinggModels = settingStorables.OffsetSettingsData.Values.ToList() ;
    }
  }
}