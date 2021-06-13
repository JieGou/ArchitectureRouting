using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.RackGuidCommand", DefaultString = "Rack Guid\nPS" )]
  [Image( "resources/PickFrom-To.png" )]
  public class RackGuidCommand : RackGuidCommandBase
  {
  }
}