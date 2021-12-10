using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;
using MathLib ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AdjustDuctSizeCommand", DefaultString = "Adjust\nDuctSize" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AdjustDuctSizeCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickAndAdjustDuctSize" ;

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      return ( true, SelectRoutes( uiDocument ) ) ;
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      return new[] { pickInfo.Route } ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var routes = state as IReadOnlyCollection<Route> ?? throw new InvalidOperationException() ;
      RouteGenerator.CorrectEnvelopes( document ) ;

      var spaces = GetAllSpaces( document ) ;
      var segments = Route.GetAllRelatedBranches( routes ).ToSegmentsWithName().EnumerateAll() ;
      var mainSegment = segments.First().Segment ;
      
      // Get start point of route
      XYZ? startPosition = null ;
      foreach ( var (_, segment) in segments ) {
        try {
          startPosition = segment.FromEndPoint.RoutingStartPosition ;
          mainSegment = segment ;
          break ;
        }
        catch {
          // Todo something
        }
      }
      if ( startPosition == null ) return segments ;

      var tees = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_DuctFitting ).Where( tee => tee.Symbol.FamilyName == "022_丸型 T 型" ) ;
      var teesOnSelectedRoute = RemoveTeeOutsideRoute(tees.ToList(), segments.ToList()) ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var count = 0 ;
      Dictionary<string, List<FamilyInstance>> passPointOnRoutes = new() ;
      foreach ( var tee in tees ) {
        foreach ( var (routeName, segment) in segments ) {
          var behindTeeConnector = tee.GetConnectors().Where( conn => conn.Id == (int)TeeConnectorType.Connector1 || conn.Id == 2 ).MaxItemOrDefault( conn => ( Vector2d.Distance( conn.Origin.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ) ) ) ;
          if ( behindTeeConnector == null ) continue ;
          if ( tee.GetNearestEndPoints( false ).First().Key == segment.ToEndPoint.Key ) {
            var passPointDir = tee.HandOrientation ;
            var passPoint = document.AddPassPoint( routeName, behindTeeConnector.Origin, passPointDir, segment.PreferredNominalDiameter/2, segment.FromEndPoint.GetLevelId( document ) ) ;
            
            if ( passPointOnRoutes.ContainsKey( routeName ) ) {
              passPointOnRoutes[ routeName ].Add( passPoint ) ;
            }
            else {
              passPointOnRoutes.Add( routeName, new List<FamilyInstance>() { passPoint } ) ;
            }            
            
            var passPointEndPoint = new PassPointEndPoint( passPoint ) ;
            
            if ( segment == mainSegment && count == 0 ) {
              var beforeSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPointEndPoint, segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
              var afterSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, passPointEndPoint, segment.ToEndPoint, segment.PreferredNominalDiameter / 2, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;              
              result.Add( ( routeName, beforeSegment ) ) ;
              result.Add( ( routeName, afterSegment ) ) ;
              count++ ;
            }
            else {
              result.Add( ( routeName, segment ) ) ;
            }

            break ;
          }
        }
      }

      // Get spaceの給気風量設定値
      foreach ( var (routeName, segment) in segments ) {
        var toEndPointConnector = segment.ToEndPoint.GetReferenceConnector() ;
        var space = GetSpaceFromVavConnector( document, toEndPointConnector!, spaces ) as Space ;
        var spaceSpecifiedSupplyAirflow = UnitUtils.ConvertFromInternalUnits( space!.DesignSupplyAirflow, UnitTypeId.CubicMeters ) ;
      }

      // Get list of new segments
      // IList<(string, RouteSegment )> newRouteSegments = new List<(string, RouteSegment)>() ;
      // foreach ( var (routeName, passPoints) in passPointOnRoutes ) {
      //   var segment = segments.FirstOrDefault( segment => segment.RouteName == routeName ).Segment ;
      //   result = removeSegmentByRouteName( routeName, segments ).ToList() ;
      //   var index = 0 ;
      //   foreach ( var passPoint in passPoints ) {
      //     var passPointEndPoint = new PassPointEndPoint( passPoint ) ;
      //     var routeSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPointEndPoint, segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
      //     var subRouteName = routeName + "_" + index ;
      //     result.Add( ( subRouteName, routeSegment ) ) ;
      //     index++ ;
      //   }
      // }

      // return result ;
      return segments ;
    }

    private static IEnumerable<FamilyInstance> RemoveTeeOutsideRoute( IEnumerable<FamilyInstance> tees, List<(string, RouteSegment)> segments )
    {
      List<FamilyInstance> resultTees = new() ;
      foreach ( var tee in tees ) {
        foreach ( var (routeName, segment) in segments ) {
          if ( tee.GetNearestEndPoints( false ).First().Key == segment.ToEndPoint.Key ) {
            resultTees.Add( tee );
          }
        }
      }

      return resultTees ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> removeSegmentByRouteName( string removeRouteName, IReadOnlyCollection<(string RouteName, RouteSegment Segment)> segments )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      foreach ( var (routeName, segment) in segments ) {
        if ( routeName != removeRouteName ) {
          result.Add( ( routeName, segment ) ) ;
        }
      }

      return result ;
    }
    
    /// <summary>
    /// Get one Vav from one space
    /// </summary>
    private static Element GetSpaceFromVavConnector( Document doc, Connector vavConnector, IEnumerable<Element> spaces )
    {
      
      foreach ( var space in spaces ) {
        BoundingBoxXYZ spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
        if ( vavConnector == null || ( ! IsInSpace( spaceBox, vavConnector.Origin ) ) ) continue ;
        return space ;
      }

      return null! ;
    }

    private static bool IsInSpace( BoundingBoxXYZ spaceBox, XYZ vavConnectorPosition )
    {
      return spaceBox.ToBox3d().Contains( vavConnectorPosition.To3dPoint(), 0.0 ) ;
    }    
    
    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }
    
    private enum TeeConnectorType
    {
      Connector1 = 1,
      Connector2 = 2
    }
  }
}