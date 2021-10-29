using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class OffsetSettingModel : IEquatable<OffsetSettingModel>
  {
    private const double DEFAULT_OFFSET = 1000 ;

    public int LevelId { get ; set ; }
    public double Offset { get ; set ; }

    public OffsetSettingModel( int levelId )
    {
      LevelId = levelId ;
      Offset = DEFAULT_OFFSET ;
    }

    public OffsetSettingModel( Level levels, double offset )
    {
      Offset = Math.Round( offset ) ;
    }

    public OffsetSettingModel( int? levelId, double? offset )
    {
      Offset = Math.Round( offset ?? DEFAULT_OFFSET ) ;
    }

    public bool Equals( OffsetSettingModel other )
    {
      return other != null && LevelId == other.LevelId && Offset == other.Offset ;
    }
  }
}