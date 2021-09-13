using Arent3d.Revit;
using Arent3d.Utility.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid("83A448F4-E120-44E0-A220-F2D3F11B6A09")]
  [StorableVisibility(AppInfo.VendorId)]
  public sealed class HeightSettingStorable : StorableBase
  {
    private const string ELEVATION_OF_LEVELS_FIELD = "ElevationOfLevels";
    private const string HEIGHT_OF_LEVELS_FIELD = "HeightOfLevels";
    private const string HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD = "HeightOfConnectorsByLevel";

    public Dictionary<string, double> ElevationOfLevels { get; set; }
    public Dictionary<string, double> HeightOfLevels { get; set; }
    public Dictionary<string, double> HeightOfConnectorsByLevel { get; set; }
    public List<Level> Levels { get; set; }


    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private HeightSettingStorable( DataStorage owner ) : base(owner, false)
    {
      Levels = new FilteredElementCollector(owner.Document).OfClass(typeof(Level))
                                                           .AsEnumerable()
                                                           .OfType<Level>()
                                                           .ToList();

      ElevationOfLevels = new Dictionary<string, double>();

      HeightOfLevels = new Dictionary<string, double>();

      HeightOfConnectorsByLevel = new Dictionary<string, double>();

    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public HeightSettingStorable( Document document ) : base(document, false)
    {
      Levels = new FilteredElementCollector(document).OfClass(typeof(Level))
                                                     .AsEnumerable()
                                                     .OfType<Level>()
                                                     .ToList();

      ElevationOfLevels = new Dictionary<string, double>();

      HeightOfLevels = new Dictionary<string, double>();

      HeightOfConnectorsByLevel = new Dictionary<string, double>();
    }

    public override string Name => throw new NotImplementedException();

    protected override void LoadAllFields( FieldReader reader )
    {
      ElevationOfLevels.Clear();
      HeightOfLevels.Clear();
      HeightOfConnectorsByLevel.Clear();

      foreach (var level in Levels)
      {
        ElevationOfLevels.Add(level.Name, double.Parse(reader.GetSingle<string>(ELEVATION_OF_LEVELS_FIELD + level.Id.IntegerValue)));
        HeightOfLevels.Add(level.Name, double.Parse(reader.GetSingle<string>(HEIGHT_OF_LEVELS_FIELD + level.Id.IntegerValue)));
        HeightOfConnectorsByLevel.Add(level.Name, double.Parse(reader.GetSingle<string>(HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD + level.Id.IntegerValue)));
      }
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      foreach (var level in Levels)
      {
        writer.SetSingle(HEIGHT_OF_LEVELS_FIELD + level.Id.IntegerValue, HeightOfLevels[level.Name].ToString());
        writer.SetSingle(HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD + level.Id.IntegerValue, HeightOfConnectorsByLevel[level.Name].ToString());
        writer.SetSingle(ELEVATION_OF_LEVELS_FIELD + level.Id.IntegerValue, ElevationOfLevels[level.Name].ToString());
      }
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      foreach (var level in Levels)
      {
        generator.SetSingle<string>(HEIGHT_OF_LEVELS_FIELD + level.Id.IntegerValue);
        generator.SetSingle<string>(HEIGHT_OF_CONNECTORS_BY_LEVEL_FIELD + level.Id.IntegerValue);
        generator.SetSingle<string>(ELEVATION_OF_LEVELS_FIELD + level.Id.IntegerValue);
      }

    }

    public void ClearAll()
    {
      ElevationOfLevels.Clear();
      HeightOfLevels.Clear();
      HeightOfConnectorsByLevel.Clear();
    }
  }

}
