using System ;
using MathLib ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class RouteEndPoint : EndPointBase
  {
    public RouteEndPoint( RouteIndicator indicator, SubRoute subRoute ) : base( subRoute.Route, subRoute.GetReferenceConnector() )
    {
      EndPointIndicator = indicator ;
    }

    public override IEndPointIndicator EndPointIndicator { get ; }
    public override Vector3d Position => throw new InvalidOperationException() ;
    public override Vector3d GetDirection( bool isFrom ) => throw new InvalidOperationException() ;

    /// <summary>
    /// Returns the end point's diameter.
    /// </summary>
    /// <returns>-1: Has no original diameter.</returns>
    public override double? GetDiameter() => null ;
  }
}