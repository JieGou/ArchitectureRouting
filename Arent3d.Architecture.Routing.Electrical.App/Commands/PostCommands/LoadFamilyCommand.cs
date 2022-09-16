using System ;
using System.ComponentModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Load connector family" )]
  [Transaction( TransactionMode.Manual )]
  public class LoadFamilyCommand : LoadFamilyCommandBase
  {
    private const string Guid = "3bbb7b53-6488-4e56-ad8d-c3df6bb540ef" ;
  }
}