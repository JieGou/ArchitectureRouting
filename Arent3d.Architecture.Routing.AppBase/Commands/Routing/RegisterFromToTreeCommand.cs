using System ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  /// <summary>
  /// Register FromToTree
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  public class RegisterFromToTreeCommand : IExternalCommand
  {
    public RegisterFromToTreeCommand( UIControlledApplication application, Guid dpId )
    {
      CreateFromToTreeUiManager( application, dpId ) ;
    }

    /// <summary>
    /// Executes the specIfied command Data
    /// </summary>
    /// <param name="commandData"></param>
    /// <param name="message"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      return Execute( commandData.Application ) ;
    }

    // view activated event

    public Result Execute( UIApplication uiApplication )
    {
      FromToTreeManager.Instance.UiApp = uiApplication ;

      //Initialize FromToTreeView when open directly rvt file
      if ( FromToTreeManager.Instance.FromToTreeUiManager is { } fromToTreeUiManager && FromToTreeManager.Instance.UiApp.ActiveUIDocument != null ) {
        fromToTreeUiManager.FromToTreeView.CustomInitiator( uiApplication ) ;
        fromToTreeUiManager.Dockable = uiApplication.GetDockablePane( fromToTreeUiManager.DpId ) ;
        fromToTreeUiManager.ShowDockablePane() ;
      }

      return Result.Succeeded ;
    }

    private void CreateFromToTreeUiManager( UIControlledApplication application, Guid dpId )
    {
      var fromToTreeUiManager = new FromToTreeUiManager( application, dpId ) ;

      FromToTreeManager.Instance.FromToTreeUiManager = fromToTreeUiManager ;
    }
  }
}