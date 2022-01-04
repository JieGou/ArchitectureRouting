using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewLimitRackCommand", DefaultString = "Create Limit Rack" )]
  [Image( "resources/rack.png" )]
  public class NewLimitRackCommand : NewLimitRackCommandBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  }
}