using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.SelectionRangeRouteCommand", DefaultString = "Selection Range\nRoute" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class SelectionRangeRouteCommand : SelectionRangeRouteCommandBase
  {
    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.SelectionRangeRoute" ;
    }

    protected override AddInType GetAddInType()
    {
      return AppCommandSettings.AddInType ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType )
    {
      return curveType.Category.Name ;
    }

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }
  }
}