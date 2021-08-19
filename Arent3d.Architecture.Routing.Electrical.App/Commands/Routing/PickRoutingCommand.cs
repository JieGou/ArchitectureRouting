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

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.PickRoutingCommand", DefaultString = "Pick\nFrom-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PickRoutingCommand : PickRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting" ;

    protected override AddInType GetAddInType()
    {
      return AddInType.Electrical ;
    }

    protected override RoutingExecutor.CreateRouteGenerator GetRouteGeneratorInstantiator()
    {
      return RoutingApp.GetRouteGeneratorInstantiator() ;
    }

    protected override SetRouteProperty? CreateSegmentDialogWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo, IEndPoint fromEndPoint, IEndPoint toEndPoint )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      var diameter = fromEndPoint.GetDiameter() ?? toEndPoint.GetDiameter() ?? 0 ;

      return SetDialog( document, classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, diameter ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }
  }
}