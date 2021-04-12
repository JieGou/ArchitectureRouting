using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;
using Autodesk.Revit.UI.Selection ;

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
        var selectedConnectors = doc.CollectRoutes().SelectMany( r => r.GetAllConnectors( doc ) ).ToList() ;
        
        //Get ElementIds in activeview
        ElementOwnerViewFilter elementOwnerViewFilter = new ElementOwnerViewFilter( doc.ActiveView.Id ) ;
        FilteredElementCollector collector = new FilteredElementCollector( doc, doc.ActiveView.Id ) ;
        var elementsInActiveView = collector.ToElementIds() ;
        
        if ( 0 < selectedRoutes.Count ) {
          var selectedRouteName = selectedRoutes.ToList()[ 0 ].RouteName ;
          var targetElements = doc?.GetAllElementsOfRouteName<Element>( selectedRouteName ).Select( elem => elem.Id ).ToList() ;

          if ( targetElements != null ) {
            if ( elementsInActiveView.Any( ids => targetElements.Contains( ids ) ) ) {
              //Select TreeViewItem
              FromToTreeViewModel.GetSelectedElementId( selectedRoutes.ToList()[ 0 ].OwnerElement?.Id ) ;
            }
          }
        }
        else if ( selectedConnectors.Any( c => _uiApp.ActiveUIDocument.Selection.GetElementIds().Contains(c.Owner.Id) )  ) {
          var selectedElementId = _uiApp.ActiveUIDocument.Selection.GetElementIds().FirstOrDefault() ;
          FromToTreeViewModel.GetSelectedElementId( selectedElementId ) ;
        }
        else {
          FromToTreeViewModel.ClearSelection();
        }
      }
    }
    
    

    // document opened event
    private void Application_DocumentOpened( object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e )
    {
      // provide ExternalCommandData object to dockable page
      if ( _dockableWindow != null && _uiApp != null ) {
        _dockableWindow.CustomInitiator( _uiApp ) ;
        FromToTreeViewModel.FromToTreePanel = _dockableWindow ;
      }
    }

    // document opened event
    private void Application_DocumentChanged( object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e )
    {
      var addedElementIds = e.GetAddedElementIds() ;
      var deletedElementIds = e.GetDeletedElementIds() ;
      var modifiedElementIds = e.GetModifiedElementIds() ;
      var transactions = e.GetTransactionNames() ;

      // provide ExternalCommandData object to dockable page
      if ( _dockableWindow != null && _uiApp != null && transactions.Any( GetRoutingTransactions ) ) {
        _dockableWindow.CustomInitiator( _uiApp ) ;
      }
    }

    private static bool GetRoutingTransactions( string t )
    {
      var routingTransactions = new List<string>
      {
        "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ),
        "TransactionName.Commands.Routing.Common.PickRouting".GetAppStringByKeyOrDefault( "Pick\nFrom-To" ),
        "TransactionName.Commands.Routing.Common.EraseSelectedRoutes".GetAppStringByKeyOrDefault( "Delete\nFrom-To" ),
        "TransactionName.Commands.Routing.Common.EraseAllRoutes".GetAppStringByKeyOrDefault( "Delete\nAll From-To" ),
        "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" )
      } ;

      return routingTransactions.Contains( t ) ;
    }


    public Result Execute( UIApplication uiApplication )
    {
      //dockable window
      var dock = new FromToTree() ;
      _dockableWindow = dock ;
      _uiApp = uiApplication ;

      // Use unique guid identifier for this dockable pane
      var dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
      try {
        // register dockable pane
        _uiApp.RegisterDockablePane( dpid, "From-To Tree", _dockableWindow ) ;
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
  }
}