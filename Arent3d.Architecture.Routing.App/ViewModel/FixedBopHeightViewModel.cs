using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Revit.UI ;
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
      var dialog = new FixedBopHeightSetting( UiDoc, selectedRoute ) ;
      OpenedDialog = dialog ;
      dialog.ShowDialog() ;
      
    }

    public static bool ApplyFixedBopHeightChange( double selectedHeight )
    {
      OpenedDialog?.Close();
      TargetHeight = selectedHeight ;
      if ( UiDoc != null ) {
        UiDoc.Application.PostCommand<Commands.PostCommands.ApplyFixedBopHeightChangeCommand>();
        return true ;
      }
      else {
        return false ;
      }
    }
  }
}