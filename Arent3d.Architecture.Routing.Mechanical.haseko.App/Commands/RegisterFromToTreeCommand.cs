﻿using System ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Mechanical.Haseko.App.Forms ;
using Arent3d.Architecture.Routing.Mechanical.Haseko.App.Manager ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Routing
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
      var fromToHasekoManager = RoutingApp.FromToTreeHasekoManager ;
      fromToHasekoManager.UiApp = uiApplication ;

      //Initialize FromToTreeHasekoView when open directly rvt file
      if ( fromToHasekoManager.FromToTreeHasekoUiManager is { } fromToTreeHasekoUiManager && fromToHasekoManager.UiApp.ActiveUIDocument != null ) {
        fromToTreeHasekoUiManager.FromToTreeHasekoView.CustomInitiator( uiApplication, AddInType.Mechanical ) ;
        fromToTreeHasekoUiManager.Dockable = uiApplication.GetDockablePane( fromToTreeHasekoUiManager.DpId ) ;
      }

      return Result.Succeeded ;
    }

    protected override void CreateFromToTreeUiManager( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor )
    {
      var fromToTreeHasekoUiManager = new FromToTreeHasekoUiManager( application, dpId, "Mechanical From-To View", postCommandExecutor, new FromToItemsUi() ) ;

      RoutingApp.FromToTreeHasekoManager.FromToTreeHasekoUiManager = fromToTreeHasekoUiManager ;
    }
  }
}