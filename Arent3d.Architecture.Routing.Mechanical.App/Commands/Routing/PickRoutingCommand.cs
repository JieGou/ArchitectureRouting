using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
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

    protected override IEndPoint CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom )
    {
      return PickCommandUtil.CreateRouteEndPoint( newPickResult ) ;
    }

    protected override SetRouteProperty? CreateSegmentDialogWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo, IEndPoint fromEndPoint, IEndPoint toEndPoint )
    {
      if ( RouteMEPSystem.GetSystemType( document, connector ) is not { } defaultSystemType ) return null ;

      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, defaultSystemType ) ;

      var diameter = fromEndPoint.GetDiameter() ?? toEndPoint.GetDiameter() ?? 0 ;

      return SetDialog( document, classificationInfo, defaultSystemType, curveType, diameter ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => systemType?.Name ?? curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      if ( null == systemType ) return null ;
      return MEPSystemClassificationInfo.From( systemType! ) ;
    }
  }
}