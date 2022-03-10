using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.RoomSelectionRangeRouteCommand", DefaultString = "Selection Range Route\nThrough Room" )]
  [Image( "resources/PickFrom-To.png" )]
  public class RoomSelectionRangeRouteCommand : RoomSelectionRangeRouteCommandBase
  {
    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.SelectionRangeRouteThroughRoom" ;
    }

    protected override AddInType GetAddInType()
    {
      return AppCommandSettings.AddInType ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    protected override SelectionRangeRouteCommandBase.DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new SelectionRangeRouteCommandBase.DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPCurveType curveType )
    {
      return curveType.Category.Name ;
    }

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType()
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }
  }
}