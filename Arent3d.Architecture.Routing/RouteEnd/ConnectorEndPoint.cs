using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  /// <summary>
  /// An <see cref="RoutingConnector"/> for a concrete connector <see cref="EndPointBase"/>.
  /// </summary>
  public class ConnectorEndPoint : EndPointBase
  {
    /// <summary>
    /// Returns the indicator for this end point.
    /// </summary>
    public override IEndPointIndicator EndPointIndicator => RoutingConnector.GetIndicator() ;

    /// <summary>
    /// Returns the related <see cref="Connector"/> which is an end point of a route.
    /// </summary>
    public Connector RoutingConnector { get ; }

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public override Vector3d Position => RoutingConnector.Origin.To3d() ;

    /// <summary>
    /// Returns the flow vector.
    /// </summary>
    public override Vector3d GetDirection( bool isFrom )
    {
      return RoutingConnector.CoordinateSystem.BasisZ.To3d().ForEndPointType( isFrom ) ;
    }

    /// <summary>
    /// Returns the end point's diameter.
    /// </summary>
    /// <returns>Diameter.</returns>
    public override double? GetDiameter() => RoutingConnector.GetDiameter() ;

    public ConnectorEndPoint( Route ownerRoute, Connector connector )
      : base( ownerRoute, connector )
    {
      RoutingConnector = connector ;
    }
  }
}