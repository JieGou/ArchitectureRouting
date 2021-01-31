using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "All Re-Routing" )]
  [Image( "resources/MEP.ico" )]
  public class AllReRouteCommand : RoutingCommandBase
  {
    protected override IAsyncEnumerable<RouteRecord>? ReadRouteRecords( UIDocument uiDocument )
    {
      return uiDocument.Document.CollectRoutes().SelectMany( RouteRecordUtils.ToRouteRecords ).ToAsyncEnumerable() ;
    }
  }
}