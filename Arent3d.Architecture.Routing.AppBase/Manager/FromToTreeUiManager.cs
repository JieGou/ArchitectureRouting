using System ;
using System.Windows.Media.Imaging ;
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
    public DockablePane? Dockable { get ; set ; }

    public BitmapImage? RouteItemIcon { get ; } = null ;
    private IPostCommandExecutorBase PostCommandExecutor { get ; }


    public FromToTreeUiManager( UIControlledApplication uiControlledApplication, Guid dpId, string fromToTreeTitle, IPostCommandExecutorBase postCommandExecutor, FromToItemsUiBase fromToItemsUi )
    {
      FromToTreeView = new FromToTree( fromToTreeTitle, postCommandExecutor, fromToItemsUi ) ;
      UiControlledApplication = uiControlledApplication ;
      DpId = new DockablePaneId( dpId ) ;
      FromToTreeView.DpId = DpId ;
      PostCommandExecutor = postCommandExecutor ;
      InitializeDockablePane( fromToItemsUi ) ;
      // subscribe DockableFrameVisibilityChanged event
      uiControlledApplication.DockableFrameVisibilityChanged += new EventHandler<DockableFrameVisibilityChangedEventArgs>( UIControlledApplication_DockableVisibilityChanged ) ;
      // subscribe DockableFrameFocusChanged event
      uiControlledApplication.DockableFrameFocusChanged += new EventHandler<DockableFrameFocusChangedEventArgs>(UIControlledApplication_DockableFrameFocusChanged) ;
    }

    private void InitializeDockablePane( FromToItemsUiBase fromToItemsUi )
    {
      // register dockable pane
      if ( ! DockablePane.PaneIsRegistered( DpId ) ) {
        UiControlledApplication.RegisterDockablePane( DpId, fromToItemsUi.TabTitle, FromToTreeView ) ;
      }
    }

    /// <summary>
    /// DockableVisibilityChanged event. Change UI Image.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="dockableFrameVisibilityChangedEventArgs"></param>
    private void UIControlledApplication_DockableVisibilityChanged( object sender, DockableFrameVisibilityChangedEventArgs dockableFrameVisibilityChangedEventArgs )
    {
      if ( ! DockablePane.PaneExists( DpId ) ) return ;
      if ( Dockable != null ) {
        RibbonHelper.ToggleShowFromToTreeCommandButton( dockableFrameVisibilityChangedEventArgs.DockableFrameShown ) ;

        if ( ! dockableFrameVisibilityChangedEventArgs.DockableFrameShown &&
             null != AppBaseManager.Instance.HasekoDockPanelId ) {
          AppBaseManager.Instance.HasekoDockPanelId = null ;
          AppBaseManager.Instance.IsFocusHasekoDockPanel = false ;
        }
      }
    }
    
    /// <summary>
    /// Change DockPanel Id
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="dockableFrameFocusChangedEventArgs"></param>
    private void UIControlledApplication_DockableFrameFocusChanged(object sender, DockableFrameFocusChangedEventArgs dockableFrameFocusChangedEventArgs)
    {
      if ( dockableFrameFocusChangedEventArgs.PaneId == AppBaseManager.Instance.HasekoDockPanelId )
        AppBaseManager.Instance.IsFocusHasekoDockPanel = dockableFrameFocusChangedEventArgs.FocusGained ;
      else
        AppBaseManager.Instance.IsFocusHasekoDockPanel = false ;
    }
  }
}