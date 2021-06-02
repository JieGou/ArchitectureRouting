using System.Windows ;
using Arent3d.Architecture.Routing.App.Forms ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Manager
{
  public class FromToTreeUiManager
  {
    public FromToTree FromToTreeView { get ; }
    public UIControlledApplication UiControlledApplication { get ; }
    public DockablePaneId DpId { get ; }
    

    public FromToTreeUiManager(UIControlledApplication uiControlledApplication)
    {
      FromToTreeView = new FromToTree() ;
      UiControlledApplication = uiControlledApplication ;
      DpId = new DockablePaneId( PaneIdentifiers.GetFromToTreePaneIdentifier() ) ;
      InitializeDockablePane();
    }

    private void InitializeDockablePane()
    {
      DockablePaneProviderData data = new DockablePaneProviderData { FrameworkElement = FromToTreeView as FrameworkElement, InitialState = new DockablePaneState { DockPosition = DockPosition.Tabbed, TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser } } ;
      // register dockable pane
      UiControlledApplication.RegisterDockablePane( DpId, "From-To Tree", FromToTreeView as IDockablePaneProvider) ;
    }
  }
}