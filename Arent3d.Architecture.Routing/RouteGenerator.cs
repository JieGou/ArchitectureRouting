using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Revit ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route generator class. This calculates route paths from routing targets and transforms revit elements.
  /// </summary>
  public class RouteGenerator : RouteGeneratorBase<AutoRoutingTarget>
  {
    private readonly Document _document ;
    private readonly IReadOnlyDictionary<Route, RouteMEPSystem> _routeMEPSystems ;
    private readonly List<Connector[]> _badConnectors = new() ;
    private readonly PassPointConnectorMapper _globalPassPointConnectorMapper = new() ;

    public RouteGenerator( IEnumerable<AutoRoutingTarget> targets, Document document, CollisionTree.ICollisionCheckTargetCollector collector )
    {
      _document = document ;

      RoutingTargets = targets.EnumerateAll() ;
      _routeMEPSystems = CreateRouteMEPSystems( document, RoutingTargets ) ;
      ErasePreviousRoutes() ; // Delete before CollisionCheckTree is built.

      CollisionCheckTree = new CollisionTree.CollisionTree( collector ) ;
      StructureGraph = DocumentMapper.Get( document ).RackCollection ;

      Specifications.Set( DiameterProvider.Instance, PipeClearanceProvider.Instance ) ;
    }

    private static IReadOnlyDictionary<Route, RouteMEPSystem> CreateRouteMEPSystems( Document document, IReadOnlyCollection<AutoRoutingTarget> routingTargets )
    {
      return ThreadDispatcher.Dispatch( () => routingTargets.Select( target => target.SubRoute.Route ).Distinct().ToDictionary( route => route, route => new RouteMEPSystem( document, route ) ) ) ;
    }

    public IReadOnlyCollection<Connector[]> GetBadConnectorSet() => _badConnectors ;

    protected override IReadOnlyCollection<AutoRoutingTarget> RoutingTargets { get ; }

    protected override ICollisionCheck CollisionCheckTree { get ; }
    
    protected override IStructureGraph StructureGraph { get ; }

    /// <summary>
    /// Erase all previous ducts and pipes in between routing targets.
    /// </summary>
    private void ErasePreviousRoutes()
    {
      ThreadDispatcher.Dispatch( () => EraseRoutes( _document, RoutingTargets.Select( t => t.SubRoute.Route.RouteId ), false ) ) ;
    }

    public static void EraseRoutes( Document document, IEnumerable<string> routeNames, bool eraseRouteStoragesAndPassPoints )
    {
      var hashSet = ( routeNames as ISet<string> ) ?? routeNames.ToHashSet() ;

      var list = document.GetAllElementsOfRoute<Element>().Where( e => e.GetRouteName() is { } routeName && hashSet.Contains( routeName ) ) ;
      if ( false == eraseRouteStoragesAndPassPoints ) {
        // do not erase pass points
        list = list.Where( p => false == p.IsPassPoint() ) ;
      }

      document.Delete( list.Select( elm => elm.Id ).ToArray() ) ;

      if ( eraseRouteStoragesAndPassPoints ) {
        // erase routes, too.
        RouteCache.Get( document ).Drop( hashSet ) ;
      }
    }

    protected override void OnGenerationStarted()
    {
      // TODO
    }

    protected override void OnRoutingTargetProcessed( AutoRoutingTarget routingTarget, IAutoRoutingResult result )
    {
      result.DebugExport( GetDebugFileName( _document, routingTarget ) ) ;
      var ductCreator = new MEPSystemCreator( _document, routingTarget, _routeMEPSystems[ routingTarget.SubRoute.Route ] ) ;

      foreach ( var routeVertex in result.RouteVertices ) {
        if ( routeVertex is not TerminalPoint ) continue ;

        ductCreator.RegisterEndPointConnector( routeVertex ) ;
      }

      foreach ( var routeEdge in result.RouteEdges ) {
        ductCreator.CreateEdgeElement( routeEdge ) ;
      }

      ductCreator.ConnectAllVertices( routingTarget ) ;

      _globalPassPointConnectorMapper.Merge( ductCreator.PassPointConnectorMapper ) ;

      RegisterBadConnectors( ductCreator.GetBadConnectorSet() ) ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector[]> badConnectorSet )
    {
      _badConnectors.AddRange( badConnectorSet ) ;
    }

    private static string GetDebugFileName( Document document, AutoRoutingTarget routingTarget )
    {
      var dir = Path.Combine( Path.GetDirectoryName( document.PathName )!, Path.GetFileNameWithoutExtension( document.PathName ) ) ;
      return Path.Combine( Directory.CreateDirectory( dir ).FullName, routingTarget.LineId + ".log" ) ;
    }

    protected override void OnGenerationFinished()
    {
      foreach ( var (passPointId, (conn1, conn2)) in _globalPassPointConnectorMapper ) {
        // pass point must have from-side and to-side connector
        if ( null == conn1 || null == conn2 ) throw new InvalidOperationException() ;

        var element = _document.GetElement( new ElementId( passPointId ) ) ;
        element.SetPassPointConnectors( new[] { conn1 }, new[] { conn2 } ) ;

        conn1.ConnectTo( conn2 ) ;
      }
    }
  }
}