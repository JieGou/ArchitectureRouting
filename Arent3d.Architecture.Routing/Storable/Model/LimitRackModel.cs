using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class LimitRackModel
  {
    public string RouteName { get ; }
    public List<string> RackIds { get ; }

    public LimitRackModel()
    {
      RouteName = string.Empty ;
      RackIds = new List<string>() ;
    }

    public LimitRackModel( string? routeName )
    {
      RouteName = routeName ?? string.Empty ;
      RackIds = new List<string>() ;
    }
  }
}