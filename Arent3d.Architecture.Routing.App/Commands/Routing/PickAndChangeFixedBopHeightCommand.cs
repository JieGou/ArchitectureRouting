using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.PickAndChangeFixedBopHeightCommand", DefaultString = "Change\nFixedBopHeight" )]
  [Image( "resources/MEP.ico" )]
  public class PickAndChangeFixedBopHeightCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      
      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndChangeFixedBopHeight.Pick".GetAppStringByKeyOrDefault( null ) ) ;

      try {

        FixedBopHeightViewModel.ShowFixedBopHeightSettingDialog(uiDocument, pickInfo.Route);

        return Result.Succeeded ;
      }
      catch(Exception e) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
      
    }
  }
}
