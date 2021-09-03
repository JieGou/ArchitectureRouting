using System ;
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
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ReplaceFromToCommandBase : RoutingCommandBase
  {
    private record PickState( Route Route, ConnectorPicker.IPickResult AnotherPickResult, bool AnotherPickIsFrom, IEndPoint OldEndPoint, ConnectorPicker.IPickResult NewPickResult ) ;

    protected abstract AddInType GetAddInType() ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var route = GetReplacingRoute( uiDocument ) ;

      var oldEndPoint = GetChangingEndPoint( uiDocument, route ) ;
      var (anotherPickResult, isFrom) = PickCommandUtil.PickResultFromAnother( route, oldEndPoint ) ;
      var newPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, !isFrom, "Dialog.Commands.Routing.ReplaceFromTo.SelectEndPoint".GetAppStringByKeyOrDefault( null ), anotherPickResult, GetAddInType() ) ; //Implement after

      return ( true, new PickState( route, anotherPickResult, ( false == isFrom ), oldEndPoint, newPickResult ) ) ;
    }

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var pickState = state as PickState ?? throw new InvalidOperationException() ;

      var (route, anotherPickResult, anotherPickIsFrom, oldEndPoint, newPickResult) = pickState ;

      IEndPoint newEndPoint ;
      Route? newAffectedRoute ;
      if ( null != newPickResult.SubRoute ) {
        ( newEndPoint, newAffectedRoute ) = CreateEndPointOnSubRoute( newPickResult, anotherPickResult, anotherPickIsFrom ) ;
      }
      else {
        newEndPoint = PickCommandUtil.GetEndPoint( newPickResult, anotherPickResult ) ;
        newAffectedRoute = null ;
      }

      return GetReplacedEndPoints( route, oldEndPoint, newEndPoint, newAffectedRoute ) ;
    }

    private static async IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetReplacedEndPoints( Route route, IEndPoint oldEndPoint, IEndPoint newEndPoint, Route? newAffectedRoute )
    {
      await Task.Yield() ;

      var affectedRoutes = new List<Route>() ;
      if ( oldEndPoint is RouteEndPoint routeEndPoint ) {
        var routes = RouteCache.Get( route.Document ) ;
        affectedRoutes.Add( routes[ routeEndPoint.RouteName ] ) ;
      }

      if ( null != newAffectedRoute ) {
        affectedRoutes.Add( newAffectedRoute ) ;
      }

      if ( 0 < affectedRoutes.Count ) {
        var affectedRouteSet = new HashSet<Route>() ;
        foreach ( var affectedRoute in affectedRoutes ) {
          affectedRouteSet.Add( affectedRoute ) ;
          affectedRouteSet.UnionWith( affectedRoute.CollectAllDescendantBranches() ) ;
        }

        foreach ( var tuple in affectedRouteSet.ToSegmentsWithName() ) {
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

    protected abstract (IEndPoint EndPoint, Route? AffectedRoute) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom ) ;

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