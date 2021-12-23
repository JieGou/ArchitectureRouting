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

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => systemType?.Name ?? curveType.Category.Name ;
  }
}