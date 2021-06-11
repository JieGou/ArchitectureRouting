using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  /// <summary>
  /// Register FromToTree
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  public class RegisterFromToTreeCommand : RegisterFromToTreeCommandBase
  {
    protected internal RegisterFromToTreeCommand( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor ) : base(application, dpId, postCommandExecutor)
    {
      CreateFromToTreeUiManager( application, dpId, postCommandExecutor ) ;
    }

    /// <summary>
    /// Executes the specIfied command Data
    /// </summary>
    /// <param name="commandData"></param>
    /// <param name="message"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    public new Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      return Initialize( commandData.Application ) ;
    }

    // view activated event

    public override Result Initialize( UIApplication uiApplication )
    {
      var fromToManager = RoutingApp.FromToTreeManager ;
      fromToManager.UiApp = uiApplication ;

      //Initialize FromToTreeView when open directly rvt file
      if ( fromToManager.FromToTreeUiManager is { } fromToTreeUiManager && fromToManager.UiApp.ActiveUIDocument != null ) {
        fromToTreeUiManager.FromToTreeView.CustomInitiator( uiApplication ) ;
        fromToTreeUiManager.Dockable = uiApplication.GetDockablePane( fromToTreeUiManager.DpId ) ;
        fromToTreeUiManager.ShowDockablePane() ;
      }

      return Result.Succeeded ;
    } 

    protected override void CreateFromToTreeUiManager( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor )
    {
      var fromToTreeUiManager = new FromToTreeUiManager( application, dpId, postCommandExecutor ) ;
      
      RoutingApp.FromToTreeManager.FromToTreeUiManager = fromToTreeUiManager ;
    }
  }
}