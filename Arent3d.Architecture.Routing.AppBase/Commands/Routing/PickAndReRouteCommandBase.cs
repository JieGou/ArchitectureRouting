using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PickAndReRouteCommandBase : RoutingCommandBase<PickAndReRouteCommandBase.ReRouteState>
  {
    public record ReRouteState( IReadOnlyCollection<Route> Routes, HashSet<string> ConduitIdsOfRoute, Dictionary<string, string> RouteNameDictionary ) ;
    
    protected abstract AddInType GetAddInType() ;

    protected override OperationResult<ReRouteState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      return new OperationResult<ReRouteState>( SelectRoutes( commandData.Application.ActiveUIDocument ) ) ;
    }

    private ReRouteState SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) {
        var reRouteNames = list.Select( r => r.Name ).ToHashSet() ;
        var conduitIds = uiDocument.Document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => reRouteNames.Contains( c.GetRouteName() ! ) ).Select( e => e.UniqueId ).ToHashSet() ;
        return new ReRouteState( list, conduitIds, new Dictionary<string, string>() ) ;
      }

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      var conduitIdsOfRoute = uiDocument.Document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == pickInfo.Route.Name ).Select( e => e.UniqueId ).ToHashSet() ;
      return new ReRouteState( new[] { pickInfo.Route }, conduitIdsOfRoute, new Dictionary<string, string>() ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, ReRouteState reRouteState )
    {
      RouteGenerator.CorrectEnvelopes( document ) ;
      var (routes, _, routeNameDictionary) = reRouteState ;
      var allRoutes = Route.GetAllRelatedBranches( routes ) ;
      if ( allRoutes.Count == 1 ) RouteGenerator.GetAllRelatedBranches( document, allRoutes, routeNameDictionary ) ;
      return allRoutes.ToSegmentsWithName().EnumerateAll() ;
    }
  }
}