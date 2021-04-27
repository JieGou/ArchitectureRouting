using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.PickRoutingCommand", DefaultString = "Pick\nFrom-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PickRoutingCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting" ;

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      return ReadRouteRecordsByPick( uiDocument ).EnumerateAll().ToAsyncEnumerable() ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> ReadRouteRecordsByPick( UIDocument uiDocument )
    {
      var segments = UiThread.RevitUiDispatcher.Invoke( () =>
      {
        var document = uiDocument.Document ;
        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null ) ;
        var tempColor = SetTempColor( uiDocument, fromPickResult ) ;
        try {
          var toPickResult = ConnectorPicker.GetConnector( uiDocument, "Dialog.Commands.Routing.PickRouting.PickSecond".GetAppStringByKeyOrDefault( null ), fromPickResult ) ;

          return CreateNewSegmentList( document, fromPickResult, toPickResult ) ;
        }
        finally {
          DisposeTempColor( document, tempColor ) ;
        }
      } ) ;

      foreach ( var record in segments ) {
        yield return record ;
      }
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var fromEndPoint = GetEndPoint( fromPickResult, toPickResult ) ;
      var toEndPoint = GetEndPoint( toPickResult, fromPickResult ) ;

      if ( fromPickResult.SubRoute is { } subRoute1 ) {
        return CreateNewSegmentListForRoutePick( subRoute1, fromPickResult, toEndPoint, false ) ;
      }

      if ( toPickResult.SubRoute is { } subRoute2 ) {
        return CreateNewSegmentListForRoutePick( subRoute2, toPickResult, fromEndPoint, true ) ;
      }

      var list = new List<(string RouteName, RouteSegment Segment)>() ;

      var routes = RouteCache.Get( document ) ;

      var connector = fromEndPoint.GetReferenceConnector() ?? toEndPoint.GetReferenceConnector() ;

      if ( connector != null ) {
        var systemType = RouteMEPSystem.GetSystemType( document, connector ) ;

        var nextIndex = GetRouteNameIndex( routes, systemType?.Name ) ;

        var name = systemType?.Name + "_" + nextIndex ;

        var segment = new RouteSegment( fromEndPoint, toEndPoint, null, false ) ;
        segment.ApplyRealNominalDiameter() ;
        routes.FindOrCreate( name ) ;
        list.Add( ( name, segment ) ) ;

        return list ;
      }

      return list ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide )
    {
      return CreateSubBranchRoute( subRoute, routePickResult, anotherEndPoint, anotherIndicatorIsFromSide ) ;

      // on adding new segment into picked route.
      //return AppendNewSegmentIntoPickedRoute( subRoute, routePickResult, anotherIndicator, anotherIndicatorIsFromSide ) ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPoint anotherEndPoint, bool anotherIndicatorIsFromSide )
    {
      var routes = RouteCache.Get( subRoute.Route.Document ) ;
      var routEndPoint = new RouteEndPoint( subRoute ) ;

      var systemType = subRoute.Route.GetMEPSystemType() ;

      var nextIndex = GetRouteNameIndex( routes, systemType.Name ) ;
      
      var name = systemType?.Name + "_" + nextIndex  ;

      RouteSegment segment ;
      if ( anotherIndicatorIsFromSide ) {
        segment = new RouteSegment( anotherEndPoint, routEndPoint, null, false ) ;
      }
      else {
        segment = new RouteSegment( routEndPoint, anotherEndPoint, null, false ) ;
      }

      segment.ApplyRealNominalDiameter() ;

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
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( false == detector.IsPassingThrough( segment ) ) continue ;

        RouteSegment newSegment ;
        if ( newEndPointIndicatorIsFromSide ) {
          newSegment = new RouteSegment( newEndPoint, segment.ToEndPoint, null, false ) ;
        }
        else {
          newSegment = new RouteSegment( segment.FromEndPoint, newEndPoint, null, false ) ;
        }

        newSegment.ApplyRealNominalDiameter() ;

        return ( subRoute.Route.RouteName, newSegment ) ;
      }

      // fall through: add terminate end point.
      {
        RouteSegment newSegment ;
        if ( newEndPointIndicatorIsFromSide ) {
          var terminateEndPoint = new TerminatePointEndPoint( pickResult.PickedElement.Document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( false ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
          newSegment = new RouteSegment( newEndPoint, terminateEndPoint, null, false ) ;
        }
        else {
          var terminateEndPoint = new TerminatePointEndPoint( pickResult.PickedElement.Document, ElementId.InvalidElementId, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( true ), newEndPoint.GetDiameter(), ElementId.InvalidElementId ) ;
          newSegment = new RouteSegment( terminateEndPoint, newEndPoint, null, false ) ;
        }

        newSegment.ApplyRealNominalDiameter() ;

        return ( subRoute.Route.RouteName, newSegment ) ;
      }
    }

    private static IDisposable SetTempColor( UIDocument uiDocument, ConnectorPicker.IPickResult pickResult )
    {
      var tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
      uiDocument.Document.Transaction( "TransactionName.Commands.Routing.Common.ChangeColor".GetAppStringByKeyOrDefault( null ), t =>
      {
        tempColor.AddRange( pickResult.GetAllRelatedElements() ) ;
        return Result.Succeeded ;
      } ) ;
      return tempColor ;
    }

    private static void DisposeTempColor( Document document, IDisposable tempColor )
    {
      document.Transaction( "TransactionName.Commands.Routing.Common.RevertColor".GetAppStringByKeyOrDefault( null ), t =>
      {
        tempColor.Dispose() ;
        return Result.Succeeded ;
      } ) ;
    }

    private static IEndPoint GetEndPoint( ConnectorPicker.IPickResult pickResult, ConnectorPicker.IPickResult anotherResult )
    {
      if ( pickResult.PickedConnector is { } connector ) return new ConnectorEndPoint( connector ) ;

      var element = pickResult.PickedElement ;
      var pos = pickResult.GetOrigin() ;
      var anotherPos = anotherResult.GetOrigin() ;
      var dir = GetPreferredDirection( pos, anotherPos ) ;
      var preferredRadius = ( pickResult.PickedConnector ?? anotherResult.PickedConnector )?.Radius ;

      return new TerminatePointEndPoint( element.Document, ElementId.InvalidElementId, pos, dir, preferredRadius, element.Id ) ;
    }

    private static XYZ GetPreferredDirection( XYZ pos, XYZ anotherPos )
    {
      var dir = anotherPos - pos ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        return ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        return ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match(k) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }
  }
}