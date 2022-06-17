using System ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class LimitRackModel
  {
    public string ConduitId { get ; init ; }
    public string RackId { get ; init ; }

    public LimitRackModel()
    {
      ConduitId = string.Empty ;
      RackId = string.Empty ;
    }

    public LimitRackModel( string? conduitId, string? rackId )
    {
      ConduitId = conduitId ?? string.Empty ;
      RackId = rackId ?? string.Empty ;
    }
  }
}