using Arent3d.Architecture.Routing.Utils ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class PickUpNumberSettingModel
  {
    private const string DefaultLevelName = "(No level name)" ;
    
    public bool IsPickUpNumberSetting { get ; set ; }
    public string LevelName { get ; set ; }
    public int LevelId { get ; set ; }

    public PickUpNumberSettingModel( Level level, bool? isPickUpNumberSetting = default )
    {
      LevelId = level.Id.IntegerValue ;
      LevelName = StringUtils.DefaultIfBlank( level.Name, DefaultLevelName ) ;
      IsPickUpNumberSetting = isPickUpNumberSetting ?? false ;
    }
    
    public PickUpNumberSettingModel( int? levelId, string? levelName, bool? isPickUpNumberSetting = default )
    {
      LevelId = levelId ?? 0 ;
      LevelName = StringUtils.DefaultIfBlank( levelName, DefaultLevelName ) ;
      IsPickUpNumberSetting = isPickUpNumberSetting ?? false ;
    }
  }
}