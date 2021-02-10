using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
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
    private readonly List<Connector> _badConnectors = new() ;
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

    public IReadOnlyCollection<Connector> GetBadConnectors() => _badConnectors ;

    protected override IReadOnlyCollection<AutoRoutingTarget> RoutingTargets { get ; }

    protected override ICollisionCheck CollisionCheckTree { get ; }
    
    protected override IStructureGraph StructureGraph { get ; }

    /// <summary>
    /// Erase all previous ducts and pipes in between routing targets.
    /// </summary>
    protected void ErasePreviousRoutes()
    {
      ThreadDispatcher.Dispatch( () => MEPSystemCreator.ErasePreviousRoutes( RoutingTargets ) ) ;
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

      ductCreator.ConnectAllVertices() ;

      _globalPassPointConnectorMapper.Merge( ductCreator.PassPointConnectorMapper ) ;

      RegisterBadConnectors( ductCreator.GetBadConnectors() ) ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector> badConnectors )
    {
      _badConnectors.AddRange( badConnectors ) ;
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