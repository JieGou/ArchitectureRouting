using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class SubRoute
  {
    private const double DefaultDiameter = 1.0 ;
    
    public Route Route { get ; }
    
    public int SubRouteIndex { get ; }

    private readonly List<RouteSegment> _routeSegments = new() ;

    public IEnumerable<IEndPointIndicator> FromEndPointIndicators => _routeSegments.Select( s => s.FromId ).Distinct() ;
    public IEnumerable<IEndPointIndicator> ToEndPointIndicators => _routeSegments.Select( s => s.ToId ).Distinct() ;

    public IEnumerable<IEndPointIndicator> AllEndPointIndicators => FromEndPointIndicators.Concat( ToEndPointIndicators ) ;

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
      var fromConnectorIndicators = _routeSegments.Select( s => s.FromId ).OfType<ConnectorIndicator>().Distinct() ;
      var toConnectorIndicators = _routeSegments.Select( s => s.ToId ).OfType<ConnectorIndicator>().Distinct() ;
      return fromConnectorIndicators.Concat( toConnectorIndicators ).Select( ind => ind.GetConnector( Route.Document ) ).NonNull().FirstOrDefault() ;
    }

    /// <summary>
    /// Returns a representative connector whose parameters are used for MEP system creation.
    /// </summary>
    /// <returns>Connector.</returns>
    public Connector GetReferenceConnector()
    {
      return GetReferenceConnectorInSubRoute() ?? Route.GetReferenceConnector() ;
    }

    public double GetDiameter( Document document )
    {
      return _routeSegments.Select( seg => seg.GetRealNominalDiameter( document ) ).NonNull().Append( DefaultDiameter ).FirstOrDefault() ;
    }
  }
}