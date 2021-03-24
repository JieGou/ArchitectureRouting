using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route definition class.
  /// </summary>
  [Guid( "83A448F4-E120-44E0-A220-F2D3F11B6A05" )]
  [StorableVisibility( AppInfo.VendorId )]
  public sealed class Route : StorableBase
  {
    public const string DefaultFluidPhase = "None" ;
    public const string DefaultInsulationType = "None" ;

    /// <summary>
    /// Unique identifier name of a route.
    /// </summary>
    public string RouteName { get ; private set ; }

    /// <summary>
    /// Reverse dictionary to search which sub route an end point belongs to.
    /// </summary>
    private readonly Dictionary<(IEndPointIndicator Indicator, bool IsFrom), SubRoute> _subRouteMap = new() ;

    public string FluidPhase => DefaultFluidPhase ;
    public LineType ServiceType => LineType.Utility ;
    public LoopType LoopType => LoopType.Non ;
    public double Temperature => 30 ; // provisional

    private readonly List<RouteSegment> _routeSegments = new() ;
    private readonly List<SubRoute> _subRoutes = new() ;

    public IReadOnlyCollection<SubRoute> SubRoutes => _subRoutes ;

    public SubRoute? GetSubRoute( int index )
    {
      if ( index < 0 || _subRoutes.Count <= index ) return null ;
      return _subRoutes[ index ] ;
    }

    public IReadOnlyCollection<RouteSegment> RouteSegments => _routeSegments ;

    public ConnectorIndicator? FirstFromConnector()
    {
      return _subRoutes.SelectMany( subRoute => subRoute.FromEndPointIndicators.OfType<ConnectorIndicator>() ).FirstOrDefault() ;
    }

    public ConnectorIndicator? FirstToConnector()
    {
      return _subRoutes.SelectMany( subRoute => subRoute.ToEndPointIndicators.OfType<ConnectorIndicator>() ).FirstOrDefault() ;
    }

    private Domain? _domain = null ;

    public Domain Domain => _domain ??= GetReferenceConnector().Domain ;

    /// <summary>
    /// for loading from storage.
    /// </summary>
    /// <param name="owner">Owner element.</param>
    private Route( Element owner ) : base( owner, false )
    {
      RouteName = string.Empty ;
    }

    /// <summary>
    /// Called by RouteCache.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="routeId"></param>
    internal Route( Document document, string routeId ) : base( document, false )
    {
      RouteName = routeId ;
    }

    public void Clear()
    {
      _subRouteMap.Clear() ;
      _routeSegments.Clear() ;
      _subRoutes.Clear() ;
      _domain = null ;
    }

    /// <summary>
    /// Add from-to information.
    /// </summary>
    /// <param name="segment">From-to segment.</param>
    /// <returns>False, if any connector id or pass point id is not found, or has any contradictions in the from-to list (i.e., one connector is registered as both from and end).</returns>
    public bool RegisterSegment( RouteSegment segment )
    {
      var fromId = segment.FromId ;
      var toId = segment.ToId ;

      // check id.
      if ( false == fromId.IsValid( Document, true ) ) return false ;
      if ( false == toId.IsValid( Document, false ) ) return false ;

      if ( fromId.IsOneSided && _subRouteMap.ContainsKey( ( fromId, false ) ) ) {
        // contradiction!
        return false ;
      }

      if ( toId.IsOneSided && _subRouteMap.ContainsKey( ( toId, true ) ) ) {
        // contradiction!
        return false ;
      }

      if ( false == _subRouteMap.TryGetValue( ( fromId, true ), out var subRoute1 ) ) subRoute1 = null ;
      if ( false == _subRouteMap.TryGetValue( ( toId, false ), out var subRoute2 ) ) subRoute2 = null ;

      if ( null != subRoute1 ) {
        if ( null != subRoute2 ) {
          if ( subRoute1 != subRoute2 ) {
            // merge.
            foreach ( var ind in subRoute2.FromEndPointIndicators ) {
              _subRouteMap[ ( ind, true ) ] = subRoute1 ;
            }

            foreach ( var ind in subRoute2.ToEndPointIndicators ) {
              _subRouteMap[ ( ind, false ) ] = subRoute1 ;
            }

            subRoute1.Merge( subRoute2 ) ;
          }
          else {
            // already added.
          }
        }
        else {
          // toId is newly added
          subRoute1.AddSegment( segment ) ;
          _subRouteMap.Add( ( toId, false ), subRoute1 ) ;
        }
      }
      else if ( null != subRoute2 ) {
        // fromId is newly added
        subRoute2.AddSegment( segment ) ;
        _subRouteMap.Add( ( fromId, true ), subRoute2 ) ;
      }
      else {
        // new sub route.
        var subRoute = new SubRoute( this, _subRoutes.Count ) ;
        subRoute.AddSegment( segment ) ;
        _subRoutes.Add( subRoute ) ;
        _subRouteMap.Add( ( fromId, true ), subRoute ) ;
        _subRouteMap.Add( ( toId, false ), subRoute ) ;
      }

      _routeSegments.Add( segment ) ;
      return true ;
    }

    /// <summary>
    /// Returns a representative connector whose parameters are used for MEP system creation.
    /// </summary>
    /// <returns>Connector.</returns>
    /// <exception cref="InvalidOperationException">Has no sub routes.</exception>
    public Connector GetReferenceConnector()
    {
      return _subRoutes.Select( subRoute => subRoute.GetReferenceConnectorInSubRoute() ).NonNull().First() ;
    }

    /// <summary>
    /// Returns all connectors.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    public IEnumerable<Connector> GetAllConnectors( Document document )
    {
      var indicators = SubRoutes.SelectMany( subRoute => subRoute.FromEndPointIndicators.Concat( subRoute.ToEndPointIndicators ) ).OfType<ConnectorIndicator>() ;
      return indicators.Select( ind => ind.GetConnector( document ) ).NonNull() ;
    }

    #region Branches

    public static HashSet<Route> GetAllRelatedBranches( IEnumerable<Route> routeList )
    {
      var routes = new HashSet<Route>() ;
      foreach ( var route in routeList ) {
        route.CollectRelatedBranches( routes ) ;
      }

      return routes ;
    }

    public HashSet<Route> GetAllRelatedBranches()
    {
      var routes = new HashSet<Route>() ;
      CollectRelatedBranches( routes ) ;
      return routes ;
    }

    private void CollectRelatedBranches( HashSet<Route> routes )
    {
      AddChildren( routes, this, r =>
      {
        r.GetParentBranches().ForEach( parent => parent.CollectRelatedBranches( routes ) ) ;
      } ) ;
    }


    public HashSet<Route> GetParentBranches()
    {
      var routes = new HashSet<Route>() ;
      foreach ( var subRoute in _subRoutes ) {
        routes.UnionWith( subRoute.AllEndPointIndicators.Select( ind => ind.ParentBranch( Document ).Route ).NonNull() ) ;
      }

      routes.Remove( this ) ;

      return routes ;
    }

    public IEnumerable<Route> GetChildBranches()
    {
      return RouteCache.Get( Document ).Values.Where( IsParentBranch ) ;
    }

    public bool IsParentBranch( Route route )
    {
      return route._subRoutes.SelectMany( subRoute => subRoute.AllEndPointIndicators ).Any( ind => ind.ParentBranch( route.Document ).Route == this ) ;
    }

    public static IReadOnlyCollection<Route> CollectAllDescendantBranches( IEnumerable<Route> routes )
    {
      var routeSet = new HashSet<Route>() ;
      foreach ( var route in routes ) {
        AddChildren( routeSet, route ) ;
      }
      return routeSet ;
    }

    public IReadOnlyCollection<Route> CollectAllDescendantBranches()
    {
      var routeSet = new HashSet<Route>() ;
      AddChildren( routeSet, this ) ;
      return routeSet ;
    }

    private static void AddChildren( HashSet<Route> routeSet, Route root, Action<Route>? onAdd = null )
    {
      if ( false == routeSet.Add( root ) ) return ;
      onAdd?.Invoke( root ) ;

      foreach ( var child in root.GetChildBranches() ) {
        AddChildren( routeSet, child, onAdd ) ;
      }
    }

    #endregion

    #region Store

    private const string RouteNameField = "RouteName" ;
    private const string RouteSegmentsField = "RouteSegments" ;

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetSingle<string>( RouteNameField ) ;
      generator.SetArray<RouteSegment>( RouteSegmentsField ) ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      RouteName = reader.GetSingle<string>( RouteNameField ) ;
      reader.GetArray<RouteSegment>( RouteSegmentsField ).ForEach( segment => RegisterSegment( segment ) ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( RouteNameField, RouteName ) ;
      writer.SetArray( RouteSegmentsField, _routeSegments ) ;
    }

    #endregion
  }
}