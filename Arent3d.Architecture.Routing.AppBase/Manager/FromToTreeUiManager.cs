using System ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public class FromToTreeUiManager
  {
    private UIControlledApplication UiControlledApplication { get ; }
    public FromToTree FromToTreeView { get ; }
    public DockablePaneId DpId { get ; }
    public DockablePane? Dockable{ get ; set ; }
    
    public IPostCommandExecutorBase PostCommandExecutor { get ; }
    

    public FromToTreeUiManager(UIControlledApplication uiControlledApplication, Guid dpId, IPostCommandExecutorBase postCommandExecutor)
    {
      FromToTreeView = new FromToTree() ;
      UiControlledApplication = uiControlledApplication ;
      DpId = new DockablePaneId( dpId ) ;
      PostCommandExecutor = postCommandExecutor ;
      InitializeDockablePane();
      // subscribe DockableFrameVisibilityChanged event
      uiControlledApplication.DockableFrameVisibilityChanged += new EventHandler<DockableFrameVisibilityChangedEventArgs>(UIControlledApplication_DockableVisibilityChanged) ;
    }

    public void ShowDockablePane()
    {
      Dockable?.Show();
    }

    private void InitializeDockablePane()
    {
      DockablePaneProviderData data = new DockablePaneProviderData { FrameworkElement = FromToTreeView as FrameworkElement, InitialState = new DockablePaneState { DockPosition = DockPosition.Tabbed, TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser } } ;
      // register dockable pane
      if ( !DockablePane.PaneIsRegistered(DpId)){
        UiControlledApplication.RegisterDockablePane( DpId, "From-To Tree", FromToTreeView as IDockablePaneProvider) ;
      }
    }
    
    /// <summary>
    /// DockableVisibilityChanged event. Change UI Image.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="dockableFrameVisibilityChangedEventArgs"></param>
    private void UIControlledApplication_DockableVisibilityChanged( object sender, DockableFrameVisibilityChangedEventArgs dockableFrameVisibilityChangedEventArgs )
    {
      if ( ! DockablePane.PaneExists( DpId )) return;
        if( Dockable != null ) { 
          RibbonHelper.ToggleShowFromToTreeCommandButton(dockableFrameVisibilityChangedEventArgs.DockableFrameShown );
        }
    }
  }
}