using Arent3d.Revit;
using Arent3d.Utility.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid("83A448F4-E120-44E0-A220-F2D3F11B6A09")]
  [StorableVisibility(AppInfo.VendorId)]
  public sealed class HeightSettingStorable : StorableBase
  {

    public const string LEVEL1_NAME = "レベル 1";
    public const string LEVEL2_NAME = "レベル 2";

    private const string ELEVATION_OF_LEVELS_FIELD = "ElevationOfLevels";
    private const string HEIGHT_OF_LEVELS_FIELD = "HeightOfLevels";
    private const string HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD = "HeightOfConnectorsByLevel";
    private const double DEFAULT_ELEVATION_LV1 = 0;
    private const double DEFAULT_ELEVATION_LV2 = 4000;
    private const double DEFAULT_HEIGHT_OF_LEVEL = 3000;
    private const double DEFAULT_HEIGHT_OF_CONNECTORS = DEFAULT_HEIGHT_OF_LEVEL/2;

    public Dictionary<string, double> ElevationOfLevels { get; set; }
    public Dictionary<string, double> HeightOfLevels { get; set; }
    public Dictionary<string, double> HeightOfConnectorsByLevel { get; set; }
    private Dictionary<int, string> Levels { get; set; } = new Dictionary<int, string> { { 1, LEVEL1_NAME }, { 2, LEVEL2_NAME } };


    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private HeightSettingStorable( DataStorage owner ) : base(owner, false)
    {
      ElevationOfLevels = new Dictionary<string, double>() { 
        { LEVEL1_NAME, DEFAULT_ELEVATION_LV1 }, 
        { LEVEL2_NAME, DEFAULT_ELEVATION_LV2 } 
      };

      HeightOfLevels = new Dictionary<string, double>() { 
        { LEVEL1_NAME, DEFAULT_HEIGHT_OF_LEVEL }, 
        { LEVEL2_NAME, DEFAULT_HEIGHT_OF_LEVEL } 
      };

      HeightOfConnectorsByLevel = new Dictionary<string, double>() { 
        { LEVEL1_NAME, DEFAULT_HEIGHT_OF_CONNECTORS }, 
        { LEVEL2_NAME, DEFAULT_HEIGHT_OF_CONNECTORS } 
      };

    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public HeightSettingStorable( Document document ) : base(document, false)
    {
      ElevationOfLevels = new Dictionary<string, double>() {
        { LEVEL1_NAME, DEFAULT_ELEVATION_LV1 },
        { LEVEL2_NAME, DEFAULT_ELEVATION_LV2 }
      };

      HeightOfLevels = new Dictionary<string, double>() {
        { LEVEL1_NAME, DEFAULT_HEIGHT_OF_LEVEL },
        { LEVEL2_NAME, DEFAULT_HEIGHT_OF_LEVEL }
      };

      HeightOfConnectorsByLevel = new Dictionary<string, double>() {
        { LEVEL1_NAME, DEFAULT_HEIGHT_OF_CONNECTORS },
        { LEVEL2_NAME, DEFAULT_HEIGHT_OF_CONNECTORS }
      };
    }

    public override string Name => throw new NotImplementedException();

    protected override void LoadAllFields( FieldReader reader )
    {
      ElevationOfLevels.Clear();
      HeightOfLevels.Clear();
      HeightOfConnectorsByLevel.Clear();

      foreach (var level in Levels)
      {
        ElevationOfLevels.Add(level.Value, double.Parse(reader.GetSingle<string>(ELEVATION_OF_LEVELS_FIELD + level.Key)));
        HeightOfLevels.Add(level.Value, double.Parse(reader.GetSingle<string>(HEIGHT_OF_LEVELS_FIELD + level.Key)));
        HeightOfConnectorsByLevel.Add(level.Value, double.Parse(reader.GetSingle<string>(HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD + level.Key)));
      }
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      foreach (var level in Levels)
      {
        writer.SetSingle(HEIGHT_OF_LEVELS_FIELD + level.Key, HeightOfLevels[level.Value].ToString());
        writer.SetSingle(HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD + level.Key, HeightOfConnectorsByLevel[level.Value].ToString());
        writer.SetSingle(ELEVATION_OF_LEVELS_FIELD + level.Key, ElevationOfLevels[level.Value].ToString());
      }
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      foreach (var level in Levels)
      {
        generator.SetSingle<string>(HEIGHT_OF_LEVELS_FIELD + level.Key);
        generator.SetSingle<string>(HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD + level.Key);
        generator.SetSingle<string>(ELEVATION_OF_LEVELS_FIELD + level.Key);
      }
     
    }
  }

}
