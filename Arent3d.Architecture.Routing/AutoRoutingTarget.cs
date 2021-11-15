using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  public class AutoRoutingTarget : IAutoRoutingTarget
  {
    /// <summary>
    /// All routes an <see cref="AutoRoutingTarget"/> is related to.
    /// </summary>
    public IReadOnlyCollection<Route> Routes { get ; }

    /// <summary>
    /// Routing end points which fluid flows from.
    /// </summary>
    private readonly IReadOnlyCollection<AutoRoutingEndPoint> _fromEndPoints ;

    /// <summary>
    /// Routing end points which fluid flows to.
    /// </summary>
    private readonly IReadOnlyCollection<AutoRoutingEndPoint> _toEndPoints ;

    private readonly IReadOnlyDictionary<AutoRoutingEndPoint, SubRoute> _ep2SubRoute ;

    /// <summary>
    /// Returns all routing end points.
    /// </summary>
    public IEnumerable<AutoRoutingEndPoint> EndPoints => _fromEndPoints.Concat( _toEndPoints ) ;

    public Domain Domain { get ; }

    public AutoRoutingTarget( Document document, IReadOnlyCollection<SubRoute> subRoutes, IReadOnlyDictionary<Route, int> priorities, IReadOnlyDictionary<SubRouteInfo, MEPSystemRouteCondition> routeConditionDictionary )
    {
      if ( 0 == subRoutes.Count ) throw new ArgumentException() ;

      Routes = subRoutes.Select( subRoute => subRoute.Route ).Distinct().EnumerateAll() ;
      Domain = Routes.Select( route => route.Domain ).First() ;

      var depths = GetDepths( subRoutes ) ;

      var dic = new Dictionary<AutoRoutingEndPoint, SubRoute>() ;
      _fromEndPoints = GenerateEndPointList( subRoutes, subRoute => GetFromEndPoints( subRoute, depths[ subRoute ], routeConditionDictionary[ new SubRouteInfo( subRoute ) ] ), dic ) ;
      _toEndPoints = GenerateEndPointList( subRoutes, subRoute => GetToEndPoints( subRoute, depths[ subRoute ], routeConditionDictionary[ new SubRouteInfo( subRoute ) ] ), dic ) ;
      _ep2SubRoute = dic ;

      AutoRoutingEndPoint.ApplyDepths( _fromEndPoints, _toEndPoints ) ;

      var firstSubRoute = subRoutes.First() ;
      LineId = $"{firstSubRoute.Route.RouteName}@{firstSubRoute.SubRouteIndex}" ;

      var trueFixedBopHeight = firstSubRoute.GetTrueFixedBopHeight( FixedHeightUsage.Default ) ;
      Condition = new AutoRoutingCondition( document, firstSubRoute, priorities[ firstSubRoute.Route ], trueFixedBopHeight ) ;
    }

    public AutoRoutingTarget( Document document, SubRoute subRoute, int priority, AutoRoutingEndPoint fromEndPoint, AutoRoutingEndPoint toEndPoint, double? forcedFixedHeight )
    {
      Routes = new[] { subRoute.Route } ;
      Domain = subRoute.Route.Domain ;

      _fromEndPoints = new[] { fromEndPoint } ;
      _toEndPoints = new[] { toEndPoint } ;
      _ep2SubRoute = new Dictionary<AutoRoutingEndPoint, SubRoute> { { fromEndPoint, subRoute }, { toEndPoint, subRoute } } ;

      AutoRoutingEndPoint.ApplyDepths( _fromEndPoints, _toEndPoints ) ;

      LineId = $"{subRoute.Route.RouteName}@{subRoute.SubRouteIndex}" ;

      Condition = new AutoRoutingCondition( document, subRoute, priority, forcedFixedHeight ) ;
    }

    private static Dictionary<SubRoute, int> GetDepths( IReadOnlyCollection<SubRoute> subRoutes )
    {
      var parentInfo = CollectSubRouteParents( subRoutes ) ;

      var result = new Dictionary<SubRoute, int>() ;
      var newDepthList = new List<SubRoute>() ;
      var newDepth = 0 ;
      while ( 0 < parentInfo.Count ) {
        newDepthList.Clear() ;

        foreach ( var (subRoute, parents) in parentInfo ) {
          if ( 0 == parents.Count ) {
            newDepthList.Add( subRoute ) ;
          }
        }

        foreach ( var subRoute in newDepthList ) {
          result.Add( subRoute, newDepth ) ;

          parentInfo.Remove( subRoute ) ;
        }

        foreach ( var (_, parents) in parentInfo ) {
          parents.RemoveAll( newDepthList.Contains ) ;
        }

        ++newDepth ;
      }

      return result ;
    }

    private static IReadOnlyCollection<AutoRoutingEndPoint> GenerateEndPointList( IEnumerable<SubRoute> subRoutes, Func<SubRoute, IEnumerable<AutoRoutingEndPoint>> generator, Dictionary<AutoRoutingEndPoint, SubRoute> dic )
    {
      var list = new List<AutoRoutingEndPoint>() ;

      foreach ( var subRoute in subRoutes ) {
        foreach ( var ep in generator( subRoute ) ) {
          list.Add( ep ) ;
          dic.Add( ep, subRoute ) ;
        }
      }

      return list ;
    }

    private static Dictionary<SubRoute, List<SubRoute>> CollectSubRouteParents( IReadOnlyCollection<SubRoute> subRoutes )
    {
      var subRouteAndParents = subRoutes.ToDictionary( r => r, r => new List<SubRoute>() ) ;

      foreach ( var subRoute in subRoutes ) {
        foreach ( var parent in subRoute.AllEndPoints.Select( ep => ep.ParentBranch().SubRoute ).NonNull() ) {
          if ( false == subRouteAndParents.TryGetValue( subRoute, out var list ) ) continue ; // not contained.

          list.Add( parent ) ;
        }
      }

      return subRouteAndParents ;
    }

    private static IEnumerable<AutoRoutingEndPoint> GetFromEndPoints( SubRoute subRoute, int depth, MEPSystemRouteCondition routeCondition )
    {
      var endPoints = subRoute.FromEndPoints.Where( IsRoutingTargetEnd ) ;
      return endPoints.Select( ep => new AutoRoutingEndPoint( ep, true, depth, subRoute.GetDiameter(), ( false == subRoute.IsRoutingOnPipeSpace ), routeCondition ) ) ;
    }

    private static IEnumerable<AutoRoutingEndPoint> GetToEndPoints( SubRoute subRoute, int depth, MEPSystemRouteCondition routeCondition )
    {
      var endPoints = subRoute.ToEndPoints.Where( IsRoutingTargetEnd ) ;
      return endPoints.Select( ep => new AutoRoutingEndPoint( ep, false, depth, subRoute.GetDiameter(), ( false == subRoute.IsRoutingOnPipeSpace ), routeCondition ) ) ;
    }

    private static bool IsRoutingTargetEnd( IEndPoint ep )
    {
      return ( ep is not RouteEndPoint ) ;
    }

    public IAutoRoutingSpatialConstraints? CreateConstraints()
    {
      if ( ( 0 < _fromEndPoints.Count ) && ( 0 < _toEndPoints.Count ) ) {
        return new AutoRoutingSpatialConstraints( _fromEndPoints, _toEndPoints ) ;
      }

      return null ;
    }

    public string LineId { get ; }

    public ICommonRoutingCondition Condition { get ; }

    public int RouteCount => _fromEndPoints.Count + _toEndPoints.Count - 1 ;

    public Action<IEnumerable<(IAutoRoutingEndPoint, Vector3d)>> PositionInitialized => SyncTermPositions ;

    private static void SyncTermPositions( IEnumerable<(IAutoRoutingEndPoint, Vector3d)> positions )
    {
      foreach ( var (autoRoutingEndPoint, position) in positions ) {
        if ( autoRoutingEndPoint is not AutoRoutingEndPoint endPoint ) throw new Exception() ;

        // do nothing now.
      }
    }

    public SubRoute? GetSubRoute( AutoRoutingEndPoint ep )
    {
      return _ep2SubRoute.TryGetValue( ep, out var subRoute ) ? subRoute : null ;
    }

    public IEnumerable<SubRoute> GetAllSubRoutes()
    {
      return _ep2SubRoute.Values.Distinct() ;
    }


    #region Inner classes

    private class AutoRoutingCondition : ICommonRoutingCondition
    {
      private readonly SubRoute _subRoute ;

      public AutoRoutingCondition( Document document, SubRoute subRoute, int priority, double? forcedFixedHeight )
      {
        var documentData = DocumentMapper.Get( document ) ;

        _subRoute = subRoute ;
        Priority = priority ;
        IsRoutingOnPipeRacks = ( 0 < documentData.RackCollection.RackCount ) && subRoute.IsRoutingOnPipeSpace ;
        AllowHorizontalBranches = documentData.AllowHorizontalBranches( subRoute ) ;
        FixedBopHeight = forcedFixedHeight ;
      }

      public bool IsRoutingOnPipeRacks { get ; }
      public bool IsCrossingPipeRacks => false ;
      public bool IsRouteMergeEnabled => true ;
      public LineType Type => _subRoute.Route.ServiceType ;
      public int Priority { get ; }
      public LoopType LoopType => LoopType.Non ;

      public bool AllowHorizontalBranches { get ; }
      public double? FixedBopHeight { get ; set ; }
    }

    private class AutoRoutingSpatialConstraints : IAutoRoutingSpatialConstraints
    {
      public AutoRoutingSpatialConstraints( IEnumerable<IAutoRoutingEndPoint> fromEndPoints, IEnumerable<IAutoRoutingEndPoint> toEndPoints )
      {
        Starts = fromEndPoints ;
        Destination = toEndPoints ;
      }

      public IEnumerable<IAutoRoutingEndPoint> Starts { get ; }

      public IEnumerable<IAutoRoutingEndPoint> Destination { get ; }
    }

    #endregion
  }
}