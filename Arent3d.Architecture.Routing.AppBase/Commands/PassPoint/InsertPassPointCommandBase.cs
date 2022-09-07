using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
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
      var routeRecords = GetRelatedBranchSegments( route ).ToList()  ;
      if ( !routeRecords.Any() ) routeRecords = GetRelatedBranchSegments( document, route, pickInfo.Element, elm, pickInfo.RouteNameDictionary ).ToList() ;
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

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRelatedBranchSegments( Document document, Route route, Element insertingElement, Instance passPointElement, Dictionary<string, string> routeNameDictionary )
    {
      const double tolerance = 0.01 ;
      var routeName = route.RouteName ;
      var conduitOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).FirstOrDefault( e => e.GetRouteName() == routeName ) ;
      var relatedSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      if ( conduitOfRoute == null ) return relatedSegments ;
      {
        var dic = RouteCache.Get( DocumentKey.Get( document ) ) ;
        var representativeRouteName = conduitOfRoute.GetRepresentativeRouteName() ;
        if ( string.IsNullOrEmpty( representativeRouteName ) ) return relatedSegments ;
        var conduit = insertingElement as Conduit ;
        var insertingElementOrigin = ( ( conduit!.Location as LocationCurve )!.Curve as Line )!.GetEndPoint( 0 ) ;
        var allBranchRouteNames = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( e => e.GetRouteName() != routeName && ( representativeRouteName == routeName ? e.GetRepresentativeRouteName() == routeName : e.GetRepresentativeRouteName() == representativeRouteName ) ).Select( e => e.GetRouteName() ! ).Distinct() ;
        foreach ( var branchRouteName in allBranchRouteNames ) {
          if ( false == dic.TryGetValue( branchRouteName, out var branchRoute ) ) continue ;
          var element = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit )
            .FirstOrDefault( e => e.GetRouteName() == branchRouteName && ( e is Conduit c && insertingElementOrigin.DistanceTo( ( ( c.Location as LocationCurve )!.Curve as Line )!.GetEndPoint( 0 ) ) < tolerance ) ) ;
          if ( element == null ) continue ;
          var subRoute = branchRoute.GetSubRoute( element.GetSubRouteIndex() ?? -1 ) ;
          if ( null == subRoute ) continue ;
          var segments = PickCommandUtil.GetNewSegmentList( subRoute, element, passPointElement ).ToSegmentsWithName( branchRouteName ) ;
          relatedSegments.AddRange( segments ) ;
          if ( ! routeNameDictionary.ContainsKey( branchRouteName ) ) {
            routeNameDictionary.Add( branchRouteName, element.GetRepresentativeRouteName()! ) ;
          }
        }
      }

      return relatedSegments ;
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