using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  public class ElectricalRoutingExecutor : RoutingExecutor
  {
    public ElectricalRoutingExecutor( Document document, View view, IFittingSizeCalculator fittingSizeCalculator ) : base( document, view, fittingSizeCalculator )
    {
    }

    protected override IEnumerable<FamilyInstance> GetRackFamilyInstances()
    {
      return Document.GetAllFamilyInstances( RoutingFamilyType.RackSpace ) ;
    }

    protected override RouteGenerator CreateRouteGenerator( IReadOnlyCollection<Route> routes, Document document, ICollisionCheckTargetCollector collector )
    {
      return new ElectricalRouteGenerator( document, routes, new ElectricalAutoRoutingTargetGenerator( document ), FittingSizeCalculator, collector ) ;
    }

    protected override ICollisionCheckTargetCollector CreateCollisionCheckTargetCollector( Domain domain, IReadOnlyCollection<Route> routesInType )
    {
      return domain switch
      {
        Domain.DomainElectrical => new CableTrayConduitCollisionCheckTargetCollector( Document, routesInType ),
        Domain.DomainCableTrayConduit => new CableTrayConduitCollisionCheckTargetCollector( Document, routesInType ), //for testing
        _ => throw new InvalidOperationException(),
      } ;
    }

    public override IFailuresPreprocessor CreateFailuresPreprocessor() => new RoutingFailuresPreprocessor( this ) ;
  }
}