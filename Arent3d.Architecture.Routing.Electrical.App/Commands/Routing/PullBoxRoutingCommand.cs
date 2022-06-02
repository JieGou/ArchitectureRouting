using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using System ;
using Arent3d.Revit ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.PullBoxRoutingCommand", DefaultString = "Create Pullbox" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PullBoxRoutingCommand : PullBoxRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.ReplaceFromTo" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.PullBox ;

    protected override ConnectorFamilyType? ConnectorType => ConnectorFamilyType.PullBox ;

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => curveType.Category.Name ;
  }
}