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

  public abstract class ApplyChangeRouteNameCommandBase : RoutingExternalAppCommandBaseWithParam<ApplyChangeRouteNameCommandParameter>
  {
    protected override string GetTransactionName() => "TransactionName.Commands.PostCommands.ApplyChangeRouteNameCommand".GetAppStringByKeyOrDefault( " Rename RouteName" ) ;

    protected override Result Execute( Document document, ApplyChangeRouteNameCommandParameter param, TransactionWrapper transaction )
    {
      param.Route.Rename( param.NewName ) ;
      return Result.Succeeded ;
    }
  }
}