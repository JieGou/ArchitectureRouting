using System ;
using System.Linq ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class PassPointBranchEndPoint : EndPointBase
  {
    /// <summary>
    /// Returns the indicator for this end point.
    /// </summary>
    public override IEndPointIndicator EndPointIndicator => new PassPointBranchEndIndicator( Element.Id.IntegerValue, AngleDegree ) ;

    public FamilyInstance Element { get ; }

    public double AngleDegree { get ; }

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public override Vector3d Position => Element.GetTotalTransform().Origin.To3d() + GetBaseSubBranchRadius() * GetPipeDirection() ;

    /// <summary>
    /// Returns the flow vector.
    /// </summary>
    public override Vector3d GetDirection( bool isFrom )
    {
      return GetPipeDirection().ForEndPointType( isFrom ) ;
    }

    /// <summary>
    /// Returns the end point's diameter.
    /// </summary>
    /// <returns>-1: Has no original diameter.</returns>
    public override double? GetDiameter() => null ;

    /// <summary>
    /// Radius of the connecting sub branch.
    /// </summary>
    /// <returns>Radius.</returns>
    private double GetBaseSubBranchRadius()
    {
      if ( Element.GetRouteName() is not { } routeName ) return 0 ;

      var document = Element.Document ;
      if ( false == CommandTermCaches.RouteCache.Get( document ).TryGetValue( routeName, out var route ) ) return 0 ;

      double radius = 0 ;
      foreach ( var subRoute in route.SubRoutes ) {
        if ( subRoute.AllEndPointIndicators.OfType<PassPointEndIndicator>().Any( i => i.ElementId != Element.Id.IntegerValue ) ) {
          radius = Math.Max( radius, subRoute.GetDiameter( document ) * 0.5 ) ;
        }
      }

      return radius ;
    }

    /// <summary>
    /// Direction of the branch.
    /// </summary>
    /// <returns></returns>
    private Vector3d GetPipeDirection()
    {
      var transform = Element.GetTotalTransform() ;
      double radian = AngleDegree.Deg2Rad(), cos = Math.Cos( radian ), sin = Math.Sin( radian ) ;
      return ( cos * transform.BasisY + sin * transform.BasisZ ).To3d() ;
    }

    public PassPointBranchEndPoint( Route ownerRoute, FamilyInstance familyInstance, double angleDegree, Connector referenceConnector )
      : base( ownerRoute, referenceConnector )
    {
      Element = familyInstance ;
      AngleDegree = angleDegree ;
    }
  }
}