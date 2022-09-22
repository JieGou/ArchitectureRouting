using System ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  /// <summary>
  /// Registers UI components of auto routing application.
  /// </summary>
  public partial class RoutingAppUI : AppUIBase
  {
    public static RoutingAppUI Create( UIControlledApplication application )
    {
      return new RoutingAppUI( application ) ;
    }


    private readonly RegisterFromToTreeCommand _registerFromToTreeCommand;

    private readonly Guid _dpid = new Guid( "1EDCF677-4FF3-438F-AD0E-3658EB9A64AE" ) ;

    public static CeedModelView? CeedModelDockPanelProvider ;
    public static Guid PaneId => new Guid( "FAF92697-2CE7-46E0-B7D2-53037BD55507" ) ;
    public static string PaneName => "Ceed Models";

    private RoutingAppUI( UIControlledApplication application ) : base( application, DefaultCommandAssemblyResolver.Instance )
    {
      _registerFromToTreeCommand = new RegisterFromToTreeCommand( application, _dpid, new PostCommandExecutor() ) ;
      CeedModelDockPanelProvider = CeedModelDockablePaneRegisters( application ) ;

      application.ControlledApplication.ApplicationInitialized += DockablePaneRegisters ;
      application.ControlledApplication.ApplicationInitialized += MonitorSelectionApplicationEvent.MonitorSelectionApplicationInitialized ;
    }

    protected override string KeyToDisplayText( string key )
    {
      return key.GetAppStringByKeyOrDefault( null ) ;
    }

    protected override bool IsInitialized( Document document ) => document.RoutingSettingsAreInitialized() ;

    private void DockablePaneRegisters( object sender, ApplicationInitializedEventArgs e )
    {
      _registerFromToTreeCommand.Initialize( new UIApplication( sender as Autodesk.Revit.ApplicationServices.Application ) ) ;
      
      // create ceed dockable pane
      var uiApplication = new UIApplication( sender as Autodesk.Revit.ApplicationServices.Application ) ;
      var uiDocument = uiApplication.ActiveUIDocument ;
      CeedModelDockPanelProvider?.CustomInitiator( uiDocument ) ;
      CeedModelDockPanelProvider?.HideDockPane( uiApplication ) ;
    }

    private CeedModelView CeedModelDockablePaneRegisters( UIControlledApplication application )
    {
      var data = new DockablePaneProviderData() ;
      var ceedModelDockPanelProvider = new CeedModelView() ;
      data.FrameworkElement = ceedModelDockPanelProvider ;
      DockablePaneState state = new()
      {
        DockPosition = DockPosition.Tabbed, 
        TabBehind = DockablePanes.BuiltInDockablePanes.ElementView
      } ;
      data.InitialState = state ;

      var dpId = new DockablePaneId( PaneId ) ;
      if ( ! DockablePane.PaneIsRegistered( dpId ) ) {
        application.RegisterDockablePane( dpId, PaneName, ceedModelDockPanelProvider ) ;
      }

      return ceedModelDockPanelProvider ;
    }
  }
}