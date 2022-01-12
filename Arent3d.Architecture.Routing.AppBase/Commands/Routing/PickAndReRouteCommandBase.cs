using System ;
using System.Collections.Generic ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PickAndReRouteCommandBase : RoutingCommandBase<IReadOnlyCollection<Route>>
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

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      return new[] { pickInfo.Route } ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, IReadOnlyCollection<Route> routes )
    {
      RouteGenerator.CorrectEnvelopes( document ) ;

      return Route.GetAllRelatedBranches( routes ).ToSegmentsWithName().EnumerateAll() ;
    }
  }
}