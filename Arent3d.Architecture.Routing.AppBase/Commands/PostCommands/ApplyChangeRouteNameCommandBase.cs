using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public class ApplyChangeRouteNameCommandParameter
  {
    public Route Route { get ; }
    public string NewName { get ; }
    
    public ApplyChangeRouteNameCommandParameter( Route route, string newName )
    {
      Route = route ;
      NewName = newName ;
    }
  }
  
  public abstract class ApplyChangeRouteNameCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      if ( CommandParameterStorage.Pop<ApplyChangeRouteNameCommandParameter>() is not { } arg ) return Result.Cancelled ;

      return document.Transaction( "TransactionName.Commands.PostCommands.ApplyChangeRouteNameCommand".GetAppStringByKeyOrDefault( " Rename RouteName" ), t =>
      {
        arg.Route.Rename( arg.NewName ) ;
        return Result.Succeeded ;
      } ) ;
    }
  }
}