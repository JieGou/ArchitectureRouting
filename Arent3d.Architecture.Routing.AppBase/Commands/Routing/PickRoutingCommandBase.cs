using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
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
  public abstract class PickRoutingCommandBase : RoutingCommandBase
  {
    protected abstract AddInType GetAddInType() ;

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      return ReadRouteRecordsByPick( uiDocument )?.EnumerateAll().ToAsyncEnumerable() ;
    }

    private IEnumerable<(string RouteName, RouteSegment Segment)>? ReadRouteRecordsByPick( UIDocument uiDocument )
    {
      var segments = UiThread.RevitUiDispatcher.Invoke( () =>
      {
        var document = uiDocument.Document ;
        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
        using var tempColor = uiDocument.SetTempColor( fromPickResult ) ;

        var toPickResult = ConnectorPicker.GetConnector( uiDocument, false, "Dialog.Commands.Routing.PickRouting.PickSecond".GetAppStringByKeyOrDefault( null ), fromPickResult, GetAddInType() ) ;
        return CreateNewSegmentList( document, fromPickResult, toPickResult ) ;
      } ) ;

      return segments ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      if ( null != fromPickResult.SubRoute ) {
        return CreateNewSegmentWhereFromIsSubRoute( document, fromPickResult, toPickResult ) ;
      }
      if ( null != toPickResult.SubRoute ) {
        return CreateNewSegmentWhereToIsSubRoute( document, fromPickResult, toPickResult ) ;
      }

      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult ) ;

      if ( ( fromEndPoint.GetReferenceConnector() ?? toEndPoint.GetReferenceConnector() ) is { } connector ) {
        return CreateNewSegmentWithConnector( document, connector, fromEndPoint, toEndPoint ) ;

      }
      else {
        return CreateNewSegmentFreeConnector( document, fromEndPoint, toEndPoint ) ;
      }
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentWhereFromIsSubRoute( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var subRoute = fromPickResult.SubRoute! ;
      var classificationInfo = subRoute.Route.GetSystemClassificationInfo() ;
      var systemType = subRoute.Route.GetMEPSystemType() ;
      var curveType = subRoute.Route.GetDefaultCurveType() ;
      var dblDiameter = subRoute.GetDiameter() ;

      var sv = SetDialog( document, classificationInfo, systemType, curveType, dblDiameter ) ;
      if ( true != sv.DialogResult ) return null ;

      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult ) ;
      return CreateNewSegmentListForRoutePick( fromPickResult, toEndPoint, false, classificationInfo, sv ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentWhereToIsSubRoute( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var subRoute = toPickResult.SubRoute! ;
      var classificationInfo = subRoute.Route.GetSystemClassificationInfo() ;
      var systemType = subRoute.Route.GetMEPSystemType() ;
      var curveType = subRoute.Route.GetDefaultCurveType() ;
      var dblDiameter = subRoute.GetDiameter() ;

      var sv = SetDialog( document, classificationInfo, systemType, curveType, dblDiameter ) ;
      if ( true != sv.DialogResult ) return null ;

      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult ) ;
      return CreateNewSegmentListForRoutePick( toPickResult, fromEndPoint, true, classificationInfo, sv ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentWithConnector( Document document, Connector connector, IEndPoint fromEndPoint, IEndPoint toEndPoint )
    {
      if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return Array.Empty<(string RouteName, RouteSegment Segment)>() ;

      if ( CreateSegmentDialogWithConnector( document, connector, classificationInfo, fromEndPoint, toEndPoint ) is not { } sv ) return null ;
      if ( true != sv.DialogResult ) return null ;

      var systemType = sv.GetSelectSystemType() ;
      var curveType = sv.GetSelectCurveType() ;

      var routes = RouteCache.Get( document ) ;

      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;

      var diameter = sv.GetSelectDiameter().MillimetersToRevitUnits() ;
      var isDirect = sv.GetCurrentDirect() ?? false ;
      var targetFixedHeight = sv.GetFixedHeight()?.MillimetersToRevitUnits() ;

      var segment = new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isDirect, targetFixedHeight, sv.GetAvoidTypeKey() ) ;
      routes.FindOrCreate( name ) ;

      return new[] { ( name, segment ) } ;
    }

    protected abstract SetRouteProperty? CreateSegmentDialogWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo, IEndPoint fromEndPoint, IEndPoint toEndPoint ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentFreeConnector( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint )
    {
      var sv = new SetRouteProperty() ;
      var propertySourceType = new PropertySource.RoutePropertySource( document ) ;
      SelectedFromToViewModel.PropertySourceType = propertySourceType ;
      sv.UpdateFromToParameters( propertySourceType.Diameters, propertySourceType.SystemTypes, propertySourceType.CurveTypes, propertySourceType.SystemType, propertySourceType.CurveType, 0 ) ;

      sv.ShowDialog() ;

      if ( true != sv.DialogResult ) return null ;

      var systemType = sv.GetSelectSystemType() ;
      if ( GetMEPSystemClassificationInfoFromSystemType( systemType ) is not { } classificationInfo ) return null ;

      var curveType = sv.GetSelectCurveType() ;

      var routes = RouteCache.Get( document ) ;

      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;

      var diameter = sv.GetSelectDiameter().MillimetersToRevitUnits() ;
      var isDirect = sv.GetCurrentDirect() ?? false ;
      var targetFixedHeight = sv.GetFixedHeight()?.MillimetersToRevitUnits() ;

      var segment = new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isDirect, targetFixedHeight, sv.GetAvoidTypeKey() ) ;
      routes.FindOrCreate( name ) ;

      return new[] { ( name, segment ) } ;
    }

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;


    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide, MEPSystemClassificationInfo classificationInfo, SetRouteProperty setRouteProperty )
    {
      return CreateSubBranchRoute( routePickResult, anotherEndPoint, anotherIndicatorIsFromSide, classificationInfo, setRouteProperty ) ;
    }

    protected static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListWithinRoute( ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide, MEPSystemClassificationInfo classificationInfo, SetRouteProperty setRouteProperty )
    {
      return AppendNewSegmentIntoPickedRoute( routePickResult, anotherEndPoint, anotherIndicatorIsFromSide ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide, MEPSystemClassificationInfo classificationInfo, SetRouteProperty setRouteProperty )
    {
      var routes = RouteCache.Get( routePickResult.SubRoute!.Route.Document ) ;
      var routeEndPoint = PickCommandUtil.CreateRouteEndPoint( routePickResult ) ;

      var systemType = setRouteProperty.GetSelectSystemType() ;
      var curveType = setRouteProperty.GetSelectCurveType() ;

      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;

      var diameter = setRouteProperty.GetSelectDiameter().MillimetersToRevitUnits() ;
      var isDirect = setRouteProperty.GetCurrentDirect() ?? false ;
      var targetFixedHeight = setRouteProperty.GetFixedHeight()?.MillimetersToRevitUnits() ;

      RouteSegment segment ;
      if ( anotherIndicatorIsFromSide ) {
        segment = new RouteSegment( classificationInfo, systemType, curveType, anotherEndPoint, routeEndPoint, diameter, isDirect, targetFixedHeight, setRouteProperty.GetAvoidTypeKey() ) ;
      }
      else {
        segment = new RouteSegment( classificationInfo, systemType, curveType, routeEndPoint, anotherEndPoint, diameter, isDirect, targetFixedHeight, setRouteProperty.GetAvoidTypeKey() ) ;
      }

      return new[] { ( name, segment ) } ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> AppendNewSegmentIntoPickedRoute( ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide )
    {
      var route = routePickResult.SubRoute!.Route ;
      var segments = route.ToSegmentsWithNameList() ;
      var segment = CreateNewSegment( routePickResult.SubRoute!, routePickResult.EndPointOverSubRoute, routePickResult, anotherEndPoint, anotherIndicatorIsFromSide ) ;
      segment.ApplyRealNominalDiameter() ;
      segments.Add( ( route.RouteName, segment ) ) ;
      return segments ;
    }

    private static RouteSegment CreateNewSegment( SubRoute subRoute, EndPointKey? endPointOverSubRoute, ConnectorPicker.IPickResult pickResult, IEndPoint newEndPoint, bool newEndPointIndicatorIsFromSide )
    {
      var detector = new RouteSegmentDetector( subRoute, pickResult.PickedElement ) ;
      var classificationInfo = subRoute.Route.GetSystemClassificationInfo() ;
      var systemType = subRoute.Route.GetMEPSystemType() ;
      var curveType = subRoute.Route.GetDefaultCurveType() ;

      if ( null != endPointOverSubRoute && subRoute.AllEndPoints.FirstOrDefault( ep => ep.Key == endPointOverSubRoute ) is { } overSubRoute ) {
        if ( newEndPointIndicatorIsFromSide ) {
          return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, overSubRoute, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }
        else {
          return new RouteSegment( classificationInfo, systemType, curveType, overSubRoute, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }
      }

      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( false == detector.IsPassingThrough( segment ) ) continue ;

        if ( newEndPointIndicatorIsFromSide ) {
          return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, segment.ToEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }
        else {
          return new RouteSegment( classificationInfo, systemType, curveType, segment.FromEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }
      }

      // fall through: add terminate end point.
      if ( newEndPointIndicatorIsFromSide ) {
        var terminateEndPoint = new TerminatePointEndPoint( pickResult.PickedElement.Document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( false ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
        return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, terminateEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
      }
      else {
        var terminateEndPoint = new TerminatePointEndPoint( pickResult.PickedElement.Document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( true ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
        return new RouteSegment( classificationInfo, systemType, curveType, terminateEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    protected static SetRouteProperty SetDialog( Document document, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, double? dbDiameter, double? connectorFixedHeight = null )
    {
      var sv = new SetRouteProperty() ;
      var propertySourceType = new PropertySource.RoutePropertySource( document, classificationInfo, systemType, curveType ) ;
      SelectedFromToViewModel.PropertySourceType = propertySourceType ;
      sv.UpdateFromToParameters( propertySourceType.Diameters, propertySourceType.SystemTypes, propertySourceType.CurveTypes, systemType, curveType, dbDiameter, connectorFixedHeight) ;

      sv.ShowDialog() ;

      return sv ;
    }
  }
}