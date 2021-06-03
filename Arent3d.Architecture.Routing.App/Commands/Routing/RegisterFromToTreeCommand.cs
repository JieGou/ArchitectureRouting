using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.App.Forms ;
using Arent3d.Architecture.Routing.App.Manager ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Events ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  /// <summary>
  /// Register FromToTree
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  public class RegisterFromToTreeCommand : IExternalCommand
  {
    public RegisterFromToTreeCommand( UIControlledApplication application )
    {
      CreateFromToTreeUiManager( application ) ;
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

    private void CreateFromToTreeUiManager( UIControlledApplication application )
    {
      var fromToTreeUiManager = new FromToTreeUiManager( application ) ;
      FromToTreeManager.Instance.FromToTreeUiManager = fromToTreeUiManager ;
    }
  }
}