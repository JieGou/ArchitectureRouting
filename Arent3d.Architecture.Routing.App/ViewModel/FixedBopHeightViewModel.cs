using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FixedBopHeightViewModel : ViewModelBase
  {
    public static Route? TargetRoute { get ; set ; }
    public static double TargetHeight { get ; set ; }

    public static void ShowFixedBopHeightSettingDialog( UIDocument uiDocument, Route selectedRoute )
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
      OpenedDialog?.Close() ;
      var connectorOwner = TargetRoute?.FirstFromConnector()?.GetConnector()?.Owner ;
      if ( connectorOwner?.Document.GetElementById<Level>( connectorOwner.LevelId ) is { } level ) {
        if ( TargetRoute?.GetSubRoute( 0 )?.GetDiameter() is { } diameter ) {
          TargetHeight = selectedHeight.MillimetersToRevitUnits() + level.Elevation - diameter / 2 ;
        }
      }
    }
  }
}