using System ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.EndPoint
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
    public override Vector3d Position => Element.GetTotalTransform().Origin.To3d() ;

    /// <summary>
    /// Returns the first pipe direction.
    /// </summary>
    public override Vector3d Direction
    {
      get
      {
        var transform = Element.GetTotalTransform() ;
        double radian = AngleDegree.Deg2Rad(), cos = Math.Cos( radian ), sin = Math.Sin( radian ) ;
        var dir = ( cos * transform.BasisY + sin * transform.BasisZ ).To3d() ;
        return IsStart ? dir : -dir ;
      }
    }

    public PassPointBranchEndPoint( Route ownerRoute, FamilyInstance familyInstance, double angleDegree, bool isStart, Connector referenceConnector )
      : base( ownerRoute, referenceConnector, isStart )
    {
      Element = familyInstance ;
      AngleDegree = angleDegree ;
    }
  }
}