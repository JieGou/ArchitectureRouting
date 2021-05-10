using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  /// <summary>
  /// Register FromToTree
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  public class RegisterFromToTreeCommand : IExternalCommand
  {
    FromToTree? _dockableWindow = null ;
    UIApplication? _uiApp = null ;
    DockablePaneId _dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
    DockablePane? _dp = null ;

    /// <summary>
    /// Executes the specIfied command Data
    /// </summary>
    /// <param name="commandData"></param>
    /// <param name="message"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      return Execute( commandData.Application ) ;
    }

    // view activated event
    public void Application_ViewActivated( object sender, ViewActivatedEventArgs e )
    {
      // provide ExternalCommandData object to dockable page
      if ( _dockableWindow != null && _uiApp != null ) {
        var doc = _uiApp.ActiveUIDocument.Document ;

        //Initialize TreeView
        _dockableWindow.CustomInitiator( _uiApp ) ;

        // Get Selected Routes
        var selectedRoutes = PointOnRoutePicker.PickedRoutesFromSelections( _uiApp.ActiveUIDocument ).EnumerateAll() ;
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
        else if ( selectedConnectors.Any( c => _uiApp.ActiveUIDocument.Selection.GetElementIds().Contains( c.Owner.Id ) ) ) {
          var selectedElementId = _uiApp.ActiveUIDocument.Selection.GetElementIds().FirstOrDefault() ;
          FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        }
        else {
          FromToTreeViewModel.ClearSelection() ;
        }
      }
    }

    // document opened event
    private void Application_DocumentOpened( object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e )
    {
      // provide ExternalCommandData object to dockable page
      if ( _dockableWindow != null && _uiApp != null && _dp != null) {
        _dockableWindow.CustomInitiator( _uiApp ) ;
        FromToTreeViewModel.FromToTreePanel = _dockableWindow ;
        _dp.Show();
      }
    }

    // document opened event
    private void Application_DocumentChanged( object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e )
    {
      var changedElementIds = e.GetAddedElementIds().Concat( e.GetDeletedElementIds() ).Concat( e.GetModifiedElementIds() ) ;

      var transactions = e.GetTransactionNames() ;

      var changedRoute = e.GetDocument().FilterStorableElements<Route>( changedElementIds ) ;

      // provide ExternalCommandData object to dockable page
      if ( _dockableWindow != null && _uiApp != null && ( transactions.Any( GetRoutingTransactions ) || changedRoute.Any() ) ) {
        _dockableWindow.CustomInitiator( _uiApp ) ;
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


    public Result Execute( UIApplication uiApplication )
    {
      _uiApp = uiApplication ;
      _dockableWindow?.CustomInitiator(uiApplication);
      _dp = uiApplication.GetDockablePane( _dpid ) ;
      _dp.Show();
      
      
      try {
        // subscribe document opend event
        _uiApp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>( Application_DocumentOpened ) ;
        // subscribe view activated event
        _uiApp.ViewActivated += new EventHandler<ViewActivatedEventArgs>( Application_ViewActivated ) ;
        // subscribe document changed event
        _uiApp.Application.DocumentChanged += new EventHandler<DocumentChangedEventArgs>( Application_DocumentChanged ) ;
      }
      catch ( Exception e ) {
        // show error info dialog
        TaskDialog.Show( "From-ToTree registering", e.Message ) ;
      }

      return Result.Succeeded ;
    }
    
    /// <summary>
    /// DockableVisibilityChanged event. Change UI Image.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="dockableFrameVisibilityChangedEventArgs"></param>
    private void UIControlledApplication_DockableVisibilityChanged( object sender, DockableFrameVisibilityChangedEventArgs dockableFrameVisibilityChangedEventArgs )
    {
      if ( ! DockablePane.PaneExists( _dpid )) return;
      if( _dp != null ) { 
        RibbonHelper.ToggleShowFromToTreeCommandButton(dockableFrameVisibilityChangedEventArgs.DockableFrameShown );
      }
    }

    public void InitializeDockablePane( UIControlledApplication application )
    {
      //dockable window
      var dock = new FromToTree() ;
      _dockableWindow = dock ;
      FromToTreeViewModel.FromToTreePanel = _dockableWindow ;
      DockablePaneProviderData data = new DockablePaneProviderData { FrameworkElement = dock as FrameworkElement, InitialState = new DockablePaneState { DockPosition = DockPosition.Tabbed, TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser } } ;
      
      // Use unique guid identifier for this dockable pane
      _dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
      // register dockable pane
      application.RegisterDockablePane( _dpid, "From-To Tree", dock as IDockablePaneProvider) ;
      // subscribe DockableFrameVisibilityChanged event
      application.DockableFrameVisibilityChanged += new EventHandler<DockableFrameVisibilityChangedEventArgs>(UIControlledApplication_DockableVisibilityChanged) ;
    }
  }
}