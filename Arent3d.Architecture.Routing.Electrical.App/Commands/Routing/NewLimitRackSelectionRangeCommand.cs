using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewLimitRackSelectionRangeCommand", DefaultString = "Create Limit Rack (Selection Range)" )]
  [Image( "resources/rack.png" )]
  public class NewLimitRackSelectionRangeCommand : NewLimitRackCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
    protected override bool IsSelectionRange => true ;
  }
}