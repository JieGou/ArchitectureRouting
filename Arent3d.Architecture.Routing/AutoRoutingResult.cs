using System ;
using System.Collections.Generic ;
using System.Diagnostics ;
using Arent3d.Routing ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Wrapper class of auto routing result.
  /// </summary>
  public class AutoRoutingResult
  {
    private readonly IAutoRoutingResult _result ;

    public IReadOnlyCollection<IRouteEdge> RouteEdges { get ; }
    public IEnumerable<IRouteVertex> RouteVertices => _result.RouteVertices ;

    private readonly IReadOnlyDictionary<IRouteEdge, PassingEndPointInfo> _passingEndPointInfo ;

    public AutoRoutingResult( IAutoRoutingResult result )
    {
      _result = result ;

      // IAutoRoutingResult.RouteEdges returns different instances between calls. AutoRoutingResult will preserve them.
      RouteEdges = result.RouteEdges.EnumerateAll() ;

      _passingEndPointInfo = PassingEndPointInfo.CollectPassingEndPointInfo( RouteEdges ) ;
    }

    public PassingEndPointInfo GetPassingEndPointInfo( IRouteEdge edge )
    {
      if ( false == _passingEndPointInfo.TryGetValue( edge, out var info ) ) throw new ArgumentException() ;

      return info ;
    }

    [Conditional( "DEBUG" )]
    public void DebugExport( string fileName )
    {
      _result.DebugExport( fileName ) ;
    }
  }
}