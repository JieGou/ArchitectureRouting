using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Core ;

namespace Arent3d.Architecture.Routing
{
  public class AutoRoutingSpatialConstraints : IAutoRoutingSpatialConstraints
  {
    public AutoRoutingSpatialConstraints( IReadOnlyCollection<EndPoint> fromEndPoints, IReadOnlyCollection<EndPoint> toEndPoints )
    {
      Starts = fromEndPoints ;
      Destination = toEndPoints ;
    }

    public IEnumerable<IAutoRoutingEndPoint> Starts { get ; }

    public IEnumerable<IAutoRoutingEndPoint> Destination { get ; }
  }
}