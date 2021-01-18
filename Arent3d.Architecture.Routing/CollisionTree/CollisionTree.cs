using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.CollisionLib ;
using Arent3d.GeometryLib ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.CollisionTree
{
  public abstract class CollisionTree : ICollisionCheck
  {
    private readonly ITree _treeBody ;

    protected CollisionTree()
    {
      _treeBody = CreateTree() ;
    }

    private ITree CreateTree()
    {
      return CreateTreeByFactory( CollectTreeElements() ) ;
    }

    protected abstract IReadOnlyCollection<TreeElement> CollectTreeElements() ;

    private static ITree CreateTreeByFactory( IReadOnlyCollection<TreeElement> treeElements )
    {
      if ( 0 == treeElements.Count ) {
       return TreeFactory.GetTreeInstanceToBuild( TreeFactory.TreeType.Dummy, null! ) ;  // Dummyの場合はtreeElementsを使用しない
      }
      else{
        var tree = TreeFactory.GetTreeInstanceToBuild( TreeFactory.TreeType.Bvh, treeElements ) ;
        tree.Build() ;
        return tree ;
      }
    }



    public IEnumerable<Box3d> GetCollidedBoxes( Box3d box )
    {
      return this._treeBody.BoxIntersects( GetGeometryBodyBox( box ) ).Select( element => element.GlobalBox3d ) ;
    }

    public IEnumerable<(Box3d, IRouteCondition?, bool)> GetCollidedBoxesInDetailToRack( Box3d box )
    {
      // Aggregated Tree から呼ぶこと、これを単独で呼ばないこと
      var tuples = this._treeBody.GetIntersectsInDetailToRack( GetGeometryBodyBox( box ) ) ;
      foreach ( var tuple in tuples ) {
        yield return ( tuple.body.GetBounds(), tuple.cond, true ) ;
      }
    }

    public IEnumerable<(Box3d, IRouteCondition?, bool)> GetCollidedBoxesAndConditions( Box3d box, bool bIgnoreStructure )
    {
      var tuples = this._treeBody.GetIntersectAndRoutingCondition( GetGeometryBodyBox( box ) ) ;
      foreach ( var tuple in tuples ) {
        if ( null != tuple.cond ) {
          yield return ( tuple.body.GetGlobalGeometryBox(), tuple.cond, false ) ;
          continue ;
        }

        foreach ( var geo in tuple.Item1.GetGlobalGeometries() ) {
          yield return ( geo.GetBounds(), null, tuple.isStructure ) ;
        }
      }
    }

    private static IGeometryBody GetGeometryBodyBox( Box3d box ) => new CollisionBox( box ) ;
  }
}