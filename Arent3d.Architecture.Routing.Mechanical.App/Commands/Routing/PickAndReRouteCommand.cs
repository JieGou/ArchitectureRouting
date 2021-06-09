using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.PickAndReRouteCommand", DefaultString = "Reroute\nSelected" )]
  [Image( "resources/MEP.ico" )]
  public class PickAndReRouteCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickAndReRoute" ;

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