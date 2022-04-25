namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class EcoSettingModel
  { 
    public bool IsEcoMode { get ; set ; }

    public EcoSettingModel()
    {
      IsEcoMode = false ;
    }

    public EcoSettingModel( bool? isEcoMode )
    {
      IsEcoMode = isEcoMode ?? false ;
    }
  }
}