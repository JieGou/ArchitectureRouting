using System.Collections.Generic ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  internal class DocumentCollisionCheckTree : ICollisionCheck
  {
    private readonly Document _document ;

    public DocumentCollisionCheckTree( Document document )
    {
      _document = document ;
    }

    public IEnumerable<Box3d> GetCollidedBoxes( Box3d box )
    {
      // TODO:
      yield break ;
    }

    public IEnumerable<(Box3d, IRouteCondition, bool)> GetCollidedBoxesAndConditions( Box3d box, bool bIgnoreStructure = false )
    {
      // TODO:
      yield break ;
    }

    public IEnumerable<(Box3d, IRouteCondition, bool)> GetCollidedBoxesInDetailToRack( Box3d box )
    {
      // TODO:
      yield break ;
    }
  }
}