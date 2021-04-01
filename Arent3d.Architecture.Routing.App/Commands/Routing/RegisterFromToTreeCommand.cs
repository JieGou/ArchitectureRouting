using System ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.Forms ;
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
        _dockableWindow.CustomInitiator( _uiApp ) ;
      }
    }

    // document opened event
    private void Application_DocumentOpened( object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e )
    {
      // provide ExternalCommandData object to dockable page
      if ( _dockableWindow != null && _uiApp != null ) {
        _dockableWindow.CustomInitiator( _uiApp ) ;
      }
    }

    public Result Execute( UIApplication uiApplication )
    {
      //dockable window
      FromToTree dock = new FromToTree() ;
      _dockableWindow = dock ;
      _uiApp = uiApplication ;
  

      // Use unique guid identifier for this dockable pane
      var dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
      try {
        // register dockable pane
        _uiApp.RegisterDockablePane( dpid, "From-To Tree", _dockableWindow as IDockablePaneProvider ) ;
        // subscribe document opend event
        _uiApp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>( Application_DocumentOpened ) ;
        // subscribe view activated event
        _uiApp.ViewActivated += new EventHandler<ViewActivatedEventArgs>( Application_ViewActivated ) ;
      }
      catch ( Exception e ) {
        // show error info dialog
        TaskDialog.Show( "From-ToTree registering", e.Message ) ;
      }

      return Result.Succeeded ;
    }
  }
}