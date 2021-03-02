using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  /// <summary>
  /// Determines whether which side of a pass point a pass point end is on.
  /// </summary>
  public enum PassPointEndSide
  {
    /// <summary>Pass point end in on the forward side of a pass point.</summary>
    Forward,
    /// <summary>Pass point end in on the reverse side of a pass point.</summary>
    Reverse,
  }

  public class PassPointEndPoint : EndPointBase
  {
    /// <summary>
    /// Returns the indicator for this end point.
    /// </summary>
    public override IEndPointIndicator EndPointIndicator => new PassPointEndIndicator( Element.Id.IntegerValue ) ;

    public FamilyInstance Element { get ; }

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public override Vector3d Position => Element.GetTotalTransform().Origin.To3d() ;

    /// <summary>
    /// Returns the flow vector.
    /// </summary>
    public override Vector3d GetDirection( bool isFrom ) => Element.GetTotalTransform().BasisX.To3d() ; // Not negated between from-end and to-end.

    /// <summary>
    /// Returns the end point's diameter.
    /// </summary>
    /// <returns>-1: Has no original diameter.</returns>
    public override double? GetDiameter() => null ;

    public PassPointEndPoint( Route ownerRoute, FamilyInstance familyInstance, Connector referenceConnector )
      : base( ownerRoute, referenceConnector )
    {
      Element = familyInstance ;
    }
  }
}