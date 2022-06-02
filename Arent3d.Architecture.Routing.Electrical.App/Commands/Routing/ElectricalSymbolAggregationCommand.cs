using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.PostCommands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ElectricalSymbolAggregation", DefaultString = "Electrical \nSymbol Aggregation" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class ElectricalSymbolAggregationCommand : ElectricalSymbolAggregationCommandBase
  {
  }
}