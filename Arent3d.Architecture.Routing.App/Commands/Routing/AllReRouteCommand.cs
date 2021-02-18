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
  [DisplayName( "Re-Route All" )]
  [Image( "resources/MEP.ico" )]
  public class AllReRouteCommand : RoutingCommandBase
  {
    protected override IAsyncEnumerable<RouteRecord>? ReadRouteRecords( UIDocument uiDocument )
    {
      return uiDocument.Document.CollectRoutes().SelectMany( RouteRecordUtils.ToRouteRecords ).EnumerateAll().ToAsyncEnumerable() ;
    }
  }
}