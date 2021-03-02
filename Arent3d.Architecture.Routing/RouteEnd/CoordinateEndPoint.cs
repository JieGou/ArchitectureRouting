using MathLib ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class CoordinateEndPoint : EndPointBase
  {
    private readonly Vector3d _direction ;
    
    public CoordinateEndPoint( CoordinateIndicator indicator, SubRoute subRoute ) : base( subRoute.Route, subRoute.GetReferenceConnector() )
    {
      EndPointIndicator = indicator ;
      Position = indicator.Origin.To3d() ;
      _direction = indicator.Direction.To3d() ;
    }

    public override IEndPointIndicator EndPointIndicator { get ; }
    public override Vector3d Position { get ; }

    public override Vector3d GetDirection( bool isFrom )
    {
      return _direction.ForEndPointType( isFrom ) ;
    }

    /// <summary>
    /// Returns the end point's diameter.
    /// </summary>
    /// <returns>-1: Has no original diameter.</returns>
    public override double? GetDiameter() => null ;
  }
}