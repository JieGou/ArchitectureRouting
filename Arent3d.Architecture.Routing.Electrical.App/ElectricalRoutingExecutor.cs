using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  public class ElectricalRoutingExecutor : RoutingExecutor
  {
    public ElectricalRoutingExecutor( Document document, View view ) : base( document, view )
    {
    }

    protected override IFittingSizeCalculator GetFittingSizeCalculator() => ElectricalFittingSizeCalculator.Instance ;

    protected override RouteGenerator CreateRouteGenerator( IReadOnlyCollection<Route> routes, Document document, ICollisionCheckTargetCollector collector )
    {
      return new ElectricalRouteGenerator( routes, document, GetFittingSizeCalculator(), collector ) ;
    }
  }
}