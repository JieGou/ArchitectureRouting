using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{ 
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.LeakRoutingCommand", DefaultString = "Leak Routing" )]
  [Image( "resources/PickFrom-To.png" )]
  public class LeakRoutingCommand : LeakRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "Electrical.App.Commands.Routing.LeakRoutingCommand" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    

    protected override LeakRoutingCommandBase.DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new LeakRoutingCommandBase.DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }
    
    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }
    
    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => curveType.Category.Name ;
  }
}