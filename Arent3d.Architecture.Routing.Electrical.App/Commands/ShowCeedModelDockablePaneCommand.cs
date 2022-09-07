using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.ShowCeedModelsCommand", DefaultString = "View\nSet Code" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class ShowCeedModelDockablePaneCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIApplication uiApp = commandData.Application ;

      var dpId = new DockablePaneId( RoutingAppUI.PaneId ) ;
      if ( DockablePane.PaneIsRegistered( dpId ) ) {
        DockablePane dockPane = uiApp.GetDockablePane( dpId ) ;

        if ( dockPane.IsShown() )
          return Result.Succeeded ;

        dockPane.Show() ;

      }
      else {
        return Result.Failed ;
      }

      return Result.Succeeded ;
    }
  }
}