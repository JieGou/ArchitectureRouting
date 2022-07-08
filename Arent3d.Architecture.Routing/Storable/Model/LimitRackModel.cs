using System ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class LimitRackModel
  {
    public string RouteName { get ; init ; }
    // public string ConduitId { get ; init ; }
    public List<string> RackIds { get ; init ; }

    public LimitRackModel()
    {
      RouteName = string.Empty ;
      RackIds = new List<string>() ;
      // ConduitId = string.Empty ;
      // RackId = string.Empty ;
    }

    public LimitRackModel( string? routeName )
    {
      RouteName = routeName ?? string.Empty ;
      RackIds = new List<string>() ;
      // ConduitId = conduitId ?? string.Empty ;
      // RackId = rackId ?? string.Empty ;
    }
  }
}