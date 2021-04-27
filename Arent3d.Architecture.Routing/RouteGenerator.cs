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
    private readonly IReadOnlyDictionary<SubRoute, RouteMEPSystem> _routeMEPSystems ;
    private readonly List<Connector[]> _badConnectors = new() ;
    private readonly PassPointConnectorMapper _globalPassPointConnectorMapper = new() ;

    public RouteGenerator( IReadOnlyCollection<Route> routes, Document document, CollisionTree.ICollisionCheckTargetCollector collector )
    {
      _document = document ;

      _routeMEPSystems = ThreadDispatcher.Dispatch( () => CreateRouteMEPSystems( document, routes ) ) ;
      var targets = AutoRoutingTargetGenerator.Run( _document, routes, _routeMEPSystems ) ;
      RoutingTargets = targets.EnumerateAll() ;
      ErasePreviousRoutes() ; // Delete before CollisionCheckTree is built.

      CollisionCheckTree = new CollisionTree.CollisionTree( collector ) ;
      StructureGraph = DocumentMapper.Get( document ).RackCollection ;

      Specifications.Set( DiameterProvider.Instance, PipeClearanceProvider.Instance ) ;
    }

    private static IReadOnlyDictionary<SubRoute, RouteMEPSystem> CreateRouteMEPSystems( Document document, IReadOnlyCollection<Route> routes )
    {
      var dic = new Dictionary<SubRoute, RouteMEPSystem>() ;
      
      foreach ( var route in routes ) {
        foreach ( var subRoute in route.SubRoutes ) {
          if ( dic.ContainsKey( subRoute ) ) break ;  // same route

          dic.Add( subRoute, new RouteMEPSystem( document, subRoute ) ) ;
        }
      }

      return dic ;
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
      ThreadDispatcher.Dispatch( () => EraseRoutes( _document, RoutingTargets.SelectMany( t => t.Routes ).Select( route => route.RouteName ), false ) ) ;
    }

    public static void EraseRoutes( Document document, IEnumerable<string> routeNames, bool eraseRouteStoragesAndPassPoints )
    {
      var hashSet = ( routeNames as ISet<string> ) ?? routeNames.ToHashSet() ;

      var list = document.GetAllElementsOfRoute<Element>().Where( e => e.GetRouteName() is { } routeName && hashSet.Contains( routeName ) ) ;
      if ( false == eraseRouteStoragesAndPassPoints ) {
        // do not erase pass points
        list = list.Where( p => false == ( p is FamilyInstance fi && fi.IsRoutingFamilyInstanceOf( RoutingFamilyType.PassPoint ) ) ) ;
      }

      document.Delete( list.SelectMany( SelectAllRelatedElements ).Select( elm => elm.Id ).Distinct().ToArray() ) ;

      if ( eraseRouteStoragesAndPassPoints ) {
        // erase routes, too.
        RouteCache.Get( document ).Drop( hashSet ) ;
      }
    }

    private static IEnumerable<Element> SelectAllRelatedElements( Element elm )
    {
      yield return elm ;

      foreach ( var neighborElement in elm.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).Select( c => c.Owner ) ) {
        if ( neighborElement.IsFittingElement() ) yield return neighborElement ;
      }
    }

    protected override void OnGenerationStarted()
    {
#if DUMP_LOGS
      RoutingTargets.DumpRoutingTargets( GetTargetsLogFileName( _document ), CollisionCheckTree ) ;
#endif

      // TODO
    }

    protected override void OnRoutingTargetProcessed( AutoRoutingTarget routingTarget, AutoRoutingResult result )
    {
#if DUMP_LOGS
      result.DebugExport( GetResultLogFileName( _document, routingTarget ) ) ;
#endif

      var ductCreator = new MEPSystemCreator( _document, routingTarget, _routeMEPSystems ) ;

      foreach ( var routeVertex in result.RouteVertices ) {
        if ( routeVertex is not TerminalPoint ) continue ;

        ductCreator.RegisterEndPointConnector( routeVertex ) ;
      }

      foreach ( var routeEdge in result.RouteEdges ) {
        ductCreator.CreateEdgeElement( routeEdge, result.GetPassingEndPoints( routeEdge ) ) ;
      }

      ductCreator.ConnectAllVertices() ;

      _globalPassPointConnectorMapper.Merge( ductCreator.PassPointConnectorMapper ) ;

      RegisterBadConnectors( ductCreator.GetBadConnectorSet() ) ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector[]> badConnectorSet )
    {
      _badConnectors.AddRange( badConnectorSet ) ;
    }

    protected override void OnGenerationFinished()
    {
      var list = new List<Connector>() ;

      foreach ( var (passPointId, (conn1, conn2, others)) in _globalPassPointConnectorMapper.GetPassPointConnections( _document ) ) {
        // pass point must have from-side and to-side connector
        if ( null == conn1 || null == conn2 ) throw new InvalidOperationException() ;

        var element = _document.GetElement( new ElementId( passPointId ) ) ;
        element.SetPassPointConnectors( new[] { conn1 }, new[] { conn2 } ) ;

        list.Clear() ;
        list.Add( conn1 ) ;
        list.Add( conn2 ) ;
        if ( null != others ) list.AddRange( others ) ;

        var routeName = conn1.Owner.GetRouteName() ?? conn2.Owner.GetRouteName() ;
        var subRouteIndex = conn1.Owner.GetSubRouteIndex() ?? conn2.Owner.GetSubRouteIndex() ;

        var (success, fitting) = MEPSystemCreator.ConnectConnectors( _document, list ) ;
        if ( success && null != fitting ) {
          // set routing id.
          if ( null != routeName ) fitting.SetProperty( RoutingParameter.RouteName, routeName ) ;
          if ( null != subRouteIndex ) fitting.SetProperty( RoutingParameter.SubRouteIndex, subRouteIndex.Value ) ;

          // Relate fitting to the pass point.
          element.SetProperty( RoutingParameter.RelatedPassPointId, passPointId ) ;
        }
      }
    }

    private static string GetLogDirectoryName( Document document )
    {
      var dir = Path.Combine( Path.GetDirectoryName( document.PathName )!, Path.GetFileNameWithoutExtension( document.PathName ) ) ;
      return Directory.CreateDirectory( dir ).FullName ;
    }

    private static string GetTargetsLogFileName( Document document )
    {
      return Path.Combine( GetLogDirectoryName( document ), "RoutingTargets.xml" ) ;
    }

    private static string GetResultLogFileName( Document document, AutoRoutingTarget routingTarget )
    {
      return Path.Combine( GetLogDirectoryName( document ), routingTarget.LineId + ".log" ) ;
    }
  }
}