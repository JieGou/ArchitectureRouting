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
      
      // Get start point of route
      XYZ? startPosition = null ;
      foreach ( var (_, segment) in segments ) {
        try {
          startPosition = segment.FromEndPoint.RoutingStartPosition ;
          break ;
        }
        catch {
          // Todo something
        }
      }
      if ( startPosition == null ) return segments ;

      var tees = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_DuctFitting ).Where( tee => tee.Symbol.FamilyName == "022_丸型 T 型" ) ;
      var teesOnSelectedRoute = RemoveTeeOutsideOfSegments(tees.ToList(), segments.ToList()) ;
      var newRouteSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      Dictionary<string, List<PassPointEndPoint>> passPointOnRoutes = new() ;
      foreach ( var tee in teesOnSelectedRoute ) {
        var behindTeeConnector = tee.GetConnectors().Where( conn => conn.Id == (int)TeeConnectorType.Connector1 || conn.Id == (int)TeeConnectorType.Connector2 ).MaxItemOrDefault( conn => ( Vector2d.Distance( conn.Origin.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ) ) ) ;
        if ( behindTeeConnector == null ) continue ;
        var passPointDir = tee.HandOrientation ;
        var teeRouteName = tee.GetRouteName() ;
        if(teeRouteName == null) continue;
        var teeSegment = segments.FirstOrDefault( segment => segment.RouteName == teeRouteName ).Segment ;
        var passPoint = document.AddPassPoint( teeRouteName, behindTeeConnector.Origin, passPointDir, teeSegment.PreferredNominalDiameter/2, teeSegment.FromEndPoint.GetLevelId( document ) ) ;
        var passPointEndPoint = new PassPointEndPoint( passPoint ) ;
        if ( passPointOnRoutes.ContainsKey( teeRouteName ) ) {
          passPointOnRoutes[ teeRouteName ].Add( passPointEndPoint ) ;
        }
        else {
          passPointOnRoutes.Add( teeRouteName, new List<PassPointEndPoint>() { passPointEndPoint } ) ;
        }
      }

      // Test Get spaceの給気風量設定値
      foreach ( var (routeName, segment) in segments ) {
        var toEndPointConnector = segment.ToEndPoint.GetReferenceConnector() ;
        var space = GetSpaceFromVavConnector( document, toEndPointConnector!, spaces ) as Space ;
        var spaceSpecifiedSupplyAirflow = UnitUtils.ConvertFromInternalUnits( space!.DesignSupplyAirflow, UnitTypeId.CubicMeters ) ;
      }

      // Get list of new segments
      foreach ( var (routeName, passPoints) in passPointOnRoutes ) {
        var segment = segments.FirstOrDefault( segment => segment.RouteName == routeName ).Segment ;
        if(segment == null) continue;
        newRouteSegments = removeSegmentByRouteName( routeName, segments ).ToList() ;

        if ( passPoints.Count() > 1 ) {
          var secondFromEndPoints = passPoints.ToList() ;
          var secondToEndPoints = secondFromEndPoints.Skip( 1 ).Append( segment.ToEndPoint ) ;
          var firstToEndPoint = secondFromEndPoints[ 0 ] ;
        
          newRouteSegments.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, firstToEndPoint, segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ) ) ;
          newRouteSegments.AddRange( secondFromEndPoints.Zip( secondToEndPoints, ( f, t ) =>
          {
            var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, f, t, segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
            return ( routeName, newSegment ) ;
          } ) ) ;             
        }
        else {
          var beforeSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPoints.First(), segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
          var afterSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, passPoints.First(), segment.ToEndPoint, segment.PreferredNominalDiameter / 2, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
          newRouteSegments.Add( ( routeName, beforeSegment ) ) ;
          newRouteSegments.Add( ( routeName, afterSegment ) ) ;             
        }

        // Test one case
        break;
      }
      
      return newRouteSegments ;
    }

    private static IEnumerable<FamilyInstance> RemoveTeeOutsideOfSegments( IEnumerable<FamilyInstance> tees, List<(string, RouteSegment)> segments )
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