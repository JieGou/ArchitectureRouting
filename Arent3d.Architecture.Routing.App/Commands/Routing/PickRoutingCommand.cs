using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Pick\nFrom-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PickRoutingCommand : RoutingCommandBase
  {
    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegments( UIDocument uiDocument )
    {
      return ReadRouteRecordsByPick( uiDocument ).EnumerateAll().ToAsyncEnumerable() ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> ReadRouteRecordsByPick( UIDocument uiDocument )
    {
      var segments = UiThread.RevitUiDispatcher.Invoke( () =>
      {
        var document = uiDocument.Document ;
        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, "Select the first connector", null ) ;
        var tempColor = SetTempColor( uiDocument, fromPickResult ) ;
        try {
          var toPickResult = ConnectorPicker.GetConnector( uiDocument, "Select the second connector", fromPickResult ) ;

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
      var fromIndicator = GetEndPointIndicator( fromPickResult, toPickResult ) ;
      var toIndicator = GetEndPointIndicator( toPickResult, fromPickResult ) ;

      if ( fromPickResult.SubRoute is { } subRoute1 ) {
        return CreateNewSegmentListForRoutePick( subRoute1, fromPickResult, toIndicator, false ) ;
      }

      if ( toPickResult.SubRoute is { } subRoute2 ) {
        return CreateNewSegmentListForRoutePick( subRoute2, toPickResult, fromIndicator, true ) ;
      }

      var parentRoute1 = ( fromIndicator as PassPointBranchEndIndicator )?.ParentBranch( document ).Route ;
      var parentRoute2 = ( toIndicator as PassPointBranchEndIndicator )?.ParentBranch( document ).Route ;
      var list = UnionRoutes( parentRoute1, parentRoute2 ) ;

      var routes = RouteCache.Get( document ) ;

      for ( var i = routes.Count + 1 ; ; ++i ) {
        var name = "Picked_" + i ;
        if ( routes.ContainsKey( name ) ) continue ;

        var segment = new RouteSegment( fromIndicator, toIndicator, -1, false ) ;
        segment.ApplyRealNominalDiameter( document ) ;
        routes.FindOrCreate( name ) ;
        list.Add( ( name, segment ) ) ;
        return list ;
      }
    }

    private static List<(string RouteName, RouteSegment Segment)> UnionRoutes( Route? parentRoute1, Route? parentRoute2 )
    {
      HashSet<Route>? routes = null ;
      if ( null != parentRoute1 ) {
        routes = parentRoute1.GetAllRelatedBranches() ;
      }

      if ( null != parentRoute2 ) {
        if ( null != routes ) {
          routes.UnionWith( parentRoute2.GetAllRelatedBranches() );
        }
        else {
          routes = parentRoute2.GetAllRelatedBranches() ;
        }
      }

      if ( null == routes ) {
        return new List<(string RouteName, RouteSegment Segment)>() ;
      }
      else {
        return routes.ToSegmentsWithName().ToList() ;
      }
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPointIndicator anotherIndicator, bool anotherIndicatorIsFromSide )
    {
      return CreateSubBranchRoute( subRoute, routePickResult, anotherIndicator, anotherIndicatorIsFromSide ) ;

      // on adding new segment into picked route.
      //return AppendNewSegmentIntoPickedRoute( subRoute, routePickResult, anotherIndicator, anotherIndicatorIsFromSide ) ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPointIndicator anotherIndicator, bool anotherIndicatorIsFromSide )
    {
      var routes = RouteCache.Get( subRoute.Route.Document ) ;
      var newIndicator = new RouteIndicator( subRoute.Route.RouteName, subRoute.SubRouteIndex ) ;

      for ( var i = routes.Count + 1 ; ; ++i ) {
        var name = "Picked_" + i ;
        if ( routes.ContainsKey( name ) ) continue ;

        RouteSegment segment ;
        if ( anotherIndicatorIsFromSide ) {
          segment = new RouteSegment( anotherIndicator, newIndicator, -1, false ) ;
        }
        else {
          segment = new RouteSegment( newIndicator, anotherIndicator, -1, false ) ;
        }
        segment.ApplyRealNominalDiameter( subRoute.Route.Document ) ;

        return new[] { ( name, segment ) } ;
      }
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> AppendNewSegmentIntoPickedRoute( SubRoute subRoute, ConnectorPicker.IPickResult routePickResult, IEndPointIndicator anotherIndicator, bool anotherIndicatorIsFromSide )
    {
      var segments = subRoute.Route.ToSegmentsWithNameList() ;
      segments.Add( CreateNewSegment( subRoute, routePickResult, anotherIndicator, anotherIndicatorIsFromSide ) ) ;
      return segments ;
    }

    private static (string RouteName, RouteSegment Segment) CreateNewSegment( SubRoute subRoute, ConnectorPicker.IPickResult pickResult, IEndPointIndicator newEndPointIndicator, bool newEndPointIndicatorIsFromSide )
    {
      var detector = new RouteSegmentDetector( subRoute, pickResult.PickedElement ) ;
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( false == detector.IsPassingThrough( segment ) ) continue ;

        RouteSegment newSegment ;
        if ( newEndPointIndicatorIsFromSide ) {
          newSegment = new RouteSegment( newEndPointIndicator, segment.ToId, -1, false ) ;
        }
        else {
          newSegment = new RouteSegment( segment.FromId, newEndPointIndicator, -1, false ) ;
        }
        newSegment.ApplyRealNominalDiameter( subRoute.Route.Document ) ;

        return ( subRoute.Route.RouteName, newSegment ) ;
      }

      // fall through: coordinational record.
      var origin = pickResult.GetOrigin() ;
      var pickedIndicator = GetEndPointIndicator( pickResult.PickedElement.Document, subRoute, origin, newEndPointIndicator ) ;
      {
        RouteSegment newSegment ;
        if ( newEndPointIndicatorIsFromSide ) {
          newSegment = new RouteSegment( newEndPointIndicator, pickedIndicator, -1, false ) ;
        }
        else {
          newSegment = new RouteSegment( pickedIndicator, newEndPointIndicator, -1, false ) ;
        }
        newSegment.ApplyRealNominalDiameter( subRoute.Route.Document ) ;

        return ( subRoute.Route.RouteName, newSegment ) ;
      }
    }

    private static IEndPointIndicator GetEndPointIndicator( Document document, SubRoute subRoute, XYZ origin, IEndPointIndicator target )
    {
      var endPoint = target.GetEndPoint( document, subRoute ) ;
      if ( null == endPoint ) return new CoordinateIndicator( origin, XYZ.BasisX ) ;

      return GetCoordinateIndicator( origin, endPoint.Position.ToXYZ() ) ;
    }

    private static IDisposable SetTempColor( UIDocument uiDocument, ConnectorPicker.IPickResult pickResult )
    {
      using var transaction = new Transaction( uiDocument.Document ) ;
      try {
        transaction.Start( "Change Picked Element Color" ) ;
        
        var tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
        tempColor.AddRange( pickResult.GetAllRelatedElements() ) ;

        transaction.Commit() ;
        return tempColor ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }

    private static void DisposeTempColor( Document document, IDisposable tempColor )
    {
      using var transaction = new Transaction( document ) ;
      try {
        transaction.Start( "Revert Picked Element Color" ) ;

        tempColor.Dispose() ;

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }

    private static IEndPointIndicator GetEndPointIndicator( ConnectorPicker.IPickResult pickResult, ConnectorPicker.IPickResult anotherResult )
    {
      if ( pickResult.PickedConnector is { } connector ) return connector.GetIndicator() ;

      var element = pickResult.PickedElement ;
      var anotherPos = anotherResult.GetOrigin() ;
      if ( element.IsPassPoint() && element is Instance instance ) {
        return GetPassPointBranchIndicator( instance, anotherPos ) ;
      }
      else {
        return GetCoordinateIndicator( pickResult.GetOrigin(), anotherPos ) ;
      }
    }

    private static IEndPointIndicator GetPassPointBranchIndicator( Instance passPointInstance, XYZ anotherPos )
    {
      var transform = passPointInstance.GetTotalTransform() ;
      var dir = anotherPos - transform.Origin ;
      double cos = transform.BasisY.DotProduct( dir ), sin = transform.BasisZ.DotProduct( dir ) ;
      var angleDegree = ToNormalizedDegree( Math.Atan2( sin, cos ) ) ;

      return new PassPointBranchEndIndicator( passPointInstance.Id.IntegerValue, angleDegree ) ;
    }

    private static double ToNormalizedDegree( double radian )
    {
      var cornerCount = Math.Round( radian / ( 0.5 * Math.PI ) ) ;
      cornerCount -= Math.Floor( cornerCount / 4 ) * 4 ; // [0, 1, 2, 3]
      return 90 * cornerCount ;// [0, 90, 180, 270]
    }

    private static IEndPointIndicator GetCoordinateIndicator( XYZ origin, XYZ anotherPos )
    {
      var dir = anotherPos - origin ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        dir = ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        dir = ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }

      return new CoordinateIndicator( origin, dir ) ;
    }
  }
}