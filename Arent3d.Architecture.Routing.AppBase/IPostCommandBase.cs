using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public enum PostCommandType
  {
    ChangeRouteNameCommand,
    ApplySelectedFromtToChangesCommand,
  }
  
  public interface IPostCommandBase
  {
    void ApplyPostCommand(UIApplication app,PostCommandType postCommandType) ;
  }
}