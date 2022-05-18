using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.PressureGuidingTubeCommandCommand", DefaultString = "Pressure Guiding\nTube" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large  )]
  public class PressureGuidingTubeCommand: PressureGuidingTubeCommandBase 
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.CreatePressureGuidingTube" ;
     
    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;
 
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  
    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => curveType.Category.Name ;
     
  }
}