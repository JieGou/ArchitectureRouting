using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewRackSelectionRangeCommand", DefaultString = "Create Rack\n(Selection Range)" )]
  [Image( "resources/rack.png" )]
  public class NewRackSelectionRangeCommand : NewRackCommandBase
  {
    protected override RoutingExecutor CreateRoutingExecutor( Document document ) => AppCommandSettings.CreateRoutingExecutor( document, document.ActiveView ) ;
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
    protected override bool IsSelectionRange => true ;
  }
}