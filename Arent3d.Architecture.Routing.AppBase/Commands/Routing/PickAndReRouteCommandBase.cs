using System ;
using System.Collections.Generic ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PickAndReRouteCommandBase : RoutingCommandBase
  {
    protected abstract AddInType GetAddInType() ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      return ( true, SelectRoutes( uiDocument ) ) ;
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      return new[] { pickInfo.Route } ;
    }

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var routes = state as IReadOnlyCollection<Route> ?? throw new InvalidOperationException() ;

      return Route.CollectAllDescendantBranches( routes ).ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
    }
  }
}