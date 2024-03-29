﻿using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public partial class CeedModelView : IDockablePaneProvider
  {
    public Document? Document { get ; set ; }

    public void SetupDockablePane( DockablePaneProviderData data )
    {
      data.FrameworkElement = this ;
      data.InitialState = new DockablePaneState
      {
        DockPosition = DockPosition.Tabbed, 
        TabBehind = DockablePanes.BuiltInDockablePanes.ElementView
      } ;
    }

    public CeedModelView()
    {
      InitializeComponent() ;
    }
    
    public void CustomInitiator( UIDocument uiDocument )
    {
      var viewModel = new CeedViewModel( uiDocument, new CeedPostCommandExecutor() ) ;
      CeedModels = new CeedDockPaneContent( viewModel ) ;
      DataContext = viewModel ;
      Document = uiDocument.Document ;
    }
    
    public void HideDockPane( UIApplication uiApplication )
    {
      var dpId = new DockablePaneId( RoutingAppUI.PaneId ) ;
      DockablePane dockPane = uiApplication.GetDockablePane( dpId ) ;
      if ( dockPane.IsShown() )
        dockPane.Hide() ;
    }
  }
}