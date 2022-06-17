using System ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Routing ;
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

    private RoutingAppUI( UIControlledApplication application ) : base( application, DefaultCommandAssemblyResolver.Instance )
    {
      _registerFromToTreeCommand = new RegisterFromToTreeCommand( application, _dpid, new PostCommandExecutor() ) ;

      application.ControlledApplication.ApplicationInitialized += DockablePaneRegisters;
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
    }
  }
}