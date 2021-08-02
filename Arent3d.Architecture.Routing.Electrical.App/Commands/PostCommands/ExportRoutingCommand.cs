using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [Transaction( TransactionMode.Manual )]
  public class ExportRoutingCommand : ExportRoutingCommandBase
  {
    private const string Guid = "99C3467D-1027-42FD-85EA-7483B9CFAE78" ;

    protected override AddInType GetAddInType() => AddInType.Electrical ;
  }
}