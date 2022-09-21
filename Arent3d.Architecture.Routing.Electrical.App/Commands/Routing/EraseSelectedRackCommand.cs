using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.EraseSelectedLimitRacksCommand", DefaultString = "Delete Selected Rack" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class EraseSelectedRackCommand : EraseSelectedRacksCommandBase
  {
    
  }
}