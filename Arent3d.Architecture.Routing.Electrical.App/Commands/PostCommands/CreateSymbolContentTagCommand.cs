using System.ComponentModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Create Symbol Content Tag" )]
  [Transaction( TransactionMode.Manual )]
  public class CreateSymbolContentTagCommand : CreateSymbolContentTagCommandBase
  {
    private const string Guid = "dccc6a1c-a708-4046-80b0-98db2517ca9d" ;
  }
}