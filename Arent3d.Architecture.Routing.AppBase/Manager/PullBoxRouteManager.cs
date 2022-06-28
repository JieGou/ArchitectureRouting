using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class PullBoxRouteManager
  {
    private static readonly double DefaultDistanceHeight = ( 200.0 ).MillimetersToRevitUnits() ;
    private const string DefaultConstructionItem = "未設定" ;
    private static readonly double PullBoxWidth = ( 20.0 ).MillimetersToRevitUnits() * 4 ;
    private static readonly double PullBoxLenght = ( 15.0 ).MillimetersToRevitUnits() * 4 ;

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, Route route, Element element, FamilyInstance pullBox, double heightConnector, 
      double heightWire, XYZ routeDirection, bool isCreatePullBoxWithoutSettingHeight, string nameBase, List<string> withoutRouteNames, XYZ? fromDirection = null, XYZ? toDirection = null )
    {
      var ( routeRecords, parentRoute ) = GetRelatedBranchSegments( route ) ;
      var subRoute = route.SubRoutes.First() ;

      var detector = new RouteSegmentDetector( subRoute, element ) ;
      var diameter = parentRoute.UniqueDiameter ;
      var radius = diameter * 0.5 ;
      var isRoutingOnPipeSpace = parentRoute.UniqueIsRoutingOnPipeSpace ?? false ;
      var toFixedHeight = parentRoute.UniqueToFixedHeight ;
      var avoidType = parentRoute.UniqueAvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = parentRoute.UniqueShaftElementUniqueId ;
      var fromFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, isCreatePullBoxWithoutSettingHeight ? heightConnector : heightConnector + DefaultDistanceHeight ) ;
      var fromFixedHeightSecond = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightWire ) ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var (pullBoxFromEndPoint, pullBoxToEndPoint) = GetFromAndToConnectorEndPoint( pullBox, isCreatePullBoxWithoutSettingHeight, radius, routeDirection, fromDirection, toDirection ) ;

      var isBeforeSegment = true ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var ceedCodes = new List<string>() ;
      var isPassingThrough = parentRoute.RouteSegments.FirstOrDefault( s => detector.IsPassingThrough( s ) ) != null ;
      var beforeSegments = new List<RouteSegment>() ;
      if ( isPassingThrough ) {
        foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
          if ( detector.IsPassingThrough( segment ) ) {
            isBeforeSegment = false ;
            var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
            result.Add( ( name, newSegment ) ) ;
            beforeSegments.Add( newSegment ) ;
            result.Add( ( parentRoute.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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

            var newSegment = isBeforeSegment ? 
              new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId )
              : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ;
            result.Add( isBeforeSegment ? ( name, newSegment ) : ( parentRoute.RouteName, newSegment ) ) ;
            if ( isBeforeSegment ) beforeSegments.Add( newSegment ) ;
          }
        }
        
        var connectorUniqueId = parentRoute.RouteSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
        GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;

        if ( ! routeRecords.Any() ) return result ;
        {
          foreach ( var (routeName, segment) in routeRecords ) {
            var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
            if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
              var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? pullBoxToEndPoint.Key ;
              var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
              if ( fromEndPointKey == pullBoxToEndPoint.Key ) {
                name = nameBase + "_" + (++nextIndex) ;
                result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
                result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
                connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
                GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
                withoutRouteNames.Add( name ) ;
              }
              else {
                result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
              }
            }
            else {
              result.Add( ( routeName, segment ) ) ;
            }
          }
        }
      }
      else {
        result = GetSegments( document, routeRecords, ceedCodes, pullBox, parentRoute, detector, nameBase, nextIndex, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, 
          fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId, radius, isCreatePullBoxWithoutSettingHeight, pullBoxFromEndPoint, pullBoxToEndPoint ) ;
      }

      if ( ceedCodes.Any() ) {
        var newCeedCode = string.Join( ";", ceedCodes ) ;
        pullBox.TrySetProperty( ElectricalRoutingElementParameter.CeedCode, newCeedCode ) ;
      }

      return result ;
    }

    private static List<(string RouteName, RouteSegment Segment)> GetSegments( Document document, List<(string RouteName, RouteSegment Segment)> routeRecords, List<string> ceedCodes, 
      FamilyInstance pullBox, Route parentRoute, RouteSegmentDetector detector, string nameBase, int nextIndex, double? diameter, bool isRoutingOnPipeSpace, FixedHeight? fromFixedHeightFirst, FixedHeight? fromFixedHeightSecond, 
      FixedHeight? toFixedHeight, AvoidType avoidType, string? shaftElementUniqueId, double? radius, bool isCreatePullBoxWithoutSettingHeight, ConnectorEndPoint pullBoxFromEndPoint, ConnectorEndPoint pullBoxToEndPoint )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      string connectorUniqueId ;
      var isBeforeSegment = true ;
      var name = nameBase + "_" + nextIndex ;
      var parentSegments = parentRoute.RouteSegments.EnumerateAll().ToList() ;
      var ( routeSegment, routeDirection ) = GetSegmentThroughPullBox( pullBox, parentSegments ) ;
      if ( routeDirection == null ) {
        result.AddRange( from segment in parentSegments select ( parentRoute.RouteName, segment ) ) ;
        connectorUniqueId = parentSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
        GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
        foreach ( var (routeName, segment) in routeRecords ) {
          if ( detector.IsPassingThrough( segment ) ) {
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
          }
          else {
            result.Add( ( routeName, segment ) ) ;
            connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
            GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
          }
        }
        return result ;
      }

      var (mainPullBoxFromEndPoint, mainPullBoxToEndPoint) = GetFromAndToConnectorEndPoint( pullBox, isCreatePullBoxWithoutSettingHeight, radius, routeDirection, null, null ) ;
      
      foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
        if ( segment == routeSegment ) {
          isBeforeSegment = false ;
          var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, mainPullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
          result.Add( ( name, newSegment ) ) ;
          result.Add( ( parentRoute.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, mainPullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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
            ? new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) 
            : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ;
          result.Add( isBeforeSegment ? ( name, newSegment ) : ( parentRoute.RouteName, newSegment ) ) ;
        }
      }
      
      connectorUniqueId = parentSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
      GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
      
      foreach ( var (routeName, segment) in routeRecords ) {
        if ( detector.IsPassingThrough( segment ) ) {
          var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
          nextIndex++ ;
          name = nameBase + "_" + nextIndex ;
          if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
            var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? mainPullBoxToEndPoint.Key ;
            var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
          }
          else {
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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
              result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            }
            else {
              nextIndex++ ;
              name = nameBase + "_" + nextIndex ;
              var (_, branchPullBoxToEndPoint) = GetFromAndToConnectorEndPoint( pullBox, isCreatePullBoxWithoutSettingHeight, radius, branchRouteDirection, null, null ) ;
              result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchPullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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

    public static (int depth, int width, int height) CalculatePullBoxDimension(int[] plumbingSizes, bool isStraightDirection)
    {
      int depth, width ;
      int maxPlumbingSize = plumbingSizes.Max() ;
      if ( isStraightDirection ) {
        depth = plumbingSizes.Sum( x => x + 30 ) + ( 30 * 2 ) ;
        width = maxPlumbingSize * 8 ;
      }
      else {
        depth = width = plumbingSizes.Sum( x => x + 30 ) + 30 + 8 * maxPlumbingSize ;
      }
      var height = GetHeightByPlumbingSize( maxPlumbingSize ) ;
      return ( depth, width, height ) ;
    }
    
    private static int GetHeightByPlumbingSize( int plumbingSize )
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
    
    public static List<Element> GetFromConnectorOfPullBox( Document document, Element element, bool isFrom = false)
    {
      List<Element> result = new List<Element>() ;
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

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSegmentsWithPullBox ( Document document, IReadOnlyCollection<Route> executeResultValue, List<string> boardUniqueIds, List<XYZ> pullBoxPositions, List<string> withoutRouteNames )
    {
      const string angleParameter = "角度" ;
      const double maxAngle = 270 ;
      const double maxLength = 30 ;

      var defaultSettingStorable = document.GetDefaultSettingStorable() ;
      var grade = defaultSettingStorable.GradeSettingData.GradeMode ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      if ( grade is 1 or 2 or 3 ) {
        string conduitFittingLengthParam = "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( document, "電線管長さ" ) ;
        string conduitLengthParam = "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ;
        var registrationOfBoardDataModels = document.GetRegistrationOfBoardDataStorable().RegistrationOfBoardData ;
        var beforeResult = executeResultValue.ToList().Where( r => ! withoutRouteNames.Contains( r.RouteName ) ) ;
        foreach ( var route in beforeResult ) {
          var allConduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
          var conduitFittingsOfRoute = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;

          double sumLength = 0 ;
          double sumAngle = 0 ;
          FamilyInstance? selectedConduitFitting = null ;
          for ( var i = conduitFittingsOfRoute.Count - 1 ; i > -1 ; i-- ) {
            if ( conduitFittingsOfRoute[ i ].HasParameter( angleParameter ) ) {
              var angle = conduitFittingsOfRoute[ i ].ParametersMap.get_Item( angleParameter ).AsDouble() ;
              sumAngle += angle ;
            }

            if ( sumAngle < maxAngle ) continue ;
            selectedConduitFitting = conduitFittingsOfRoute[ i ] ;
            break ;
          }

          Element? selectedConduit = null ;
          for ( var i = allConduitsOfRoute.Count - 1 ; i > -1 ; i-- ) {
            if ( allConduitsOfRoute[ i ].HasParameter( conduitLengthParam ) ) {
              var length = allConduitsOfRoute[ i ].ParametersMap.get_Item( conduitLengthParam ).AsDouble() ;
              sumLength += length ;
            }

            if ( allConduitsOfRoute[ i ].HasParameter( conduitFittingLengthParam ) ) {
              var length = allConduitsOfRoute[ i ].ParametersMap.get_Item( conduitFittingLengthParam ).AsDouble() ;
              sumLength += length ;
            }

            if ( sumLength < maxLength ) continue ;
            selectedConduit = allConduitsOfRoute[ i ] ;
            break ;
          }

          var connectorsOfRoute = route.GetAllConnectors().ToList() ;
          var boardConnector = new List<Element>() ;
          foreach ( var connector in connectorsOfRoute ) {
            var element = connector.Owner ;
            if ( element == null || boardUniqueIds.Contains( element.UniqueId ) ) continue;
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
            result = CreatePullBoxAndGetSegments( document, route, conduitInfo.Conduit, originX, originY, originZ, conduitInfo.Level, conduitInfo.ConduitDirection, nameBase!, withoutRouteNames ).ToList() ;
            boardUniqueIds.Add( board.UniqueId ) ;
            return result ;
          }
          
          if ( conduitFittingsOfRoute.Count >= 4 ) {
            var conduitFitting = conduitFittingsOfRoute.ElementAt( conduitFittingsOfRoute.Count - 4 ) ;
            var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
            var isSamePullBoxPositions = ComparePullBoxPosition( pullBoxPositions, pullBoxInfo.Position ) ;
            if ( isSamePullBoxPositions ) continue ;
            
            var ( originX, originY, originZ)  = pullBoxInfo.Position ;
            var fromDirection = pullBoxInfo.FromDirection ;
            var toDirection = pullBoxInfo.ToDirection ;
            var height = originZ - pullBoxInfo.Level.Elevation ;
            result = CreatePullBoxAndGetSegments( document, route, conduitFitting, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, withoutRouteNames, fromDirection, toDirection ).ToList() ;
            pullBoxPositions.Add( pullBoxInfo.Position ) ;
            return result ;
          }

          if ( sumAngle > maxAngle && selectedConduitFitting != null ) {
            var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, selectedConduitFitting ) ;
            var isSamePullBoxPositions = ComparePullBoxPosition( pullBoxPositions, pullBoxInfo.Position ) ;
            if ( isSamePullBoxPositions ) continue ;
            
            var ( originX, originY, originZ)  = pullBoxInfo.Position ;
            var fromDirection = pullBoxInfo.FromDirection ;
            var toDirection = pullBoxInfo.ToDirection ;
            var height = originZ - pullBoxInfo.Level.Elevation ;
            result = CreatePullBoxAndGetSegments( document, route, selectedConduitFitting, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, withoutRouteNames, fromDirection, toDirection ).ToList() ;
            pullBoxPositions.Add( pullBoxInfo.Position ) ;
            return result ;
          }

          if ( sumLength > maxLength && selectedConduit != null ) {
            double originX = 0, originY = 0, originZ = 0, height = 0 ;
            Level? level = null ;
            XYZ? direction = null ;
            XYZ? fromDirection = null ;
            XYZ? toDirection = null ;
            if ( selectedConduit is Conduit ) {
              level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == selectedConduit.GetLevelId() ) ;
              var location = ( selectedConduit.Location as LocationCurve ) ! ;
              var line = ( location.Curve as Line ) ! ;
              ( originX, originY, originZ) = line.GetEndPoint( 0 ) ;
              direction = line.Direction ;
              var length = sumLength - maxLength ;
              if ( direction.X is 1 or -1 ) {
                originX += direction.X * length ;
              }
              else if ( direction.Y is 1 or -1 ) { 
                originY += direction.Y * length ;
              }
              else if ( direction.Z is 1 or -1 ) { 
                originZ += direction.Z * length ;
              }
              height = originZ - level!.Elevation ;
              var isSamePullBoxPositions = ComparePullBoxPosition( pullBoxPositions, new XYZ( originX, originY, originZ ) ) ;
              if ( isSamePullBoxPositions ) continue ;
            }
            else if ( selectedConduit is FamilyInstance conduitFitting ) {
              var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
              var isSamePullBoxPositions = ComparePullBoxPosition( pullBoxPositions, pullBoxInfo.Position ) ;
              if ( isSamePullBoxPositions ) continue ;
            
              ( originX, originY, originZ ) = pullBoxInfo.Position ;
              level = pullBoxInfo.Level ;
              height = originZ - level.Elevation ;
              direction = pullBoxInfo.FromDirection ;
              fromDirection = pullBoxInfo.FromDirection ;
              toDirection = pullBoxInfo.ToDirection ;
            }
            
            result = CreatePullBoxAndGetSegments( document, route, selectedConduit, originX, originY, height, level, direction, nameBase!, withoutRouteNames, fromDirection, toDirection ).ToList() ;
            pullBoxPositions.Add( new XYZ( originX, originY, originZ ) ) ;
            return result ;
          }
        }
      }
      
      return result ;
    }

    private static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreatePullBoxAndGetSegments( Document document, Route route, Element element, double originX, double originY, double originZ, 
      Level? level, XYZ? direction, string nameBase, List<string> withoutRouteNames, XYZ? fromDirection = null, XYZ? toDirection = null )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      try {
        using Transaction t = new( document, "Create pull box" ) ;
        t.Start() ;
        var pullBox = GenerateConnector( document, ElectricalRoutingFamilyType.PullBox, ConnectorFamilyType.PullBox, originX, originY, originZ , level!, route.RouteName ) ;
        t.Commit() ;

        using Transaction t1 = new( document, "Get segments" ) ;
        t1.Start() ;
        result.AddRange( GetRouteSegments( document, route, element, pullBox, originZ, originZ, direction!, true, nameBase, withoutRouteNames, fromDirection, toDirection ) ) ;
        t1.Commit() ;
      }
      catch {
        //
      }
      return result ;
    }

    public static void CreateTextNoteAndGroupWithPullBox(Document doc, XYZ point, Element pullBox, string text)
    {
      var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;
      
      var txtPosition = new XYZ( point.X, point.Y, point.Z ) ;
      
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, text, opts ) ;

      var textNoteType = textNote.TextNoteType ;
      double newSize = ( 1.0 / 4.0 ) * TextNoteHelper.TextSize.MillimetersToRevitUnits() ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( newSize ) ;
      textNote.ChangeTypeId( textNoteType.Id ) ;
      
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      groupIds.Add( pullBox.Id ) ;
      groupIds.Add( textNote.Id ) ;
      doc.Create.NewGroup( groupIds ) ;
    }

    private static bool ComparePullBoxPosition( IEnumerable<XYZ> pullBoxPositions, XYZ newPullBoxPosition )
    {
      var minDistance = ( 300.0 ).MillimetersToRevitUnits() ;
      foreach ( var pullBoxPosition in pullBoxPositions ) {
        if ( newPullBoxPosition.DistanceTo( pullBoxPosition ) < minDistance ) {
          return true ;
        }
      }

      return false ;
    }

    private static ( ConnectorEndPoint, ConnectorEndPoint ) GetFromAndToConnectorEndPoint( FamilyInstance pullBox, bool isCreatePullBoxWithoutSettingHeight, double? radius, XYZ routeDirection, XYZ? fromDirection, XYZ? toDirection )
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
          else {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.Z is 1 ? RoutingElementExtensions.ConnectorPosition.Bottom : RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
          }
          
          if ( toDirection.X is 1 or -1 ) {
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.X is 1 ? RoutingElementExtensions.ConnectorPosition.Right : RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
          }
          else if ( toDirection.Y is 1 or -1 ) {
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Y is 1 ? RoutingElementExtensions.ConnectorPosition.Back : RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
          }
          else {
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Z is 1 ? RoutingElementExtensions.ConnectorPosition.Top : RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
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
          else {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
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

    public static bool IsStraightDirection( XYZ direction1, XYZ direction2 )
    {
      return ( direction1.X is 1 && direction2.X is -1 )
             || ( direction1.X is -1 && direction2.X is 1 ) 
             || ( direction1.Y is 1 && direction2.Y is -1 ) 
             || ( direction1.Y is -1 && direction2.Y is 1 ) 
             || ( direction1.Z is 1 && direction2.Z is -1 )
             || ( direction1.Z is -1 && direction2.Z is 1 ) ;
    }

    private static ConduitInfo? GetConduitOfBoard( Document document, string routeName, Element board )
    {
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
        originX += distanceOrigin < distanceEnd ? direction.X * PullBoxWidth : - direction.X * PullBoxWidth ;
      }
      else if ( direction.Y is 1 or -1 ) {
        originY += distanceOrigin < distanceEnd ? direction.Y * PullBoxLenght : - direction.Y * PullBoxLenght ;
      }

      origin = new XYZ( originX, originY, height ) ;
      var conduitInfo = new ConduitInfo( conduit, origin, direction, level! ) ;
      return conduitInfo ;
    }

    private static ConduitInfo? GetConduitInfo( Document document, List<Element> allConduits, Element conduitFitting )
    {
      string conduitLengthParam = "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ;
      var defaultMinLenght = ( 100.0 ).MillimetersToRevitUnits() ;
      var conduitFittingLocation = ( conduitFitting.Location as LocationPoint ) ! ;
      var conduitFittingPoint = conduitFittingLocation.Point ;
      var conduits = allConduits.Where( c => c.GetBuiltInCategory() == BuiltInCategory.OST_Conduit ).ToList() ;
      var minDistance = double.MaxValue ;
      var fromConduit = conduits.First() ;
      foreach ( var conduit in conduits ) {
        var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var fromConduitLine = ( fromConduitLocation.Curve as Line ) ! ;
        var fromConduitPoint = fromConduitLine.GetEndPoint( 1 ) ;
        var distance = fromConduitPoint.DistanceTo( conduitFittingPoint ) ;
        var length = conduit.ParametersMap.get_Item( conduitLengthParam ).AsDouble() ;
        if ( distance > minDistance || length < defaultMinLenght ) continue ;
        minDistance = distance ;
        fromConduit = conduit ;
      }

      if ( fromConduit == null ) return null ;
      {
        var fromConduitLocation = ( fromConduit.Location as LocationCurve ) ! ;
        var fromConduitLine = ( fromConduitLocation.Curve as Line ) ! ;
        var fromConduitPoint = fromConduitLine.GetEndPoint( 1 ) ;
        var fromConduitDirection = fromConduitLine.Direction ;

        double x = 0 ;
        double y = 0 ;
        if ( fromConduitDirection.X is 1 or -1 ) {
          x = fromConduitPoint.X - fromConduitDirection.X * PullBoxWidth ;
          y = fromConduitPoint.Y ;
        }
        else if ( fromConduitDirection.Y is 1 or -1 ) {
          x = fromConduitPoint.X ;
          y = fromConduitPoint.Y - fromConduitDirection.Y * PullBoxLenght ;
        }

        var level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == fromConduit.GetLevelId() ) ;
        var height = fromConduitPoint.Z - level!.Elevation ;
        var pullBoxPosition = new XYZ( x, y, height) ;
        return new ConduitInfo( fromConduit, pullBoxPosition, fromConduitDirection, level! ) ;
      }
    }

    public static PullBoxInfo GetPullBoxInfo( Document document, string routeName, FamilyInstance conduitFitting )
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
      var level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == conduits.First().GetLevelId() ) ;
      return new PullBoxInfo( position, fromConduitDirection, toConduitDirection, level! ) ;
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