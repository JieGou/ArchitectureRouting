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
  public abstract class InsertPassPointCommandBase : RoutingCommandBase<PointOnRoutePicker.PickInfo>
  {
    protected abstract AddInType GetAddInType() ;

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override OperationResult<PointOnRoutePicker.PickInfo> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var pickInfo = PointOnRoutePicker.PickRoute( commandData.Application.ActiveUIDocument, true, "Dialog.Commands.PassPoint.Insert.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType(), PointOnRouteFilters.RepresentativeElement ) ;
      return new OperationResult<PointOnRoutePicker.PickInfo>( pickInfo ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PointOnRoutePicker.PickInfo pickInfo )
    {
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
      var (fromElementId, toElementId) = NewRackCommandBase.GetFromAndToConnectorUniqueId( pickInfo.Element ) ;
      var (fromConnectorUniqueId, toConnectorUniqueId) = GetFromAndToConnectorUniqueId( document, fromElementId, toElementId ) ;
      var passPoint = document.AddPassPoint( pickInfo.Route.RouteName, pickInfo.Position, pickInfo.RouteDirection, pickInfo.Radius, pickInfo.Element.GetLevelId() ) ;
      passPoint.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toConnectorUniqueId ) ;
      passPoint.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromConnectorUniqueId ) ;
      return passPoint ;
    }

    private static (string, string) GetFromAndToConnectorUniqueId( Document document, string fromElementUniqueId, string toElementUniqueId )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;

      if ( ! string.IsNullOrEmpty( fromElementUniqueId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromElementUniqueId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          if ( fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorUniqueId ) && ! string.IsNullOrEmpty( fromConnectorUniqueId ) ) {
            fromElementUniqueId = fromConnectorUniqueId! ;
          }
        }
      }

      if ( string.IsNullOrEmpty( toElementUniqueId ) ) return ( fromElementUniqueId, toElementUniqueId ) ;
      {
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementUniqueId ) ;
        if ( ! toConnector!.IsTerminatePoint() && ! toConnector!.IsPassPoint() ) return ( fromElementUniqueId, toElementUniqueId ) ;
        if ( toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? toConnectorUniqueId ) && ! string.IsNullOrEmpty( toConnectorUniqueId ) ) {
          toElementUniqueId = toConnectorUniqueId! ;
        }
      }

      return ( fromElementUniqueId, toElementUniqueId ) ;
    }
  }
}