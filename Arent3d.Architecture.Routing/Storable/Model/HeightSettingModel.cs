using Arent3d.Revit;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class HeightSettingModel
  {
    private const double DEFAULT_HEIGHT_OF_LEVEL = 3000;
    private const double DEFAULT_HEIGHT_OF_CONNECTORS = DEFAULT_HEIGHT_OF_LEVEL / 2;
    private const double DEFAULT_UNDERFLOOR = -500;

    public int LevelId { get; set; }
    public string? LevelName { get; set; }
    public double Elevation { get; set; }
    public double Underfloor { get; set; }
    public double HeightOfLevel { get; set; }
    public double HeightOfConnectors { get; set; }

    public HeightSettingModel( Level levels )
    {
      if(levels == null) throw new ArgumentNullException(nameof(levels));

      LevelId = levels.Id.IntegerValue;
      LevelName = levels.Name;
      Elevation = levels.Elevation.RevitUnitsToMillimeters();
      Underfloor = DEFAULT_UNDERFLOOR;
      HeightOfLevel = DEFAULT_HEIGHT_OF_LEVEL;
      HeightOfConnectors = DEFAULT_HEIGHT_OF_CONNECTORS;
    }

    public HeightSettingModel( Level levels, double elevation, double underfloor, double heightOfLevel, double heightOfConnectors )
    {
      if(levels == null) throw new ArgumentNullException(nameof(levels));

      LevelId = levels.Id.IntegerValue;
      LevelName = levels.Name;
      Elevation = elevation;
      Underfloor = underfloor;
      HeightOfLevel = heightOfLevel;
      HeightOfConnectors = heightOfConnectors;
    }

    public HeightSettingModel( int? levelId, string? levelName, double? elevation, double? underfloor, double? heightOfLevel, double? heightOfConnectors )
    {
      LevelId = levelId ?? throw new ArgumentNullException(nameof(levelId));
      ;
      LevelName = levelName;
      Elevation = elevation ?? 0;
      Underfloor = underfloor ?? DEFAULT_UNDERFLOOR;
      HeightOfLevel = heightOfLevel ?? DEFAULT_HEIGHT_OF_LEVEL;
      HeightOfConnectors = heightOfConnectors ?? DEFAULT_HEIGHT_OF_CONNECTORS;
    }
  }
}
