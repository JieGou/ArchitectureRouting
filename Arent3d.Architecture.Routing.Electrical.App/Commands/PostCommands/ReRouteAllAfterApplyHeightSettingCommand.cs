using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using System.ComponentModel ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Re-route All After Apply Height Setting" )]
  [Transaction( TransactionMode.Manual )]
  public class ReRouteAllAfterApplyHeightSettingCommand : AllReRouteCommandBase
  {
    private const string Guid = "84DEAE07-6A8B-46D2-BE22-1D6B8650CC70" ;
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.RerouteAll" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;
  }
}