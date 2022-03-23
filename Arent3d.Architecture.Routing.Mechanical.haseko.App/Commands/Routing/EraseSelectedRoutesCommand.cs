using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.Haseko.App.Commands.Routing.EraseSelectedRoutesCommand", DefaultString = "Delete\nFrom-To" )]
  [Image( "resources/DeleteFrom-To.png" )]
  public class EraseSelectedRoutesCommand : EraseSelectedRoutesCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.EraseSelectedRoutes" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;
  }
}