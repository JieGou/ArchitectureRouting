using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Re-Route Selected" )]
  [Image( "resources/MEP.ico" )]
  public class PickAndReRouteCommand : RoutingCommandBase
  {
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegments( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;

      if ( 0 < list.Count ) {
        return list.ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;

      }
      else {
        // newly select
        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route." ) ;

        return pickInfo.Route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll().ToAsyncEnumerable() ;
      }
    }
  }
}