using System ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  //testing image size
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      if ( document.RoutingSettingsAreInitialized() ) return Result.Succeeded ;

      try {
        var result = document.Transaction( "TransactionName.Commands.Initialization.Initialize".GetAppStringByKeyOrDefault( "Setup Routing" ), _ =>
        {
          return document.SetupRoutingFamiliesAndParameters() ? Result.Succeeded : Result.Failed ;
        } ) ;

        if ( Result.Failed == result ) {
          TaskDialog.Show( "Dialog.Commands.Initialization.Dialog.Title.Error".GetAppStringByKeyOrDefault( null ), "Dialog.Commands.Initialization.Dialog.Body.Error.FailedToSetup".GetAppStringByKeyOrDefault( null ) ) ;
        }

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }
}