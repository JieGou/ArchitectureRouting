using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
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

  public class PassPointEndPoint : EndPoint
  {
    /// <summary>
    /// Returns the indicator for this end point.
    /// </summary>
    public override IEndPointIndicator EndPointIndicator => new PassPointEndIndicator( Element.Id.IntegerValue, SideType ) ;

    public FamilyInstance Element { get ; }

    public PassPointEndSide SideType { get ; }

    /// <summary>
    /// Returns the starting position to be routed.
    /// </summary>
    public override Vector3d Position => Element.GetTotalTransform().Origin.To3d() ;

    /// <summary>
    /// Returns the first pipe direction.
    /// </summary>
    public override Vector3d Direction => Element.GetTotalTransform().BasisX.To3d() ;

    public PassPointEndPoint( Route ownerRoute, FamilyInstance familyInstance, PassPointEndSide sideType, Connector referenceConnector )
      : base( ownerRoute, referenceConnector, ( sideType == PassPointEndSide.Forward ) )
    {
      Element = familyInstance ;
      SideType = sideType ;
    }
  }
}