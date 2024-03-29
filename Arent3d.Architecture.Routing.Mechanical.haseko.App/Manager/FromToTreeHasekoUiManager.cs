﻿using System ;
using System.Windows ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Mechanical.Haseko.App.Forms ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Manager
{
  public class FromToTreeHasekoUiManager
  {
    private UIControlledApplication UiControlledApplication { get ; }
    public FromToTreeHaseko FromToTreeHasekoView { get ;  }
    public DockablePaneId DpId { get ; }
    public DockablePane? Dockable { get ; set ; }

    public BitmapImage? RouteItemIcon { get ; } = null ;
    private IPostCommandExecutorBase PostCommandExecutor { get ; }

    public FromToTreeHasekoUiManager( UIControlledApplication uiControlledApplication, Guid dpId, string fromToTreeTitle, IPostCommandExecutorBase postCommandExecutor, FromToItemsUiBase fromToItemsUi )
    {
      FromToTreeHasekoView = new FromToTreeHaseko( fromToTreeTitle, postCommandExecutor, fromToItemsUi ) ;
      UiControlledApplication = uiControlledApplication ;
      DpId = new DockablePaneId( dpId ) ;
      PostCommandExecutor = postCommandExecutor ;
      InitializeDockablePane( fromToItemsUi ) ;
      // subscribe DockableFrameVisibilityChanged event
      uiControlledApplication.DockableFrameVisibilityChanged += new EventHandler<DockableFrameVisibilityChangedEventArgs>( UIControlledApplication_DockableVisibilityChanged ) ;
    }

    private void InitializeDockablePane( FromToItemsUiBase fromToItemsUi )
    {
      // register dockable pane
      if ( ! DockablePane.PaneIsRegistered( DpId ) ) {
        UiControlledApplication.RegisterDockablePane( DpId, fromToItemsUi.TabTitle, FromToTreeHasekoView ) ;
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
      }
    }
  }
}