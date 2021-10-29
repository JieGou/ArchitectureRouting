using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class OffsetSettingModel : IEquatable<OffsetSettingModel>
  {
    private const double DEFAULT_HEIGHT_OF_LEVEL = 3000 ;
    private const double DEFAULT_HEIGHT_OF_CONNECTORS = 2000 ;
    private const double DEFAULT_UNDERFLOOR = -500 ;

    public int LevelId { get ; set ; }
    public double Underfloor { get ; set ; }
    public double HeightOfLevel { get ; set ; }
    public double HeightOfConnectors { get ; set ; }

    public OffsetSettingModel( int levelId )
    {
      LevelId = levelId ;
      Underfloor = DEFAULT_UNDERFLOOR ;
      HeightOfLevel = DEFAULT_HEIGHT_OF_LEVEL ;
      HeightOfConnectors = DEFAULT_HEIGHT_OF_CONNECTORS ;
    }

    public OffsetSettingModel( Level levels, double elevation, double underfloor, double heightOfLevel, double heightOfConnectors )
    {
      Underfloor = Math.Round( underfloor ) ;
      HeightOfLevel = Math.Round( heightOfLevel ) ;
      HeightOfConnectors = Math.Round( heightOfConnectors ) ;
    }

    public OffsetSettingModel( int? levelId, string? levelName, double? elevation, double? underfloor, double? heightOfLevel, double? heightOfConnectors )
    {
      Underfloor = Math.Round( underfloor ?? DEFAULT_UNDERFLOOR ) ;
      HeightOfLevel = Math.Round( heightOfLevel ?? DEFAULT_HEIGHT_OF_LEVEL ) ;
      HeightOfConnectors = Math.Round( heightOfConnectors ?? DEFAULT_HEIGHT_OF_CONNECTORS ) ;
    }

    public bool Equals( OffsetSettingModel other )
    {
      return other != null &&
             LevelId == other.LevelId &&
             Underfloor == other.Underfloor &&
             HeightOfLevel == other.HeightOfLevel &&
             HeightOfConnectors == other.HeightOfConnectors ;
    }
  }
}