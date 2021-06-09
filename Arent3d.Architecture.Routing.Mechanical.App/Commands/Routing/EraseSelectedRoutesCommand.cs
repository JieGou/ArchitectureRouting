using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.Exceptions ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.EraseSelectedRoutesCommand", DefaultString = "Delete\nFrom-To" )]
  [Image( "resources/DeleteFrom-To.png" )]
  public class EraseSelectedRoutesCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.EraseSelectedRoutes" ;

    protected override IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      return GetSelectedRouteSegments( uiDocument ) ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetSelectedRouteSegments( UIDocument uiDocument )
    {
      // use lazy evaluation because GetRouteSegments()'s call time is not in the transaction.
      var document = uiDocument.Document ;
      var recreatedRoutes = ThreadDispatcher.Dispatch( () =>
      {
        var selectedRoutes = Route.CollectAllDescendantBranches( SelectRoutes( uiDocument ) ) ;

        var allRoutes = Route.GetAllRelatedBranches( selectedRoutes ) ;
        allRoutes.ExceptWith( selectedRoutes ) ;
        RouteGenerator.EraseRoutes( document, selectedRoutes.Select( route => route.RouteName ), true ) ;
        return allRoutes ;
      } ) ;

      // Returns affected, but not deleted routes to recreate them.
      foreach ( var seg in recreatedRoutes.ToSegmentsWithName().EnumerateAll() ) {
        yield return seg ;
      }
    }

    private static IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to delete." ) ;
      return new[] { pickInfo.Route } ;
    }
  }
}