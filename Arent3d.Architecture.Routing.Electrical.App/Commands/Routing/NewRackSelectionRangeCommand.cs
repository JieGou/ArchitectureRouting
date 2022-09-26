using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewRackSelectionRangeCommand", DefaultString = "Create Rack\n(Selection Range)" )]
  [Image( "resources/rack.png" )]
  public class NewRackSelectionRangeCommand : NewRackCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
    protected override bool IsSelectionRange => true ;
  }
}