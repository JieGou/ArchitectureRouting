using System ;
using System.Collections.Generic ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseSelectedRoutesCommandBase : RoutingCommandBase<IReadOnlyCollection<Route>>
  {
    protected abstract AddInType GetAddInType() ;

    protected override OperationResult<IReadOnlyCollection<Route>> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      return new OperationResult<IReadOnlyCollection<Route>>( SelectRoutes( commandData.Application.ActiveUIDocument ) ) ;
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to delete.", GetAddInType() ) ;
      return new[] { pickInfo.Route } ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, IReadOnlyCollection<Route> routes )
    {
      return GetSelectedRouteSegments( document, routes ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSelectedRouteSegments( Document document, IReadOnlyCollection<Route> pickedRoutes )
    {
      var selectedRoutes = Route.CollectAllDescendantBranches( pickedRoutes ) ;

      var recreatedRoutes = Route.GetAllRelatedBranches( selectedRoutes ) ;
      recreatedRoutes.ExceptWith( selectedRoutes ) ;
      RouteGenerator.EraseRoutes( document, selectedRoutes.ConvertAll( route => route.RouteName ), true ) ;

      // Returns affected but not deleted routes to recreate them.
      return recreatedRoutes.ToSegmentsWithName().EnumerateAll() ;
    }
  }
}