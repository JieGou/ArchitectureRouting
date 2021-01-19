using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  public enum RoutingExecutionResult
  {
    Success,
    Failure,
    Cancel,
  }

  /// <summary>
  /// Routing execution object.
  /// </summary>
  public class RoutingExecutor
  {
    private readonly Document _document ;
    private readonly List<Connector> _badConnectors = new() ;

    /// <summary>
    /// Generates a routing execution object.
    /// </summary>
    /// <param name="document"></param>
    public RoutingExecutor( Document document )
    {
      _document = document ;
    }

    /// <summary>
    /// Whether some connectors between ducts which elbows, tees or crosses could not be inserted. 
    /// </summary>
    public bool HasBadConnectors => ( 0 < _badConnectors.Count ) ;

    /// <summary>
    /// Returns connectors between ducts which elbows, tees or crosses could not be inserted. 
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Connector> GetBadConnectors() => _badConnectors ;

    /// <summary>
    /// Execute routing for the passed routing records.
    /// </summary>
    /// <param name="fromToList">Routing from-to records.</param>
    /// <param name="collector">Collision check target collector.</param>
    /// <param name="progressData">Progress data which is notified the status.</param>
    /// <returns>Result of execution.</returns>
    public async Task<RoutingExecutionResult> Run( IAsyncEnumerable<RouteRecord> fromToList, ICollisionCheckTargetCollector collector, IProgressData? progressData = null )
    {
      try {
        IReadOnlyCollection<Route> routes ;
        using ( progressData?.Reserve( 0.05 ) ) {
          routes = await ConvertToRoutes( fromToList ) ;
        }

        var targets = routes.Select( route => new AutoRoutingTarget( _document, route ) ) ;

        var generator = new RouteGenerator( targets, _document, collector ) ;
        using ( var generatorProgressData = progressData?.Reserve( 1 - progressData.Position ) ) {
          generator.Execute( generatorProgressData ) ;
        }

        RegisterBadConnectors( generator.GetBadConnectors() ) ;

        return RoutingExecutionResult.Success ;
      }
      catch ( OperationCanceledException ) {
        return RoutingExecutionResult.Cancel ;
      }
    }

    /// <summary>
    /// Converts routing from-to records to routing objects.
    /// </summary>
    /// <param name="fromToList">Routing from-to records.</param>
    /// <returns>Routing objects</returns>
    private static async Task<IReadOnlyCollection<Route>> ConvertToRoutes( IAsyncEnumerable<RouteRecord> fromToList )
    {
      var dic = new Dictionary<string, Route>() ;
      var result = new List<Route>() ; // Ordered by the original from-to record order.

      await foreach ( var record in fromToList ) {
        if ( false == dic.TryGetValue( record.RouteId, out var route ) ) {
          route = new Route( record.RouteId ) ;
          dic.Add( record.RouteId, route ) ;
          result.Add( route ) ;
        }

        route.FromElementIds.Add( record.FromId ) ;
        route.ToElementIds.Add( record.ToId ) ;
      }

      foreach ( var route in result ) {
        route.RemoveDuplicatedElementIds() ;
      }

      return result ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector> badConnectors )
    {
      _badConnectors.AddRange( badConnectors ) ;
    }
  }
}