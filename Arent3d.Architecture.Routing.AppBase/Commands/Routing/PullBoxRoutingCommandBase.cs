using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PullBoxRoutingCommandBase : RoutingCommandBase<PullBoxRoutingCommandBase.PickState>
  {
    public record PickState( string Height) ;
    
    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    
    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var routingExecutor = GetRoutingExecutor() ;
      var route = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.ReplaceFromTo.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      var sv = new PullBoxDialog() ;
      sv.ShowDialog() ;
      
      return new OperationResult<PickState>(new PickState( "height" )) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      return new List<(string RouteName, RouteSegment Segment)>() ;
    }
    
  }
}