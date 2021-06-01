using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.App.Manager
{
  public class FromToTreeManager
  {
    public static FromToTreeManager Instance { get ; } = new FromToTreeManager() ;
    
    public FromToTree? FromToTreeView = null ;
    public UIApplication? UiApp = null ;
    public DockablePane? Dockable = null ;

    private FromToTreeManager()
    {
    }
    
    // view activated event
    public void Application_ViewActivated(ViewActivatedEventArgs e )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeView != null && UiApp != null ) {
        var doc = UiApp.ActiveUIDocument.Document ;

        //Initialize TreeView
        FromToTreeView.CustomInitiator( UiApp ) ;

        // Get Selected Routes
        var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( UiApp.ActiveUIDocument ).EnumerateAll() ;
        var selectedConnectors = doc.CollectRoutes().SelectMany( r => r.GetAllConnectors() ).ToList() ;

        //Get ElementIds in activeview
        ElementOwnerViewFilter elementOwnerViewFilter = new ElementOwnerViewFilter( doc.ActiveView.Id ) ;
        FilteredElementCollector collector = new FilteredElementCollector( doc, doc.ActiveView.Id ) ;
        var elementsInActiveView = collector.ToElementIds() ;

        if ( selectedRoutes.FirstOrDefault() is { } selectedRoute ) {
          var selectedRouteName = selectedRoute.RouteName ;
          var targetElements = doc?.GetAllElementsOfRouteName<Element>( selectedRouteName ).Select( elem => elem.Id ).ToList() ;

          if ( targetElements != null ) {
            if ( elementsInActiveView.Any( ids => targetElements.Contains( ids ) ) ) {
              //Select TreeViewItem
              FromToTreeViewModel.GetSelectedElementId( selectedRoute.OwnerElement?.Id ) ;
            }
          }
        }
        else if ( selectedConnectors.Any( c => UiApp.ActiveUIDocument.Selection.GetElementIds().Contains( c.Owner.Id ) ) ) {
          var selectedElementId = UiApp.ActiveUIDocument.Selection.GetElementIds().FirstOrDefault() ;
          FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        }
        else {
          FromToTreeViewModel.ClearSelection() ;
        }
      }
    }
    
    // document opened event
    public void Application_DocumentOpened( )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeView == null || UiApp == null || Dockable == null ) return ;
      FromToTreeView.CustomInitiator( UiApp ) ;
      FromToTreeViewModel.FromToTreePanel = FromToTreeView ;
      Dockable.Show();
    }
    
    // document opened event
    public void Application_DocumentChanged( Autodesk.Revit.DB.Events.DocumentChangedEventArgs e )
    {
      var changedElementIds = e.GetAddedElementIds().Concat( e.GetDeletedElementIds() ).Concat( e.GetModifiedElementIds() ) ;

      var transactions = e.GetTransactionNames() ;

      var changedRoute = e.GetDocument().FilterStorableElements<Route>( changedElementIds ) ;

      // provide ExternalCommandData object to dockable page
      if ( FromToTreeView != null && UiApp != null && ( transactions.Any( GetRoutingTransactions ) || changedRoute.Any() ) ) {
        FromToTreeView.CustomInitiator( UiApp ) ;
      }
    }

    private static bool GetRoutingTransactions( string t )
    {
      var routingTransactions = new List<string>
      {
        "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ),
        "TransactionName.Commands.Routing.PickRouting".GetAppStringByKeyOrDefault( "Add From-To" ),
        "TransactionName.Commands.Routing.EraseSelectedRoutes".GetAppStringByKeyOrDefault( "Erase Selected" ),
        "TransactionName.Commands.Routing.EraseAllRoutes".GetAppStringByKeyOrDefault( "Erase All From-Tos" ),
        "TransactionName.Commands.Routing.PickAndReRoute".GetAppStringByKeyOrDefault("Reroute All"),
        "TransactionName.Commands.Routing.RerouteAll".GetAppStringByKeyOrDefault("Reroute Selected"),
      } ;

      return routingTransactions.Contains( t ) ;
    }

  }
}