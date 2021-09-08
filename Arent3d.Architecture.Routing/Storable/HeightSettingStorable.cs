using Arent3d.Revit;
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
    private const string HeightOfLevelsField = "HeightOfLevels";
    private const string HeightOfConnectorsByLevelField = "HeightOfConnectorsByLevel";
    private const string Level1Name = "レベル 1";
    private const string Level2Name = "レベル 2";

    public Dictionary<string, double> HeightOfLevels { get; set; }
    public Dictionary<string, double> HeightOfConnectorsByLevel { get; set; }


    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private HeightSettingStorable( DataStorage owner ) : base(owner, false)
    {
      HeightOfLevels = new Dictionary<string, double>() { { Level1Name, 4000 }, { Level2Name, 8000 } };
      HeightOfConnectorsByLevel = new Dictionary<string, double>() { { Level1Name, 2000 }, { Level2Name, 2000 } };

    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    public HeightSettingStorable( Document document ) : base(document, false)
    {
      HeightOfLevels = new Dictionary<string, double>() { { Level1Name, 0 }, { Level2Name, 4000 } };
      HeightOfConnectorsByLevel = new Dictionary<string, double>() { { Level1Name, 2000 }, { Level2Name, 2000 } };
    }

    public override string Name => throw new NotImplementedException();

    protected override void LoadAllFields( FieldReader reader )
    {
      HeightOfLevels.Clear();
      HeightOfConnectorsByLevel.Clear();

      HeightOfLevels.Add(Level1Name, double.Parse(reader.GetSingle<string>(HeightOfLevelsField + "1")));
      HeightOfLevels.Add(Level2Name, double.Parse(reader.GetSingle<string>(HeightOfLevelsField + "2")));

      HeightOfConnectorsByLevel.Add(Level1Name, double.Parse(reader.GetSingle<string>(HeightOfConnectorsByLevelField + "1")));
      HeightOfConnectorsByLevel.Add(Level2Name, double.Parse(reader.GetSingle<string>(HeightOfConnectorsByLevelField + "2")));
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle(HeightOfLevelsField + "1", HeightOfLevels[Level1Name].ToString());
      writer.SetSingle(HeightOfLevelsField + "2", HeightOfLevels[Level2Name].ToString());
      writer.SetSingle(HeightOfConnectorsByLevelField + "1", HeightOfConnectorsByLevel[Level1Name].ToString());
      writer.SetSingle(HeightOfConnectorsByLevelField + "2", HeightOfConnectorsByLevel[Level2Name].ToString());

    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<string>(HeightOfLevelsField + "1");
      generator.SetSingle<string>(HeightOfLevelsField + "2");
      generator.SetSingle<string>(HeightOfConnectorsByLevelField + "1");
      generator.SetSingle<string>(HeightOfConnectorsByLevelField + "2");
    }
  }
}
