using System.Collections.Generic ;
using System.Linq ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.UI ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ReplaceFromToCommandBase : RoutingCommandBase
  {
    protected abstract AddInType GetAddInType() ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsParallelToTransaction( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var route = GetReplacingRoute( uiDocument ) ;

      var oldEndPoint = GetChangingEndPoint( uiDocument, route ) ;
      var newEndPoint = PickNewEndPoint( uiDocument, routingExecutor, route, oldEndPoint ) ;

      return GetReplacedEndPoints( route, oldEndPoint, newEndPoint ) ;
    }

    private static async IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetReplacedEndPoints( Route route, IEndPoint oldEndPoint, IEndPoint newEndPoint )
    {
      await Task.Yield() ;

      if ( oldEndPoint is RouteEndPoint routeEndPoint ) {
        var routes = RouteCache.Get( route.Document ) ;
        foreach ( var tuple in routes[ routeEndPoint.RouteName ].ToSegmentsWithName().EnumerateAll() ) {
          yield return tuple ;
        }
      }

      ThreadDispatcher.Dispatch( () =>
      {
        oldEndPoint.EraseInstance() ;
        newEndPoint.GenerateInstance( route.RouteName ) ;
      } ) ;

      foreach ( var (routeName, segment) in route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll() ) {
        segment.ReplaceEndPoint( oldEndPoint, newEndPoint ) ;
        yield return ( routeName, segment ) ;
      }
    }

    private static IEndPoint GetChangingEndPoint( UIDocument uiDocument, Route route )
    {
      using var _ = new TempZoomToFit( uiDocument ) ;

      var message = "Dialog.Commands.Routing.ReplaceFromTo.SelectFromTo".GetAppStringByKeyOrDefault( "Select which end is to be changed." ) ;

      var array = route.RouteSegments.SelectMany( GetReplaceableEndPoints ).ToArray() ;
      // TODO: selection ui

      var sv = new SelectEndPoint( uiDocument.Document, array ) { Title = message } ;
      sv.ShowDialog() ;

      uiDocument.ClearSelection() ;

      if ( true != sv.DialogResult ) {
        return array[ 0 ] ;
      }

      return sv.GetSelectedEndPoint() ;
    }

    private static IEnumerable<IEndPoint> GetReplaceableEndPoints( RouteSegment segment )
    {
      if ( segment.FromEndPoint.IsReplaceable ) yield return segment.FromEndPoint ;
      if ( segment.ToEndPoint.IsReplaceable ) yield return segment.ToEndPoint ;
    }

    private IEndPoint PickNewEndPoint( UIDocument uiDocument, RoutingExecutor routingExecutor, Route route, IEndPoint endPoint )
    {
      var (anotherPickResult, isFrom) = PickCommandUtil.PickResultFromAnother( route, endPoint ) ;
      var newPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, !isFrom, "Dialog.Commands.Routing.ReplaceFromTo.SelectEndPoint".GetAppStringByKeyOrDefault( null ), anotherPickResult, GetAddInType() ) ; //Implement after

      if ( null != newPickResult.SubRoute ) {
        return CreateEndPointOnSubRoute( newPickResult, anotherPickResult, ( false == isFrom ) ) ;
      }
      
      return PickCommandUtil.GetEndPoint( newPickResult, anotherPickResult ) ;
    }

    protected abstract IEndPoint CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom ) ;

    private Route GetReplacingRoute( UIDocument uiDocument )
    {
      return GetReplacingRouteFromCurrentSelection( uiDocument ) ?? PickReplacingRoute( uiDocument ) ;
    }

    private static Route? GetReplacingRouteFromCurrentSelection( UIDocument uiDocument )
    {
      return PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).UniqueOrDefault() ;
    }

    private Route PickReplacingRoute( UIDocument uiDocument )
    {
      return PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.ReplaceFromTo.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ).Route ;
    }
  }
}