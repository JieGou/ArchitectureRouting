using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public class FromToTreeManager
  {

    public UIApplication? UiApp = null ;
    public FromToTreeUiManager? FromToTreeUiManager = null ;

    public FromToTreeManager()
    {
    }

    // view activated event
    public void OnViewActivated( ViewActivatedEventArgs e, AddInType addInType )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeUiManager?.FromToTreeView == null || UiApp == null ) return ;
      var doc = UiApp.ActiveUIDocument.Document ;

      //Initialize TreeView
      FromToTreeUiManager?.FromToTreeView.ClearSelection();
      FromToTreeUiManager?.FromToTreeView.CustomInitiator( UiApp, addInType ) ;
      SetSelectionInViewToFromToTree( doc, addInType ) ;
    }

    /// <summary>
    /// Set Selection to FromToTree if any route or connector are selected in View
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="selectedRoutes"></param>
    /// <param name="selectedConnectors"></param>
    private void SetSelectionInViewToFromToTree( Document doc, AddInType addInType )
    {
      if ( UiApp == null ) return ;

      //Get ElementIds in activeview
      FilteredElementCollector collector = new FilteredElementCollector( doc, doc.ActiveView.Id ) ;
      var elementsInActiveView = collector.ToElementIds() ;

      // Get Selected Routes
      var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( UiApp.ActiveUIDocument ) ;
      var connectorsInView = doc.CollectRoutes(addInType).SelectMany( r => r.GetAllConnectors() ).Where( c => elementsInActiveView.Contains( c.Owner.Id ) ) ;


      if ( selectedRoutes.FirstOrDefault() is { } selectedRoute ) {
        var selectedRouteName = selectedRoute.RouteName ;
        var targetElements = doc?.GetAllElementsOfRouteName<Element>( selectedRouteName ).Select( elem => elem.Id ).ToList() ;

        if ( targetElements == null ) return ;
        if ( elementsInActiveView.Any( ids => targetElements.Contains( ids ) ) ) {
          //Select TreeViewItem
          FromToTreeViewModel.GetSelectedElementId( selectedRoute.OwnerElement?.Id ) ;
        }
      }
      else if ( connectorsInView.Any( c => UiApp.ActiveUIDocument.Selection.GetElementIds().Contains( c.Owner.Id ) ) ) {
        var selectedElementId = UiApp.ActiveUIDocument.Selection.GetElementIds().FirstOrDefault() ;
        FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
      }
      else {
        FromToTreeViewModel.ClearSelection() ;
      }
    }

    // document opened event
    public void OnDocumentOpened(AddInType addInType)
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeUiManager is { } fromToTreeUiManager && UiApp != null ) {
        fromToTreeUiManager.FromToTreeView.CustomInitiator( UiApp, addInType ) ;
        FromToTreeViewModel.FromToTreePanel = FromToTreeUiManager?.FromToTreeView ;
        if ( fromToTreeUiManager.Dockable == null ) {
          fromToTreeUiManager.Dockable = UiApp.GetDockablePane( fromToTreeUiManager.DpId ) ;
        }
        fromToTreeUiManager.ShowDockablePane() ;
      }
      
    }

    // document opened event
    public void OnDocumentChanged( Autodesk.Revit.DB.Events.DocumentChangedEventArgs e, AddInType addInType )
    {
      if ( FromToTreeUiManager?.FromToTreeView is not {} fromToTreeView || UiApp == null) return;
      var changedElementIds = e.GetAddedElementIds().Concat( e.GetDeletedElementIds() ).Concat( e.GetModifiedElementIds() ) ;

      var transactions = e.GetTransactionNames() ;

      var changedRoute = e.GetDocument().FilterStorableElements<Route>( changedElementIds ) ;

      // provide ExternalCommandData object to dockable page
      if ( ( transactions.Any( GetRoutingTransactions ) || changedRoute.Any() ) ) {
        fromToTreeView.CustomInitiator( UiApp, addInType ) ;
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
        "TransactionName.Commands.Routing.PickAndReRoute".GetAppStringByKeyOrDefault( "Reroute All" ),
        "TransactionName.Commands.Routing.RerouteAll".GetAppStringByKeyOrDefault( "Reroute Selected" ),
      } ;
      return routingTransactions.Contains( t ) ;
    }
  }
}