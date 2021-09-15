using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.Mechanical.App.Commands.PostCommands ;
using Arent3d.Revit.UI;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  public class PostCommandExecutor : IPostCommandExecutorBase
  {
    public void ChangeRouteNameCommand( UIApplication app, Route route, string newName )
    {
      CommandParameterStorage.Set( new ApplyChangeRouteNameCommandParameter( route, newName ) ) ;
      app.PostCommand<ApplyChangeRouteNameCommand>() ;
    }

    public void ApplySelectedFromToChangesCommand(UIApplication app)
    {
      app.PostCommand<ApplySelectedFromToChangesCommand>() ;
    }
  }
}