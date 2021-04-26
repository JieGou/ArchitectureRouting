using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Routing ;
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
    private string _routeName = "None" ;
    /// <summary>
    /// Unique identifier name of a route.
    /// </summary>
    public string RouteName
    {
      get => this._routeName ;
      set
      {
        var oldName = this._routeName ;
        
        this._routeName = value ;
        RenameAllDescendents(oldName, value);
      }
    }

    /// <summary>
    /// Reverse dictionary to search which sub route an end point belongs to.
    /// </summary>
    private readonly Dictionary<(EndPointKey Key, bool IsFrom), SubRoute> _subRouteMap = new() ;

    public LineType ServiceType => LineType.Utility ;
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

    public ConnectorEndPoint? FirstFromConnector()
    {
      return _subRoutes.SelectMany( subRoute => subRoute.FromEndPoints.OfType<ConnectorEndPoint>() ).FirstOrDefault() ;
    }

    public ConnectorEndPoint? FirstToConnector()
    {
      return _subRoutes.SelectMany( subRoute => subRoute.ToEndPoints.OfType<ConnectorEndPoint>() ).FirstOrDefault() ;
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
    public bool RegisterSegment( RouteSegment segment ) => RegisterSegment( segment, true ) ;

    private bool RegisterSegment( RouteSegment segment, bool generateInstance )
    {
      var fromEndPoint = segment.FromEndPoint ;
      var toEndPoint = segment.ToEndPoint ;

      var generatedFrom = generateInstance && fromEndPoint.GenerateInstance( RouteName ) ;
      var generatedTo = generateInstance && toEndPoint.GenerateInstance( RouteName ) ;
      if ( false == RegisterSegment( segment, fromEndPoint, toEndPoint, generateInstance ) ) {
        // cleanup
        if ( generatedFrom ) fromEndPoint.EraseInstance() ;
        if ( generatedTo ) toEndPoint.EraseInstance() ;

        return false ;
      }

      return true ;
    }

    private bool RegisterSegment( RouteSegment segment, IEndPoint fromEndPoint, IEndPoint toEndPoint, bool needCheckId )
    {
      if ( needCheckId ) {
        if ( false == fromEndPoint.HasValidElement( true ) ) return false ;
        if ( false == toEndPoint.HasValidElement( false ) ) return false ;
      }

      if ( fromEndPoint.IsOneSided && _subRouteMap.ContainsKey( ( fromEndPoint.Key, false ) ) ) {
        // contradiction!
        return false ;
      }

      if ( toEndPoint.IsOneSided && _subRouteMap.ContainsKey( ( toEndPoint.Key, true ) ) ) {
        // contradiction!
        return false ;
      }

      if ( false == _subRouteMap.TryGetValue( ( fromEndPoint.Key, true ), out var subRoute1 ) ) subRoute1 = null ;
      if ( false == _subRouteMap.TryGetValue( ( toEndPoint.Key, false ), out var subRoute2 ) ) subRoute2 = null ;

      if ( null != subRoute1 ) {
        if ( null != subRoute2 ) {
          if ( subRoute1 != subRoute2 ) {
            // merge.
            foreach ( var endPoint in subRoute2.FromEndPoints ) {
              _subRouteMap[ ( endPoint.Key, true ) ] = subRoute1 ;
            }

            foreach ( var endPoint in subRoute2.ToEndPoints ) {
              _subRouteMap[ ( endPoint.Key, false ) ] = subRoute1 ;
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
          _subRouteMap.Add( ( toEndPoint.Key, false ), subRoute1 ) ;
        }
      }
      else if ( null != subRoute2 ) {
        // fromId is newly added
        subRoute2.AddSegment( segment ) ;
        _subRouteMap.Add( ( fromEndPoint.Key, true ), subRoute2 ) ;
      }
      else {
        // new sub route.
        var subRoute = new SubRoute( this, _subRoutes.Count ) ;
        subRoute.AddSegment( segment ) ;
        _subRoutes.Add( subRoute ) ;
        _subRouteMap.Add( ( fromEndPoint.Key, true ), subRoute ) ;
        _subRouteMap.Add( ( toEndPoint.Key, false ), subRoute ) ;
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
    /// <returns></returns>
    public IEnumerable<Connector> GetAllConnectors()
    {
      var endPoints = SubRoutes.SelectMany( subRoute => subRoute.FromEndPoints.Concat( subRoute.ToEndPoints ) ).OfType<ConnectorEndPoint>() ;
      return endPoints.Select( endPoint => endPoint.GetConnector() ).NonNull() ;
    }

    public IEnumerable<PassPointEndPoint> GetAllPassPointEndPoints()
    {
      return Enumerable.Empty<PassPointEndPoint>() ;
    }

    private void RenameAllDescendents( string oldName, string newRouteName )
    {
      if ( oldName != "" && oldName !="None" ) {
        // replacing indicator is on developing
        /*var childBranches = GetChildBranches() ;
        foreach ( var route in childBranches ) {
          var endPoint = route._subRoutes
          .SelectMany( subRoute => subRoute.AllEndPointIndicators ).LastOrDefault( i => i.ParentBranch( Document ).Route?.RouteName == oldName ) ;
        }*/
        var allElements = Document.GetAllElementsOfRouteName<Element>(oldName) ;
        foreach ( var element in allElements ) {
          element.SetProperty( RoutingParameter.RouteName, newRouteName ) ;
        }
        this.Save();
      }
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
        routes.UnionWith( subRoute.AllEndPoints.Select( endPoint => endPoint.ParentBranch().Route ).NonNull() ) ;
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
      return route._subRoutes.SelectMany( subRoute => subRoute.AllEndPoints ).Any( endPoint => endPoint.ParentBranch().Route == this ) ;
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

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool HasParent()
    {
      return _subRoutes.SelectMany( subRoute => subRoute.AllEndPoints ).Any( endPoint => null != endPoint.ParentBranch().Route ) ;
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
      reader.GetArray<RouteSegment>( RouteSegmentsField ).ForEach( segment => RegisterSegment( segment, false ) ) ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetSingle( RouteNameField, RouteName ) ;
      writer.SetArray( RouteSegmentsField, _routeSegments ) ;
    }

    #endregion
  }
}