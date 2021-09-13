using Arent3d.Architecture.Routing.AppBase.Model;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Revit;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class HeightSettingViewModel : ViewModelBase
  {
    public const double DEFAULT_HEIGHT_OF_LEVEL = 3000;
    public const double DEFAULT_HEIGHT_OF_CONNECTORS = DEFAULT_HEIGHT_OF_LEVEL / 2;

    public List<HeightSettingModel> HeightSettingModels { get; set; }

    public HeightSettingStorable SettingStorable { get; set; }

    public HeightSettingViewModel( HeightSettingStorable settingStorables )
    {
      SettingStorable = settingStorables;
      HeightSettingModels = settingStorables.Levels
                                            .Select(x => new HeightSettingModel()
                                            {
                                              LevelName = x.Name,
                                              Elevation = settingStorables.ElevationOfLevels.GetOrDefault(x.Name, x.Elevation.RevitUnitsToMillimeters()),
                                              HeightOfLevel = settingStorables.HeightOfLevels.GetOrDefault(x.Name, DEFAULT_HEIGHT_OF_LEVEL),
                                              HeightOfConnectors = settingStorables.HeightOfConnectorsByLevel.GetOrDefault(x.Name, DEFAULT_HEIGHT_OF_CONNECTORS),
                                            })
                                            .ToList();
      if (HeightSettingModels == null)
      {
        HeightSettingModels = new List<HeightSettingModel>();
      }
    }

    public HeightSettingStorable GetStorable()
    {
      SettingStorable.ClearAll();
      foreach (var item in HeightSettingModels)
      {
        SettingStorable.ElevationOfLevels.Add(item.LevelName, item.Elevation);
        SettingStorable.HeightOfLevels.Add(item.LevelName, item.HeightOfLevel);
        SettingStorable.HeightOfConnectorsByLevel.Add(item.LevelName, item.HeightOfConnectors);
      }


      return SettingStorable;
    }
  }
}
