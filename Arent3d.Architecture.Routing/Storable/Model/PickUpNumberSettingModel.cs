namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class PickUpNumberSettingModel
  {
    public bool IsPickUpNumberSetting { get ; set ; }
    public string Level { get ; set ; }
    
    public PickUpNumberSettingModel(string? level, bool? isPickUpNumberSetting = default)
    {
      Level = level ?? string.Empty ;
      IsPickUpNumberSetting = isPickUpNumberSetting ?? false ;
    }
  }
}