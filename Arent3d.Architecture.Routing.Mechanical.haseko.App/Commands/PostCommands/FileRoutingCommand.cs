using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [Transaction( TransactionMode.Manual )]
  public class FileRoutingCommand : FileRoutingCommandBase
  {
    private const string Guid = "723FA998-5EF9-4D05-A4FD-E93112960BAC" ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;
  }
}