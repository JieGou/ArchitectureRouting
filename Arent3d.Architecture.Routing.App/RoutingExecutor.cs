using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

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
    /// <param name="progressData">Progress data which is notified the status.</param>
    /// <returns>Result of execution.</returns>
    public async Task<RoutingExecutionResult> Run( IAsyncEnumerable<RouteRecord> fromToList, IProgressData? progressData = null )
    {
      try {
        IReadOnlyCollection<Route> routes ;
        using ( progressData?.Reserve( 0.01 ) ) {
          routes = await ConvertToRoutes( fromToList ) ;
        }

        var domainRoutes = GroupByDomain( routes ) ;
        foreach ( var (domain, routesOfDomain) in domainRoutes ) {
          using var p = progressData?.Reserve( 0.99 * routesOfDomain.Count / routes.Count ) ;
          ExecuteRouting( domain, routesOfDomain, p ) ;
        }

        return RoutingExecutionResult.Success ;
      }
      catch ( OperationCanceledException ) {
        return RoutingExecutionResult.Cancel ;
      }
    }

    private static IEnumerable<(Domain, IReadOnlyCollection<Route>)> GroupByDomain( IReadOnlyCollection<Route> routes )
    {
      var dic = new Dictionary<Domain, List<Route>>() ;

      foreach ( var route in routes ) {
        var domain = route.Domain ;
        if ( false == IsRoutingDomain( domain ) ) continue ;

        if ( false == dic.TryGetValue( domain, out var list ) ) {
          list = new List<Route>() ;
          dic.Add( domain, list ) ;
        }

        list.Add( route ) ;
      }

      return dic.Select( pair => ( pair.Key, (IReadOnlyCollection<Route>) pair.Value ) ) ;
    }

    private static bool IsRoutingDomain( Domain domain )
    {
      return domain switch
      {
        Domain.DomainHvac => true,
        Domain.DomainPiping => true,
        _ => false,
      } ;
    }

    private void ExecuteRouting( Domain domain, IReadOnlyCollection<Route> routesInType, IProgressData? progressData )
    {
      var targets = routesInType.SelectMany( route => route.SubRoutes.Select( subRoute => new AutoRoutingTarget( _document, subRoute ) ) ).EnumerateAll() ;

      ICollisionCheckTargetCollector collector ;
      using ( progressData?.Reserve( 0.05 ) ) {
        collector = CreateCollisionCheckTargetCollector( domain, routesInType ) ;
      }

      RouteGenerator generator ;
      using ( progressData?.Reserve( 0.02 ) ) {
        generator = new RouteGenerator( targets, _document, collector ) ;
      }

      using ( var generatorProgressData = progressData?.Reserve( 1 - progressData.Position ) ) {
        generator.Execute( generatorProgressData ) ;
      }

      RegisterBadConnectors( generator.GetBadConnectors() ) ;
    }

    private ICollisionCheckTargetCollector CreateCollisionCheckTargetCollector( Domain domain, IReadOnlyCollection<Route> routesInType )
    {
      return domain switch
      {
        Domain.DomainHvac => new HVacCollisionCheckTargetCollector( _document, routesInType ),
        Domain.DomainPiping => new PipingCollisionCheckTargetCollector( _document, routesInType ),
        _ => throw new InvalidOperationException(),
      } ;
    }

    /// <summary>
    /// Converts routing from-to records to routing objects.
    /// </summary>
    /// <param name="fromToList">Routing from-to records.</param>
    /// <returns>Routing objects</returns>
    private async Task<IReadOnlyCollection<Route>> ConvertToRoutes( IAsyncEnumerable<RouteRecord> fromToList )
    {
      var dic = new Dictionary<string, Route>() ;
      var result = new List<Route>() ; // Ordered by the original from-to record order.

      await foreach ( var record in fromToList ) {
        if ( false == dic.TryGetValue( record.RouteId, out var route ) ) {
          route = new Route( _document, record.RouteId ) ;
          dic.Add( record.RouteId, route ) ;
          result.Add( route ) ;
        }

        route.RegisterConnectors( record.FromId, record.ToId, record.PassPoints ) ;
      }

      return result ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector> badConnectors )
    {
      _badConnectors.AddRange( badConnectors ) ;
    }
  }
}