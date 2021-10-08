using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.Exceptions ;
using MathLib ;
using InvalidOperationException = System.InvalidOperationException ;

namespace Arent3d.Architecture.Routing.AppBase
{
  /// <summary>
  /// Routing execution object.
  /// </summary>
  public abstract class RoutingExecutor
  {
    private readonly PipeSpecDictionary _pipeSpecDictionary ;
    protected Document Document { get ; }
    public IFittingSizeCalculator FittingSizeCalculator { get ; }
    private readonly List<Connector[]> _badConnectors = new() ;

    /// <summary>
    /// Generates a routing execution object.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="view"></param>
    /// <param name="fittingSizeCalculator"></param>
    protected RoutingExecutor( Document document, View view, IFittingSizeCalculator fittingSizeCalculator )
    {
      Document = document ;
      FittingSizeCalculator = fittingSizeCalculator ;
      _pipeSpecDictionary = new PipeSpecDictionary( document, fittingSizeCalculator ) ;
      CollectRacks( document, view, GetRackFamilyInstances() ) ;
    }

    protected abstract IEnumerable<FamilyInstance> GetRackFamilyInstances() ;

    private static void CollectRacks( Document document, View view, IEnumerable<FamilyInstance> rackFamilyInstances )
    {
      const double beamInterval = 6.0 ; // TODO
      const double sideBeamWidth = 0.2 ; // TODO
      const double sideBeamHeight = 0.2 ; // TODO
      var racks = DocumentMapper.Get( document ).RackCollection ;

      racks.Clear() ;
      foreach ( var familyInstance in rackFamilyInstances ) {
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
    /// <param name="progressData">Progress bar.</param>
    /// <returns>Result of execution.</returns>
    public RoutingExecutionResult Run( IReadOnlyCollection<(string RouteName, RouteSegment Segment)> fromToList, IProgressData? progressData = null )
    {
      try {
        IReadOnlyCollection<Route> routes ;
        using ( var p = progressData?.Reserve( 0.01 ) ) {
          routes = ConvertToRoutes( fromToList, p ) ;
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

    public MEPSystemPipeSpec GetMEPSystemPipeSpec( SubRoute subRoute ) => _pipeSpecDictionary.GetMEPSystemPipeSpec( subRoute ) ;

    private void ExecuteRouting( Domain domain, IReadOnlyCollection<Route> routes, IProgressData? progressData )
    {
      progressData?.ThrowIfCanceled() ;
      
      ICollisionCheckTargetCollector collector ;
      using ( progressData?.Reserve( 0.05 ) ) {
        collector = CreateCollisionCheckTargetCollector( domain, routes ) ;
      }

      progressData?.ThrowIfCanceled() ;
      
      RouteGenerator generator ;
      using ( progressData?.Reserve( 0.02 ) ) {
        generator = CreateRouteGenerator( routes, Document, collector ) ;
      }

      progressData?.ThrowIfCanceled() ;
      
      using ( var generatorProgressData = progressData?.Reserve( 1 - progressData.Position ) ) {
        generator.Execute( generatorProgressData ) ;
      }

      RegisterBadConnectors( generator.GetBadConnectorSet() ) ;

      routes.ForEach( r => r.Save() ) ;
    }

    protected abstract RouteGenerator CreateRouteGenerator( IReadOnlyCollection<Route> routes, Document document, ICollisionCheckTargetCollector collector ) ;

    protected abstract ICollisionCheckTargetCollector CreateCollisionCheckTargetCollector( Domain domain, IReadOnlyCollection<Route> routesInType ) ;

    /// <summary>
    /// Converts routing from-to records to routing objects.
    /// </summary>
    /// <param name="fromToList">Routing from-to records.</param>
    /// <param name="progressData">Progress bar.</param>
    /// <returns>Routing objects</returns>
    private IReadOnlyCollection<Route> ConvertToRoutes( IReadOnlyCollection<(string RouteName, RouteSegment Segment)> fromToList, IProgressData? progressData )
    {
      var oldRoutes = RouteCache.Get( Document ) ;

      var dic = new Dictionary<string, Route>() ;
      var result = new List<Route>() ;

      var parents = new HashSet<Route>() ;
      progressData.ForEach( fromToList, tuple =>
      {
        var (routeName, segment) = tuple ;

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

        route.RegisterSegment( segment ) ;

        progressData?.ThrowIfCanceled() ;
      } ) ;

      result.AddRange( parents.Where( p => false == dic.ContainsKey( p.RouteName ) ) ) ;

      return result ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector[]> badConnectorSet )
    {
      _badConnectors.AddRange( badConnectorSet ) ;
    }

    private readonly List<RoutingElementInfo> _deletedElementInfo = new() ;

    public bool HasDeletedElements => ( 0 < _deletedElementInfo.Count ) ;

    public void RegisterDeletedElement( ElementId deletingElementId )
    {
      var elm = Document.GetElement( deletingElementId ) ;
      if ( null == elm ) throw new InvalidOperationException() ;

      if ( RoutingElementInfo.Create( elm ) is { } info ) {
        _deletedElementInfo.Add( info ) ;
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

      foreach ( var pipeInfo in Document.GetAllElementsOfRoute<MEPCurve>().Select( c => RoutingElementInfo.Create( c, routeDic ) ).NonNull().Concat( _deletedElementInfo ) ) {
        foreach ( var reducer in pipeInfo.GetNeighborReducers( Document ) ) {
          pipeInfo.ApplyToReducer( reducer ) ;
        }
      }
    }

    private class RoutingElementInfo
    {
      public static RoutingElementInfo? Create( Element elm )
      {
        return Create( elm, null ) ;
      }

      public static RoutingElementInfo? Create( Element elm, IReadOnlyDictionary<string, Route>? routeDic )
      {
        if ( elm.GetRouteName() is not { } routeName ) return null ;
        if ( null != routeDic && false == routeDic.ContainsKey( routeName ) ) return null ;
        if ( elm.GetSubRouteIndex() is not { } subRouteIndex ) return null ;
        if ( false == elm.TryGetProperty( RoutingParameter.NearestFromSideEndPoints, out string? fromSide ) ) return null ;
        if ( false == elm.TryGetProperty( RoutingParameter.NearestToSideEndPoints, out string? toSide ) ) return null ;

        var connectors = elm.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).ToList() ;

        return new RoutingElementInfo( elm.Id, routeName, subRouteIndex, fromSide!, toSide!, connectors ) ;
      }

      public ElementId ElementId { get ; }
      public string RouteName { get ; }
      public int SubRouteIndex { get ; }
      public string RoutedElementFromSideConnectorIds { get ; }
      public string RoutedElementToSideConnectorIds { get ; }
      public IReadOnlyCollection<Connector> ConnectingConnectors { get ; }

      private RoutingElementInfo( ElementId elmId, string routeName, int subRouteIndex, string fromSide, string toSide, List<Connector> connectors )
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
        return document.ParseEndPoints( connectors ).OfType<ConnectorEndPoint>().Select( c => new ConnectorId( c ) ).ToHashSet() ;
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

    public abstract IFailuresPreprocessor CreateFailuresPreprocessor() ;
  }
}