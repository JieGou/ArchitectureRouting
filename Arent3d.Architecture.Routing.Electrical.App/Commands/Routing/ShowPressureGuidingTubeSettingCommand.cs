using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.ShowPressureGuidingTubeSettingCommand", DefaultString = "Pressure Guiding\nTube" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large  )]
  public class ShowPressureGuidingTubeSettingCommand: ShowPressureGuidingTubeSettingCommandBase 
  {
    //protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.CreatePressureGuidingTube" ;
    //protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    
  }
}