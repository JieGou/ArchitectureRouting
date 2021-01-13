using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.Core ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Route generator class. This calculates route paths from routing targets and transforms revit elements.
  /// </summary>
  public class RouteGenerator : RouteGeneratorBase<AutoRoutingTarget>
  {
    private readonly Document _document ;
    private readonly List<Connector> _badConnectors = new() ;

    public RouteGenerator( IEnumerable<AutoRoutingTarget> targets, Document document )
    {
      _document = document ;
      RoutingTargets = targets.EnumerateAll() ;
      CollisionCheckTree = new DocumentCollisionCheckTree( document ) ;

      Specifications.Set( DiameterProvider.Instance, PipeClearanceProvider.Instance ) ;
    }

    public IReadOnlyCollection<Connector> GetBadConnectors() => _badConnectors ;

    protected override IEnumerable<AutoRoutingTarget> RoutingTargets { get ; }

    protected override ICollisionCheck CollisionCheckTree { get ; }

    protected override void OnGenerationStarted()
    {
      // TODO
    }

    protected override void OnRoutingTargetProcessed( AutoRoutingTarget routingTarget, IAutoRoutingResult result )
    {
      result.DebugExport( GetDebugFileName( _document, routingTarget ) ) ;
      var ductCreator = new MechanicalSystemCreator( _document, routingTarget ) ;

      foreach ( var routeVertex in result.RouteVertices ) {
        if ( routeVertex is not TerminalPoint ) continue ;

        ductCreator.RegisterEndPointConnector( routeVertex ) ;
      }

      foreach ( var routeEdge in result.RouteEdges ) {
        ductCreator.CreateDuct( routeEdge ) ;
      }

      ductCreator.ConnectAllVertices() ;

      RegisterBadConnectors( ductCreator.GetBadConnectors() ) ;
    }

    private void RegisterBadConnectors( IEnumerable<Connector> badConnectors )
    {
      _badConnectors.AddRange( badConnectors ) ;
    }

    private static string GetDebugFileName( Document document, AutoRoutingTarget routingTarget )
    {
      var dir = Path.Combine( Path.GetDirectoryName( document.PathName )!, Path.GetFileNameWithoutExtension( document.PathName ) ) ;
      return Path.Combine( Directory.CreateDirectory( dir ).FullName, routingTarget.LineId + ".log" ) ;
    }

    protected override void OnGenerationFinished()
    {
      // TODO
    }
  }
}