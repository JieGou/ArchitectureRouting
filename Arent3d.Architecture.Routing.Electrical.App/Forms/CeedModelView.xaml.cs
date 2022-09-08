using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Forms
{
  public partial class CeedModelView : IDockablePaneProvider
  {
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
    
    public void CustomInitiator( UIDocument uiDocument, Document document )
    {
      var viewModel = new CeedViewModel( uiDocument, document, new PostCommandExecutor() ) ;
      CeedModels = new CeedDockPaneContent( viewModel ) ;
      DataContext = viewModel ;
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