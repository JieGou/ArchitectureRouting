using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.AllReRouteCommand", DefaultString = "Reroute\nAll" )]
  [Image( "resources/RerouteAll.png" )]
  public class AllReRouteCommand : AllReRouteCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.RerouteAll" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, Dictionary<string, HashSet<string>> allConduitsByRoute )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
      
      foreach ( var ( routeName, oldConduitIds ) in allConduitsByRoute ) {
        var ( wireTypeName, isLeakRoute ) = ChangeWireTypeCommand.RemoveDetailLines( document, oldConduitIds ) ;
        if ( string.IsNullOrEmpty( wireTypeName ) ) return ;
        var reRoute = new HashSet<string>() { routeName } ;
        ChangeWireTypeCommand.ChangeWireType( document, reRoute, wireTypeName, isLeakRoute ) ;
      }
    }
  }
}