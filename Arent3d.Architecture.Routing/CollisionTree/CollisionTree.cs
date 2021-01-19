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
  public class CollisionTree : ICollisionCheck
  {
    private readonly ITree _treeBody ;

    public CollisionTree( ICollisionCheckTargetCollector collector )
    {
      _treeBody = CreateTree( collector ) ;
    }

    private static ITree CreateTree( ICollisionCheckTargetCollector collector )
    {
      return CreateTreeByFactory( CollectTreeElements( collector ) ) ;
    }

    private static IReadOnlyCollection<TreeElement> CollectTreeElements( ICollisionCheckTargetCollector collector )
    {
      var treeElements = new List<TreeElement>() ;

      foreach ( var familyInstance in collector.GetCollisionCheckTargets() ) {
        var geom = familyInstance.get_Geometry( new Options { DetailLevel = ViewDetailLevel.Coarse, ComputeReferences = false, IncludeNonVisibleObjects = false } ) ;
        if ( null == geom ) continue ;

        if ( false == collector.IsTargetGeometryElement( geom ) ) continue ;

        treeElements.Add( new TreeElement( new BoxGeometryBody( geom.GetBoundingBox().To3d() ) ) ) ;
      }

      return treeElements ;
    }

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