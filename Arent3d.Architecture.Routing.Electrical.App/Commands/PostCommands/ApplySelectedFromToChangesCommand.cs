using System.ComponentModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Apply Selected Electrical From-To Changes" )]
  [Transaction( TransactionMode.Manual )]
  public class ApplySelectedFromToChangesCommand : ApplySelectedFromToChangesCommandBase
  {
    private const string Guid = "C21F26D7-B7DD-4612-9341-3B69A9C53664" ;
    protected override string GetTransactionNameKey() => "TransactionName.Commands.PostCommands.ApplySelectedFromToChangesCommand" ;
  }
}