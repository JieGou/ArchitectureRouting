using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Routing ;
using Arent3d.Routing.Conditions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route definition class.
  /// </summary>
  public class Route
  {
    public const string DefaultFluidPhase = "None" ;
    public const string DefaultInsulationType = "None" ;

    public Document Document { get ; }
    
    /// <summary>
    /// Unique identifier of a route.
    /// </summary>
    public string RouteId { get ; }

    /// <summary>
    /// Reverse dictionary to search which sub route an end point belongs to.
    /// </summary>
    private readonly Dictionary<IEndPointIndicator, (SubRoute, bool)> _subRouteMap = new() ;

    public string FluidPhase => DefaultFluidPhase ;
    public LineType ServiceType => LineType.Utility ;
    public LoopType LoopType => LoopType.Non ;
    public double Temperature => 30 ; // provisional

    private readonly List<SubRoute> _subRoutes = new() ;
    public IReadOnlyCollection<SubRoute> SubRoutes => _subRoutes ;

    private Domain? _domain = null ;

    public Domain Domain => _domain ??= GetReferenceConnector().Domain ;

    public Route( Document document, string routeId )
    {
      Document = document ;
      RouteId = routeId ;
    }

    /// <summary>
    /// Add from-to information.
    /// </summary>
    /// <param name="fromId">From connector.</param>
    /// <param name="toId">To connector.</param>
    /// <param name="passPointIds">Pass point sequence, if needed.</param>
    /// <returns>False, if any connector id or pass point id is not found, or has any contradictions in the from-to list (i.e., one connector is registered as both from and end).</returns>
    public bool RegisterConnectors( ConnectorIndicator fromId, ConnectorIndicator toId, params int[] passPointIds )
    {
      // check id.
      var fromConn = Document.FindConnector( fromId ) ;
      if ( null == fromConn ) return false ;
      var toConn = Document.FindConnector( toId ) ;
      if ( null == toConn ) return false ;

      foreach ( var passPointId in passPointIds ) {
        if ( null == Document.FindPassPointElement( passPointId ) ) return false ;
      }

      // split by segments
      var segments = new List<(IEndPointIndicator, IEndPointIndicator)>() ;
      if ( 0 == passPointIds.Length ) {
        segments.Add( ( fromId, toId ) ) ;
      }
      else {
        int n = passPointIds.Length ;
        segments.Add( ( fromId, new PassPointEndIndicator( passPointIds[ 0 ], PassPointEndSide.Reverse ) ) ) ;
        for ( int i = 1 ; i < n ; ++i ) {
          segments.Add( ( new PassPointEndIndicator( passPointIds[ i - 1 ], PassPointEndSide.Forward ), new PassPointEndIndicator( passPointIds[ i ], PassPointEndSide.Reverse ) ) ) ;
        }
        segments.Add( ( new PassPointEndIndicator( passPointIds[ n - 1 ], PassPointEndSide.Forward ), toId ) ) ;
      }

      foreach ( var (from, to) in segments ) {
        if ( _subRouteMap.TryGetValue( from, out var pair1 ) && true == pair1.Item2 ) {
          // contradiction!
          return false ;
        }
        if ( _subRouteMap.TryGetValue( to, out var pair2 ) && false == pair2.Item2 ) {
          // contradiction!
          return false ;
        }
      }

      foreach ( var (from, to) in segments ) {
        _subRouteMap.TryGetValue( from, out var pair1 ) ;
        _subRouteMap.TryGetValue( to, out var pair2 ) ;
        var subRoute1 = pair1.Item1 ;
        var subRoute2 = pair2.Item1 ;
        if ( null != subRoute1 ) {
          if ( null != subRoute2 ) {
            if ( subRoute1 != subRoute2 ) {
              // merge.
              foreach ( var ind in subRoute2.FromEndPointIndicators ) {
                _subRouteMap[ ind ] = ( subRoute1, false ) ;
              }
              foreach ( var ind in subRoute2.ToEndPointIndicators ) {
                _subRouteMap[ ind ] = ( subRoute1, true ) ;
              }
              subRoute1.Merge( subRoute2 ) ;
            }
            else {
              // already added.
            }
          }
          else {
            // to is newly added
            subRoute1.AddTo( to ) ;
            _subRouteMap.Add( to, ( subRoute1, true ) ) ;
          }
        }
        else if ( null != subRoute2 ) {
          // from is newly added
          subRoute2.AddFrom( from ) ;
          _subRouteMap.Add( from, ( subRoute2, false ) ) ;
        }
        else {
          // new sub route.
          var subRoute = new SubRoute( this ) ;
          subRoute.AddFrom( from ) ;
          subRoute.AddTo( to ) ;
          _subRoutes.Add( subRoute ) ;
          _subRouteMap.Add( from, ( subRoute, false ) ) ;
          _subRouteMap.Add( to, ( subRoute, true ) ) ;
        }
      }

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
  }
}