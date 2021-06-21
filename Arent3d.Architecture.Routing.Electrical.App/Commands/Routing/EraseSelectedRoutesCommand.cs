using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.EraseSelectedRoutesCommand", DefaultString = "Delete\nFrom-To" )]
  [Image( "resources/DeleteFrom-To.png" )]
  public class EraseSelectedRoutesCommand : EraseSelectedRoutesCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.EraseSelectedRoutes" ;

    protected override AddInType GetAddInType()
    {
      return AddInType.Electrical ;
    }
  }
}