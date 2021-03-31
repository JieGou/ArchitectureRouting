using System.Collections.Generic ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "App.Commands.Routing.ShowFrom_ToWindowCommand", DefaultString = "From-To\nWindow" )]
    [Image( "resources/From-ToWindow.png")]
    public class ShowFrom_ToWindowCommand : Routing.RoutingCommandBase
    {
        protected override string GetTransactionNameKey() => "TransactionName.Commands.ShowFrom_ToWindowCommand" ;

        protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsBeforeTransaction( UIDocument uiDocument )
        {
            return ShowFromToWindow( uiDocument ) ;
        }
        private IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? ShowFromToWindow( UIDocument uiDocument )
        {
            var allRoutes = uiDocument.Document.CollectRoutes() ;
            
            FromToWindowViewModel.ShowFromToWindow(uiDocument, allRoutes);

            return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
        }
    }
}