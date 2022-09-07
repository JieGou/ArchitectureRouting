using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewLimitRackCircleCommand", DefaultString = "Create Limit\nRack (Circle)" )]
  [Image( "resources/rack.png" )]
  public class NewLimitRackCircleCommand : NewLimitRackCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
    protected override bool IsCircle => true ;
    protected override bool IsSelectionRange => false ;
  }
}