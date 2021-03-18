using System ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.App
{
  public enum RoutingExecutionResultType
  {
    Success,
    Failure,
    Cancel,
  }

  public class RoutingExecutionResult
  {
    public static RoutingExecutionResult Cancel { get ; } = new RoutingExecutionResult( RoutingExecutionResultType.Cancel ) ;
    public static RoutingExecutionResult Failure { get ; } = new RoutingExecutionResult( RoutingExecutionResultType.Failure ) ;

    public RoutingExecutionResultType Type { get ; }
    public IReadOnlyCollection<Route> GeneratedRoutes { get ; }

    private RoutingExecutionResult( RoutingExecutionResultType type, IReadOnlyCollection<Route>? routes = null )
    {
      Type = type ;
      GeneratedRoutes = routes ?? Array.Empty<Route>() ;
    }

    public static RoutingExecutionResult GetSuccess( IReadOnlyCollection<Route> routes )
    {
      return new RoutingExecutionResult( RoutingExecutionResultType.Success, routes ) ;
    }
  }
}