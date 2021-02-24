using MathLib ;

namespace Arent3d.Architecture.Routing.EndPoint
{
  public class CoordinateEndPoint : EndPointBase
  {
    public CoordinateEndPoint( CoordinateIndicator indicator, SubRoute subRoute, bool isStart ) : base( subRoute.Route, subRoute.GetReferenceConnector(), isStart )
    {
      EndPointIndicator = indicator ;
      Position = indicator.Origin.To3d() ;
      var dir = indicator.Direction.To3d() ;
      Direction = isStart ? dir : -dir ;
    }

    public override IEndPointIndicator EndPointIndicator { get ; }
    public override Vector3d Position { get ; }
    public override Vector3d Direction { get ; }
  }
}