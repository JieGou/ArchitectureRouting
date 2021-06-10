using System ;
using System.Runtime.Remoting.Messaging ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Revit.UI;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Mechanical.App
{
  public class PostCommandExecutor : IPostCommandBase
  {
    public void ApplyPostCommand( UIApplication app, PostCommandType postCommandType )
    {
      switch ( postCommandType ) {
        case PostCommandType.ChangeRouteNameCommand :
          app.PostCommand<Commands.PostCommands.ApplyChangeRouteNameCommand>() ;
          break;
        case PostCommandType.ApplySelectedFromtToChangesCommand :
          app.PostCommand<Commands.PostCommands.ApplySelectedFromToChangesCommand>() ;
          break;
      }
    }
  }
}