using System.Collections.Generic ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.CollisionLib ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing
{
  internal class DocumentCollisionCheckTree : CollisionTree.CollisionTree
  {
    private readonly Document _document ;

    public DocumentCollisionCheckTree( Document document )
    {
      _document = document ;
    }

    protected override IReadOnlyCollection<TreeElement> CollectTreeElements()
    {
      var treeElements = new List<TreeElement>() ;

      // TODO
      treeElements.Add( new TreeElement( new BoxGeometryBody( new Box3d( new Vector3d( 9, 50, 30 ), new Vector3d( 10, 65, 50 ) ) ) ) ) ;

      return treeElements ;
    }
  }
}