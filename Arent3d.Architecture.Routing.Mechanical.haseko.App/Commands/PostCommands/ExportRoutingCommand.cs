using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [Transaction( TransactionMode.Manual )]
  public class ExportRoutingCommand : ExportRoutingCommandBase
  {
    private const string Guid = "EA07AB4B-91F7-41BC-9289-58C98FC6DBB6" ;

    protected override AddInType GetAddInType() => AddInType.Mechanical ;
  }
}