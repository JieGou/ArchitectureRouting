using System ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class LimitRackModel
  {
    public string ConduitId { get ; set ; }
    public string RackId { get ; set ; }

    public LimitRackModel()
    {
      ConduitId = string.Empty;
      RackId = string.Empty ;
    }

    public LimitRackModel( string? conduitId, string? rackId )
    {
      ConduitId = conduitId ?? string.Empty ;
      RackId = rackId ?? string.Empty ;
    }
  }
}