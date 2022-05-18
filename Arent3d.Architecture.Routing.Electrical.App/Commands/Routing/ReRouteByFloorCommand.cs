using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.AllReRouteByFloorCommand", DefaultString = "Re-Route\nBy Floor" )]
  [Image( "resources/RerouteAll.png" )]
  public class AllReRouteByFloorCommand : AllReRouteByFloorCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.AllReRouteByFloor" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
    }
  }
}