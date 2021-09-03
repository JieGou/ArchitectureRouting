using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.PickRoutingCommand", DefaultString = "Pick\nFrom-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PickRoutingCommand : PickRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting" ;

    protected override AddInType GetAddInType()
    {
      return AddInType.Mechanical ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return new MechanicalRoutingExecutor( document, view ) ;
    }

    protected override (IEndPoint EndPoint, Route? AffectedRoute) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom )
    {
      return ( PickCommandUtil.CreateRouteEndPoint( newPickResult ), null ) ;
    }

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      if ( RouteMEPSystem.GetSystemType( document, connector ) is not { } defaultSystemType ) return null ;

      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, defaultSystemType ) ;

      return new DialogInitValues( classificationInfo, defaultSystemType, curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => systemType?.Name ?? curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      if ( null == systemType ) return null ;
      return MEPSystemClassificationInfo.From( systemType! ) ;
    }
  }
}