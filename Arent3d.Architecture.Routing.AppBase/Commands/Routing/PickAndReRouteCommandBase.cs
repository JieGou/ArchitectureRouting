using System.Collections.Generic ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.PickAndReRouteCommand", DefaultString = "Reroute\nSelected" )]
  [Image( "resources/MEP.ico" )]
  public abstract class PickAndReRouteCommandBase : RoutingCommandBase
  {
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;

      if ( 0 < list.Count ) {
        return Route.CollectAllDescendantBranches( list ).ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
      }
      else {
        // newly select
        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ) ) ;

        return pickInfo.Route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
      }
    }
  }
}