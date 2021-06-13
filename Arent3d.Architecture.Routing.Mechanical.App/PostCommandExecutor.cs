using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Revit.UI;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  public class PostCommandExecutor : IPostCommandExecutorBase
  {
    public void ChangeRouteNameCommand(UIApplication app)
    { 
      app.PostCommand<Commands.PostCommands.ApplyChangeRouteNameCommand>() ;
    }

    public void ApplySelectedFromToChangesCommand(UIApplication app)
    {
      app.PostCommand<Commands.PostCommands.ApplySelectedFromToChangesCommand>() ;
    }
  }
}