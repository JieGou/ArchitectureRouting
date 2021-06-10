using System.ComponentModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands
{
  [RevitAddin( Guid )]
  [DisplayName( "Apply Selected From-To Changes" )]
  [Transaction( TransactionMode.Manual )]
  public class ApplySelectedFromToChangesCommand : ApplySelectedFromToChangesCommandBase
  {
    private const string Guid = "1ED7E7D1-57F0-45EB-BDB7-29762A3F0963" ;
  }
}