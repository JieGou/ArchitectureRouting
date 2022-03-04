using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.ViewModel ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Manager
{
  public class FromToTreeHasekoManager : FromToTreeManager
  {
    public FromToTreeHasekoUiManager? FromToTreeHasekoUiManager { get ; set ; } = null ; 
    
    public FromToTreeHasekoManager()
    {
    }
    
    // view activated event
    public override void OnViewActivated( ViewActivatedEventArgs e, AddInType addInType )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeHasekoUiManager?.FromToTreeHasekoView == null || UiApp == null ) return ;
      var doc = UiApp.ActiveUIDocument.Document ;

      //Initialize TreeView
      FromToTreeHasekoUiManager?.FromToTreeHasekoView.ClearSelection() ;
      FromToTreeHasekoUiManager?.FromToTreeHasekoView.CustomInitiator( UiApp, addInType ) ;
      SetSelectionInViewToFromToTreeHaseko( doc, addInType ) ;
    }
    
    /// <summary>
    /// Set Selection to FromToTreeHáeko if any route or connector are selected in View
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="selectedRoutes"></param>
    /// <param name="selectedConnectors"></param>
    private void SetSelectionInViewToFromToTreeHaseko( Document doc, AddInType addInType )
    {
      if ( UiApp == null ) return ;

      //Get ElementIds in activeview
      FilteredElementCollector collector = new FilteredElementCollector( doc, doc.ActiveView.Id ) ;
      var elementsInActiveView = collector.ToElementIds() ;

      // Get Selected Routes
      var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( UiApp.ActiveUIDocument ) ;
      var connectorsInView = doc.CollectRoutes( addInType ).SelectMany( r => r.GetAllConnectors() ).Where( c => elementsInActiveView.Contains( c.Owner.Id ) ) ;


      if ( selectedRoutes.FirstOrDefault() is { } selectedRoute ) {
        var selectedRouteName = selectedRoute.RouteName ;
        var targetElements = doc?.GetAllElementsOfRouteName<Element>( selectedRouteName ).Select( elem => elem.Id ).ToList() ;

        if ( targetElements == null ) return ;
        if ( elementsInActiveView.Any( ids => targetElements.Contains( ids ) ) ) {
          //Select TreeViewItem
          FromToTreeHasekoViewModel.GetSelectedElementId( selectedRoute.OwnerElement?.UniqueId ) ;
        }
      }
      else if ( connectorsInView.Any( c => UiApp.ActiveUIDocument.Selection.GetElementIds().Contains( c.Owner.Id ) ) ) {
        var selectedElementId = UiApp.ActiveUIDocument.Selection.GetElementIds().First() ;
        FromToTreeHasekoViewModel.GetSelectedElementId( doc.GetElementById<Element>( selectedElementId )?.UniqueId ) ;
      }
      else {
        FromToTreeHasekoViewModel.ClearSelection() ;
      }
    }
    
    // Document opened event
    public override void OnDocumentOpened( AddInType addInType )
    {
      // provide ExternalCommandData object to dockable page
      if ( FromToTreeHasekoUiManager is { } fromToTreeHasekoUiManager && UiApp != null ) {
        fromToTreeHasekoUiManager.FromToTreeHasekoView.CustomInitiator( UiApp, addInType ) ;
        FromToTreeHasekoViewModel.FromToTreeHasekoPanel = FromToTreeHasekoUiManager?.FromToTreeHasekoView ;
        if ( fromToTreeHasekoUiManager.Dockable == null ) {
          fromToTreeHasekoUiManager.Dockable = UiApp.GetDockablePane( fromToTreeHasekoUiManager.DpId ) ;
        }
      }
    }
    
    // Document changed event
    public override void OnDocumentChanged( Autodesk.Revit.DB.Events.DocumentChangedEventArgs e, AddInType addInType )
    {
      if ( FromToTreeHasekoUiManager?.FromToTreeHasekoView is not { } fromToTreeHasekoView || UiApp == null ) return ;
      if ( UpdateSuppressed() ) return ;

      var changedElementIds = e.GetAddedElementIds().Concat( e.GetModifiedElementIds() ) ;
      var changedRoute = e.GetDocument().FilterStorableElements<Route>( changedElementIds ) ;

      // provide ExternalCommandData object to dockable page
      if ( changedRoute.Any() || fromToTreeHasekoView.HasInvalidRoute() ) {
        fromToTreeHasekoView.CustomInitiator( UiApp, addInType ) ;
      }
    }

    // Update tree event
    public override void UpdateTreeView( AddInType addInType )
    {
      if ( FromToTreeHasekoUiManager?.FromToTreeHasekoView is not { } fromToTreeHasekoView || UiApp == null ) return ;
      if ( UpdateSuppressed() ) return ;

      fromToTreeHasekoView.CustomInitiator( UiApp, addInType ) ;
    }
  }
}