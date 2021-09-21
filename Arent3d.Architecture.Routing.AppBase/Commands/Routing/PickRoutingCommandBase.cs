using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PickRoutingCommandBase : RoutingCommandBase
  {
    private record PickState( ConnectorPicker.IPickResult FromPickResult, ConnectorPicker.IPickResult ToPickResult, RoutePropertyDialog PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;
    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, bool newPickIsFrom ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
      ConnectorPicker.IPickResult toPickResult ;

      using ( uiDocument.SetTempColor( fromPickResult ) ) {
        toPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, false, "Dialog.Commands.Routing.PickRouting.PickSecond".GetAppStringByKeyOrDefault( null ), fromPickResult, GetAddInType() ) ;
      }

      var property = ShowPropertyDialog( uiDocument.Document, fromPickResult, toPickResult ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;

      if ( GetMEPSystemClassificationInfo( fromPickResult, toPickResult, property.GetSelectSystemType() ) is not { } classificationInfo ) return ( false, null ) ;

      return ( true, new PickState( fromPickResult, toPickResult, property, classificationInfo ) ) ;
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, MEPSystemType? systemType )
    {
      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute )?.Route.GetSystemClassificationInfo() is { } routeSystemClassificationInfo ) return routeSystemClassificationInfo ;

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private RoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute ) is { } subRoute ) {
        var route = subRoute.Route ;
        return ShowDialog( document, new DialogInitValues( route.GetSystemClassificationInfo(), route.GetMEPSystemType(), route.GetDefaultCurveType(), subRoute.GetDiameter() ) ) ;
      }

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues ) ;
      }

      return ShowDialog( document ) ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues )
    {
      var sv = new RoutePropertyDialog( document, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }
    private static RoutePropertyDialog ShowDialog( Document document )
    {
      var sv = new RoutePropertyDialog( document, new RouteProperties( document ) ) ;
      sv.ShowDialog() ;

      return sv ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var pickState = state as PickState ?? throw new InvalidOperationException() ;
      var (fromPickResult, toPickResult, property, classificationInfo) = pickState ;

      if ( null != fromPickResult.SubRoute ) {
        return CreateNewSegmentListForRoutePick( fromPickResult, toPickResult, false, property, classificationInfo ) ;
      }
      if ( null != toPickResult.SubRoute ) {
        return CreateNewSegmentListForRoutePick( toPickResult, fromPickResult, true, property, classificationInfo ) ;
      }

      return CreateNewSegmentList( document, fromPickResult, toPickResult, property, classificationInfo ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, RoutePropertyDialog propertyDialog, MEPSystemClassificationInfo classificationInfo )
    {
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult ) ;

      var (name, segment) = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, propertyDialog, classificationInfo ) ;

      return new[] { ( name, segment ) } ;
    }

    private (string RouteName, RouteSegment Segment) CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, RoutePropertyDialog propertyDialog, MEPSystemClassificationInfo classificationInfo )
    {
      var systemType = propertyDialog.GetSelectSystemType() ;
      var curveType = propertyDialog.GetSelectCurveType() ;

      var routes = RouteCache.Get( document ) ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = propertyDialog.GetSelectDiameter() ;
      var isRoutingOnPipeSpace = propertyDialog.GetRouteOnPipeSpace() ;
      var fixedHeight = propertyDialog.GetFixedHeight() ;
      var avoidType = propertyDialog.GetSelectedAvoidType() ;
      var shaftElementId = propertyDialog.GetShaftElementId() ;

      double? trueFixedHeight = ( fixedHeight.HasValue ? RouteProperties.GetTrueFixedHeight( GetLevel( document, fromEndPoint ) ?? GetLevel( document, toEndPoint ), diameter, fixedHeight.Value ) : null ) ;

      return ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, trueFixedHeight, avoidType,shaftElementId ) ) ;
    }

    private static Level? GetLevel( Document document, IEndPoint endPoint )
    {
      if ( endPoint.GetReferenceConnector()?.Owner.LevelId is not { } levelId ) return null ;

      return document.GetElementById<Level>( levelId ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool anotherIndicatorIsFromSide, RoutePropertyDialog propertyDialog, MEPSystemClassificationInfo classificationInfo )
    {
      //return AppendNewSegmentIntoPickedRoute( routePickResult, anotherPickResult, anotherIndicatorIsFromSide ) ;  // Use this, when a branch is to be merged into the parent from-to.
      return CreateSubBranchRoute( routePickResult, anotherPickResult, anotherIndicatorIsFromSide, propertyDialog, classificationInfo ).EnumerateAll() ;
    }

    private IEnumerable<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool anotherIndicatorIsFromSide, RoutePropertyDialog propertyDialog, MEPSystemClassificationInfo classificationInfo )
    {
      var affectedRoutes = new List<Route>() ;
      var (routeEndPoint, otherSegments1) = CreateEndPointOnSubRoute( routePickResult, anotherPickResult, true ) ;

      IEndPoint anotherEndPoint ;
      IReadOnlyCollection<( string RouteName, RouteSegment Segment )>? otherSegments2 = null ;
      if ( null != anotherPickResult.SubRoute ) {
        ( anotherEndPoint, otherSegments2 ) = CreateEndPointOnSubRoute( anotherPickResult, routePickResult, false ) ;
      }
      else {
        anotherEndPoint = PickCommandUtil.GetEndPoint( anotherPickResult, routePickResult ) ;
      }

      var fromEndPoint = anotherIndicatorIsFromSide ? anotherEndPoint : routeEndPoint ;
      var toEndPoint = anotherIndicatorIsFromSide ? routeEndPoint : anotherEndPoint ;

      var document = routePickResult.SubRoute!.Route.Document ;
      var (name, segment) = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, propertyDialog, classificationInfo ) ;

      // Inserted segment
      yield return ( name, segment ) ;

      // Routes where pass points are inserted
      var routes = RouteCache.Get( routePickResult.SubRoute!.Route.Document ) ;
      var changedRoutes = new HashSet<Route>() ;
      if ( null != otherSegments1 ) {
        foreach ( var tuple in otherSegments1 ) {
          yield return tuple ;

          if ( routes.TryGetValue( tuple.RouteName, out var route ) ) {
            changedRoutes.Add( route ) ;
          }
        }
      }
      if ( null != otherSegments2 ) {
        foreach ( var tuple in otherSegments2 ) {
          yield return tuple ;

          if ( routes.TryGetValue( tuple.RouteName, out var route ) ) {
            changedRoutes.Add( route ) ;
          }
        }
      }

      // Affected routes
      if ( 0 != affectedRoutes.Count ) {
        var affectedRouteSet = new HashSet<Route>() ;
        foreach ( var route in affectedRoutes ) {
          affectedRouteSet.Add( route ) ;
          affectedRouteSet.UnionWith( route.CollectAllDescendantBranches() ) ;
        }
        affectedRouteSet.ExceptWith( changedRoutes ) ;

        foreach ( var tuple in affectedRouteSet.ToSegmentsWithName() ) {
          yield return tuple ;
        }
      }
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> AppendNewSegmentIntoPickedRoute( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool anotherIndicatorIsFromSide )
    {
      var route = routePickResult.SubRoute!.Route ;
      var segments = route.ToSegmentsWithNameList() ;
      var anotherEndPoint = PickCommandUtil.GetEndPoint( anotherPickResult, routePickResult ) ;
      var segment = CreateNewSegment( routePickResult.SubRoute!, routePickResult.EndPointOverSubRoute, routePickResult, anotherEndPoint, anotherIndicatorIsFromSide ) ;
      segment.ApplyRealNominalDiameter() ;
      segments.Add( ( route.RouteName, segment ) ) ;
      return segments ;
    }

    private static RouteSegment CreateNewSegment( SubRoute subRoute, EndPointKey? endPointOverSubRoute, ConnectorPicker.IPickResult pickResult, IEndPoint newEndPoint, bool newEndPointIndicatorIsFromSide )
    {
      var document = subRoute.Route.Document ;
      var detector = new RouteSegmentDetector( subRoute, pickResult.PickedElement ) ;
      var classificationInfo = subRoute.Route.GetSystemClassificationInfo() ;
      var systemType = subRoute.Route.GetMEPSystemType() ;
      var curveType = subRoute.Route.GetDefaultCurveType() ;

      if ( null != endPointOverSubRoute && subRoute.AllEndPoints.FirstOrDefault( ep => ep.Key == endPointOverSubRoute ) is { } overSubRoute ) {
        var shaft = ( newEndPoint.GetLevelId( document ) != overSubRoute.GetLevelId( document ) ) ? subRoute.ShaftElementId : ElementId.InvalidElementId ;
        if ( newEndPointIndicatorIsFromSide ) {
          return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, overSubRoute, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType, shaft ) ;
        }
        else {
          return new RouteSegment( classificationInfo, systemType, curveType, overSubRoute, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType, shaft ) ;
        }
      }

      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( false == detector.IsPassingThrough( segment ) ) continue ;

        if ( newEndPointIndicatorIsFromSide ) {
          var shaft = ( newEndPoint.GetLevelId( document ) != segment.ToEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementId : ElementId.InvalidElementId ;
          return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, segment.ToEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType, shaft ) ;
        }
        else {
          var shaft = ( segment.FromEndPoint.GetLevelId( document ) != newEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementId : ElementId.InvalidElementId ;
          return new RouteSegment( classificationInfo, systemType, curveType, segment.FromEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType, shaft ) ;
        }
      }

      // fall through: add terminate end point.
      if ( newEndPointIndicatorIsFromSide ) {
        var terminateEndPoint = new TerminatePointEndPoint( document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( false ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
        var shaft = ( newEndPoint.GetLevelId( document ) != terminateEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementId : ElementId.InvalidElementId ;
        return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, terminateEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType, shaft ) ;
      }
      else {
        var terminateEndPoint = new TerminatePointEndPoint( document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( true ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
        var shaft = ( terminateEndPoint.GetLevelId( document ) != newEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementId : ElementId.InvalidElementId ;
        return new RouteSegment( classificationInfo, systemType, curveType, terminateEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType, shaft ) ;
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }
  }
}