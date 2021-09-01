using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  public class MechanicalRoutingExecutor : RoutingExecutor
  {
    public MechanicalRoutingExecutor( Document document, View view ) : base( document, view )
    {
    }

    protected override IFittingSizeCalculator GetFittingSizeCalculator() => DefaultFittingSizeCalculator.Instance ;

    protected override RouteGenerator CreateRouteGenerator( IReadOnlyCollection<Route> routes, Document document, ICollisionCheckTargetCollector collector )
    {
      return new RouteGenerator( routes, document, GetFittingSizeCalculator(), collector ) ;
    }
  }
}