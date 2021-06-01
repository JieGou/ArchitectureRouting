using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.App.Manager ;
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
    
    public Result Execute( UIApplication uiApplication )
    {
      _uiApp =  FromToTreeManager.Instance.UiApp = uiApplication ;
      //Initialize FromToTreeView when open directly rvt file
      if ( _uiApp.ActiveUIDocument != null ) {
        _dockableWindow?.CustomInitiator(uiApplication);
        _dp = FromToTreeManager.Instance.Dockable = uiApplication.GetDockablePane( _dpid ) ;
        _dp.Show();
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
      _dockableWindow = FromToTreeManager.Instance.FromToTreeView = FromToTreeViewModel.FromToTreePanel = new FromToTree() ; ;
      DockablePaneProviderData data = new DockablePaneProviderData { FrameworkElement = _dockableWindow as FrameworkElement, InitialState = new DockablePaneState { DockPosition = DockPosition.Tabbed, TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser } } ;
      
      // Use unique guid identifier for this dockable pane
      _dpid = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
      // register dockable pane
      application.RegisterDockablePane( _dpid, "From-To Tree", _dockableWindow as IDockablePaneProvider) ;
      // subscribe DockableFrameVisibilityChanged event
      application.DockableFrameVisibilityChanged += new EventHandler<DockableFrameVisibilityChangedEventArgs>(UIControlledApplication_DockableVisibilityChanged) ;
    }
  }
}