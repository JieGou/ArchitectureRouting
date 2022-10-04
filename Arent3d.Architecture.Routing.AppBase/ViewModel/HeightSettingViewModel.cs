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
      var heightSettingModels = settingStorables.HeightSettingsData.Values.OrderBy( h => h.Elevation ).ToList() ;
      for (int i = 0; i < heightSettingModels.Count ; i++)
      {
        if (i == heightSettingModels.Count - 1) heightSettingModels[i].FloorHeight = null;
        else heightSettingModels[i].FloorHeight =  heightSettingModels[i+1].Elevation- heightSettingModels[i].Elevation;
      }
      HeightSettingModels = heightSettingModels;
      SettingStorable = settingStorables ;
    }
  }
}