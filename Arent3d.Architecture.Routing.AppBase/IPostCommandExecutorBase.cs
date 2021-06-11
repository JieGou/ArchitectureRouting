using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{

  public interface IPostCommandExecutorBase
  {
    void ChangeRouteNameCommand(UIApplication app) ;
    
    void ApplySelectedFromToChangesCommand(UIApplication app) ;
  }
}