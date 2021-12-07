using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AutoRoutingVavCommand", DefaultString = "Auto \nRouting VAVs" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AutoRoutingVavCommand : AutoRoutingVavCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.AutoRoutingVav" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      if ( RouteMEPSystem.GetSystemType( document, connector ) is not { } defaultSystemType ) return null ;

      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, defaultSystemType ) ;

      return new DialogInitValues( classificationInfo, defaultSystemType, curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => systemType?.Name ?? curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      return null == systemType ? null : MEPSystemClassificationInfo.From( systemType! ) ;
    }
  }
}