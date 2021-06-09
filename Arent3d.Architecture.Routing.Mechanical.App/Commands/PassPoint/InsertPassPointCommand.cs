using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.PassPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.PassPoint.InsertPassPointCommand", DefaultString = "Insert\nPass Point" )]
  [Image( "resources/InsertPassPoint.png", ImageType = ImageType.Large )]
  public class InsertPassPointCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.PassPoint.Insert" ;

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      var segments = UiThread.RevitUiDispatcher.Invoke( () =>
      {
        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, true, "Dialog.Commands.PassPoint.Insert.Pick".GetAppStringByKeyOrDefault( null ) ) ;

        var document = uiDocument.Document ;
        var elm = InsertPassPointElement( document, pickInfo ) ;
        var route = pickInfo.SubRoute.Route ;
        var routeRecords = GetRelatedBranchSegments( route ) ;
        return routeRecords.Concat( GetNewSegmentList( pickInfo.SubRoute, pickInfo.Element, elm ).ToSegmentsWithName( route.RouteName ) ).EnumerateAll() ;
      } ) ;

      foreach ( var record in segments ) {
        yield return record ;
      }
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
      return document.AddPassPoint( pickInfo.Route.RouteName, pickInfo.Position, pickInfo.RouteDirection, pickInfo.Radius ) ;
    }

    private static IEnumerable<RouteSegment> GetNewSegmentList( SubRoute subRoute, Element insertingElement, Instance passPointElement )
    {
      var detector = new RouteSegmentDetector( subRoute, insertingElement ) ;
      var passPoint = new PassPointEndPoint( passPointElement ) ;
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( detector.IsPassingThrough( segment ) ) {
          // split segment
          var diameter = segment.GetRealNominalDiameter() ?? segment.PreferredNominalDiameter ;
          var isRoutingOnPipeSpace = segment.IsRoutingOnPipeSpace ;
          var fixeBopHeight = segment.FixedBopHeight ;
          var avoidType = segment.AvoidType ;
          yield return new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPoint, diameter, isRoutingOnPipeSpace, fixeBopHeight, avoidType ) ;
          yield return new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, passPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fixeBopHeight, avoidType ) ;
        }
        else {
          yield return segment ;
        }
      }
    }
  }
}