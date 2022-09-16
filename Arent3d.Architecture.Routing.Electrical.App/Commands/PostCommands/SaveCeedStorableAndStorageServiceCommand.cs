using System ;
using System.ComponentModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Save storable" )]
  [Transaction( TransactionMode.Manual )]
  public class SaveCeedStorableAndStorageServiceCommand : SaveCeedStorableAndStorageServiceCommandBase
  {
    private const string Guid = "d20630e5-c8cb-4491-a605-6041c6c07714" ;
  }
}