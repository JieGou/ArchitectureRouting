using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AllReRouteCommand", DefaultString = "Reroute\nAll" )]
  [Image( "resources/RerouteAll.png" )]
  public class AllReRouteCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.RerouteAll" ;

    protected override IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      return uiDocument.Document.CollectRoutes().ToSegmentsWithName() ;
    }
  }
}