using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FixedBopHeightViewModel : ViewModelBase
  {
    public static Route? TargetRoute { get ; set ; }
    public static double TargetHeight { get ; set ; }
    public static void ShowFixedBopHeightSettingDialog(UIDocument uiDocument, Route selectedRoute)
    {
      UiDoc = uiDocument ;
      if ( OpenedDialog != null ) {
        OpenedDialog.Close() ;
      }

      TargetRoute = selectedRoute ;
      var dialog = new FixedBopHeightSetting( UiDoc, TargetRoute ) ;
      OpenedDialog = dialog ;
      dialog.ShowDialog() ;
    }

    public static void ApplyFixedBopHeightChange( double selectedHeight )
    {
      OpenedDialog?.Close();
      var connector = TargetRoute?.FirstFromConnector()?.GetConnector()?.Owner ;
      var level = connector?.Document.GetElement(connector.LevelId) as Level;
      var floorHeight = level?.Elevation ;
      if ( floorHeight != null && TargetRoute?.GetSubRoute(0)?.GetDiameter() is {} diameter) {
        TargetHeight = UnitUtils.ConvertToInternalUnits(selectedHeight, UnitTypeId.Millimeters  )  + (double)floorHeight - diameter/2;
        var test = UnitUtils.ConvertFromInternalUnits( TargetHeight, UnitTypeId.Millimeters ) ;
      }
    }
  }
}