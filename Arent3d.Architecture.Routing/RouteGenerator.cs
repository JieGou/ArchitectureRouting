using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.StorableCaches ;
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
    private readonly IReadOnlyDictionary<(string RouteName, int SubRouteIndex), MEPSystemRouteCondition> _routeConditions ;
    private readonly List<Connector[]> _badConnectors = new() ;
    private readonly PassPointConnectorMapper _globalPassPointConnectorMapper = new() ;

    public RouteGenerator( IReadOnlyCollection<Route> routes, Document document, IFittingSizeCalculator fittingSizeCalculator, ICollisionCheckTargetCollector collector )
    {
      _document = document ;

      _routeConditions = ThreadDispatcher.Dispatch( () => CreateRouteConditions( document, routes, fittingSizeCalculator ) ) ;
      var targets = AutoRoutingTargetGenerator.Run( _document, routes, _routeConditions ) ;
      RoutingTargets = targets.EnumerateAll() ;
      ErasePreviousRoutes() ; // Delete before CollisionCheckTree is built.

      CollisionCheckTree = new CollisionTree.CollisionTree( document, collector, _routeConditions ) ;
      StructureGraph = DocumentMapper.Get( document ).RackCollection ;

      Specifications.Set( DiameterProvider.Instance, PipeClearanceProvider.Instance ) ;
    }

    private static IReadOnlyDictionary<(string RouteName, int SubRouteIndex), MEPSystemRouteCondition> CreateRouteConditions( Document document, IReadOnlyCollection<Route> routes, IFittingSizeCalculator fittingSizeCalculator )
    {
      var dic = new Dictionary<(string RouteName, int SubRouteIndex), MEPSystemRouteCondition>() ;
      
      foreach ( var route in routes ) {
        foreach ( var subRoute in route.SubRoutes ) {
          var key = subRoute.GetKey() ;
          if ( dic.ContainsKey( key ) ) break ;  // same sub route

          var mepSystem = new RouteMEPSystem( document, subRoute ) ;

          var edgeDiameter = subRoute.GetDiameter() ;
          var spec = new MEPSystemPipeSpec( mepSystem, fittingSizeCalculator ) ;
          var routeCondition = new MEPSystemRouteCondition( spec, edgeDiameter, subRoute.AvoidType ) ;

          dic.Add( key, routeCondition ) ;
        }
      }

      return dic ;
    }

    public IReadOnlyCollection<Connector[]> GetBadConnectorSet() => _badConnectors ;

    protected override IReadOnlyCollection<AutoRoutingTarget> RoutingTargets { get ; }

    protected override CollisionTree.CollisionTree CollisionCheckTree { get ; }

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
        list = list.Where( p => false == ( p is FamilyInstance fi && ( fi.IsFamilyInstanceOf( RoutingFamilyType.PassPoint ) || fi.IsFamilyInstanceOf( RoutingFamilyType.TerminatePoint )) ) );
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

      // TODO, if needed
    }

    protected override void OnRoutingTargetProcessed( AutoRoutingTarget routingTarget, AutoRoutingResult result )
    {
#if DUMP_LOGS
      result.DebugExport( GetResultLogFileName( _document, routingTarget ) ) ;
#endif

      var mepSystemCreator = new MEPSystemCreator( _document, routingTarget, _routeConditions ) ;

      foreach ( var routeVertex in result.RouteVertices ) {
        if ( routeVertex is not TerminalPoint ) continue ;

        mepSystemCreator.RegisterEndPointConnector( routeVertex ) ;
      }

      var newElements = CreateEdges( mepSystemCreator, result ).ToList() ;
      newElements.AddRange( mepSystemCreator.ConnectAllVertices() ) ;

      _document.Regenerate() ;

      _globalPassPointConnectorMapper.Merge( mepSystemCreator.PassPointConnectorMapper ) ;

      RegisterBadConnectors( mepSystemCreator.GetBadConnectorSet() ) ;
    }

    protected virtual IEnumerable<Element> CreateEdges( MEPSystemCreator mepSystemCreator, AutoRoutingResult result )
    {
      return result.RouteEdges.Select( routeEdge => mepSystemCreator.CreateEdgeElement( routeEdge, result.GetPassingEndPointInfo( routeEdge ) ) ) ;
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