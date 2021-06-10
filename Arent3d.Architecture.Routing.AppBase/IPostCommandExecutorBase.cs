using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public enum PostCommandType
  {
    ChangeRouteNameCommand,
    ApplySelectedFromtToChangesCommand,
  }
  
  public interface IPostCommandExecutorBase
  {
    void ApplyPostCommand(UIApplication app,PostCommandType postCommandType) ;
  }
}