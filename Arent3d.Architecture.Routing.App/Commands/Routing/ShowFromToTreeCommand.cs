using System.Collections.Generic ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  [DisplayNameKey( "App.Commands.Routing.ShowFromTreeCommand", DefaultString = "From-To\nTree" )]
  [Image( "resources/MEP.ico" )]
  public class ShowFromToTreeCommand : Routing.RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.ShowFromTreeCommand" ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsBeforeTransaction( UIDocument uiDocument )
    {
      return ShowFromTree( uiDocument ) ;
    }

    private IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? ShowFromTree( UIDocument uiDocument )
    {
      var allRoutes = uiDocument.Document.CollectRoutes() ;


      var dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
      var dp = uiDocument.Application.GetDockablePane( dpid ) ;
   
      if ( ! dp.IsShown() ) {
        dp.Show() ;
      }


      return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
    }
  }
}