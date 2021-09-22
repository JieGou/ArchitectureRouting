using System ;
using System.Collections.Generic ;
using System.Diagnostics ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  [DebuggerDisplay( "{Route.Name}@{SubRouteIndex}" )]
  public class SubRoute
  {
    private const double DefaultDiameter = 1.0 ;
    
    public Route Route { get ; }
    public int SubRouteIndex { get ; }

    public SubRoute? PreviousSubRoute => Route.GetSubRoute( SubRouteIndex - 1 ) ;
    public SubRoute? NextSubRoute => Route.GetSubRoute( SubRouteIndex + 1 ) ;

    private readonly List<RouteSegment> _routeSegments = new() ;

    public IEnumerable<IEndPoint> FromEndPoints => _routeSegments.Select( s => s.FromEndPoint ).Distinct() ;
    public IEnumerable<IEndPoint> ToEndPoints => _routeSegments.Select( s => s.ToEndPoint ).Distinct() ;

    public IEnumerable<IEndPoint> AllEndPoints => FromEndPoints.Concat( ToEndPoints ) ;

    internal SubRoute( Route route, int index )
    {
      Route = route ;
      SubRouteIndex = index ;
    }

    internal void AddSegment( RouteSegment segment )
    {
      _routeSegments.Add( segment ) ;
    }

    public IReadOnlyCollection<RouteSegment> Segments => _routeSegments ;

    public bool IsRoutingOnPipeSpace
    {
      get
      {
        if ( Domain.DomainHvac == Route.Domain ) return false ; // HVac is direct-routing only.

        return Segments.First().IsRoutingOnPipeSpace ;
      }
    }

    public void ChangeIsRoutingOnPipeSpace( bool value )
    {
      foreach ( var seg in Segments ) {
        seg.IsRoutingOnPipeSpace = value ;
      }
    }

    public double? FixedBopHeight => Segments.First().FixedBopHeight ;

    public void ChangeFixedBopHeight( double? value )
    {
      foreach ( var seg in Segments ) {
        seg.FixedBopHeight = value ;
      }
    }

    public AvoidType AvoidType => Segments.First().AvoidType ;

    public void ChangeAvoidType( AvoidType avoidType )
    {
      foreach ( var seg in Segments ) {
        seg.AvoidType = avoidType ;
      }
    }

    public ElementId ShaftElementId => Segments.First().ShaftElementId ;

    public void ChangeShaftElement( Element? shaftElement )
    {
      var shaftElementId = shaftElement?.Id ?? ElementId.InvalidElementId ;
      foreach ( var seg in Segments ) {
        seg.ShaftElementId = shaftElementId ;
      }
    }

    public MEPCurveType GetMEPCurveType()
    {
      return _routeSegments.Select( seg => seg.CurveType ).NonNull().FirstOrDefault() ?? Route.GetDefaultCurveType() ;
    }

    public void SetMEPCurveType( MEPCurveType curveType )
    {
      Segments.ForEach( seg => seg.CurveType = curveType ) ;
    }

    public double GetDiameter()
    {
      return _routeSegments.Select( seg => seg.GetRealNominalDiameter() ).NonNull().Append( DefaultDiameter ).First() ;
    }

    public void ChangePreferredNominalDiameter( double nominalDiameter )
    {
      foreach ( var seg in Segments ) {
        seg.ChangePreferredNominalDiameter( nominalDiameter ) ;
      }
    }

    public int GetMultiplicity() => Math.Max( 1, GetSubRouteGroup().Count ) ;
    
    public IReadOnlyCollection<SubRouteInfo> GetSubRouteGroup()
    {
      return _routeSegments.Select( seg => seg.SubRouteGroup ).FirstOrDefault() ?? Array.Empty<SubRouteInfo>() ;
    }

    public void SetSubRouteGroup( IReadOnlyCollection<SubRouteInfo> subRouteGroup )
    {
      foreach ( var seg in Segments ) {
        seg.SetSubRouteGroup( subRouteGroup ) ;
      }
    }

    internal void Merge( SubRoute another )
    {
      _routeSegments.AddRange( another._routeSegments ) ;
    }

    /// <summary>
    /// Returns a representative connector of this sub route if exists.
    /// </summary>
    /// <returns>Connector.</returns>
    internal Connector? GetReferenceConnectorInSubRoute()
    {
      var fromConnectorEndPoints = _routeSegments.Select( s => s.FromEndPoint ).OfType<ConnectorEndPoint>().Distinct() ;
      var toConnectorEndPoints = _routeSegments.Select( s => s.ToEndPoint ).OfType<ConnectorEndPoint>().Distinct() ;
      return fromConnectorEndPoints.Concat( toConnectorEndPoints ).Select( ind => ind.GetConnector() ).NonNull().FirstOrDefault() ;
    }

    /// <summary>
    /// Returns a representative connector whose parameters are used for MEP system creation.
    /// </summary>
    /// <returns>Connector.</returns>
    public Connector GetReferenceConnector()
    {
      return GetReferenceConnectorInSubRoute() ?? Route.GetReferenceConnector() ;
    }
  }
}