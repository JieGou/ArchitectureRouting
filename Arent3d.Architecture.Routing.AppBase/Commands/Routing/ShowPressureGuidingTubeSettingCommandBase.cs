using System.Collections.Generic ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using View = Autodesk.Revit.DB.View ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class ShowPressureGuidingTubeSettingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIDocument uiDocument = commandData.Application.ActiveUIDocument ;
      Document document = uiDocument.Document ;
    
      var pressureGuidingTubeStorable = document.GetPressureGuidingTubeStorable() ;
      var pressureSettingViewModel = new PressureGuidingTubeSettingViewModel( pressureGuidingTubeStorable.PressureGuidingTubeModelData ) ;
      var dialog = new PressureGuidingTubeSettingDialog( pressureSettingViewModel ) ;
    
      var result = dialog.ShowDialog() ;
      if ( true ==  result) {
        try {
          using Transaction t = new Transaction( document, "Create pressure guiding tubes" ) ;
          t.Start() ;
          pressureGuidingTubeStorable.PressureGuidingTubeModelData = pressureSettingViewModel.PressureGuidingTube ;
          pressureGuidingTubeStorable.Save() ;
          CreatePressureGuidingTube() ;
          t.Commit() ;
        }
        catch {
          MessageBox.Show( "Save construction item failed.", "Error Message" ) ;
          return Result.Failed ;
        }
      }
    
      return Result.Succeeded ;
    }

    private void CreatePressureGuidingTube()
    {
      
    }
    
    // protected override OperationResult<PickRoutingCommandBase.PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    // {
    //   throw new System.NotImplementedException() ;
    // }
    //
    // protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickRoutingCommandBase.PickState state )
    // {
    //   throw new System.NotImplementedException() ;
    // }
  }
}