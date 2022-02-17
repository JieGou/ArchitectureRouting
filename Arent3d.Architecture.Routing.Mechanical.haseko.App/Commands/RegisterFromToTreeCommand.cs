using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Mechanical.haseko.App.Forms ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  /// <summary>
  /// Register FromToTree
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  public class RegisterFromToTreeCommand : RegisterFromToTreeCommandBase
  {
    protected internal RegisterFromToTreeCommand( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor ) : base( application, dpId, postCommandExecutor )
    {
      CreateFromToTreeUiManager( application, dpId, postCommandExecutor ) ;
    }

    // view activated event

    public override Result Initialize( UIApplication uiApplication )
    {
      var fromToManager = RoutingApp.FromToTreeManager ;
      fromToManager.UiApp = uiApplication ;

      //Initialize FromToTreeView when open directly rvt file
      if ( fromToManager.FromToTreeUiManager is { } fromToTreeUiManager && fromToManager.UiApp.ActiveUIDocument != null ) {
        fromToTreeUiManager.FromToTreeView.CustomInitiator( uiApplication, AddInType.Mechanical ) ;
        fromToTreeUiManager.Dockable = uiApplication.GetDockablePane( fromToTreeUiManager.DpId ) ;
        fromToTreeUiManager.Dockable.Hide();
      }

      return Result.Succeeded ;
    }

    protected override void CreateFromToTreeUiManager( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor )
    {
      var fromToTreeUiManager = new FromToTreeUiManager( application, dpId, "Mechanical From-To View", postCommandExecutor, new FromToItemsUi() ) ;

      RoutingApp.FromToTreeManager.FromToTreeUiManager = fromToTreeUiManager ;
    }
  }
}