using System ;
using System.Collections.Generic ;
using System.Threading.Tasks ;
using Arent3d.Revit ;
using Arent3d.Routing ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Base class of route generators.
  /// </summary>
  public abstract class RouteGeneratorBase<TAutoRoutingTarget> where TAutoRoutingTarget : class, IAutoRoutingTarget
  {
    /// <summary>
    /// When overridden in a derived class, returns routing targets to generate routes.
    /// </summary>
    protected abstract IReadOnlyCollection<TAutoRoutingTarget> RoutingTargets { get ; }

    /// <summary>
    /// When overridden in a derived class, returns an collision check tree.
    /// </summary>
    protected abstract ICollisionCheck CollisionCheckTree { get ; }

    /// <summary>
    /// When overridden in a derived class, returns a structure graph.
    /// </summary>
    protected abstract IStructureGraph StructureGraph { get ; }
    
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
    /// <param name="progressData">Progress data which is notified the status.</param>
    public void Execute( IProgressData? progressData )
    {
      using ( progressData?.Reserve( 0.05 ) ) {
        ThreadDispatcher.Dispatch( OnGenerationStarted ) ;
      }

      using ( var mainProgress = progressData?.Reserve( 0.9 ) ) {
        mainProgress.ForEach( RoutingTargets.Count, ApiForAutoRouting.Execute( StructureGraph, RoutingTargets, CollisionCheckTree ), item =>
        {
          var (src, result) = item ;
          if ( null == result || ! ( src is TAutoRoutingTarget srcTarget ) ) return ;

          ThreadDispatcher.Dispatch( () => OnRoutingTargetProcessed( srcTarget, result ) ) ;
        } ) ;
      }

      using ( progressData?.Reserve( 1 - progressData.Position ) ) {
        ThreadDispatcher.Dispatch( OnGenerationFinished ) ;
      }
    }
  }
}