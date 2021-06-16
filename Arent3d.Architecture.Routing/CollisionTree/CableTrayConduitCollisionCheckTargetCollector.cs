using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public class CableTrayConduitCollisionCheckTargetCollector: CollisionCheckTargetCollectorBase
  {
    public CableTrayConduitCollisionCheckTargetCollector( Document document, IReadOnlyCollection<Route> routes ) : base( document )
    {
    }

    protected override bool IsCollisionCheckElement( Element elm )
    {
      if ( elm is not FamilyInstance fi ) return true ;

      // Racks are collision targets.
      return fi.IsRoutingFamilyInstanceExcept( RoutingFamilyType.PassPoint ) ;
    }
  }
}