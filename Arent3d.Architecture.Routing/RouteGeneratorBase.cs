using System ;
using System.Collections.Generic ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.Core ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Base class of route generators.
  /// </summary>
  public abstract class RouteGeneratorBase<TAutoRoutingTarget> where TAutoRoutingTarget : class, IAutoRoutingTarget
  {
    /// <summary>
    /// When overridden in a derived class, gets routing targets to generate routes.
    /// </summary>
    protected abstract IEnumerable<TAutoRoutingTarget> RoutingTargets { get ; }

    /// <summary>
    /// When overridden in a derived class, gets an collision check tree.
    /// </summary>
    protected abstract ICollisionCheck CollisionCheckTree { get ; }

    /// <summary>
    /// When overridden in a derived class, this method is called before all route generations. It is good to preprocess for an execution.
    /// </summary>
    protected abstract void OnGenerationStarted() ;

    /// <summary>
    /// When overridden in a derived class, this method is called after each routing result is processed.
    /// </summary>
    /// <param name="routingTarget">Processed routing target, given by <see cref="RoutingTargets"/> property.</param>
    /// <param name="result">Routing result.</param>
    protected abstract void OnRoutingTargetProcessed( TAutoRoutingTarget routingTarget, IAutoRoutingResult result ) ;

    /// <summary>
    /// When overridden in a derived class, this method is called after all route generations. It is good to postprocess for an execution.
    /// </summary>
    protected abstract void OnGenerationFinished() ;

    /// <summary>
    /// Execute generation routes.
    /// </summary>
    public void Execute()
    {
      OnGenerationStarted() ;

      foreach ( var (src, result) in ApiForAutoRouting.Execute( NoneStructureGraph.Instance, RoutingTargets, CollisionCheckTree ) ) {
        if ( null == result || ! ( src is TAutoRoutingTarget srcTarget ) ) continue ;

        OnRoutingTargetProcessed( srcTarget, result ) ;
      }

      OnGenerationFinished() ;
    }

    private class NoneStructureGraph : IStructureGraph
    {
      public static NoneStructureGraph Instance { get ; } = new() ;

      private NoneStructureGraph()
      {
      }

      public IEnumerable<IStructureInfo> Nodes => Array.Empty<IStructureInfo>() ;

      public IEnumerable<(IStructureInfo, IStructureInfo)> Edges => Array.Empty<( IStructureInfo, IStructureInfo )>() ;
    }
  }
}