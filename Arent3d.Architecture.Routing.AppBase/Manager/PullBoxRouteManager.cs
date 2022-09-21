using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class PullBoxRouteManager
  {
    private static readonly double DefaultDistanceHeight = 300d.MillimetersToRevitUnits() ;
    private const string DefaultConstructionItem = "未設定" ;
    private const double NearShaftTolerance = 0.01 ;
    private static readonly double PullBoxWidth = 300d.MillimetersToRevitUnits() ;
    private static readonly double PullBoxLenght = 250d.MillimetersToRevitUnits() ;
    public static readonly double NotationOfPullBoxXAxis = 715d.MillimetersToRevitUnits() ;
    public static readonly double NotationOfPullBoxYAxis = 200d.MillimetersToRevitUnits() ;
    private const string HinmeiOfPullBox = "プルボックス" ;
    public const string DefaultPullBoxLabel = "PB" ;
    public const string MaterialCodeParameter = "Material Code" ;
    private const string DepthParameter = "Depth" ;
    private const string ScaleFactorParameter = "ScaleFactor" ;
    private const double DefaultDepthOfPullBox = 200d ;
    public const string IsAutoCalculatePullBoxSizeParameter = "IsAutoCalculatePullBoxSize" ;
    private const string TaniOfPullBox = "個" ;

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, Route route, Element element, FamilyInstance pullBox, double heightConnector, 
      double heightWire, XYZ routeDirection, bool isCreatePullBoxWithoutSettingHeight, string nameBase, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, XYZ? fromDirection = null, XYZ? toDirection = null, FixedHeight? firstHeight = null, bool isWireEnteredShaft = false, bool allowedTiltedPiping = false )
    {
      var index = 1 ;
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var ( routeRecords, parentRoute ) = GetRelatedBranchSegments( route ) ;
      if ( parentRoute.RouteName == route.RouteName ) {
        foreach ( var routeRecord in GetBranchSegmentsByRepresentativeName( document, routes, parentRoute ) ) {
          if ( routeRecords.Any( rc => rc.RouteName == routeRecord.RouteName ) ) continue ;
          routeRecords.Add( routeRecord );
        }
      }
      var subRoute = route.SubRoutes.First() ;

      var detector = new RouteSegmentDetector( subRoute, element ) ;
      var diameter = parentRoute.UniqueDiameter ;
      var radius = diameter * 0.5 ;
      var isRoutingOnPipeSpace = parentRoute.UniqueIsRoutingOnPipeSpace ?? false ;
      var avoidType = parentRoute.UniqueAvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = parentRoute.UniqueShaftElementUniqueId ;
      var fromFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, isCreatePullBoxWithoutSettingHeight ? heightConnector : heightConnector + DefaultDistanceHeight ) ;
      var toFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightConnector ) ;
      var fromFixedHeightSecond = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightWire ) ;
      var toFixedHeightSecond = parentRoute.UniqueToFixedHeight ;
      
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var (pullBoxFromEndPoint, pullBoxToEndPoint) = GetFromAndToConnectorEndPoint( document, pullBox, isCreatePullBoxWithoutSettingHeight, radius, routeDirection, fromDirection, toDirection ) ;

      var isBeforeSegment = true ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var ceedCodes = new List<string>() ;
      var isPassingThrough = parentRoute.RouteSegments.FirstOrDefault( s => detector.IsPassingThrough( s ) ) != null ;
      var beforeSegments = new List<RouteSegment>() ;
      if ( isPassingThrough ) {
        // Increase index in duplicated case
        name = IndexRouteName( routes, parentRoute.RouteName, ref parentIndex ) ;
        parentIndex++ ;
        foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
          if ( detector.IsPassingThrough( segment ) ) {
            isBeforeSegment = false ;
            firstHeight ??= fromFixedHeightFirst ;
            var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, firstHeight, toFixedHeightFirst, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ;
            result.Add( ( parentRoute.RouteName, newSegment ) ) ;
            beforeSegments.Add( newSegment ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, isWireEnteredShaft ? null : shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
          }
          else {
            if ( ! isBeforeSegment ) {
              if ( segment.FromEndPoint.TypeName == PassPointEndPoint.Type ) {
                var passPoint = document.GetElement( segment.FromEndPoint.Key.GetElementUniqueId() ) ;
                passPoint.TrySetProperty( RoutingParameter.RouteName, name ) ;
              }
              
              if ( segment.ToEndPoint.TypeName == PassPointEndPoint.Type ) {
                var passPoint = document.GetElement( segment.ToEndPoint.Key.GetElementUniqueId() ) ;
                passPoint.TrySetProperty( RoutingParameter.RouteName, name ) ;
              }
            }

            var newSegment = isBeforeSegment ? new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeightFirst, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping )
              : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ;
            result.Add( isBeforeSegment ? ( parentRoute.RouteName, newSegment ) : ( name, newSegment ) ) ;
            if ( isBeforeSegment ) beforeSegments.Add( newSegment ) ;
          }
        }
        
        var connectorUniqueId = parentRoute.RouteSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
        GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;

        if ( ! routeRecords.Any() ) return result ;
        {
          foreach ( var (routeName, segment) in routeRecords ) {
            var routeNameArray = routeName.Split( '_' ) ;
            var mainRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
            var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
            if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
              var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? pullBoxToEndPoint.Key ;
              var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
              if ( fromEndPointKey == pullBoxToEndPoint.Key ) {
                // Increase index in duplicated case
                name = IndexRouteName( routes, routeName, ref index ) ;
                result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
                result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
                var pullBoxes = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e is FamilyInstance && e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ).EnumerateAll() ;
                if ( pullBoxes.Any( p => p.UniqueId == segment.ToEndPoint.Key.GetElementUniqueId() ) )
                  result.AddRange( GetRouteSegmentsForBranchRoutesContainingPullBoxes( document, routeName, segment, name, routes, mainRouteName, pullBoxes, ref index ) ) ;

                connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
                GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
                if ( parentAndChildRoute.ContainsKey( parentRoute.RouteName ) )
                  parentAndChildRoute[ parentRoute.RouteName ].Add( routeName ) ;
                else
                  parentAndChildRoute.Add( parentRoute.RouteName, new List<string> { routeName } ) ;
              }
              else
                result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
            }
            else
              result.Add( ( routeName, segment ) ) ;
          }
        }
      }
      else {
        result = GetSegments( document, routeRecords, ceedCodes, pullBox, parentRoute, detector, ref parentIndex, ref parentAndChildRoute, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeightFirst, 
          fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, radius, isCreatePullBoxWithoutSettingHeight, pullBoxFromEndPoint, pullBoxToEndPoint, allowedTiltedPiping ) ;
      }

      if ( ceedCodes.Any() ) {
        var newCeedCode = string.Join( ";", ceedCodes ) ;
        pullBox.TrySetProperty( ElectricalRoutingElementParameter.CeedCode, newCeedCode ) ;
      }

      return result ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsForBranchRoutesContainingPullBoxes( Document document, string routeName, RouteSegment segment, string name, RouteCache routes, string mainRouteName, IReadOnlyCollection<Element> pullBoxes, ref int index )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;

      // If segment contains no conduits, pull box is created at the conduit fitting after pass point branch route (renaming segments after this pull box is unnecessary)
      var conduitsOfSegment = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c =>
      {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == routeName ;
      } ).Where( conduit =>
      {
        var toEndPoints = conduit.GetNearestEndPoints( false ) ;
        var toEndPointUniqueId = toEndPoints.FirstOrDefault()?.Key.GetElementUniqueId() ;
        var fromEndPoints = conduit.GetNearestEndPoints( true ).ToList() ;
        var fromEndPointUniqueId = fromEndPoints.FirstOrDefault()?.Key.GetElementUniqueId() ;
        return toEndPointUniqueId == segment.ToEndPoint.Key.GetElementUniqueId() && fromEndPointUniqueId == segment.FromEndPoint.Key.GetElementUniqueId() ;
      } ) ;
      if ( ! conduitsOfSegment.Any() ) return result ;

      // Increase index in duplicated case
      name = IndexRouteName( routes, name, ref index ) ;
      var allRouteSegments = routes.Where( r =>
      {
        var rNameArray = r.Key.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == mainRouteName ;
      } ).SelectMany( r => r.Value.RouteSegments ).ToList() ;
      var routeSegment = allRouteSegments.Single( rs => rs.FromEndPoint.Key.GetElementUniqueId() == segment.ToEndPoint.Key.GetElementUniqueId() ) ;
      result.Add( ( name, routeSegment ) ) ;
      while ( pullBoxes.Any( p => p.UniqueId == routeSegment.ToEndPoint.Key.GetElementUniqueId() ) ) {
        // Increase index in duplicated case
        name = IndexRouteName( routes, name, ref index ) ;
        var rSegment = allRouteSegments.Single( rs => rs.FromEndPoint.Key.GetElementUniqueId() == routeSegment.ToEndPoint.Key.GetElementUniqueId() ) ;
        result.Add( ( name, rSegment ) ) ;
        routeSegment = rSegment ;
      }

      return result ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsThroughShaft( Document document, Route route, FamilyInstance pullBox, double heightConnector, double heightWire,
      XYZ routeDirection, bool isCreatePullBoxWithoutSettingHeight, string nameBase, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, XYZ? fromDirection = null, XYZ? toDirection = null,
      FixedHeight? firstHeight = null, bool isWireEnteredShaft = true )
    {
      const int index = 1 ;
      var (routeRecords, parentRoute) = GetRelatedBranchSegments( route ) ;

      var diameter = parentRoute.UniqueDiameter ;
      var radius = diameter * 0.5 ;
      var isRoutingOnPipeSpace = parentRoute.UniqueIsRoutingOnPipeSpace ?? false ;
      var avoidType = parentRoute.UniqueAvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = parentRoute.UniqueShaftElementUniqueId ;
      var fromFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, isCreatePullBoxWithoutSettingHeight ? heightConnector : heightConnector + DefaultDistanceHeight ) ;
      var toFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightConnector ) ;
      var fromFixedHeightSecond = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightWire ) ;
      var toFixedHeightSecond = parentRoute.UniqueToFixedHeight ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var (pullBoxFromEndPoint, pullBoxToEndPoint) = GetFromAndToConnectorEndPoint( document, pullBox, isCreatePullBoxWithoutSettingHeight, radius, routeDirection, fromDirection, toDirection ) ;

      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var ceedCodes = new List<string>() ;
      var beforeSegments = new List<RouteSegment>() ;
      name = parentRoute.RouteName + "_" + parentIndex ;
      parentIndex++ ;
      foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
        firstHeight ??= fromFixedHeightFirst ;
        var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType,
          segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, firstHeight, toFixedHeightFirst,
          avoidType, shaftElementUniqueId ) ;
        result.Add( ( parentRoute.RouteName, newSegment ) ) ;
        beforeSegments.Add( newSegment ) ;
        result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint,
            segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, isWireEnteredShaft ? shaftElementUniqueId : null ) ) ) ;
      }

      var connectorUniqueId = parentRoute.RouteSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
      GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;

      if ( ! routeRecords.Any() ) return result ;
      {
        foreach ( var (routeName, segment) in routeRecords ) {
          var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
          if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
            var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? pullBoxToEndPoint.Key ;
            var branchEndPoint =
              new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
            if ( fromEndPointKey == pullBoxToEndPoint.Key ) {
              name = routeName + "_" + index ;
              result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, 
                isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId ) ) ) ;
              connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
              GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
              if ( parentAndChildRoute.ContainsKey( parentRoute.RouteName ) )
                parentAndChildRoute[ parentRoute.RouteName ].Add( routeName ) ;
              else
                parentAndChildRoute.Add( parentRoute.RouteName, new List<string> { routeName } ) ;
            }
            else {
              result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, 
                fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId ) ) ) ;
            }
          }
          else {
            result.Add( ( routeName, segment ) ) ;
          }
        }
      }

      if ( ! ceedCodes.Any() ) return result ;
      var newCeedCode = string.Join( ";", ceedCodes ) ;
      pullBox.TrySetProperty( ElectricalRoutingElementParameter.CeedCode, newCeedCode ) ;

      return result ;
    }

    private static List<FamilyInstance> GetPullBoxesInShafts( Document document, Route route,
      Element conduitFittingShaft, IReadOnlyCollection<XYZ> pullBoxPositions, bool isPickedFromBottomToTop )
    {
      var pullBoxesInShaft = new List<FamilyInstance>() ;

      var conduitFittingPoint = ( conduitFittingShaft.Location as LocationPoint )?.Point ;
      if ( conduitFittingPoint == null ) return pullBoxesInShaft ;

      var pullBoxLocationPoint = pullBoxPositions.LastOrDefault() ;
      if ( pullBoxLocationPoint == null ) return pullBoxesInShaft ;

      if ( string.IsNullOrEmpty( route.UniqueShaftElementUniqueId ) ) return pullBoxesInShaft ;

      var shaftElementUniqueId = route.UniqueShaftElementUniqueId ;
      var shaft = document.GetElementById<Opening>( shaftElementUniqueId ?? string.Empty ) ;
      if ( shaft == null ) return pullBoxesInShaft ;
      var shaftLocation = GetShaftLocation( route, document ) ;
      if ( shaftLocation == null ) return pullBoxesInShaft ;

      pullBoxesInShaft = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( c =>
        {
          if ( c.GetConnectorFamilyType() != ConnectorFamilyType.PullBox ) return false ;

          var locationPoint = ( c.Location as LocationPoint )?.Point ;
          if ( locationPoint == null || ! IsNearShaft( locationPoint, shaftLocation ) ||
               pullBoxPositions.Any( p => IsAlmostOrEqual( p.Z, locationPoint.Z ) ) )
            return false ;

          if ( IsAlmostOrEqual( conduitFittingPoint.Z, locationPoint.Z ) ) return false ;

          if ( isPickedFromBottomToTop )
            return pullBoxLocationPoint.Z < locationPoint.Z && locationPoint.Z < conduitFittingPoint.Z ;

          return pullBoxLocationPoint.Z > locationPoint.Z && locationPoint.Z > conduitFittingPoint.Z ;
        } ).ToList() ;

      if ( isPickedFromBottomToTop )
        pullBoxesInShaft = pullBoxesInShaft.OrderBy( p =>
        {
          var locationPoint = ( p.Location as LocationPoint )?.Point ;
          return locationPoint!.Z ;
        } ).ToList() ;
      else
        pullBoxesInShaft = pullBoxesInShaft.OrderByDescending( p =>
        {
          var locationPoint = ( p.Location as LocationPoint )?.Point ;
          return locationPoint!.Z ;
        } ).ToList() ;
      return pullBoxesInShaft ;
    }

    private static List<(string RouteName, RouteSegment Segment)> GetSegments( Document document, List<(string RouteName, RouteSegment Segment)> routeRecords, List<string> ceedCodes, 
      FamilyInstance pullBox, Route parentRoute, RouteSegmentDetector detector, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, double? diameter, bool isRoutingOnPipeSpace, FixedHeight? fromFixedHeightFirst, FixedHeight? toFixedHeightFirst, FixedHeight? fromFixedHeightSecond, 
      FixedHeight? toFixedHeightSecond, AvoidType avoidType, string? shaftElementUniqueId, double? radius, bool isCreatePullBoxWithoutSettingHeight, ConnectorEndPoint pullBoxFromEndPoint, ConnectorEndPoint pullBoxToEndPoint, bool allowedTiltedPiping = false )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      string connectorUniqueId ;
      var isBeforeSegment = true ;
      var index = 1 ;
      // Increase index in duplicated case
      var name = IndexRouteName( routes, parentRoute.RouteName, ref parentIndex ) ;
      var parentSegments = parentRoute.RouteSegments.EnumerateAll().ToList() ;
      var ( routeSegment, routeDirection ) = GetSegmentThroughPullBox( pullBox, parentSegments ) ;
      if ( routeDirection == null ) {
        result.AddRange( from segment in parentSegments select ( parentRoute.RouteName, segment ) ) ;
        connectorUniqueId = parentSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
        GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
        foreach ( var (routeName, segment) in routeRecords ) {
          if ( detector.IsPassingThrough( segment ) ) {
            name = routeName + "_" + index ;
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeightFirst, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
            var pullBoxes = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ElectricalFixtures ).Where( e => e is FamilyInstance && e.Name == ElectricalRoutingFamilyType.PullBox.GetFamilyName() ).ToList() ;
            if ( pullBoxes.Any( p => p.UniqueId == segment.ToEndPoint.Key.GetElementUniqueId() ) ) {
              // Increase index in duplicated case
              name = IndexRouteName( routes, name, ref index ) ;
              var allRouteSegments = routes.SelectMany( r => r.Value.RouteSegments ).ToList() ;
              var firstRouteSegment = allRouteSegments.Single( rs => rs.FromEndPoint.Key.GetElementUniqueId() == segment.ToEndPoint.Key.GetElementUniqueId() ) ;
              result.Add( ( name, firstRouteSegment ) ) ;
              while ( pullBoxes.Any( p => p.UniqueId == firstRouteSegment.ToEndPoint.Key.GetElementUniqueId() ) ) {
                // Increase index in duplicated case
                name = IndexRouteName( routes, name, ref index ) ;
                var nextRouteSegment = allRouteSegments.Single( rs => rs.FromEndPoint.Key.GetElementUniqueId() == firstRouteSegment.ToEndPoint.Key.GetElementUniqueId() ) ;
                result.Add( ( name, nextRouteSegment ) ) ;
                firstRouteSegment = nextRouteSegment ;
              }
            }
          }
          else {
            result.Add( ( routeName, segment ) ) ;
            connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
            GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
          }
        }
        
        return result ;
      }

      var (mainPullBoxFromEndPoint, mainPullBoxToEndPoint) = GetFromAndToConnectorEndPoint( document, pullBox, isCreatePullBoxWithoutSettingHeight, radius, routeDirection, null, null ) ;
      var beforeSegments = new List<RouteSegment>() ;
      foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
        if ( segment == routeSegment ) {
          isBeforeSegment = false ;
          var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, mainPullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeightFirst, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ;
          result.Add( ( name, newSegment ) ) ;
          result.Add( ( parentRoute.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, mainPullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
          beforeSegments.Add( newSegment ) ;
        }
        else {
          if ( isBeforeSegment ) {
            if ( segment.FromEndPoint.TypeName == PassPointEndPoint.Type ) {
              var passPoint = document.GetElement( segment.FromEndPoint.Key.GetElementUniqueId() ) ;
              passPoint.TrySetProperty( RoutingParameter.RouteName, name ) ;
            }

            if ( segment.ToEndPoint.TypeName == PassPointEndPoint.Type ) {
              var passPoint = document.GetElement( segment.ToEndPoint.Key.GetElementUniqueId() ) ;
              passPoint.TrySetProperty( RoutingParameter.RouteName, name ) ;
            }
          }

          var newSegment = isBeforeSegment 
            ? new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeightFirst, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) 
            : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ;
          result.Add( isBeforeSegment ? ( name, newSegment ) : ( parentRoute.RouteName, newSegment ) ) ;
          if ( isBeforeSegment ) beforeSegments.Add( newSegment ) ;
        }
      }
      
      connectorUniqueId = parentSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
      GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
      parentIndex++ ;
      
      foreach ( var (routeName, segment) in routeRecords ) {
        index = 1 ;
        // Increase index in duplicated case
        name = IndexRouteName( routes, routeName, ref index ) ;
        if ( detector.IsPassingThrough( segment ) ) {
          var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
          if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
            var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? mainPullBoxToEndPoint.Key ;
            var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
            if ( fromEndPointKey == mainPullBoxToEndPoint.Key ) {
              result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
              // Increase index in duplicated case
              index++ ;
              name = IndexRouteName( routes, name, ref index ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
              connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
              GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
              if ( parentAndChildRoute.ContainsKey( parentRoute.RouteName ) ) {
                parentAndChildRoute[parentRoute.RouteName].Add( routeName ) ;
              }
              else {
                parentAndChildRoute.Add( parentRoute.RouteName, new List<string> { routeName } ) ;
              }
            }
            else {
              result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
            }
          }
          else {
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
          }

          connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
          GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
        }
        else {
          var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
          if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
            var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? mainPullBoxToEndPoint.Key ;
            var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
            var branchRouteDirection = GetDirectionOfConduitThroughPullBox( document, pullBox, routeName, routeDirection ) ;
            if ( branchRouteDirection == null ) {
              if ( fromEndPointKey == mainPullBoxToEndPoint.Key ) {
                result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
                result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeightSecond, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
                if ( parentAndChildRoute.Any() ) {
                  parentAndChildRoute.First().Value.Add( routeName ) ;
                }
                else {
                  parentAndChildRoute.Add( parentRoute.RouteName, new List<string> { routeName } ) ;
                }
              }
              else {
                result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
              }
              connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
              GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
            }
            else {
              var (_, branchPullBoxToEndPoint) = GetFromAndToConnectorEndPoint( document, pullBox, isCreatePullBoxWithoutSettingHeight, radius, branchRouteDirection, null, null ) ;
              result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchPullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping || segment.AllowedTiltedPiping ) ) ) ;
              connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
              GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
            }
          }
          else {
            result.Add( ( routeName, segment ) ) ;
          }
        }
      }

      return result ;
    }

    private static string IndexRouteName( RouteCache routes, string routeName, ref int index )
    {
      var temporaryRouteName = routeName + "_" + index ;
      while ( routes.ContainsKey( temporaryRouteName ) ) {
        temporaryRouteName = routeName + "_" + ++index ;
      }

      return temporaryRouteName ;
    }

    private static EndPointKey? GetFromEndPointKey( Document document, List<(string RouteName, RouteSegment Segment)> segments, string passPointEndPointUniqueId )
    {
      var fromRouteName = string.Empty ;
      foreach ( var ( routeName, segment ) in segments ) {
        if ( segment.FromEndPoint.Key.GetElementUniqueId() != passPointEndPointUniqueId ) continue ;
        fromRouteName = routeName ;
        break ;
      }

      if ( string.IsNullOrEmpty( fromRouteName ) ) return null ;
      var fromSegment = segments.FirstOrDefault( s => s.RouteName == fromRouteName ) ;
      var fromEndPointKey = fromSegment.Segment.FromEndPoint.Key ;
      var passPoint = document.GetElementById<Instance>( passPointEndPointUniqueId ) ;
      passPoint?.SetProperty( RoutingParameter.RouteName, fromRouteName ) ;
      
      return fromEndPointKey ;
    }

    private static ( List<(string RouteName, RouteSegment Segment)>, Route ) GetRelatedBranchSegments( Route route )
    {
      // add all related branches
      var relatedBranches = route.GetAllRelatedBranches() ;
      var parentBranch = route.GetParentBranches().FirstOrDefault() ;
      if ( parentBranch == null ) parentBranch = route ;
      relatedBranches.Remove( parentBranch ) ;
      return ( relatedBranches.ToSegmentsWithName().ToList(), parentBranch ) ;
    }

    public static FamilyInstance GenerateConnector( Document document, ElectricalRoutingFamilyType electricalRoutingFamilyType, ConnectorFamilyType? connectorType, double originX, double originY, double originZ, Level level, string routeName )
    {
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      var symbol = document.GetFamilySymbols( electricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      var toConnectorOfRoute = GetToConnectorOfRoute( document, routeName ) ;
      var constructionItem = toConnectorOfRoute != null && toConnectorOfRoute.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItemOfToConnector ) && ! string.IsNullOrEmpty( constructionItemOfToConnector ) 
        ? constructionItemOfToConnector !
        : DefaultConstructionItem ;
      var isEcoMode = toConnectorOfRoute != null && toConnectorOfRoute.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoModeOfToConnector ) && ! string.IsNullOrEmpty( isEcoModeOfToConnector ) 
        ? isEcoModeOfToConnector !
        : document.GetDefaultSettingStorable().EcoSettingData.IsEcoMode.ToString() ;
      var ceedCode = toConnectorOfRoute != null && toConnectorOfRoute.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfToConnector ) && ! string.IsNullOrEmpty( ceedCodeOfToConnector ) 
        ? ceedCodeOfToConnector !
        : string.Empty ;

      if ( instance.HasParameter( ElectricalRoutingElementParameter.ConstructionItem ) )
        instance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem ) ;

      if ( instance.HasParameter( ElectricalRoutingElementParameter.IsEcoMode ) )
        instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, isEcoMode ) ;

      if ( instance.HasParameter( ElectricalRoutingElementParameter.CeedCode ) )
        instance.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;
      
      // Update width, depth parameter for pull box
      instance.GetParameter( ScaleFactorParameter )?.Set( baseLengthOfLine ) ;

      instance.SetConnectorFamilyType( connectorType ?? ConnectorFamilyType.PullBox ) ;

      return instance ;
    }

    private static void GetPullBoxCeedCodes( Document document, List<string> ceedCodes, string connectorUniqueId )
    {
      var connector = document.GetElement( connectorUniqueId ) ;
      if ( connector == null ) return ;
      connector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfConnector ) ;
      if ( ! string.IsNullOrEmpty( ceedCodeOfConnector ) ) ceedCodes.Add( ceedCodeOfConnector! ) ;
    }

    private static (int depth, int width, int height) GetPullBoxDimension(int[] plumbingSizes, bool isStraightDirection)
    {
      int depth = 0, width = 0, height = 0 ;
      if ( plumbingSizes.Any() ) {
        int maxPlumbingSize = plumbingSizes.Max() ;
        if ( isStraightDirection ) {
          depth = plumbingSizes.Sum( x => x + 30 ) + ( 30 * 2 ) ;
          width = maxPlumbingSize * 8 ;
        }
        else {
          depth = width = plumbingSizes.Sum( x => x + 30 ) + 30 + 8 * maxPlumbingSize ;
        }
        height = GetHeightOfPullBoxByPlumbingSize( maxPlumbingSize ) ;
      }
      return ( depth, width, height ) ;
    }
    
    private static int GetHeightOfPullBoxByPlumbingSize( int plumbingSize )
    {
      switch ( plumbingSize ) {
        case 19: case 16: case 25: case 22: case 31: case 28: return 200 ;
        case 39: case 36: case 51: case 42: return 300 ;
        case 63: case 54: case 75: case 70: case 82: case 92: case 104: return 400 ;
        default: return 0 ;
      }
    }

    private static Element? GetToConnectorOfRoute( Document document, string routeName )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
        return toConnector ;
      }

      return null ;
    }

    private static List<Element> GetFromConnectorOfPullBox( Document document, Element element, bool isFrom = false)
    {
      List<Element> result = new() ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ) ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( isFrom ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementId = toEndPointKey.GetElementUniqueId() ;
        var pullBoxElementId = element.UniqueId ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        if ( pullBoxElementId.Equals( toElementId ) ) {
          var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
          if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
            result.Add( conduit );
        }
      }

      return result ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSegmentsWithPullBox ( Document document, IReadOnlyCollection<Route> executeResultValue, List<string> boardUniqueIds, List<XYZ> pullBoxPositions, List<(FamilyInstance, XYZ?)> pullBoxElements, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute )
    {
      const string angleParameter = "角度" ;
      const double pullBoxAutomaticPlacementCondition3Threshold = 270 ;
      var pullBoxAutomaticPlacementCondition4Threshold = ( 30.0 ).MetersToRevitUnits() ;

      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      string conduitFittingLengthParam = "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( document, "電線管長さ" ) ;
      string conduitLengthParam = "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ;
      var registrationOfBoardDataModels = document.GetRegistrationOfBoardDataStorable().RegistrationOfBoardData ;
      foreach ( var route in executeResultValue ) {
        var allConduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        SortConduitFitting( ref allConduitsOfRoute, route ) ;
        var conduitFittingsOfRoute = allConduitsOfRoute.OfType<FamilyInstance>().ToList() ;

        double sumLength = 0 ;
        double sumAngle = 0 ;
        FamilyInstance? selectedConduitFitting = null ;
        foreach ( var conduitFitting in conduitFittingsOfRoute ) {
          if ( conduitFitting.HasParameter( angleParameter ) ) {
            var angle = conduitFitting.ParametersMap.get_Item( angleParameter ).AsDouble() ;
            sumAngle += angle ;
          }

          if ( sumAngle < pullBoxAutomaticPlacementCondition3Threshold ) continue ;
          selectedConduitFitting = conduitFitting ;
          break ;
        }

        Element? selectedConduit = null ;
        foreach ( var conduit in allConduitsOfRoute ) {
          if ( conduit.HasParameter( conduitLengthParam ) ) {
            var length = conduit.ParametersMap.get_Item( conduitLengthParam ).AsDouble() ;
            sumLength += length ;
          }

          if ( conduit.HasParameter( conduitFittingLengthParam ) ) {
            var length = conduit.ParametersMap.get_Item( conduitFittingLengthParam ).AsDouble() ;
            sumLength += length ;
          }

          if ( sumLength < pullBoxAutomaticPlacementCondition4Threshold ) continue ;
          selectedConduit = conduit ;
          break ;
        }

        var connectorsOfRoute = route.GetAllConnectors().ToList() ;
        var boardConnector = new List<Element>() ;
        foreach ( var connector in connectorsOfRoute ) {
          var element = connector.Owner ;
          if ( element == null || boardUniqueIds.Contains( element.UniqueId ) ) continue ;
          element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfConnector ) ;
          if ( string.IsNullOrEmpty( ceedCodeOfConnector ) ) continue ;
          var registrationOfBoardDataModel = registrationOfBoardDataModels.FirstOrDefault( x => x.AutoControlPanel == ceedCodeOfConnector || x.SignalDestination == ceedCodeOfConnector ) ;
          if ( registrationOfBoardDataModel == null ) continue ;
          boardConnector.Add( element ) ;
        }

        var curveType = route.UniqueCurveType ;
        var nameBase = curveType?.Category.Name ;

        if ( boardConnector.Any() ) {
          var board = boardConnector.First() ;
          var conduitInfo = GetConduitOfBoard( document, route.RouteName, board ) ;
          if ( conduitInfo == null ) continue ;
          var (originX, originY, originZ) = conduitInfo.ConduitOrigin ;
          result = CreatePullBoxAndGetSegments( document, route, conduitInfo.Conduit, originX, originY, originZ,conduitInfo.Level, conduitInfo.ConduitDirection, nameBase!, out var pullBoxElement, ref parentIndex, ref parentAndChildRoute ).ToList() ;
          pullBoxPositions.Add( conduitInfo.ConduitOrigin ) ;
          if ( pullBoxElement != null ) pullBoxElements.Add( ( pullBoxElement, conduitInfo.ConduitOrigin ) ) ;
          boardUniqueIds.Add( board.UniqueId ) ;
          return result ;
        }

        if ( conduitFittingsOfRoute.Count >= 4 ) {
          var conduitFitting = conduitFittingsOfRoute.ElementAt( 3 ) ;
          var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
          var (originX, originY, originZ) = pullBoxInfo.Position ;
          var fromDirection = pullBoxInfo.FromDirection ;
          var toDirection = pullBoxInfo.ToDirection ;
          var height = originZ - pullBoxInfo.Level.Elevation ;
          var pullBoxPosition = new XYZ( originX, originY, height ) ;
          var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, pullBoxPosition ) ;
          if ( isSamePullBoxPositions ) continue ;
          result = CreatePullBoxAndGetSegments( document, route, conduitFitting, originX, originY, height,pullBoxInfo.Level, fromDirection, nameBase!, out var pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection ).ToList() ;
          if ( pullBoxElement != null ) pullBoxElements.Add( ( pullBoxElement, pullBoxPosition ) ) ;
          pullBoxPositions.Add( pullBoxPosition ) ;
          return result ;
        }

        if ( sumAngle > pullBoxAutomaticPlacementCondition3Threshold && selectedConduitFitting != null ) {
          var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, selectedConduitFitting ) ;
          var (originX, originY, originZ) = pullBoxInfo.Position ;
          var fromDirection = pullBoxInfo.FromDirection ;
          var toDirection = pullBoxInfo.ToDirection ;
          var height = originZ - pullBoxInfo.Level.Elevation ;
          var pullBoxPosition = new XYZ( originX, originY, height ) ;
          var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, pullBoxPosition ) ;
          if ( isSamePullBoxPositions ) continue ;
          result = CreatePullBoxAndGetSegments( document, route, selectedConduitFitting, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, out var pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection ).ToList() ;
          if ( pullBoxElement != null ) pullBoxElements.Add( ( pullBoxElement, pullBoxPosition ) ) ;
          pullBoxPositions.Add( pullBoxPosition ) ;
          return result ;
        }

        if ( sumLength > pullBoxAutomaticPlacementCondition4Threshold && selectedConduit != null ) {
          double originX = 0, originY = 0, originZ = 0, height = 0 ;
          Level? level = null ;
          XYZ? direction = null ;
          XYZ? fromDirection = null ;
          XYZ? toDirection = null ;
          if ( selectedConduit is Conduit ) {
            level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == selectedConduit.GetLevelId() ) ;
            var location = ( selectedConduit.Location as LocationCurve ) ! ;
            var line = ( location.Curve as Line ) ! ;
            ( originX, originY, originZ ) = line.GetEndPoint( 0 ) ;
            direction = line.Direction ;
            var length = sumLength - pullBoxAutomaticPlacementCondition4Threshold ;
            if ( direction.X is 1 or -1 )
              originX += direction.X * length ;
            else if ( direction.Y is 1 or -1 )
              originY += direction.Y * length ;
            else if ( direction.Z is 1 or -1 ) originZ += direction.Z * length ;
            height = originZ - level!.Elevation ;
            var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, new XYZ( originX, originY, height ) ) ;
            if ( isSamePullBoxPositions ) continue ;
          }
          else if ( selectedConduit is FamilyInstance conduitFitting ) {
            var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
            var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, new XYZ( originX, originY, height ) ) ;
            if ( isSamePullBoxPositions ) continue ;

            ( originX, originY, originZ ) = pullBoxInfo.Position ;
            level = pullBoxInfo.Level ;
            height = originZ - level.Elevation ;
            direction = pullBoxInfo.FromDirection ;
            fromDirection = pullBoxInfo.FromDirection ;
            toDirection = pullBoxInfo.ToDirection ;
          }

          if ( level == null || direction == null )
            return result ;
          
          result = CreatePullBoxAndGetSegments( document, route, selectedConduit, originX, originY, height, level, direction, nameBase!, out var pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection ).ToList() ;
          var pullBoxPosition = new XYZ( originX, originY, height ) ;
          if ( pullBoxElement != null ) pullBoxElements.Add( ( pullBoxElement, pullBoxPosition ) ) ;
          pullBoxPositions.Add( pullBoxPosition ) ;
          return result ;
        }
      }

      return result ;
    }

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSegmentsWithPullBoxShaft( Document document, IReadOnlyCollection<Route> executeResultValue, List<XYZ> pullBoxPositions, List<(FamilyInstance, XYZ?)> pullBoxElements, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, ref bool isWireEnteredShaft, bool isPickedFromBottomToTop )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var conduitFittingsOfRoute = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => !string.IsNullOrEmpty( c.GetRouteName() ) && executeResultValue.Any( r => c.GetRouteName() == r.RouteName ) ).ToList() ;
      foreach ( var route in executeResultValue ) {
        if( route == null || string.IsNullOrEmpty( route.UniqueShaftElementUniqueId ) ) continue ;
        var shaftId = route.UniqueShaftElementUniqueId ;
        var fromHeight = route.UniqueFromFixedHeight ;
        var curveType = route.UniqueCurveType ;
        var nameBase = curveType?.Category.Name ;

        if ( shaftId == null ) continue ;
        var shaft = document.GetElementById<Opening>( route.UniqueShaftElementUniqueId ?? string.Empty ) ;
        if ( shaft == null ) continue ;
        var shaftLocation = GetShaftLocation( route, document ) ;
        if ( shaftLocation == null ) continue ;
        
        if ( ! pullBoxPositions.Any() ) {
          // When wire enter shaft
          var conduitFittingShaft = GetConduitFittingShaft( shaftLocation, conduitFittingsOfRoute, !isPickedFromBottomToTop ) ;
          if ( conduitFittingShaft == null || conduitFittingShaft.GetRouteName() != route.Name ) continue ;
          
          var pullBoxInfo = GetPullBoxInShaftInfo( document, route.RouteName, conduitFittingShaft ) ;
          var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, pullBoxInfo.Position ) ;
          if ( isSamePullBoxPositions ) continue ;

          var (originX, originY, originZ) = pullBoxInfo.Position ;
          var fromDirection = pullBoxInfo.FromDirection ;
          var toDirection = pullBoxInfo.ToDirection ;
          var height = originZ - pullBoxInfo.Level.Elevation ;
          
          result = CreatePullBoxAndGetSegments( document, route, conduitFittingShaft, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, out var pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection, fromHeight, true, false ).ToList() ;
          if ( pullBoxElement != null ) pullBoxElements.Add( ( pullBoxElement, null ) ) ;
          pullBoxPositions.Add( pullBoxInfo.Position ) ;
        }
        else {
          var conduitFittingShaft = GetConduitFittingShaft( shaftLocation, conduitFittingsOfRoute, isPickedFromBottomToTop ) ;
          if ( conduitFittingShaft == null || conduitFittingShaft.GetRouteName() != route.Name ) continue ;

          var pullBoxesInShaft = GetPullBoxesInShafts( document, route, conduitFittingShaft, pullBoxPositions, isPickedFromBottomToTop ) ;
          if ( pullBoxesInShaft.Any() ) {
            // When wire go through shaft and cross pull boxes
            var pullBox = pullBoxesInShaft.First() ;
            var pullBoxPosition = ( pullBox.Location as LocationPoint )!.Point ;
            if ( pullBoxPosition == null ) continue ;

            var levelOfPullBox = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).First( l => l.Id == pullBox.LevelId ) ;
            var defaultDirectionThroughPullBoxInShaft = isPickedFromBottomToTop ? new XYZ( 0, 0, 1 ) : new XYZ( 0, 0, -1 ) ;

            var height = pullBoxPosition.Z - levelOfPullBox.Elevation ;

            using Transaction t1 = new(document, "Get segments") ;
            t1.Start() ;
            result.AddRange( GetRouteSegmentsThroughShaft( document, route, pullBox, height, height,
              defaultDirectionThroughPullBoxInShaft, true, nameBase!, ref parentIndex, ref parentAndChildRoute,
              defaultDirectionThroughPullBoxInShaft, defaultDirectionThroughPullBoxInShaft, fromHeight ) ) ;
            t1.Commit() ;

            pullBoxElements.Add( ( pullBox, null ) ) ;
            pullBoxPositions.Add( pullBoxPosition ) ;
          }
          else {
            // When wire exit shaft
            var pullBoxInfo = GetPullBoxInShaftInfo( document, route.RouteName, conduitFittingShaft ) ;
            var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, pullBoxInfo.Position ) ;
            if ( isSamePullBoxPositions ) continue ;

            var (originX, originY, originZ) = pullBoxInfo.Position ;
            var fromDirection = pullBoxInfo.FromDirection ;
            var toDirection = pullBoxInfo.ToDirection ;
            var levelOfPullBox = pullBoxInfo.Level ;
            var height = originZ - levelOfPullBox.Elevation ;
            var existedPullBox = FindPullBoxByLocation( document, originX, originY, originZ ) ;
            if ( existedPullBox != null ) {
              var pullBoxPosition = ( existedPullBox.Location as LocationPoint )!.Point ;
              if ( pullBoxPosition == null ) continue ;
            
              levelOfPullBox = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).First( l => l.Id == existedPullBox.LevelId ) ;
              height = pullBoxPosition.Z - levelOfPullBox.Elevation ;
            }

            result = CreatePullBoxAndGetSegments( document, route, conduitFittingShaft, originX, originY, height,
              levelOfPullBox, fromDirection, nameBase!, out var pullBoxElement, ref parentIndex,
              ref parentAndChildRoute, fromDirection, toDirection, fromHeight, true ).ToList() ;
            if ( pullBoxElement != null ) pullBoxElements.Add( ( pullBoxElement, null ) ) ;
            pullBoxPositions.Add( pullBoxInfo.Position ) ;
            isWireEnteredShaft = true ;
          }
        }
        
        return result ;
      }

      return result ;
    }

    public static bool IsGradeUnderThree( Document document )
    {
      var defaultSettingStorable = document.GetDefaultSettingStorable() ;
      var grade = defaultSettingStorable.GradeSettingData.GradeMode ;
      return grade is 1 or 2 or 3 ;
    }

    private static XYZ? GetShaftLocation( Route route, Document document )
    {
      var shaftUniqueId = route.UniqueShaftElementUniqueId ;
      if ( string.IsNullOrEmpty( shaftUniqueId ) ) return null ;

      var shaft = document.GetElement( shaftUniqueId ) ;
      if ( shaft is not Opening opening ) return null ;

      var shaftArc = opening.BoundaryCurves.Cast<Arc>().SingleOrDefault() ;

      return shaftArc == null ? null : shaftArc.Center ;
    }

    private static FamilyInstance? GetConduitFittingShaft( XYZ shaftLocation, IEnumerable<FamilyInstance> conduitFittings, bool isBottom = false )
    {
      FamilyInstance? conduitFittingShaft = null ;
      XYZ? conduitPosition = null ;
      foreach ( var conduitFitting in conduitFittings ) {
        var conduitFittingLocationPoint = ( conduitFitting.Location as LocationPoint )?.Point ;
        if ( ! conduitFittingLocationPoint.IsNearShaft( shaftLocation ) ) continue ;
        if ( conduitFittingShaft == null ) {
          conduitFittingShaft = conduitFitting ;
          conduitPosition = conduitFittingLocationPoint ;
        }

        if ( ( isBottom && conduitFittingLocationPoint?.Z < conduitPosition?.Z ) ||
             ( !isBottom && conduitFittingLocationPoint?.Z > conduitPosition?.Z ) ) continue ;
        conduitFittingShaft = conduitFitting ;
        conduitPosition = conduitFittingLocationPoint ;
      }

      return conduitFittingShaft ;
    }

    private static bool IsNearShaft( this XYZ? thisPoint, XYZ anotherPoint )
    {
      if ( thisPoint == null ) {
        return false ;
      }

      return thisPoint.X.IsAlmostOrEqual( anotherPoint.X ) && thisPoint.Y.IsAlmostOrEqual( anotherPoint.Y ) ;
    }

    private static bool IsAlmostOrEqual( this double firstValue, double secondValue )
    {
      return Math.Abs( firstValue - secondValue ) <= NearShaftTolerance ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreatePullBoxAndGetSegments( Document document, Route route, Element element, double originX, double originY, double originZ,
      Level level, XYZ direction, string nameBase, out FamilyInstance? pullBox, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, XYZ? fromDirection = null, XYZ? toDirection = null, FixedHeight? firstHeight = null, bool isPullBoxInShaft = false, bool isWireEnteredShaft = true )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      pullBox = null ;
      try {
        if( isPullBoxInShaft )
          pullBox = FindPullBoxByLocation( document, originX, originY, originZ + level.Elevation ) ;
        
        if ( ! isPullBoxInShaft || pullBox == null ) {
          using Transaction t = new( document, "Create pull box" ) ;
          t.Start() ;
          pullBox = GenerateConnector( document, ElectricalRoutingFamilyType.PullBox, ConnectorFamilyType.PullBox, originX, originY, originZ , level, route.RouteName ) ;
          t.Commit() ;

          if ( isPullBoxInShaft ) {
            var heightPositionOfPullBox = originZ + level.Elevation ;
            var shaftElementUniqueId = route.UniqueShaftElementUniqueId ;
            var shaft = document.GetElementById<Opening>( shaftElementUniqueId ?? string.Empty ) ;
            if ( shaft != null && GetShaftLocation( route, document ) is { } shaftLocation ) {
              var routesWithDirection = GetOldRoutesWithDirectionWherePullBoxesAreCreated( document, route, shaftLocation, heightPositionOfPullBox ) ;

              // Reroute old routes when new pull boxes are created
              if ( routesWithDirection.Any() ) {
                foreach ( var (reRoute, routeDirection) in routesWithDirection ) {
                  using Transaction t3 = new( document, "Get segments" ) ;
                  t3.Start() ;
                  result.AddRange( GetRouteSegmentsThroughShaft( document, reRoute, pullBox, originZ, originZ,
                    routeDirection, true, nameBase, ref parentIndex, ref parentAndChildRoute,
                    routeDirection, routeDirection, reRoute.UniqueFromFixedHeight, isWireEnteredShaft ) ) ;
                  t3.Commit() ;
                }
              }
            }
          }
        }
        using Transaction t1 = new( document, "Get segments" ) ;
        t1.Start() ;
        result.AddRange( GetRouteSegments( document, route, element, pullBox, originZ, originZ, direction, true, nameBase, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection, firstHeight, isWireEnteredShaft ) ) ;
        t1.Commit() ;
      }
      catch {
        //
      }

      return result ;
    }

    private static List<(Route ReRoute, XYZ Direction)> GetOldRoutesWithDirectionWherePullBoxesAreCreated( Document document, Route route,
      XYZ shaftLocation, double heightPositionOfPullBox )
    {
      var routesWithDirection = new List<( Route ReRoute, XYZ Direction )>() ;

      var routeNameArray = route.RouteName.Split( '_' ) ;
      var routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;

      var pullBoxesInShaft = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( c =>
        {
          if ( c.GetConnectorFamilyType() != ConnectorFamilyType.PullBox ) return false ;

          var locationPoint = ( c.Location as LocationPoint )?.Point ;
          return locationPoint != null && IsNearShaft( locationPoint, shaftLocation ) &&
                 ! IsAlmostOrEqual( locationPoint.Z, heightPositionOfPullBox ) ;
        } ).ToList() ;

      var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;
      foreach ( var r in routeCache ) {
        if ( routesWithDirection.Any( re => re.ReRoute.RouteName == r.Key ) ) continue ;

        var rNameArray = r.Key.Split( '_' ) ;
        var rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        if ( rName == routeName ) continue ;

        var routeSegments = r.Value.RouteSegments ;
        foreach ( var routeSegment in routeSegments ) {
          var fromEndPointElementUniqueId = routeSegment.FromEndPoint.Key.GetElementUniqueId() ;
          var toEndPointElementUniqueId = routeSegment.ToEndPoint.Key.GetElementUniqueId() ;
          var fromPullBox = pullBoxesInShaft.FirstOrDefault( p => p.UniqueId == fromEndPointElementUniqueId ) ;
          var toPullBox = pullBoxesInShaft.FirstOrDefault( p => p.UniqueId == toEndPointElementUniqueId ) ;
          if ( fromPullBox == null || toPullBox == null ) continue ;

          var fromPullBoxPosition = ( fromPullBox.Location as LocationPoint )?.Point ;
          var toPullBoxPosition = ( toPullBox.Location as LocationPoint )?.Point ;
          if ( fromPullBoxPosition == null || toPullBoxPosition == null ) continue ;

          if ( fromPullBoxPosition.Z < heightPositionOfPullBox && heightPositionOfPullBox < toPullBoxPosition.Z )
            routesWithDirection.Add( ( r.Value, new XYZ( 0, 0, 1 ) ) ) ;
          else if ( fromPullBoxPosition.Z > heightPositionOfPullBox && heightPositionOfPullBox > toPullBoxPosition.Z )
            routesWithDirection.Add( ( r.Value, new XYZ( 0, 0, -1 ) ) ) ;
        }
      }

      return routesWithDirection ;
    }
    
    public static void ChangeDimensionOfPullBoxAndSetLabel( Document document, List<(FamilyInstance, XYZ?)> pullBoxElements )
    {
      var csvStorable = document.GetCsvStorable() ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      var level = document.ActiveView.GenLevel ;
      StorageService<Level, DetailSymbolModel>? storageDetailSymbolService = null ;
      StorageService<Level, PullBoxInfoModel>? storagePullBoxInfoServiceByLevel = null ;
      if ( level != null ) {
        storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( level ) ;
        storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
      }

      foreach ( var pullBoxElement in pullBoxElements ) {
        var (pullBox, position) = pullBoxElement ;
        var positionLabel = position != null ? new XYZ( position.X - NotationOfPullBoxXAxis * baseLengthOfLine, position.Y + NotationOfPullBoxYAxis * baseLengthOfLine, position.Z ) : null ;
        ChangeDimensionOfPullBoxAndSetLabel( document, baseLengthOfLine, pullBox, csvStorable, storageDetailSymbolService, storagePullBoxInfoServiceByLevel, conduitsModelData, hiroiMasterModels, DefaultPullBoxLabel, positionLabel, true ) ;
      }
    }

    public static void ChangeDimensionOfPullBoxAndSetLabel( Document document, double baseLengthOfLine, FamilyInstance pullBox,
      CsvStorable? csvStorable, StorageService<Level, DetailSymbolModel>? storageDetailSymbolService, StorageService<Level, PullBoxInfoModel>? storagePullBoxInfoServiceByLevel,
      List<ConduitsModel>? conduitsModelData, List<HiroiMasterModel>? hiroiMasterModels, string textLabel,
      XYZ? positionLabel, bool isAutoCalculatePullBoxSize, PullBoxModel? selectedPullBoxModel = null )
    {
      var defaultLabel = textLabel ;
      var buzaiCd = string.Empty ;
      int depth = 0, height = 0 ;

      //Case 1: Automatically calculate dimension of pull box
      if ( isAutoCalculatePullBoxSize ) {
        if ( csvStorable != null && conduitsModelData != null && hiroiMasterModels != null && storageDetailSymbolService != null ) {
          var pullBoxModel = GetPullBoxWithAutoCalculatedDimension( document, pullBox, csvStorable, storageDetailSymbolService, conduitsModelData, hiroiMasterModels ) ;
          if ( pullBoxModel != null ) {
            buzaiCd = pullBoxModel.Buzaicd ;
            ( depth, _, height ) = ParseKikaku( pullBoxModel.Kikaku ) ;
          }
        }
      }
      //Case 2: Use dimension of selected pull box
      else if ( selectedPullBoxModel != null ) {
        buzaiCd = selectedPullBoxModel.Buzaicd ;
        ( depth, _, height ) = ParseKikaku( selectedPullBoxModel.Kikaku ) ;
      }
      else if ( storagePullBoxInfoServiceByLevel != null && storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.All( pullBoxInfoItemModel => pullBoxInfoItemModel.PullBoxUniqueId != pullBox.UniqueId ) ) {
        buzaiCd = pullBox.GetParameter( MaterialCodeParameter )?.AsString() ;
        if ( ! string.IsNullOrEmpty( buzaiCd ) && hiroiMasterModels != null ) {
          var kikaku = hiroiMasterModels.FirstOrDefault( h => h.Buzaicd == buzaiCd )?.Kikaku ;
          if ( ! string.IsNullOrEmpty( kikaku ) )
            ( depth, _, height ) = ParseKikaku( kikaku! ) ;
        }
      }
      
      if ( ! string.IsNullOrEmpty( buzaiCd ) )
        pullBox.GetParameter( MaterialCodeParameter )?.Set( buzaiCd ) ;
      pullBox.GetParameter( IsAutoCalculatePullBoxSizeParameter )?.Set( Convert.ToString( isAutoCalculatePullBoxSize ) ) ;
      if ( isAutoCalculatePullBoxSize )
        storageDetailSymbolService?.Data.DetailSymbolData.RemoveAll( d => d.DetailSymbolUniqueId == pullBox.UniqueId ) ;

      ResizePullBoxAndRelatedConduits( document, baseLengthOfLine, pullBox ) ;

      if ( storagePullBoxInfoServiceByLevel == null ) return ;
      
      textLabel = GetPullBoxTextBox( depth, height, defaultLabel ) ;
      if ( positionLabel != null )
        CreateTextNoteAndGroupWithPullBox( document, storagePullBoxInfoServiceByLevel, positionLabel, pullBox, textLabel, isAutoCalculatePullBoxSize ) ;
      else if ( storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.All( pullBoxInfoItemModel => pullBoxInfoItemModel.PullBoxUniqueId != pullBox.UniqueId ) ) {
        var position = (pullBox.Location as LocationPoint)?.Point ;
        positionLabel = position != null ? new XYZ( position.X - NotationOfPullBoxXAxis * baseLengthOfLine, position.Y + NotationOfPullBoxYAxis * baseLengthOfLine, position.Z ) : null ;
        if ( positionLabel != null )
          CreateTextNoteAndGroupWithPullBox( document, storagePullBoxInfoServiceByLevel, positionLabel, pullBox, textLabel, isAutoCalculatePullBoxSize ) ;
      } else if ( isAutoCalculatePullBoxSize )
        ChangeLabelOfPullBox( document, storagePullBoxInfoServiceByLevel, pullBox, textLabel, isAutoCalculatePullBoxSize ) ;
    }

    public static void ResizePullBoxAndRelatedConduits( Document document, double baseLengthOfLine, FamilyInstance pullBox )
    {
      // Update width, depth parameter for pull box
      var oldDepthByScale = pullBox.GetParameter( DepthParameter )?.AsDouble() ?? -1d ;
      var depthByScale = ( DefaultDepthOfPullBox * baseLengthOfLine ).MillimetersToRevitUnits() ;
      pullBox.GetParameter( ScaleFactorParameter )?.Set( baseLengthOfLine ) ;

      //Resize conduits related pull box
      var pullBoxLocation = ( pullBox.Location as LocationPoint )?.Point ;
      if ( pullBoxLocation == null || ! ( oldDepthByScale > 0 ) ) return ;
      
      var depthDifferenceByScale = ( depthByScale - oldDepthByScale ) / 2 ;
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var routesRelatedPullBox = GetRoutesRelatedPullBoxByNearestEndPoints( document, pullBox, routes ) ;
      var conduitsRelatedPullBox = GetConduitsRelatedPullBox( document, pullBoxLocation, routesRelatedPullBox ) ;

      foreach ( var c in conduitsRelatedPullBox ) {
        if ( c.Conduit.Location is not LocationCurve { Curve: Line line } curve ) continue ;

        if ( c.EndPointIndex == 0 ) {
          var fromEndPoint = line.GetEndPoint( 0 ) ;
          if ( line.Direction.X is 1 )
            curve.Curve = Line.CreateBound( new XYZ( fromEndPoint.X + depthDifferenceByScale, fromEndPoint.Y, fromEndPoint.Z ), line.GetEndPoint( 1 ) ) ;
          else if ( line.Direction.X is -1 )
            curve.Curve = Line.CreateBound( new XYZ( fromEndPoint.X - depthDifferenceByScale, fromEndPoint.Y, fromEndPoint.Z ), line.GetEndPoint( 1 ) ) ;
          else if ( line.Direction.Y is 1 )
            curve.Curve = Line.CreateBound( new XYZ( fromEndPoint.X, fromEndPoint.Y + depthDifferenceByScale, fromEndPoint.Z ), line.GetEndPoint( 1 ) ) ;
          else if ( line.Direction.Y is -1 )
            curve.Curve = Line.CreateBound( new XYZ( fromEndPoint.X, fromEndPoint.Y - depthDifferenceByScale, fromEndPoint.Z ), line.GetEndPoint( 1 ) ) ;
        }
        else {
          var toEndPoint = line.GetEndPoint( 1 ) ;
          if ( line.Direction.X is 1 )
            curve.Curve = Line.CreateBound( line.GetEndPoint( 0 ), new XYZ( toEndPoint.X - depthDifferenceByScale, toEndPoint.Y, toEndPoint.Z ) ) ;
          else if ( line.Direction.X is -1 )
            curve.Curve = Line.CreateBound( line.GetEndPoint( 0 ), new XYZ( toEndPoint.X + depthDifferenceByScale, toEndPoint.Y, toEndPoint.Z ) ) ;
          else if ( line.Direction.Y is 1 )
            curve.Curve = Line.CreateBound( line.GetEndPoint( 0 ), new XYZ( toEndPoint.X, toEndPoint.Y - depthDifferenceByScale, toEndPoint.Z ) ) ;
          else if ( line.Direction.Y is -1 )
            curve.Curve = Line.CreateBound( line.GetEndPoint( 0 ), new XYZ( toEndPoint.X, toEndPoint.Y + depthDifferenceByScale, toEndPoint.Z ) ) ;
        }
      }
    }

    private static List<(Conduit Conduit, int EndPointIndex)> GetConduitsRelatedPullBox( Document document, XYZ pullBoxLocation, List<Route> routesRelatedPullBox )
    {
      var conduitsRelatedPullBox = new List<(Conduit Conduit, int EndPointIndex)>() ;
      foreach ( var routeRelatedPullBox in routesRelatedPullBox ) {
        var minDistance = 0d ;
        Conduit? conduit = null ;
        var endPointIndex = -1 ;
        var conduitsOfRoute = document.GetAllElements<Conduit>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRouteName() == routeRelatedPullBox.RouteName ).EnumerateAll() ;
        foreach ( var conduitOfRoute in conduitsOfRoute ) {
          if ( conduitOfRoute.Location is not LocationCurve { Curve : Line line } ) continue ;

          for ( var i = 0 ; i < 2 ; i++ ) {
            var distance = line.GetEndPoint( i ).DistanceTo( pullBoxLocation ) ;
            if ( distance > minDistance && minDistance > 0 ) continue ;

            minDistance = distance ;
            conduit = conduitOfRoute ;
            endPointIndex = i ;
          }
        }

        if ( conduit != null && endPointIndex >= 0 )
          conduitsRelatedPullBox.Add( ( conduit, endPointIndex ) ) ;
      }

      return conduitsRelatedPullBox ;
    }

    private static List<Route> GetRoutesRelatedPullBoxByNearestEndPoints( Document document, Element pullBox, RouteCache routes )
    {
      var routesRelatedPullBox = new List<Route>() ;
      var allConduits = document.GetAllElements<Conduit>().OfCategory( BuiltInCategory.OST_Conduit ).EnumerateAll() ;
      foreach ( var conduit in allConduits ) {
        if ( conduit?.GetSubRouteInfo() is not { } subRouteInfo ) continue ;
        if ( routes.GetSubRoute( subRouteInfo ) == null ) continue ;

        var fromEndPoint = conduit.GetNearestEndPoints( true ).FirstOrDefault() ;
        var toEndPoint = conduit.GetNearestEndPoints( false ).FirstOrDefault() ;
        if ( fromEndPoint == null || toEndPoint == null ) continue ;

        if ( fromEndPoint.Key.GetElementUniqueId() != pullBox.UniqueId && toEndPoint.Key.GetElementUniqueId() != pullBox.UniqueId ) continue ;
        var route = routes.FirstOrDefault( r => r.Key == conduit.GetRouteName() ).Value ;
        if ( route != null && routesRelatedPullBox.All( r => r.RouteName != route.RouteName ) )
          routesRelatedPullBox.Add( route ) ;
      }

      return routesRelatedPullBox ;
    }

    private static void ChangeLabelOfPullBox( Document document, StorageService<Level, PullBoxInfoModel> storagePullBoxInfoServiceByLevel, Element pullBoxElement, string textLabel, bool isAutoCalculatePullBoxSize )
    {
      // Find text note compatible with pull box, change label if exists
      var pullBoxInfoModel = storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.FirstOrDefault( p => p.PullBoxUniqueId == pullBoxElement.UniqueId ) ;
      var textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => pullBoxInfoModel?.TextNoteUniqueId == t.UniqueId ) ;
      if ( textNote == null ) return ;
      
      textNote.Text = textLabel ;
      if ( ! isAutoCalculatePullBoxSize ) return ;
      
      var color = new Color( 255, 0, 0 ) ;
      ConfirmUnsetCommandBase.ChangeElementColor( new []{ textNote }, color ) ;
    }

    private static void CreateTextNoteAndGroupWithPullBox(Document doc, StorageService<Level, PullBoxInfoModel> storagePullBoxInfoServiceByLevel, XYZ point, Element pullBox, string text, bool isAutoCalculatePullBoxSize)
    {
      var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc, TextNoteHelper.TextSize, false )!.Id ;
      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;
      
      var txtPosition = new XYZ( point.X, point.Y, point.Z ) ;
      
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, text, opts ) ;
      
      if ( isAutoCalculatePullBoxSize ) {
        var color = new Color( 255, 0, 0 ) ;
        ConfirmUnsetCommandBase.ChangeElementColor( new []{ textNote }, color ) ;
      }

      storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.Add( new PullBoxInfoItemModel( pullBox.UniqueId, textNote.UniqueId ) );
      storagePullBoxInfoServiceByLevel.SaveChange() ;
    }

    private static string GetPullBoxTextBox( int depth, int height, string text)
    {
      if ( depth == 0 || height == 0 ) return text ;
      Dictionary<int, (int, int)> defaultDimensions = new()
        {
          { 1, ( 150, 100 ) }, 
          { 2, ( 200, 200 ) }, 
          { 3, ( 300, 300 ) },
          { 4, (400, 300) },
          { 5, (500, 400) },
          { 6, (600, 400) },
          { 8, (800, 400) },
          { 10, (1000, 400) }
        } ;
      foreach ( var defaultDimension in defaultDimensions ) {
        var (d, h) = defaultDimension .Value;
        if ( d >= depth && h >= height )
          return text + defaultDimension.Key ;
      }

      return text ;
    }

    private static bool IsPullBoxExistInThePosition(Document document, IEnumerable<XYZ> pullBoxPositions, XYZ newPullBoxPosition )
    {
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      var minDistance = ( 250.0 ).MillimetersToRevitUnits() * baseLengthOfLine;
      foreach ( var pullBoxPosition in pullBoxPositions ) {
        if ( newPullBoxPosition.DistanceTo( pullBoxPosition ) < minDistance ) {
          return true ;
        }
      }

      return false ;
    }

    private static ( ConnectorEndPoint, ConnectorEndPoint ) GetFromAndToConnectorEndPoint( Document document, FamilyInstance pullBox, bool isCreatePullBoxWithoutSettingHeight, double? radius, XYZ routeDirection, XYZ? fromDirection, XYZ? toDirection )
    {
      ConnectorEndPoint pullBoxFromEndPoint ;
      ConnectorEndPoint pullBoxToEndPoint ;
      if ( isCreatePullBoxWithoutSettingHeight ) {
        if ( fromDirection != null && toDirection != null ) {
          if ( fromDirection.X is 1 or -1 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.X is 1 ? RoutingElementExtensions.ConnectorPosition.Left : RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
          }
          else if ( fromDirection.Y is 1 or -1 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.Y is 1 ? RoutingElementExtensions.ConnectorPosition.Front : RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
          }
          else if ( fromDirection.Z is 1 or -1 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.Z is 1 ? RoutingElementExtensions.ConnectorPosition.Bottom : RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
          }
          else {
            if ( toDirection.Z is 1 or -1 ) {
              var pullBoxOrigin = ( pullBox.Location as LocationPoint )!.Point ;
              var rotationAngle = Math.Atan2( fromDirection.Y, fromDirection.X ) ;
              ElementTransformUtils.RotateElement( document, pullBox.Id, Line.CreateBound( pullBoxOrigin, pullBoxOrigin + XYZ.BasisZ ), rotationAngle ) ;
              pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
            }
            else {
              pullBoxFromEndPoint = Math.Abs( fromDirection.X ) > Math.Abs( fromDirection.Y ) 
                ? new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.X > 0 ? RoutingElementExtensions.ConnectorPosition.Left : RoutingElementExtensions.ConnectorPosition.Right ), radius ) 
                : new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.Y < 0 ? RoutingElementExtensions.ConnectorPosition.Front : RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
            }
          }
          
          if ( toDirection.X is 1 or -1 ) {
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.X is 1 ? RoutingElementExtensions.ConnectorPosition.Right : RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
          }
          else if ( toDirection.Y is 1 or -1 ) {
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Y is 1 ? RoutingElementExtensions.ConnectorPosition.Back : RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
          }
          else if ( toDirection.Z is 1 or -1 )  {
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Z is 1 ? RoutingElementExtensions.ConnectorPosition.Top : RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
          }
          else {
            if ( fromDirection.Z is 1 or -1 ) {
              var pullBoxOrigin = ( pullBox.Location as LocationPoint )!.Point ;
              var rotationAngle = Math.Atan2( toDirection.Y, toDirection.X ) ;
              ElementTransformUtils.RotateElement( document, pullBox.Id, Line.CreateBound( pullBoxOrigin, pullBoxOrigin + XYZ.BasisZ ), rotationAngle ) ;
              pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
            }
            else {
              pullBoxToEndPoint = Math.Abs( toDirection.X ) > Math.Abs( toDirection.Y ) 
                ? new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.X < 0 ? RoutingElementExtensions.ConnectorPosition.Left : RoutingElementExtensions.ConnectorPosition.Right ), radius ) 
                : new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Y > 0 ? RoutingElementExtensions.ConnectorPosition.Front : RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
            }
          }
        }
        else {
          if ( Math.Abs( routeDirection.X - 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
          }
          else if ( Math.Abs( routeDirection.X + 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
          }
          else if ( Math.Abs( routeDirection.Y - 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
          }
          else if ( Math.Abs( routeDirection.Y + 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
          }
          else if ( Math.Abs( routeDirection.Z - 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
          }
          else if ( Math.Abs( routeDirection.Z + 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
          }
          else {
            var pullBoxOrigin = ( pullBox.Location as LocationPoint )!.Point ;
            var rotationAngle = Math.Atan2( routeDirection.Y, routeDirection.X ) ;
            ElementTransformUtils.RotateElement( document, pullBox.Id, Line.CreateBound( pullBoxOrigin, pullBoxOrigin + XYZ.BasisZ ), rotationAngle ) ;
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
          }
        }
      }
      else {
        pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetTopConnectorOfConnectorFamily(), radius ) ;
        pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily(), radius ) ;
      }

      return ( pullBoxFromEndPoint, pullBoxToEndPoint ) ;
    }
    
    private static ( RouteSegment?, XYZ? ) GetSegmentThroughPullBox( FamilyInstance pullBox, List<RouteSegment> parentSegments )
    {
      var pullBoxLocation = ( pullBox.Location as LocationPoint ) ! ;
      var pullBoxPoint = pullBoxLocation.Point ;
      RouteSegment? routeSegment = null ;
      XYZ? routeDirection = null ;
      
      foreach ( var segment in parentSegments ) {
        var fromConduitPoint = segment.FromEndPoint.RoutingStartPosition ;
        var toConduitPoint = segment.ToEndPoint.RoutingStartPosition ;
        var direction = segment.FromEndPoint.GetRoutingDirection( true ) ;
        if ( direction.X is 1 or -1 && Math.Abs( pullBoxPoint.Y - fromConduitPoint.Y ) < 0.01 ) {
          if ( ( fromConduitPoint.X < toConduitPoint.X && fromConduitPoint.X < pullBoxPoint.X && pullBoxPoint.X < toConduitPoint.X ) 
               || ( fromConduitPoint.X > toConduitPoint.X && fromConduitPoint.X > pullBoxPoint.X && pullBoxPoint.X > toConduitPoint.X ) ) {
            routeSegment = segment ;
            routeDirection = direction ;
          }
        }
        else if ( direction.Y is 1 or -1 && Math.Abs( pullBoxPoint.X - fromConduitPoint.X ) < 0.01 ) {
          if ( ( fromConduitPoint.Y < toConduitPoint.Y && fromConduitPoint.Y < pullBoxPoint.Y && pullBoxPoint.Y < toConduitPoint.Y ) 
               || ( fromConduitPoint.Y > toConduitPoint.Y && fromConduitPoint.Y > pullBoxPoint.Y && pullBoxPoint.Y > toConduitPoint.Y ) ) {
            routeSegment = segment ;
            routeDirection = direction ;
          }
        }
      }

      return ( routeSegment, routeDirection ) ;
    }

    private static XYZ? GetDirectionOfConduitThroughPullBox( Document document, FamilyInstance pullBox, string routeName, XYZ mainRouteDirection )
    {
      var pullBoxLocation = ( pullBox.Location as LocationPoint ) ! ;
      var pullBoxPoint = pullBoxLocation.Point ;
      XYZ? routeDirection = null ;
      var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( e => e.GetRouteName() == routeName ) ;
      var minDistance = double.MaxValue ;
      
      foreach ( var conduit in conduits ) {
        var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var fromConduitLine = (  fromConduitLocation.Curve as Line ) ! ;
        var fromConduitPoint = fromConduitLine.GetEndPoint( 0 ) ;
        var direction = fromConduitLine.Direction ;
        if ( ( ( mainRouteDirection.X is not 1 && mainRouteDirection.X is not -1 ) && direction.X is 1 or -1 && Math.Abs( pullBoxPoint.Y - fromConduitPoint.Y ) < 0.01 ) 
             || ( ( mainRouteDirection.Y is not 1 && mainRouteDirection.Y is not -1 ) && direction.Y is 1 or -1 && Math.Abs( pullBoxPoint.X - fromConduitPoint.X ) < 0.01 ) ) {
          var distance = pullBoxPoint.DistanceTo( fromConduitPoint ) ;
          if ( ! ( distance < minDistance ) ) continue ;
          minDistance = distance ;
          routeDirection = direction ;
        }
      }

      return routeDirection ;
    }
    
    public static XYZ? GetDirectionOfConduit( Element pullBox, List<Element> conduits)
    {
      var pullBoxLocation = ( pullBox.Location as LocationPoint ) ! ;
      var pullBoxPoint = pullBoxLocation.Point ;
      XYZ? routeDirection = null ;
      var minDistance = double.MaxValue ;
      
      foreach ( var conduit in conduits ) {
        if ( conduit is Conduit ) {
          var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
          var fromConduitLine = (  fromConduitLocation.Curve as Line ) ! ;
          var fromConduitPoint = fromConduitLine.GetEndPoint( 0 ) ;
          var direction = fromConduitLine.Direction ;
          if ( (  direction.X is 1 or -1 && Math.Abs( pullBoxPoint.Y - fromConduitPoint.Y ) < 0.01 ) 
               || ( direction.Y is 1 or -1 && Math.Abs( pullBoxPoint.X - fromConduitPoint.X ) < 0.01 ) ) {
            var distance = pullBoxPoint.DistanceTo( fromConduitPoint ) ;
            if ( ! ( distance < minDistance ) ) continue ;
            minDistance = distance ;
            routeDirection = direction ;
          }
        }
        else if(conduit is FamilyInstance conduitFitting){
          var location = ( conduitFitting.Location as LocationPoint )! ;
          var origin = location.Point ;
          var direction = conduitFitting.FacingOrientation ;
          if ( (  direction.X is 1 or -1 && Math.Abs( pullBoxPoint.Y - origin.Y ) < 0.01 ) 
               || ( direction.Y is 1 or -1 && Math.Abs( pullBoxPoint.X - origin.X ) < 0.01 ) ) {
            var distance = pullBoxPoint.DistanceTo( origin ) ;
            if ( ! ( distance < minDistance ) ) continue ;
            minDistance = distance ;
            routeDirection = direction ;
          }
        }
      }

      return routeDirection ;
    }

    private static bool IsStraightDirection( XYZ direction1, XYZ direction2 )
    {
      return ( direction1.X is 1 && direction2.X is 1 )
             || ( direction1.X is -1 && direction2.X is -1 ) 
             || ( direction1.Y is 1 && direction2.Y is 1 ) 
             || ( direction1.Y is -1 && direction2.Y is -1 ) 
             || ( direction1.Z is 1 && direction2.Z is 1 )
             || ( direction1.Z is -1 && direction2.Z is -1 ) ;
    }

    private static ConduitInfo? GetConduitOfBoard( Document document, string routeName, Element board )
    {
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      Element? conduit = null ;
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( e => e.GetRouteName() == routeName ).ToList() ;
      var boardLocation = ( board.Location as LocationPoint ) ! ;
      var boardOrigin = boardLocation.Point ;
      var minDistance = double.MaxValue ;
      XYZ origin = XYZ.Zero ;
      XYZ direction = XYZ.Zero ;
      double distanceOrigin = 0 ;
      double distanceEnd = 0 ;
      foreach ( var conduitOfRoute in conduitsOfRoute ) {
        var conduitLocation = ( conduitOfRoute.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitLocation.Curve as Line ) ! ;
        var conduitOrigin = conduitLine.GetEndPoint( 0 ) ;
        var conduitEndPoint = conduitLine.GetEndPoint( 1 ) ;
        var conduitDirection = conduitLine.Direction ;
        if ( conduitDirection.Z is 1 or -1 ) continue ;
        distanceOrigin = conduitOrigin.DistanceTo( boardOrigin ) ;
        distanceEnd = conduitEndPoint.DistanceTo( boardOrigin ) ;
        var distance = distanceOrigin < distanceEnd ? distanceOrigin : distanceEnd ;
        if ( distance < minDistance ) {
          minDistance = distance ;
          conduit = conduitOfRoute ;
          origin = distanceOrigin < distanceEnd ? conduitOrigin : conduitEndPoint ;
          direction = conduitDirection ;
        }
      }

      if ( conduit == null ) return null ;
      var level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == conduit.GetLevelId() ) ;
      var ( originX, originY, originZ) = origin ;
      var height = originZ - level!.Elevation ;
      if ( direction.X is 1 or -1 ) {
        originX += direction.X * PullBoxWidth * baseLengthOfLine ;
      }
      else if ( direction.Y is 1 or -1 ) {
        originY += direction.Y * PullBoxLenght * baseLengthOfLine ;
      }

      origin = new XYZ( originX, originY, height ) ;
      var conduitInfo = new ConduitInfo( conduit, origin, direction, level ) ;
      return conduitInfo ;
    }

    private static void SortConduitFitting( ref List<Element> conduits, Route route )
    {
      var sortConduits = new List<Element>() ;
      var toEndPoint = route.FirstToConnector()?.RoutingStartPosition ;
      if ( toEndPoint == null ) return ;
      while ( sortConduits.Count != conduits.Count ) {
        var minDistance = double.MaxValue ;
        var nearestConduit = conduits.First() ;
        XYZ nearestPoint = new() ; 
        foreach ( var conduit in conduits ) {
          if ( sortConduits.Any() && conduit == sortConduits.Last() ) continue ;
          if ( conduit is Conduit ) {
            var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
            var fromConduitLine = (  fromConduitLocation.Curve as Line ) ! ;
            var fromConduitPoint = fromConduitLine.GetEndPoint( 0 ) ;
            var toConduitPoint = fromConduitLine.GetEndPoint( 1 ) ;

            var distance = toConduitPoint.DistanceTo( toEndPoint ) ;
            if ( distance >= minDistance ) continue ;
            minDistance = distance ;
            nearestConduit = conduit ;
            nearestPoint = fromConduitPoint ;
          }
          else if ( conduit is FamilyInstance conduitFitting ) {
            var location = ( conduitFitting.Location as LocationPoint )! ;
            var origin = location.Point ;
            var distance = origin.DistanceTo( toEndPoint ) ;
            if ( distance >= minDistance ) continue ;
            minDistance = distance ;
            nearestConduit = conduit ;
            nearestPoint = origin ;
          }
        }

        toEndPoint = nearestPoint ;
        sortConduits.Add( nearestConduit ) ;
      }

      conduits = sortConduits ;
    }

    private static HiroiMasterModel? GetPullBoxWithAutoCalculatedDimension( Document document, Element pullBoxElement, CsvStorable csvStorable,
      StorageService<Level, DetailSymbolModel> storageDetailSymbolService, List<ConduitsModel> conduitsModelData, List<HiroiMasterModel> hiroiMasterModels )
    {
      var conduitsFromPullBox = GetFromConnectorOfPullBox( document, pullBoxElement, true ) ;
      var conduitsToPullBox = GetFromConnectorOfPullBox( document, pullBoxElement ) ;
      var directionFrom = GetDirectionOfConduit( pullBoxElement, conduitsFromPullBox ) ;
      var directionTo = GetDirectionOfConduit( pullBoxElement, conduitsToPullBox ) ;

      var isStraightDirection = directionFrom != null && directionTo != null &&
                                IsStraightDirection( directionFrom, directionTo ) ;

      conduitsFromPullBox = conduitsFromPullBox.Where( c => c is Conduit ).ToList() ;
      var groupConduits = conduitsFromPullBox.GroupBy( c => c.GetRepresentativeRouteName() ).Select( c => c.First() ) ;
      foreach ( var conduit in groupConduits )
        AddWiringInformationCommandBase.CreateDetailSymbolModel( document, conduit, csvStorable, storageDetailSymbolService,
          pullBoxElement.UniqueId ) ;

      var elementIds = conduitsFromPullBox.Select( c => c.UniqueId ).ToList() ;
      var (detailTableItemModels, _, _) = CreateDetailTableCommandBase.CreateDetailTableItemAddWiringInfo( document, csvStorable,
        storageDetailSymbolService, conduitsFromPullBox, elementIds, false ) ;

      var newDetailTableItemModels = DetailTableViewModel.SummarizePlumbing( detailTableItemModels, conduitsModelData,
        storageDetailSymbolService, new List<DetailTableItemModel>(), false, new Dictionary<string, string>() ) ;

      var plumbingSizes = newDetailTableItemModels.Where( p => int.TryParse( p.PlumbingSize, out _ ) )
        .Select( p => Convert.ToInt32( p.PlumbingSize ) ).ToArray() ;
      var (depth, width, height) = GetPullBoxDimension( plumbingSizes, isStraightDirection ) ;

      if ( depth == 0 || width == 0 || height == 0 )
        return null ;
      var satisfiedHiroiMasterModels = hiroiMasterModels
        .Where( p => p.Tani == TaniOfPullBox && p.Hinmei.Contains( HinmeiOfPullBox ) ).Where( p =>
        {
          var (d, w, h) = ParseKikaku( p.Kikaku ) ;
          return d >= depth && w >= width && h >= height ;
        } ).ToList() ;

      if ( ! satisfiedHiroiMasterModels.Any() )
        return null ;
        
      var minPullBoxModelDepth = satisfiedHiroiMasterModels.Min( x =>
        {
          var (d, _, _) = ParseKikaku( x.Kikaku ) ;
          return d ;
        } ) ;

      var pullBoxModel = hiroiMasterModels.FirstOrDefault( p =>
      {
        var (d, _, h) = ParseKikaku( p.Kikaku ) ;
        return h == height && d == minPullBoxModelDepth ;
      } ) ;
      return pullBoxModel ;
    }

    public static (int depth, int width, int height) ParseKikaku( string kikaku )
    {
      var kikakuRegex = new Regex( "(?!\\d)*(?<kikaku>((\\d+(x)){2}(\\d+)))(?!\\d)*" ) ;
      var m = kikakuRegex.Match( kikaku ) ;
      if ( m.Success ) {
        var strKikaku = m.Groups[ "kikaku" ].Value.Split( 'x' ) ;
        if ( strKikaku.Length == 3 ) {
          var depth = Convert.ToInt32( strKikaku[ 0 ] ) ;
          var width = Convert.ToInt32( strKikaku[ 1 ] ) ;
          var height = Convert.ToInt32( strKikaku[ 2 ] ) ;
          return ( depth, width, height ) ;
        }
      }

      return ( 0, 0, 0 ) ;
    }

    public static PullBoxInfo GetPullBoxInfo( Document document, string routeName, FamilyInstance conduitFitting )
    {
      var offset = ( 300.0 ).MillimetersToRevitUnits() ;
      var conduitFittingLocation = ( conduitFitting.Location as LocationPoint ) ! ;
      var conduitFittingPoint = conduitFittingLocation.Point ;
      var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      var fromMinDistance = double.MaxValue ;
      var toMinDistance = double.MaxValue ;
      var fromPoint = new XYZ() ;
      var toPoint = new XYZ() ;
      var fromConduitDirection = new XYZ() ;
      var toConduitDirection = new XYZ() ;
      foreach ( var conduit in conduits ) {
        var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var fromConduitLine = ( fromConduitLocation.Curve as Line ) ! ;
        var fromConduitPoint = fromConduitLine.GetEndPoint( 1 ) ;
        var fromDistance = fromConduitPoint.DistanceTo( conduitFittingPoint ) ;
        if ( fromDistance < fromMinDistance ) {
          fromMinDistance = fromDistance ;
          fromConduitDirection = fromConduitLine.Direction ;
          fromPoint = fromConduitPoint ;
        }

        var toConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var toConduitLine = ( toConduitLocation.Curve as Line ) ! ;
        var toConduitPoint = toConduitLine.GetEndPoint( 0 ) ;
        var toDistance = toConduitPoint.DistanceTo( conduitFittingPoint ) ;
        if ( toDistance > toMinDistance ) continue ;
        toMinDistance = toDistance ;
        toConduitDirection = fromConduitLine.Direction ;
        toPoint = toConduitPoint ;
      }

      double x = 0, y = 0, z = 0 ;
      var ( xInterSection, yInterSection ) = GetInterSectionOfTwoLines( fromPoint.X, fromPoint.Y, fromConduitDirection.X, fromConduitDirection.Y, toPoint.X, toPoint.Y, toConduitDirection.X, toConduitDirection.Y ) ;
      if ( fromConduitDirection.X is 1 or -1 ) {
        if ( toConduitDirection.X is 1 or -1 || toConduitDirection.Y is 1 or -1 || toConduitDirection.Z is 1 or -1 ) {
          x = toPoint.X ;
          y = fromPoint.Y ;
          z = fromPoint.Z ;
        }
        else {
          var fromLine = new MathLib.Line( new Vector3d( fromPoint.X, fromPoint.Y, fromPoint.Z ), new Vector3d( fromConduitDirection.X, fromConduitDirection.Y, fromConduitDirection.Z ) ) ;
          ( x, y, z ) = fromLine.GetPointAt( fromPoint.Y - toPoint.Y ) ;
          if ( xInterSection != null ) x = (double) xInterSection - fromConduitDirection.X * offset ;
          toConduitDirection = fromConduitDirection ;
        }
      }
      else if ( fromConduitDirection.Y is 1 or -1 ) {
        if ( toConduitDirection.X is 1 or -1 || toConduitDirection.Y is 1 or -1 || toConduitDirection.Z is 1 or -1 ) {
          x = fromPoint.X ;
          y = toPoint.Y ;
          z = fromPoint.Z ;
        }
        else {
          var fromLine = new MathLib.Line( new Vector3d( fromPoint.X, fromPoint.Y, fromPoint.Z ), new Vector3d( fromConduitDirection.X, fromConduitDirection.Y, fromConduitDirection.Z ) ) ;
          ( x, y, z ) = fromLine.GetPointAt( fromPoint.X - toPoint.X ) ;
          if ( yInterSection != null ) y = (double) yInterSection - fromConduitDirection.Y * offset ;
          toConduitDirection = fromConduitDirection ;
        }
      }
      else if ( fromConduitDirection.Z is 1 or -1 ) {
        x = fromPoint.X ;
        y = fromPoint.Y ;
        z = toPoint.Z ;
      }
      else {
        if ( toConduitDirection.X is 1 or -1 ) {
          var toLine = new MathLib.Line( new Vector3d( toPoint.X, toPoint.Y, toPoint.Z ), new Vector3d( toConduitDirection.X, toConduitDirection.Y, toConduitDirection.Z ) ) ;
          ( x, y, z ) = toLine.GetPointAt( toPoint.Y - fromPoint.Y ) ;
          if ( xInterSection != null ) x = (double) xInterSection + toConduitDirection.X * offset ;
          fromConduitDirection = toConduitDirection ;
        }
        else if ( toConduitDirection.Y is 1 or -1 ) {
          var toLine = new MathLib.Line( new Vector3d( toPoint.X, toPoint.Y, toPoint.Z ), new Vector3d( toConduitDirection.X, toConduitDirection.Y, toConduitDirection.Z ) ) ;
          ( x, y, z ) = toLine.GetPointAt( toPoint.X - fromPoint.X ) ;
          if ( yInterSection != null ) y = (double) yInterSection + toConduitDirection.Y * offset ;
          fromConduitDirection = toConduitDirection ;
        }
        else {
          x = toPoint.X ;
          y = toPoint.Y ;
          z = fromPoint.Z ;
        }
      }

      var position = new XYZ( x, y, z ) ;
      var level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == conduits.First().GetLevelId() ) ;
      return new PullBoxInfo( position, fromConduitDirection, toConduitDirection, level! ) ;
    }
    
    private static PullBoxInfo GetPullBoxInShaftInfo( Document document, string routeName, FamilyInstance conduitFitting )
    {
      var conduitFittingLocation = ( conduitFitting.Location as LocationPoint ) ! ;
      var conduitFittingPoint = conduitFittingLocation.Point ;
      var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      var fromMinDistance = double.MaxValue ;
      var toMinDistance = double.MaxValue ;
      var fromPoint = new XYZ() ;
      var toPoint = new XYZ() ;
      var fromConduitDirection = new XYZ() ;
      var toConduitDirection = new XYZ() ;
      foreach ( var conduit in conduits ) {
        var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var fromConduitLine = ( fromConduitLocation.Curve as Line ) ! ;
        var fromConduitPoint = fromConduitLine.GetEndPoint( 1 ) ;
        var fromDistance = fromConduitPoint.DistanceTo( conduitFittingPoint ) ;
        if ( fromDistance < fromMinDistance ) {
          fromMinDistance = fromDistance ;
          fromConduitDirection = fromConduitLine.Direction ;
          fromPoint = fromConduitPoint ;
        }

        var toConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var toConduitLine = ( toConduitLocation.Curve as Line ) ! ;
        var toConduitPoint = toConduitLine.GetEndPoint( 0 ) ;
        var toDistance = toConduitPoint.DistanceTo( conduitFittingPoint ) ;
        if ( toDistance > toMinDistance ) continue ;
        toMinDistance = toDistance ;
        toConduitDirection = fromConduitLine.Direction ;
        toPoint = toConduitPoint ;
      }

      double x = 0, y = 0, z = 0 ;
      if ( fromConduitDirection.X is 1 or -1 ) {
        x = toPoint.X ;
        y = fromPoint.Y ;
        z = fromPoint.Z ;
      }
      else if ( fromConduitDirection.Y is 1 or -1 ) {
        x = fromPoint.X ;
        y = toPoint.Y ;
        z = fromPoint.Z ;
      }
      else if ( fromConduitDirection.Z is 1 or -1 ) {
        x = fromPoint.X ;
        y = fromPoint.Y ;
        z = toPoint.Z ;
      }

      var position = new XYZ( x, y, z ) ;
      var level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == conduitFitting.GetLevelId() ) ;
      return new PullBoxInfo( position, fromConduitDirection, toConduitDirection, level! ) ;
    }
    
    private static ( double?, double? ) GetInterSectionOfTwoLines( double x1, double y1, double u1, double a1, double x2, double y2, double u2, double a2 )
    {
      var b1 = -u1 ;
      var b2 = -u2 ;
      var c1 = a1 * x1 + b1 * y1 ;
      var c2 = a2 * x2 + b2 * y2 ;
      var delta = a1 * b2 - a2 * b1 ;
      if ( delta == 0 ) return ( null, null ) ;
      var x = ( b2 * c1 - b1 * c2 ) / delta ;
      var y = ( a1 * c2 - a2 * c1 ) / delta ;
      return ( x, y ) ;
    }

    public static IEnumerable<TextNote> GetTextNotesOfPullBox( Document document, bool isOnlyCalculatedSizePullBoxes = false )
    {
      var pullBoxes = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( e => e.GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) ;
      if ( isOnlyCalculatedSizePullBoxes )
        pullBoxes = pullBoxes.Where( e => Convert.ToBoolean( e.ParametersMap.get_Item( IsAutoCalculatePullBoxSizeParameter ).AsString() ) ) ;
      var pullBoxUniqueIds = pullBoxes.Select( e => e.UniqueId ).ToList() ;
      var level = document.ActiveView.GenLevel ;
      var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
      var pullBoxInfoModels = storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData
        .Where( p => pullBoxUniqueIds.Contains( p.PullBoxUniqueId ) ) ;
      var textNote = document.GetAllElements<TextNote>()
        .Where( t => pullBoxInfoModels.Any( p => p.TextNoteUniqueId == t.UniqueId ) ) ;
      return textNote ;
    }
    
    public static string IsSegmentConnectedToPullBox( Document document, RouteSegment lastSegment )
    {
      var pullBoxUniqueId = string.Empty ;
      var toEndPointKey = lastSegment.ToEndPoint.Key ;
      var toElementId = toEndPointKey.GetElementUniqueId() ;
      if ( string.IsNullOrEmpty( toElementId ) ) 
        return pullBoxUniqueId ;
      var toConnector = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .FirstOrDefault( c => c.UniqueId == toElementId ) ;
      if ( toConnector != null && toConnector.GetConnectorFamilyType() == ConnectorFamilyType.PullBox )
        pullBoxUniqueId = toConnector.UniqueId ;
      return pullBoxUniqueId ;
    }

    public static IEnumerable<Route> GetParentRoutesInTheSamePosition( Document document, List<Route> routes, Route selectedRoute, Element selectedConduit )
    {
      var result = new List<Route> { selectedRoute } ;
      var (fromEndPointOfSelectedConduit, toEndPointOfSelectedConduit) = ConduitUtil.GetFromElementIdAndToElementIdOfConduit( selectedConduit ) ;
      if ( fromEndPointOfSelectedConduit == null || toEndPointOfSelectedConduit == null ) return result ;

      var fromElementIdOfSelectedConduit = fromEndPointOfSelectedConduit.Key.GetElementUniqueId() ;
      var toElementIdOfSelectedConduit = toEndPointOfSelectedConduit.Key.GetElementUniqueId() ;

      var isConduitFittingAtBranchRoute = selectedConduit.Category.GetBuiltInCategory() == BuiltInCategory.OST_ConduitFitting && fromEndPointOfSelectedConduit.DisplayTypeName == PassPointBranchEndPoint.Type ;

      var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ) ;
      foreach ( var conduit in allConduits ) {
        if ( string.IsNullOrEmpty( conduit.GetRouteName() ) ) continue ;
        var route = routes.FirstOrDefault( r => r.RouteName == conduit.GetRouteName() ) ;
        if ( route == null || result.Any( r => r.RouteName == route.RouteName ) ) continue ;

        var (fromEndPoint, toEndPoint) = ConduitUtil.GetFromElementIdAndToElementIdOfConduit( conduit ) ;
        if ( fromEndPoint == null || toEndPoint == null ) continue ;

        if ( fromEndPoint.Key.GetElementUniqueId() == fromElementIdOfSelectedConduit && ( isConduitFittingAtBranchRoute || toEndPoint.Key.GetElementUniqueId() == toElementIdOfSelectedConduit ) )
          result.Add( route ) ;
      }

      result = result.Where( r => r.IsParentBranch( r ) || ! r.HasParent() ).ToList() ;
      if ( ! result.Any() )
        result.Add( selectedRoute ) ;

      return result ;
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetBranchSegmentsByRepresentativeName( Document document, RouteCache routeCache, Route parentRoute )
    {
      var routes = new List<Route>() ;
      var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ) ;
      foreach ( var conduit in allConduits ) {
        if ( string.IsNullOrEmpty( conduit.GetRouteName() ) ) continue ;
        var route = routeCache.FirstOrDefault( r => r.Key == conduit.GetRouteName() ).Value ;
        if ( route == null || routes.Any( r => r.RouteName == route.RouteName ) ) continue ;
        if ( parentRoute.RouteName == conduit.GetRepresentativeRouteName() && parentRoute.RouteName != route.RouteName )
          routes.Add( route ) ;
      }

      return routes.Where( x => x.RouteSegments.Count() <= 1 ).ToSegmentsWithName().ToList() ;
    }
    
    private static FamilyInstance? FindPullBoxByLocation( Document document, double originX, double originY, double originZ )
    {
      var pullBoxes = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( c => c.GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) ;
      
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      double minDistance = ( 400.0 ).MillimetersToRevitUnits() * baseLengthOfLine;

      return pullBoxes.SingleOrDefault( p =>
      {
        var locationPoint = ( p.Location as LocationPoint )?.Point ;
        return locationPoint != null && locationPoint.DistanceTo( new XYZ( originX, originY, originZ ) ) < minDistance ;
      } ) ;
    }
    
    private class ConduitInfo
    {
      public Element Conduit { get ; }
      public XYZ ConduitOrigin { get ; }
      public XYZ  ConduitDirection { get ; }
      public Level Level { get ; }

      public ConduitInfo( Element conduit, XYZ conduitOrigin, XYZ conduitDirection, Level level )
      {
        Conduit = conduit ;
        ConduitOrigin = conduitOrigin ;
        ConduitDirection = conduitDirection ;
        Level = level ;
      }
    }

    public class PullBoxInfo
    {
      public XYZ Position { get ; }
      public XYZ FromDirection { get ; }
      public XYZ ToDirection { get ; }
      public Level Level { get ; }

      public PullBoxInfo( XYZ position, XYZ fromDirection, XYZ toDirection, Level level  )
      {
        Position = position ;
        FromDirection = fromDirection ;
        ToDirection = toDirection ;
        Level = level ;
      }
    }
    
    public class FailurePreprocessor : IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures( FailuresAccessor failuresAccessor )
      {
        var failureMessages = failuresAccessor.GetFailureMessages() ;
        foreach ( var message in failureMessages ) {
          if ( message.GetSeverity() == FailureSeverity.Warning )
            failuresAccessor.DeleteWarning( message ) ;
        }

        return FailureProcessingResult.Continue ;
      }
    }
  }
}