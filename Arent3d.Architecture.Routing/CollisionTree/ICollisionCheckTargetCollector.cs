using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public interface ICollisionCheckTargetCollector
  {
    IEnumerable<Element> GetCollisionCheckTargets() ;

    bool IsTargetGeometryElement( GeometryElement gElm ) ;
  }
}