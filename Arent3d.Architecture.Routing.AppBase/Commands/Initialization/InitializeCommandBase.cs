using System ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class InitializeCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      if ( RoutingSettingsAreInitialized( document ) ) return Result.Succeeded ;

      try {
        BeforeInitialize( document ) ;
        var result = document.Transaction( "TransactionName.Commands.Initialization.Initialize".GetAppStringByKeyOrDefault( "Setup Routing" ), _ => { return Setup( document ) ? Result.Succeeded : Result.Failed ; } ) ;

        if ( Result.Failed == result ) {
          TaskDialog.Show( "Dialog.Commands.Initialization.Dialog.Title.Error".GetAppStringByKeyOrDefault( null ), "Dialog.Commands.Initialization.Dialog.Body.Error.FailedToSetup".GetAppStringByKeyOrDefault( null ) ) ;
        }

        AfterInitialize( document ) ;

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected virtual void BeforeInitialize( Document document )
    {
    }

    protected virtual void AfterInitialize( Document document )
    {
    }

    protected virtual bool RoutingSettingsAreInitialized( Document document )
    {
      return document.RoutingSettingsAreInitialized() ;
    }

    protected virtual bool Setup( Document document )
    {
      try {
        return document.SetupRoutingFamiliesAndParameters() ;
      }
      catch ( Exception e ) {
        Console.WriteLine( e ) ;
        throw ;
      }
    }
  }
}