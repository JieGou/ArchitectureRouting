using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands ;
using Arent3d.Revit.UI;
using Autodesk.Revit.UI;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  public class PostCommandExecutor : IPostCommandExecutorBase
  {
    private static UIApplication? UiApp => RoutingApp.FromToTreeManager.UiApp ;
    
    public void ChangeRouteNameCommand( Route route, string newName )
    {
      if ( UiApp is not { } uiApp ) return ;

      CommandParameterStorage.Set( new ApplyChangeRouteNameCommandParameter( route, newName ) ) ;
      uiApp.PostCommand<ApplyChangeRouteNameCommand>() ;
    }

    public void ApplySelectedFromToChangesCommand( Route route, IReadOnlyCollection<SubRoute> subRoutes, RouteProperties properties )
    {
      if ( UiApp is not { } uiApp ) return ;

      CommandParameterStorage.Set( new ApplySelectedFromToChangesCommandParameter( route, subRoutes, properties ) ) ;
      uiApp.PostCommand<ApplySelectedFromToChangesCommand>() ;
    }
  }
}