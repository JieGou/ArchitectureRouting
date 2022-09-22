using System.Collections.Generic ;
using System.Linq ;
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
      var routeNames = list.Where( r => ! string.IsNullOrEmpty( r.Name ) ).Select( r => r.Name ).ToHashSet() ;
      if ( GetAddInType() == AddInType.Electrical ) EraseRackNotations( uiDocument.Document, routeNames ) ;
      if ( routeNames.Any() ) ChangeWireTypeCommand.RemoveDetailLinesByRoutes( uiDocument.Document, routeNames ) ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to delete.", GetAddInType() ) ;
      var pickRouteNames = new HashSet<string>() { pickInfo.Route.Name } ;
      if ( GetAddInType() == AddInType.Electrical ) EraseRackNotations( uiDocument.Document, routeNames ) ;
      if ( pickRouteNames.Any() ) ChangeWireTypeCommand.RemoveDetailLinesByRoutes( uiDocument.Document, pickRouteNames ) ;
      return new[] { pickInfo.Route } ;
    }

    private void EraseRackNotations( Document document, IEnumerable<string>? routeNames )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Delete Rack Notations" ) ;
      try {
        EraseRackCommandBase.RemoveRackNotationsByRouteNames( document, routeNames ) ;
      }
      catch {
        // Ignore
      }

      transaction.Commit() ;
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
      RouteGenerator.EraseRoutes( document, selectedRoutes.ConvertAll( route => route.RouteName ), true, false, true ) ;

      // Returns affected but not deleted routes to recreate them.
      return recreatedRoutes.ToSegmentsWithName().EnumerateAll() ;
    }
  }
}