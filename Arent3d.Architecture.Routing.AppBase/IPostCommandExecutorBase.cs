using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public interface IPostCommandExecutorBase
  {
    void ChangeRouteNameCommand( UIApplication app, Route route, string newName ) ;
    
    void ApplySelectedFromToChangesCommand(UIApplication app) ;
  }
}