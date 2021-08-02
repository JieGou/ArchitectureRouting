using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [Transaction( TransactionMode.Manual )]
  public class ExportRoutingCommand : ExportRoutingCommandBase
  {
    private const string Guid = "F0EA35B4-8A1C-4E54-B12B-3D025CD9651B" ;

    protected override AddInType GetAddInType() => AddInType.Mechanical ;
  }
}