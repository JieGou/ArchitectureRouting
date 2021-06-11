using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.PickAndReRouteCommand", DefaultString = "Reroute\nSelected" )]
  [Image( "resources/MEP.ico" )]
  public class PickAndReRouteCommand : PickAndReRouteCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickAndReRoute" ;
  }
}