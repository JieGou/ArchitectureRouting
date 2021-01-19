using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public interface ICollisionCheckTargetCollector
  {
    IEnumerable<FamilyInstance> GetCollisionCheckTargets() ;

    bool IsTargetGeometryElement( GeometryElement gElm ) ;
  }
}