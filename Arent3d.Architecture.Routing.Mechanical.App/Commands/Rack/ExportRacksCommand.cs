using Arent3d.Architecture.Routing.AppBase.Commands.Rack ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Rack
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Rack.ExportRacksCommand", DefaultString = "Export\nPS" )]
  [Image( "resources/ExportPS.png" )]
  public class ExportRacksCommand : ExportRacksCommandBase
  {
  }
}