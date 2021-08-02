using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase
{
  /// <summary>
  /// Routing execution object.
  /// </summary>
  public class RoutingExecutor
  {
    private readonly Document _document ;
    private readonly IFittingSizeCalculator _fittingSizeCalculator ;
    private readonly List<Connector[]> _badConnectors = new() ;

    /// <summary>
    /// Generates a routing execution object.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="fittingSizeCalculator"></param>
    /// <param name="view"></param>
    public RoutingExecutor( Document document, IFittingSizeCalculator fittingSizeCalculator, View view )
    {
      _document = document ;
      _fittingSizeCalculator = fittingSizeCalculator ;

      CollectRacks( document, view ) ;
    }

    private static void CollectRacks( Document document, View view )
    {
      const double beamInterval = 6.0 ; // TODO
      const double sideBeamWidth = 0.2 ; // TODO
      const double sideBeamHeight = 0.2 ; // TODO
      var racks = DocumentMapper.Get( document ).RackCollection ;

      racks.Clear() ;
      foreach ( var familyInstance in document.GetAllFamilyInstances( RoutingFamilyType.RackGuide ) ) {
        var (min, max) = familyInstance.get_BoundingBox( view ).To3dRaw() ;

        racks.AddRack( new Rack.Rack( new Box3d( min, max ), beamInterval, sideBeamWidth, sideBeamHeight ) { IsMainRack = true } ) ;
      }

      racks.CreateLinkages() ;
    }

    /// <summary>
    /// Whether some connectors between ducts which elbows, tees or crosses could not be inserted. 
    /// </summary>
    public bool HasBadConnectors => ( 0 < _badConnectors.Count ) ;

    /// <summary>
    /// Returns connectors between ducts which elbows, tees or crosses could not be inserted. 
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<Connector[]> GetBadConnectorSet() => _badConnectors ;

    /// <summary>
    /// Execute routing for the passed routing records.
    /// </summary>
    /// <param name="fromToList">Routing from-to records.</param>
    /// <param name="progressData">Progress data which is notified the status.</param>
    /// <returns>Result of execution.</returns>
    public async Task<RoutingExecutionResult> Run( IAsyncEnumerable<(string RouteName, RouteSegment Segment)> fromToList, IProgressData? progressData = null )
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

        return RoutingExecutionResult.GetSuccess( routes ) ;
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
        Domain.DomainCableTrayConduit => true,
        Domain.DomainElectrical => true,
        _ => false,
      } ;
    }

    private void ExecuteRouting( Domain domain, IReadOnlyCollection<Route> routes, IProgressData? progressData )
    {
      ThreadDispatcher.Dispatch( () => routes.ForEach( r => r.Save() ) ) ;

      ICollisionCheckTargetCollector collector ;
      using ( progressData?.Reserve( 0.05 ) ) {
        collector = CreateCollisionCheckTargetCollector( domain, routes ) ;
      }

      RouteGenerator generator ;
      using ( progressData?.Reserve( 0.02 ) ) {
        generator = new RouteGenerator( routes, _document, _fittingSizeCalculator, collector ) ;
      }

      using ( var generatorProgressData = progressData?.Reserve( 1 - progressData.Position ) ) {
        generator.Execute( generatorProgressData ) ;
      }

      RegisterBadConnectors( generator.GetBadConnectorSet() ) ;
    }

    private ICollisionCheckTargetCollector CreateCollisionCheckTargetCollector( Domain domain, IReadOnlyCollection<Route> routesInType )
    {
      return domain switch
      {
        Domain.DomainHvac => new HVacCollisionCheckTargetCollector( _document, routesInType ),
        Domain.DomainPiping => new PipingCollisionCheckTargetCollector( _document, routesInType ),
        Domain.DomainCableTrayConduit => new CableTrayConduitCollisionCheckTargetCollector( _document, routesInType ), //for testing
        _ => throw new InvalidOperationException(),
      } ;
    }

    /// <summary>
    /// Converts routing from-to records to routing objects.
    /// </summary>
    /// <param name="fromToList">Routing from-to records.</param>
    /// <returns>Routing objects</returns>
    private async Task<IReadOnlyCollection<Route>> ConvertToRoutes( IAsyncEnumerable<(string RouteName, RouteSegment Segment)> fromToList )
    {
      var oldRoutes = ThreadDispatcher.Dispatch( () => RouteCache.Get( _document ) ) ;

      var dic = new Dictionary<string, Route>() ;
      var result = new List<Route>() ;

      var parents = new HashSet<Route>() ;
      await foreach ( var (routeName, segment) in fromToList ) {
        if ( false == dic.TryGetValue( routeName, out var route ) ) {
          route = oldRoutes.FindOrCreate( routeName ) ;
          route.Clear() ;

          dic.Add( routeName, route ) ;
          result.Add( route ) ;
        }

        if ( segment.FromEndPoint.ParentBranch().Route is { } fromParent ) {
          parents.UnionWith( fromParent.GetAllRelatedBranches() ) ;
        }

        if ( segment.ToEndPoint.ParentBranch().Route is { } toParent ) {
          parents.UnionWith( toParent.GetAllRelatedBranches() ) ;
        }

        ThreadDispatcher.Dispatch( () => route.RegisterSegment( segment ) ) ;
      }

      result.AddRange( parents.Where( p => false == dic.ContainsKey( p.RouteName ) ) ) ;

      return result ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector[]> badConnectorSet )
    {
      _badConnectors.AddRange( badConnectorSet ) ;
    }

    private readonly List<PipeInfo> _deletedPipeInfo = new() ;

    public bool HasDeletedElements => ( 0 < _deletedPipeInfo.Count ) ;

    public void RegisterDeletedPipe( ElementId deletingPipeId )
    {
      var elm = _document.GetElement( deletingPipeId ) ;
      if ( null == elm ) throw new InvalidOperationException() ;

      if ( PipeInfo.Create( elm ) is { } info ) {
        _deletedPipeInfo.Add( info ) ;
      }
    }

    public void RunPostProcess( RoutingExecutionResult result )
    {
      if ( RoutingExecutionResultType.Success != result.Type ) return ;

      AddReducerParameters( result.GeneratedRoutes ) ;
    }

    private void AddReducerParameters( IEnumerable<Route> routes )
    {
      var routeDic = routes.ToDictionary( route => route.RouteName ) ;

      foreach ( var pipeInfo in _document.GetAllElementsOfRoute<MEPCurve>().Select( c => PipeInfo.Create( c, routeDic ) ).NonNull().Concat( _deletedPipeInfo ) ) {
        foreach ( var reducer in pipeInfo.GetNeighborReducers( _document ) ) {
          pipeInfo.ApplyToReducer( reducer ) ;
        }
      }
    }

    private class PipeInfo
    {
      public static PipeInfo? Create( Element elm )
      {
        return Create( elm, null ) ;
      }

      public static PipeInfo? Create( Element elm, IReadOnlyDictionary<string, Route>? routeDic )
      {
        if ( elm.GetRouteName() is not { } routeName ) return null ;
        if ( null != routeDic && false == routeDic.ContainsKey( routeName ) ) return null ;
        if ( elm.GetSubRouteIndex() is not { } subRouteIndex ) return null ;
        if ( false == elm.TryGetProperty( RoutingParameter.NearestFromSideEndPoints, out string? fromSide ) ) return null ;
        if ( false == elm.TryGetProperty( RoutingParameter.NearestToSideEndPoints, out string? toSide ) ) return null ;

        var connectors = elm.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).ToList() ;

        return new PipeInfo( elm.Id, routeName, subRouteIndex, fromSide!, toSide!, connectors ) ;
      }

      public ElementId ElementId { get ; }
      public string RouteName { get ; }
      public int SubRouteIndex { get ; }
      public string RoutedElementFromSideConnectorIds { get ; }
      public string RoutedElementToSideConnectorIds { get ; }
      public IReadOnlyCollection<Connector> ConnectingConnectors { get ; }

      private PipeInfo( ElementId elmId, string routeName, int subRouteIndex, string fromSide, string toSide, List<Connector> connectors )
      {
        ElementId = elmId ;
        RouteName = routeName ;
        SubRouteIndex = subRouteIndex ;
        RoutedElementFromSideConnectorIds = fromSide ;
        RoutedElementToSideConnectorIds = toSide ;
        ConnectingConnectors = connectors ;
      }

      public IEnumerable<FamilyInstance> GetNeighborReducers( Document document )
      {
        var fromReducers = GetNeighborReducers( document, GetEndPoints( document, RoutedElementFromSideConnectorIds ), true ) ;
        var toReducers = GetNeighborReducers( document, GetEndPoints( document, RoutedElementToSideConnectorIds ), false ) ;
        return fromReducers.Concat( toReducers ) ;
      }

      public void ApplyToReducer( FamilyInstance reducer )
      {
        reducer.SetProperty( RoutingParameter.RouteName, RouteName ) ;
        reducer.SetProperty( RoutingParameter.SubRouteIndex, SubRouteIndex ) ;
        reducer.SetProperty( RoutingParameter.NearestFromSideEndPoints, RoutedElementFromSideConnectorIds ) ;
        reducer.SetProperty( RoutingParameter.NearestToSideEndPoints, RoutedElementToSideConnectorIds ) ;
      }

      private static HashSet<ConnectorId> GetEndPoints( Document document, string connectors )
      {
        return EndPointExtensions.ParseEndPoints( document, connectors ).OfType<ConnectorEndPoint>().Select( c => new ConnectorId( c ) ).ToHashSet() ;
      }

      private IEnumerable<FamilyInstance> GetNeighborReducers( Document document, HashSet<ConnectorId> endPoints, bool isFrom )
      {
        var doneElms = new HashSet<ElementId> { ElementId } ;
        var stack = new Stack<Connector>() ;
        ConnectingConnectors.Where( c => c.IsValidObject ).ForEach( stack.Push ) ;

        while ( 0 < stack.Count ) {
          var connector = stack.Pop() ;
          var connId = new ConnectorId( connector ) ;
          if ( endPoints.Contains( connId ) ) continue ;

          var owner = connId.GetOwner( document ) ;
          if ( owner is not FamilyInstance nextElm ) continue ;
          if ( false == doneElms.Add( nextElm.Id ) ) continue ;
          if ( false == nextElm.IsFittingElement() ) continue ;
          if ( null != nextElm.GetRouteName() ) continue ;

          var conn = connId.GetConnector( owner ) ;
          if ( null == conn ) continue ;

          conn.GetOtherConnectorsInOwner().SelectMany( c => c.GetConnectedConnectors() ).ForEach( stack.Push ) ;
          yield return nextElm ;
        }
      }
    }
  }
}