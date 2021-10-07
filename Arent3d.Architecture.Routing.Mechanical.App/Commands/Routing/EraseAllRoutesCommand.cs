using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.EraseAllRoutesCommand", DefaultString = "Delete\nAll From-To" )]
  [Image( "resources/DeleteAllFrom-To.png" )]
  public class EraseAllRoutesCommand : EraseAllRoutesCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  }
}