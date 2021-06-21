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
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
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
        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
        var tempColor = uiDocument.SetTempColor( fromPickResult ) ;
        try {
          var toPickResult = ConnectorPicker.GetConnector( uiDocument, "Dialog.Commands.Routing.PickRouting.PickSecond".GetAppStringByKeyOrDefault( null ), fromPickResult, GetAddInType() ) ;

          return CreateNewSegmentList( document, fromPickResult, toPickResult ) ;
        }
        finally {
          document.DisposeTempColor( tempColor ) ;
        }
      } ) ;

      return segments ;
    }

    protected abstract IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult ) ;


    protected static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide, MEPSystemClassificationInfo classificationInfo, SetRouteProperty setRouteProperty )
    {
      return CreateSubBranchRoute( subRoute, anotherEndPoint, anotherIndicatorIsFromSide, classificationInfo, setRouteProperty ) ;

      // on adding new segment into picked route.
      //return AppendNewSegmentIntoPickedRoute( subRoute, routePickResult, anotherIndicator, anotherIndicatorIsFromSide ) ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( SubRoute subRoute, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide, MEPSystemClassificationInfo classificationInfo, SetRouteProperty setRouteProperty )
    {
      var routes = RouteCache.Get( subRoute.Route.Document ) ;
      var routEndPoint = new RouteEndPoint( subRoute ) ;

      var systemType = setRouteProperty.GetSelectSystemType() ;
      var nextIndex = GetRouteNameIndex( routes, systemType?.Name ) ;

      var name = systemType?.Name + "_" + nextIndex ;

      var isDirect = false ;
      if ( setRouteProperty.GetCurrentDirect() is { } currentDirect ) {
        isDirect = currentDirect ;
      }

      double? targetFixedBopHeight = setRouteProperty.GetFixedHeight() ;
      if ( targetFixedBopHeight is { } fixedBopHeight ) {
        targetFixedBopHeight = fixedBopHeight.MillimetersToRevitUnits() ;
      }

      RouteSegment segment ;
      if ( anotherIndicatorIsFromSide ) {
        segment = new RouteSegment( classificationInfo, systemType, setRouteProperty.GetSelectCurveType(), anotherEndPoint, routEndPoint, setRouteProperty.GetSelectDiameter().MillimetersToRevitUnits(), isDirect, targetFixedBopHeight, setRouteProperty.GetAvoidTypeKey() ) ;
      }
      else {
        segment = new RouteSegment( classificationInfo, systemType, setRouteProperty.GetSelectCurveType(), routEndPoint, anotherEndPoint, setRouteProperty.GetSelectDiameter().MillimetersToRevitUnits(), isDirect, targetFixedBopHeight, setRouteProperty.GetAvoidTypeKey() ) ;
      }


      return new[] { ( name, segment ) } ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> AppendNewSegmentIntoPickedRoute( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide )
    {
      var segments = subRoute.Route.ToSegmentsWithNameList() ;
      segments.Add( CreateNewSegment( subRoute, routePickResult, anotherEndPoint, anotherIndicatorIsFromSide ) ) ;
      return segments ;
    }

    private static (string RouteName, RouteSegment Segment) CreateNewSegment( SubRoute subRoute, ConnectorPicker.IPickResult pickResult, IEndPoint newEndPoint, bool newEndPointIndicatorIsFromSide )
    {
      var detector = new RouteSegmentDetector( subRoute, pickResult.PickedElement ) ;
      var classificationInfo = subRoute.Route.GetSystemClassificationInfo() ;
      var systemType = subRoute.Route.GetMEPSystemType() ;
      var curveType = subRoute.Route.GetDefaultCurveType() ;
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( false == detector.IsPassingThrough( segment ) ) continue ;

        RouteSegment newSegment ;
        if ( newEndPointIndicatorIsFromSide ) {
          newSegment = new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, segment.ToEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }
        else {
          newSegment = new RouteSegment( classificationInfo, systemType, curveType, segment.FromEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }

        newSegment.ApplyRealNominalDiameter() ;

        return ( subRoute.Route.RouteName, newSegment ) ;
      }

      // fall through: add terminate end point.
      {
        RouteSegment newSegment ;
        if ( newEndPointIndicatorIsFromSide ) {
          var terminateEndPoint = new TerminatePointEndPoint( pickResult.PickedElement.Document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( false ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
          newSegment = new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, terminateEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }
        else {
          var terminateEndPoint = new TerminatePointEndPoint( pickResult.PickedElement.Document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( true ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
          newSegment = new RouteSegment( classificationInfo, systemType, curveType, terminateEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FixedBopHeight, subRoute.AvoidType ) ;
        }

        newSegment.ApplyRealNominalDiameter() ;

        return ( subRoute.Route.RouteName, newSegment ) ;
      }
    }

    protected static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    protected static SetRouteProperty SetDialog( Document document, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, double? dbDiameter )
    {
      SetRouteProperty sv = new SetRouteProperty() ;
      PropertySource.RoutePropertySource PropertySourceType = new PropertySource.RoutePropertySource( document, classificationInfo, systemType, curveType ) ;
      SelectedFromToViewModel.PropertySourceType = PropertySourceType ;
      sv.UpdateFromToParameters( PropertySourceType.Diameters, PropertySourceType.SystemTypes, PropertySourceType.CurveTypes, systemType, curveType, dbDiameter ) ;

      sv.ShowDialog() ;

      return sv ;
    }
  }
}