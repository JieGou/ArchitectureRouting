using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
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
    private static readonly Dictionary<double, double> DiameterLinkWithAirflow = new()
    {
      { 150, 195 },
      { 200, 420 },
      { 250, 765 },
      { 300, 1240 },
      { 350, 1870 },
      { 400, 2670 },
      { 450, 3650 },
      { 500, 4820 },
      { 550, 6200 },
      { 600, 7800 },
      { 650, 9600 },
      { 700, 1170 }
    } ;

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
      var rootConnector = segments.First().Segment.FromEndPoint.GetReferenceConnector() ;
      var mainRouteName = segments.First().RouteName ;
      XYZ? startPosition = null ;
      foreach ( var (routeName, segment) in segments ) {
        try {
          startPosition = segment.FromEndPoint.RoutingStartPosition ;
          rootConnector = segment.FromEndPoint.GetReferenceConnector() ;
          mainRouteName = routeName ;
          break ;
        }
        catch {
          // Todo something
        }
      }

      if ( startPosition == null || rootConnector == null ) return segments ;

      // Edit sub route index
      var newRouteSegments = EditSubRouteIndex( document, segments.ToList(), mainRouteName, rootConnector ) ;

      // Edit diameter for grand child route
      // newRouteSegments = EditGrandChildDiameter( document, newRouteSegments.ToList(), spaces.ToList() ) ;

      // Add pass point into selected route
      var passPointOnRoutes = AddPassPoint( document, newRouteSegments, startPosition, mainRouteName, spaces.ToList() ) ;

      // Get list of new segments
      newRouteSegments = GetListOfNewSegments( document, segments, newRouteSegments, spaces.ToList(), passPointOnRoutes, startPosition ) ;

      return newRouteSegments ;
    }

    private static List<(string RouteName, RouteSegment Segment)> EditSubRouteIndex( Document document, List<(string, RouteSegment)> segments, string mainRouteName, Connector rootConnector )
    {
      segments.Sort( ( a, b ) => CompareDistanceBasisZ( mainRouteName, rootConnector, a, b ) ) ;
      var newRouteSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      Dictionary<string, int> subRouteIndexes = new() ;
      foreach ( var (routeName, segment) in segments ) {
        var branchEndPoint = segment.FromEndPoint ;
        if ( segment.FromEndPoint is RouteEndPoint routeEndPoint ) {
          if ( ! subRouteIndexes.ContainsKey( routeEndPoint.RouteName ) ) {
            subRouteIndexes.Add( routeEndPoint.RouteName, 0 ) ;
          }

          branchEndPoint = new RouteEndPoint( document, routeEndPoint.RouteName, subRouteIndexes[ routeEndPoint.RouteName ]++ ) ;
        }

        var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
        newRouteSegments.Add( ( routeName, newSegment ) ) ;
      }

      return newRouteSegments ;
    }

    private static List<(string RouteName, RouteSegment Segment)> EditGrandChildDiameter( Document document, List<(string, RouteSegment)> segments, IReadOnlyCollection<Element> spaces )
    {
      var newRouteSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      foreach ( var (routeName, segment) in segments ) {
        var ductDiameter = segment.PreferredNominalDiameter ;
        var space = GetSpaceFromVavConnector( document, segment.ToEndPoint.GetReferenceConnector()!, spaces ) as Space ;
        var spaceSpecifiedSupplyAirflow = space is null ? 0 : UnitUtils.ConvertFromInternalUnits( space.DesignSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;
        if ( segment.ToEndPoint is ConnectorEndPoint ) {
          ductDiameter = ConvertAirflowToDiameter( spaceSpecifiedSupplyAirflow ).MillimetersToRevitUnits() ;
        }

        var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, ductDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
        newRouteSegments.Add( ( routeName, newSegment ) ) ;
      }

      return newRouteSegments ;
    }

    private static Dictionary<string, List<PassPointEndPoint>> AddPassPoint( Document document, List<(string RouteName, RouteSegment Segment)> newRouteSegments, XYZ startPosition, string mainRouteName, List<Element> spaces )
    {
      var tees = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_DuctFitting ).Where( tee => tee.Symbol.FamilyName == "022_丸型 T 型" ) ;
      var teesOnSelectedRoute = RemoveTeeOutsideOfSegments( tees.ToList(), newRouteSegments ) ;
      Dictionary<string, List<PassPointEndPoint>> passPointOnRoutes = new() ;
      foreach ( var tee in teesOnSelectedRoute ) {
        var behindTeeConnector = tee.GetConnectors().Where( conn => conn.Id == (int)TeeConnectorType.Connector1 || conn.Id == (int)TeeConnectorType.Connector2 ).MaxItemOrDefault( conn => ( Vector2d.Distance( conn.Origin.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ) ) ) ;
        if ( behindTeeConnector == null ) continue ;
        var passPointDir = behindTeeConnector.CoordinateSystem.BasisZ ;
        var teeRouteName = tee.GetRouteName() ;
        if ( teeRouteName == null ) continue ;
        var teeSegment = newRouteSegments.FirstOrDefault( segment => segment.RouteName == teeRouteName ).Segment ;

        // Move pass point's position
        const double offset = 100 ;
        var passPointPosition = behindTeeConnector.Origin + passPointDir * offset.MillimetersToRevitUnits() ;
        var passPoint = document.AddPassPoint( teeRouteName, passPointPosition, passPointDir, teeSegment.PreferredNominalDiameter / 2, teeSegment.FromEndPoint.GetLevelId( document ) ) ;
        var passPointEndPoint = new PassPointEndPoint( passPoint ) ;
        if ( passPointOnRoutes.ContainsKey( teeRouteName ) ) {
          passPointOnRoutes[ teeRouteName ].Add( passPointEndPoint ) ;
        }
        else {
          passPointOnRoutes.Add( teeRouteName, new List<PassPointEndPoint>() { passPointEndPoint } ) ;
        }
      }

      return passPointOnRoutes ;
    }

    private static Dictionary<string, List<double>> GetAllPassPointDiameter( Document document, Dictionary<string, List<PassPointEndPoint>> passPointOnRoutes, XYZ startPosition, IReadOnlyCollection<Element> spaces )
    {
      var passPointDiameters = new Dictionary<string, List<double>>() ;
      foreach ( var (routeName, passPoints) in passPointOnRoutes ) {
        // Get all branch route
        var routeCache = RouteCache.Get( document ) ;
        if ( false == routeCache.TryGetValue( routeName, out var parentRoute ) ) continue ;
        var childBranches = parentRoute.GetChildBranches().ToList() ;
        childBranches.Sort( ( a, b ) => CompareByDistanceFromEndPoint( startPosition, a, b ) ) ;
        var spaceParent = GetSpaceFromVavConnector( document, parentRoute.FirstToConnector()!.GetConnector()!, spaces ) as Space ;
        var spaceParentSpecifiedSupplyAirflow = spaceParent is null ? 0 : UnitUtils.ConvertFromInternalUnits( spaceParent.DesignSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;
        foreach ( var route in childBranches ) {
          var space = GetSpaceFromVavConnector( document, route.FirstToConnector()!.GetConnector()!, spaces ) as Space ;
          var spaceSpecifiedSupplyAirflow = space is null ? 0 : UnitUtils.ConvertFromInternalUnits( space.DesignSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;
          var sumSpecifiedSupplyAirflow = spaceSpecifiedSupplyAirflow + spaceParentSpecifiedSupplyAirflow ;
          var ductDiameter = ConvertAirflowToDiameter( sumSpecifiedSupplyAirflow ).MillimetersToRevitUnits() ;
          if ( passPointDiameters.ContainsKey( routeName ) ) {
            passPointDiameters[ routeName ].Add( ductDiameter ) ;
          }
          else {
            passPointDiameters.Add( routeName, new List<double>() { ductDiameter } ) ;
          }
        }
      }

      return passPointDiameters ;
    }

    private static List<(string RouteName, RouteSegment Segment)> GetListOfNewSegments( Document document, IReadOnlyCollection<(string RouteName, RouteSegment Segment)> segments, List<(string, RouteSegment)> newRouteSegments, IReadOnlyCollection<Element> spaces, Dictionary<string, List<PassPointEndPoint>> passPointOnRoutes, XYZ startPosition )
    {
      var passPointDiameters = GetAllPassPointDiameter( document, passPointOnRoutes, startPosition, spaces ) ;
      foreach ( var (routeName, passPoints) in passPointOnRoutes ) {
        var segment = segments.FirstOrDefault( segment => segment.RouteName == routeName ).Segment ;
        if ( segment == null ) continue ;
        var ductDiameter = segment.PreferredNominalDiameter ;
        var space = GetSpaceFromVavConnector( document, segment.ToEndPoint.GetReferenceConnector()!, spaces ) as Space ;
        var spaceSpecifiedSupplyAirflow = space is null ? 0 : UnitUtils.ConvertFromInternalUnits( space.DesignSupplyAirflow, UnitTypeId.CubicMetersPerHour ) ;

        newRouteSegments = RemoveSegmentByRouteName( routeName, newRouteSegments ).ToList() ;
        if ( passPoints.Count() > 1 ) {
          passPoints.Sort( ( a, b ) => CompareDistance( startPosition, a, b ) ) ;
          var secondFromEndPoints = passPoints.ToList() ;
          var secondToEndPoints = secondFromEndPoints.Skip( 1 ).Append( segment.ToEndPoint ) ;
          var firstToEndPoint = secondFromEndPoints[ 0 ] ;

          newRouteSegments.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, firstToEndPoint, segment.PreferredNominalDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ) ) ;
          newRouteSegments.AddRange( secondFromEndPoints.Zip( secondToEndPoints, ( f, t ) =>
          {
            if ( t == segment.ToEndPoint ) {
              ductDiameter = ConvertAirflowToDiameter( spaceSpecifiedSupplyAirflow ).MillimetersToRevitUnits() ;
            }

            var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, f, t, ductDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
            return ( routeName, newSegment ) ;
          } ) ) ;
        }
        else {
          ductDiameter = ConvertAirflowToDiameter( spaceSpecifiedSupplyAirflow ).MillimetersToRevitUnits() ;
          var beforeSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPoints.First(), ductDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
          var afterSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, passPoints.First(), segment.ToEndPoint, ductDiameter, false, segment.FromFixedHeight, segment.FromFixedHeight, segment.AvoidType, ElementId.InvalidElementId ) ;
          newRouteSegments.Add( ( routeName, beforeSegment ) ) ;
          newRouteSegments.Add( ( routeName, afterSegment ) ) ;
        }
      }

      return newRouteSegments ;
    }

    /// <summary>
    /// Convert airflow to duct diameter
    /// </summary>
    /// <param name="airflow">Unit: CubicMetersPerHour</param>
    private static double ConvertAirflowToDiameter( double airflow )
    {
      // TODO : 仮対応、風量が11700m3/h以上の場合はルート径が700にします。
      var ductDiameter = DiameterLinkWithAirflow.Keys.Last() ;
      foreach ( var (diameterType, airflowType) in DiameterLinkWithAirflow ) {
        if ( airflowType - airflow < 0 ) continue ;
        ductDiameter = diameterType ;
        break ;
      }

      return ductDiameter ;
    }

    private static int CompareDistanceBasisZ( string mainRouteName, Connector rootConnector, (string, RouteSegment) a, (string, RouteSegment) b )
    {
      var (aRouteName, aRouteSegment) = a ;
      var (bRouteName, bRouteSegment) = b ;
      var aConnector = aRouteSegment.ToEndPoint.GetReferenceConnector() ;
      var bConnector = bRouteSegment.ToEndPoint.GetReferenceConnector() ;
      if ( aConnector == null || bConnector == null || aRouteName == mainRouteName || bRouteName == mainRouteName || aRouteName == bRouteName ) return default ;
      if ( aRouteSegment.FromEndPoint.ParentRoute()!.RouteName == mainRouteName && bRouteSegment.FromEndPoint.ParentRoute()!.RouteName == mainRouteName ) {
        return DistanceFromRoot( rootConnector, aConnector, false ).CompareTo( DistanceFromRoot( rootConnector, bConnector, false ) ) ;
      }
      else if ( aRouteSegment.FromEndPoint.ParentRoute() != null && aRouteSegment.FromEndPoint.ParentRoute()!.RouteName != mainRouteName && aRouteSegment.FromEndPoint.ParentRoute() != null && bRouteSegment.FromEndPoint.ParentRoute()!.RouteName != mainRouteName ) {
        return DistanceFromRoot( rootConnector, aConnector, true ).CompareTo( DistanceFromRoot( rootConnector, bConnector, true ) ) ;
      }
      else {
        return default ;
      }
    }

    private static double DistanceFromRoot( IConnector rootConnector, IConnector targetConnector, bool isRotate90 )
    {
      var rootConnectorPosXyz = rootConnector.Origin ;
      var rootConnectorPos2d = rootConnectorPosXyz.To3dPoint().To2d() ;
      var targetConnectorPos = targetConnector.Origin ;
      var targetConnector2d = targetConnectorPos.To3dPoint().To2d() ;

      var rootConnectorBasisZ = rootConnector.CoordinateSystem.BasisZ.To3dPoint().To2d() ;
      var calculateDir = isRotate90 ? new Vector2d( -rootConnectorBasisZ.y, rootConnectorBasisZ.x ) : rootConnectorBasisZ ;
      var rootToVavVector = targetConnector2d - rootConnectorPos2d ;
      var angle = GetAngleBetweenVector( calculateDir, rootToVavVector ) ;

      return Math.Abs( Math.Cos( angle ) * rootToVavVector.magnitude ) ;
    }

    // Get the angle between two vectors
    private static double GetAngleBetweenVector( Vector2d rootVec, Vector2d otherVector )
    {
      // return the angle (in radian)
      return Math.Acos( Vector2d.Dot( rootVec, otherVector ) / ( rootVec.magnitude * otherVector.magnitude ) ) ;
    }

    private static int CompareByDistanceFromEndPoint( XYZ startPosition, Route a, Route b )
    {
      if ( a.FirstFromConnector() == null || b.FirstFromConnector() == null ) return default ;

      return Vector2d.Distance( b.FirstFromConnector()!.GetConnector()!.Origin.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ).CompareTo( Vector2d.Distance( a.FirstFromConnector()!.GetConnector()!.Origin.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ) ) ;
    }

    private static int CompareDistance( XYZ startPosition, PassPointEndPoint a, PassPointEndPoint b )
    {
      if ( a.GetPassPoint()!.Location is not LocationPoint aPos || b.GetPassPoint()!.Location is not LocationPoint bPos ) return default ;

      return Vector2d.Distance( aPos.Point.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ).CompareTo( Vector2d.Distance( bPos.Point.To3dPoint().To2d(), startPosition.To3dPoint().To2d() ) ) ;
    }

    private static IEnumerable<FamilyInstance> RemoveTeeOutsideOfSegments( IEnumerable<FamilyInstance> tees, List<(string, RouteSegment)> segments )
    {
      List<FamilyInstance> resultTees = new() ;
      foreach ( var tee in tees ) {
        foreach ( var (routeName, segment) in segments ) {
          if ( tee.GetNearestEndPoints( false ).First().Key == segment.ToEndPoint.Key ) {
            resultTees.Add( tee ) ;
          }
        }
      }

      return resultTees ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> RemoveSegmentByRouteName( string removeRouteName, IEnumerable<(string RouteName, RouteSegment Segment)> segments )
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