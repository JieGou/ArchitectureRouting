using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.PickAndReRouteCommand", DefaultString = "Reroute\nSelected" )]
  [Image( "resources/MEP.ico" )]
  public class PickAndReRouteCommand : PickAndReRouteCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickAndReRoute" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, ReRouteState reRouteState )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
      var reRouteNames = executeResultValue.Select( r => r.RouteName ).Distinct().ToHashSet() ;
      var (_, oldConduitIds) = reRouteState ;
      var wireTypeName = ChangeWireTypeCommand.RemoveDetailLines( document, oldConduitIds ) ;
      if ( string.IsNullOrEmpty( wireTypeName ) ) return ;
      ChangeWireTypeCommand.ChangeWireType( document, reRouteNames, wireTypeName ) ;
    }
  }
}