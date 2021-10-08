using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PassPoint
{
  public abstract class InsertPassPointCommandBase : RoutingCommandBase
  {
    protected abstract AddInType GetAddInType() ;

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, true, "Dialog.Commands.PassPoint.Insert.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      return ( true, pickInfo ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var pickInfo = state as PointOnRoutePicker.PickInfo ?? throw new InvalidOperationException() ;

      var elm = InsertPassPointElement( document, pickInfo ) ;
      var route = pickInfo.SubRoute.Route ;
      var routeRecords = GetRelatedBranchSegments( route ) ;
      return routeRecords.Concat( PickCommandUtil.GetNewSegmentList( pickInfo.SubRoute, pickInfo.Element, elm ).ToSegmentsWithName( route.RouteName ) ).EnumerateAll() ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRelatedBranchSegments( Route route )
    {
      // add all related branches
      var relatedBranches = route.GetAllRelatedBranches() ;
      relatedBranches.Remove( route ) ;
      return relatedBranches.ToSegmentsWithName() ;
    }

    private static FamilyInstance InsertPassPointElement( Document document, PointOnRoutePicker.PickInfo pickInfo )
    {
      return document.AddPassPoint( pickInfo.Route.RouteName, pickInfo.Position, pickInfo.RouteDirection, pickInfo.Radius, pickInfo.Element.GetLevelId() ) ;
    }
  }
}