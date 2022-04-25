namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class EcoSettingModel
  {
    //Default eco is EcoMode
    private const bool DEFAULT_ECOMODE = true ;
    public bool IsEcoMode { get ; set ; }

    public EcoSettingModel()
    {
      IsEcoMode = DEFAULT_ECOMODE ;
    }
    public EcoSettingModel( bool? isEcoMode )
    {
      IsEcoMode = isEcoMode ?? false ;
    }
  }
}