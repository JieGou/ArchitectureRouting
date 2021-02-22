using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// An <see cref="EndPoint"/> for a concrete connector <see cref="RoutingConnector"/>.
  /// </summary>
  public class ConnectorEndPoint : EndPoint
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
    /// Returns the first pipe direction.
    /// </summary>
    public override Vector3d Direction
    {
      get
      {
        var dir = RoutingConnector.CoordinateSystem.BasisZ.To3d() ;
        return IsStart ? dir : -dir ;
      }
    }

    public ConnectorEndPoint( Route ownerRoute, Connector connector, bool isFromSide )
      : base( ownerRoute, connector, isFromSide )
    {
      RoutingConnector = connector ;
    }
  }
}