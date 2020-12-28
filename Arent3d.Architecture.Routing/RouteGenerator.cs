using System.Collections.Generic ;
using System.IO ;
using Arent3d.Architecture.Routing.Core ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route generator class. This calculates route paths from routing targets and transforms revit elements.
  /// </summary>
  public class RouteGenerator : RouteGeneratorBase<AutoRoutingTarget>
  {
    private readonly Document _document ;

    public RouteGenerator( IEnumerable<AutoRoutingTarget> targets, Document document )
    {
      _document = document ;
      RoutingTargets = targets.EnumerateAll() ;
      CollisionCheckTree = new DocumentCollisionCheckTree( document ) ;

      Specifications.Set( DiameterProvider.Instance, PipeClearanceProvider.Instance ) ;
    }

    protected override IEnumerable<AutoRoutingTarget> RoutingTargets { get ; }

    protected override ICollisionCheck CollisionCheckTree { get ; }

    protected override void OnGenerationStarted()
    {
      // TODO
    }

    protected override void OnRoutingTargetProcessed( AutoRoutingTarget routingTarget, IAutoRoutingResult result )
    {
      // TODO
      var dir = new DirectoryInfo( Path.Combine( Path.GetDirectoryName( _document.PathName )!, Path.GetFileNameWithoutExtension( _document.PathName ) + "_dump" ) ) ;
      if ( ! dir.Exists ) dir.Create() ;

      result.DebugExport( Path.Combine( dir.FullName, routingTarget.Route.RouteId + ".log" ) ) ;
    }

    protected override void OnGenerationFinished()
    {
      // TODO
    }
  }
}