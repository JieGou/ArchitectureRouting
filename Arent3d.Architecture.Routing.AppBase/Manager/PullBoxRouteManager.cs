using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
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
    private const double NearShaftTolerance = 0.01 ;
    private static readonly double PullBoxWidth = ( 300.0 ).MillimetersToRevitUnits() ;
    private static readonly double PullBoxLenght = ( 250.0 ).MillimetersToRevitUnits() ;
    private const string HinmeiOfPullBox = "プルボックス" ;
    public const string DefaultPullBoxLabel = "PB" ;
    public const string MaterialCodeParameter = "Material Code" ;
    public const string IsAutoCalculatePullBoxSizeParameter = "IsAutoCalculatePullBoxSize" ;
    private const string TaniOfPullBox = "個" ;

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, Route route, Element element, FamilyInstance pullBox, double heightConnector, 
      double heightWire, XYZ routeDirection, bool isCreatePullBoxWithoutSettingHeight, string nameBase, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute,
      XYZ? fromDirection = null, XYZ? toDirection = null, FixedHeight? firstHeight = null )
    {
      const int index = 1 ;
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
        name = parentRoute.RouteName + "_" + parentIndex ;
        parentIndex++ ;
        foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
          if ( detector.IsPassingThrough( segment ) ) {
            isBeforeSegment = false ;
            firstHeight ??= fromFixedHeightFirst ;
            var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, firstHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
            result.Add( ( parentRoute.RouteName, newSegment ) ) ;
            beforeSegments.Add( newSegment ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, null ) ) ) ;
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

            var newSegment = isBeforeSegment ? 
              new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId )
              : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ;
            result.Add( isBeforeSegment ? ( parentRoute.RouteName, newSegment ) : ( name, newSegment ) ) ;
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
                name = routeName + "_" + index ;
                result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
                result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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
        result = GetSegments( document, routeRecords, ceedCodes, pullBox, parentRoute, detector, ref parentIndex, ref parentAndChildRoute, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, 
          fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId, radius, isCreatePullBoxWithoutSettingHeight, pullBoxFromEndPoint, pullBoxToEndPoint ) ;
      }

      if ( ceedCodes.Any() ) {
        var newCeedCode = string.Join( ";", ceedCodes ) ;
        pullBox.TrySetProperty( ElectricalRoutingElementParameter.CeedCode, newCeedCode ) ;
      }

      return result ;
    }

    private static List<(string RouteName, RouteSegment Segment)> GetSegments( Document document, List<(string RouteName, RouteSegment Segment)> routeRecords, List<string> ceedCodes, 
      FamilyInstance pullBox, Route parentRoute, RouteSegmentDetector detector, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, double? diameter, bool isRoutingOnPipeSpace, FixedHeight? fromFixedHeightFirst, FixedHeight? fromFixedHeightSecond, 
      FixedHeight? toFixedHeight, AvoidType avoidType, string? shaftElementUniqueId, double? radius, bool isCreatePullBoxWithoutSettingHeight, ConnectorEndPoint pullBoxFromEndPoint, ConnectorEndPoint pullBoxToEndPoint )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      string connectorUniqueId ;
      var isBeforeSegment = true ;
      var index = 1 ;
      var name = parentRoute.RouteName + "_" + parentIndex ;
      var parentSegments = parentRoute.RouteSegments.EnumerateAll().ToList() ;
      var ( routeSegment, routeDirection ) = GetSegmentThroughPullBox( pullBox, parentSegments ) ;
      if ( routeDirection == null ) {
        result.AddRange( from segment in parentSegments select ( parentRoute.RouteName, segment ) ) ;
        connectorUniqueId = parentSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
        GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
        foreach ( var (routeName, segment) in routeRecords ) {
          if ( detector.IsPassingThrough( segment ) ) {
            name = routeName + "_" + index ;
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
      var beforeSegments = new List<RouteSegment>() ;
      foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
        if ( segment == routeSegment ) {
          isBeforeSegment = false ;
          var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, mainPullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
          result.Add( ( name, newSegment ) ) ;
          result.Add( ( parentRoute.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, mainPullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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
            ? new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) 
            : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ;
          result.Add( isBeforeSegment ? ( name, newSegment ) : ( parentRoute.RouteName, newSegment ) ) ;
          if ( isBeforeSegment ) beforeSegments.Add( newSegment ) ;
        }
      }
      
      connectorUniqueId = parentSegments.Last().ToEndPoint.Key.GetElementUniqueId() ;
      GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
      parentIndex++ ;
      
      foreach ( var (routeName, segment) in routeRecords ) {
        index = 1 ;
        name = routeName + "_" + index ;
        if ( detector.IsPassingThrough( segment ) ) {
          var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
          if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
            var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? mainPullBoxToEndPoint.Key ;
            var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
            if ( fromEndPointKey == mainPullBoxToEndPoint.Key ) {
              result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
              name = routeName + "_" + (++index) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
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
              result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
              result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            }
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
              if ( fromEndPointKey == mainPullBoxToEndPoint.Key ) {
                result.AddRange( from branchSegment in beforeSegments select ( routeName, branchSegment ) ) ;
                result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
                if ( parentAndChildRoute.Any() ) {
                  parentAndChildRoute.First().Value.Add( routeName ) ;
                }
                else {
                  parentAndChildRoute.Add( parentRoute.RouteName, new List<string> { routeName } ) ;
                }
              }
              else {
                result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, segment.ToFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
              }
              connectorUniqueId = segment.ToEndPoint.Key.GetElementUniqueId() ;
              GetPullBoxCeedCodes( document, ceedCodes, connectorUniqueId ) ;
            }
            else {
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

      instance.SetConnectorFamilyType( connectorType ?? ConnectorFamilyType.PullBox ) ;
      
      var depthParam = instance.ParametersMap.get_Item( ChangePullBoxDimensionCommandBase.PullBoxDimensions.Depth ) ;
      depthParam?.Set( depthParam.AsDouble() * baseLengthOfLine ) ;
      var widthParam =  instance.ParametersMap.get_Item( ChangePullBoxDimensionCommandBase.PullBoxDimensions.Width ) ;
      widthParam?.Set( widthParam.AsDouble() * baseLengthOfLine ) ;
      var heightParam = instance.ParametersMap.get_Item( ChangePullBoxDimensionCommandBase.PullBoxDimensions.Height ) ;
      heightParam?.Set( heightParam.AsDouble() * baseLengthOfLine ) ;

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

      var defaultSettingStorable = document.GetDefaultSettingStorable() ;
      var grade = defaultSettingStorable.GradeSettingData.GradeMode ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      if ( grade is 1 or 2 or 3 ) {
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
            result = CreatePullBoxAndGetSegments( document, route, conduitInfo.Conduit, originX, originY, originZ, conduitInfo.Level, conduitInfo.ConduitDirection, nameBase!, out FamilyInstance? pullBoxElement, ref parentIndex, ref parentAndChildRoute ).ToList() ;
            pullBoxPositions.Add( conduitInfo.ConduitOrigin ) ;
            if ( pullBoxElement != null ) pullBoxElements.Add( (pullBoxElement, conduitInfo.ConduitOrigin) ) ;
            boardUniqueIds.Add( board.UniqueId ) ;
            return result ;
          }
          
          if ( conduitFittingsOfRoute.Count >= 4 ) {
            var conduitFitting = conduitFittingsOfRoute.ElementAt( 3 ) ;
            var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
            var ( originX, originY, originZ)  = pullBoxInfo.Position ;
            var fromDirection = pullBoxInfo.FromDirection ;
            var toDirection = pullBoxInfo.ToDirection ;
            var height = originZ - pullBoxInfo.Level.Elevation ;
            var pullBoxPosition = new XYZ( originX, originY, height ) ;
            var isSamePullBoxPositions = IsPullBoxExistInThePosition(document, pullBoxPositions, pullBoxPosition ) ;
            if ( isSamePullBoxPositions ) continue ;
            result = CreatePullBoxAndGetSegments( document, route, conduitFitting, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, out FamilyInstance? pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection ).ToList() ;
            if ( pullBoxElement != null ) pullBoxElements.Add( (pullBoxElement, pullBoxPosition) ) ;
            pullBoxPositions.Add( pullBoxPosition ) ;
            return result ;
          }

          if ( sumAngle > pullBoxAutomaticPlacementCondition3Threshold && selectedConduitFitting != null ) {
            var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, selectedConduitFitting ) ;
            var ( originX, originY, originZ)  = pullBoxInfo.Position ;
            var fromDirection = pullBoxInfo.FromDirection ;
            var toDirection = pullBoxInfo.ToDirection ;
            var height = originZ - pullBoxInfo.Level.Elevation ;
            var pullBoxPosition = new XYZ( originX, originY, height ) ;
            var isSamePullBoxPositions = IsPullBoxExistInThePosition(document, pullBoxPositions, pullBoxPosition ) ;
            if ( isSamePullBoxPositions ) continue ;
            result = CreatePullBoxAndGetSegments( document, route, selectedConduitFitting, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, out FamilyInstance? pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection ).ToList() ;
            if ( pullBoxElement != null ) pullBoxElements.Add( (pullBoxElement, pullBoxPosition) ) ;
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
              ( originX, originY, originZ) = line.GetEndPoint( 0 ) ;
              direction = line.Direction ;
              var length = ( sumLength - pullBoxAutomaticPlacementCondition4Threshold ) ;
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
              var isSamePullBoxPositions = IsPullBoxExistInThePosition(document, pullBoxPositions, new XYZ( originX, originY, height ) ) ;
              if ( isSamePullBoxPositions ) continue ;
            }
            else if ( selectedConduit is FamilyInstance conduitFitting ) {
              var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
              var isSamePullBoxPositions = IsPullBoxExistInThePosition(document, pullBoxPositions, new XYZ( originX, originY, height ) ) ;
              if ( isSamePullBoxPositions ) continue ;
            
              ( originX, originY, originZ ) = pullBoxInfo.Position ;
              level = pullBoxInfo.Level ;
              height = originZ - level.Elevation ;
              direction = pullBoxInfo.FromDirection ;
              fromDirection = pullBoxInfo.FromDirection ;
              toDirection = pullBoxInfo.ToDirection ;
            }
            
            result = CreatePullBoxAndGetSegments( document, route, selectedConduit, originX, originY, height, level, direction, nameBase!, out FamilyInstance? pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection ).ToList() ;
            var pullBoxPosition = new XYZ( originX, originY, height ) ;
            if ( pullBoxElement != null ) pullBoxElements.Add( (pullBoxElement, pullBoxPosition) ) ;
            pullBoxPositions.Add( pullBoxPosition) ;
            return result ;
          }
        }
      }
      
      return result ;
    }

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSegmentsWithPullBoxShaft( Document document, IReadOnlyCollection<Route> executeResultValue, List<XYZ> pullBoxPositions, List<(FamilyInstance, XYZ?)> pullBoxElements, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute )
    {
      var defaultSettingStorable = document.GetDefaultSettingStorable() ;
      var grade = defaultSettingStorable.GradeSettingData.GradeMode ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      if ( grade is 1 or 2 or 3 ) {
        var passedShaftRoute = executeResultValue.SingleOrDefault( e => e.UniqueShaftElementUniqueId != null ) ;
        var shaftId = passedShaftRoute?.UniqueShaftElementUniqueId ;
        var fromHeight = passedShaftRoute?.UniqueFromFixedHeight ;
        foreach ( var route in executeResultValue ) {
          var conduitFittingsOfRoute = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;

          var curveType = route.UniqueCurveType ;
          var nameBase = curveType?.Category.Name ;

          if ( shaftId == null ) continue ;
          FamilyInstance? conduitFittingBottomShaft = null ;
          var shaft = document.GetElementById<Opening>( route.UniqueShaftElementUniqueId ?? string.Empty ) ;
          if ( shaft == null ) continue ;
          var shaftLocation = GetShaftLocation( route, document ) ;
          if ( shaftLocation != null ) {
            conduitFittingBottomShaft = GetConduitFittingAtBottomShaft( shaftLocation, conduitFittingsOfRoute ) ;
          }

          if ( conduitFittingBottomShaft == null ) continue ;
          var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFittingBottomShaft ) ;
          var isSamePullBoxPositions = IsPullBoxExistInThePosition( document, pullBoxPositions, pullBoxInfo.Position ) ;
          if ( isSamePullBoxPositions ) continue ;

          var (originX, originY, originZ) = pullBoxInfo.Position ;
          var fromDirection = pullBoxInfo.FromDirection ;
          var toDirection = pullBoxInfo.ToDirection ;
          var height = originZ - pullBoxInfo.Level.Elevation ;
          result = CreatePullBoxAndGetSegments( document, route, conduitFittingBottomShaft, originX, originY, height, pullBoxInfo.Level, fromDirection, nameBase!, out FamilyInstance? pullBoxElement, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection, fromHeight ).ToList() ;
          if ( pullBoxElement != null ) pullBoxElements.Add( (pullBoxElement, null) ) ;          
          pullBoxPositions.Add( pullBoxInfo.Position ) ;
          return result ;
        }
      }

      return result ;
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

    private static FamilyInstance? GetConduitFittingAtBottomShaft( XYZ shaftLocation, IEnumerable<FamilyInstance> conduitFittings )
    {
      FamilyInstance? conduitFittingAtBottomShaft = null ;
      XYZ? lowestConduitPosition = null ;
      foreach ( var conduitFitting in conduitFittings ) {
        var conduitFittingLocationPoint = ( conduitFitting.Location as LocationPoint )?.Point ;
        if ( ! conduitFittingLocationPoint.IsNearShaft( shaftLocation ) ) continue ;
        if ( conduitFittingAtBottomShaft == null ) {
          conduitFittingAtBottomShaft = conduitFitting ;
          lowestConduitPosition = conduitFittingLocationPoint ;
        }
        else {
          if ( conduitFittingLocationPoint?.Z > lowestConduitPosition?.Z ) continue ;
          conduitFittingAtBottomShaft = conduitFitting ;
          lowestConduitPosition = conduitFittingLocationPoint ;
        }
      }

      return conduitFittingAtBottomShaft ;
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
      Level? level, XYZ? direction, string nameBase, out FamilyInstance? pullBox, ref int parentIndex, ref Dictionary<string, List<string>> parentAndChildRoute, XYZ? fromDirection = null, XYZ? toDirection = null, FixedHeight? firstHeight = null )
    {
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      pullBox = null ;
      try {
        using Transaction t = new( document, "Create pull box" ) ;
        t.Start() ;
        pullBox = GenerateConnector( document, ElectricalRoutingFamilyType.PullBox, ConnectorFamilyType.PullBox, originX, originY, originZ , level!, route.RouteName ) ;
        t.Commit() ;

        using Transaction t1 = new( document, "Get segments" ) ;
        t1.Start() ;
        result.AddRange( GetRouteSegments( document, route, element, pullBox, originZ, originZ, direction!, true, nameBase, ref parentIndex, ref parentAndChildRoute, fromDirection, toDirection, firstHeight ) ) ;
        t1.Commit() ;
      }
      catch {
        //
      }

      return result ;
    }

    public static void ChangeDimensionOfPullBoxAndSetLabel( Document document, FamilyInstance pullBox,
      CsvStorable? csvStorable, DetailSymbolStorable detailSymbolStorable, PullBoxInfoStorable pullBoxInfoStorable,
      List<ConduitsModel>? conduitsModelData, List<HiroiMasterModel>? hiroiMasterModels, double scale, string textLabel,
      XYZ? positionLabel, bool isAutoCalculatePullBoxSize, PullBoxModel? selectedPullBoxModel = null )
    {
      var defaultLabel = textLabel ;
      var buzaiCd = string.Empty ;
      int depth = 0, height = 0 ;

      //Case 1: Automatically calculate dimension of pull box
      if ( isAutoCalculatePullBoxSize && csvStorable != null && conduitsModelData != null &&
           hiroiMasterModels != null ) {
        var pullBoxModel = GetPullBoxWithAutoCalculatedDimension( document, pullBox, csvStorable, detailSymbolStorable,
          conduitsModelData, hiroiMasterModels ) ;
        if ( pullBoxModel != null ) {
          buzaiCd = pullBoxModel.Buzaicd ;
          ( depth, _, height ) = ParseKikaku( pullBoxModel.Kikaku ) ;
        }
      }
      //Case 2: Use dimension of selected pull box
      else {
        if ( selectedPullBoxModel != null ) {
          buzaiCd = selectedPullBoxModel.Buzaicd ;
          ( depth, _, height ) = ParseKikaku( selectedPullBoxModel.Kikaku ) ;
        }
      }
      
      textLabel = GetPullBoxTextBox( depth, height, defaultLabel ) ;

      using Transaction t = new(document, "Update dimension of pull box") ;
      t.Start() ;
      if (!string.IsNullOrEmpty( buzaiCd ))
        pullBox.ParametersMap.get_Item( MaterialCodeParameter )?.Set( buzaiCd ) ;
      pullBox.ParametersMap.get_Item( IsAutoCalculatePullBoxSizeParameter )?.Set( Convert.ToString( isAutoCalculatePullBoxSize ) ) ;
      detailSymbolStorable.DetailSymbolModelData.RemoveAll( d => d.DetailSymbolId == pullBox.UniqueId ) ;

      if(positionLabel != null)
        CreateTextNoteAndGroupWithPullBox( document, pullBoxInfoStorable, positionLabel, pullBox, textLabel, isAutoCalculatePullBoxSize ) ;
      else 
        ChangeLabelOfPullBox( document, pullBoxInfoStorable, pullBox, textLabel, isAutoCalculatePullBoxSize ) ;
      t.Commit() ;
    }

    private static void ChangeLabelOfPullBox( Document document, PullBoxInfoStorable pullBoxInfoStorable, Element pullBoxElement, string textLabel, bool isAutoCalculatePullBoxSize )
    {
      // Find text note compatible with pull box, change label if exists
      var pullBoxInfoModel = pullBoxInfoStorable.PullBoxInfoModelData.FirstOrDefault( p => p.PullBoxUniqueId == pullBoxElement.UniqueId ) ;
      var textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => pullBoxInfoModel?.TextNoteUniqueId == t.UniqueId ) ;
      if ( textNote != null ) {
        textNote.Text = textLabel ;
        if ( ! isAutoCalculatePullBoxSize ) return ;
        var color = new Color( 255, 0, 0 ) ;
        ConfirmUnsetCommandBase.ChangeElementColor( document, new []{ textNote }, color ) ;
      }
    }

    private static void CreateTextNoteAndGroupWithPullBox(Document doc, PullBoxInfoStorable pullBoxInfoStorable, XYZ point, Element pullBox, string text, bool isAutoCalculatePullBoxSize)
    {
      var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
      TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;
      
      var txtPosition = new XYZ( point.X, point.Y, point.Z ) ;
      
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, text, opts ) ;

      var textNoteType = textNote.TextNoteType ;
      double newSize = ( 1.0 / 4.0 ) * TextNoteHelper.TextSize.MillimetersToRevitUnits() ;
      textNoteType.get_Parameter( BuiltInParameter.TEXT_SIZE ).Set( newSize ) ;
      textNote.ChangeTypeId( textNoteType.Id ) ;

      if ( isAutoCalculatePullBoxSize ) {
        var color = new Color( 255, 0, 0 ) ;
        ConfirmUnsetCommandBase.ChangeElementColor( doc, new []{ textNote }, color ) ;
      }

      pullBoxInfoStorable.PullBoxInfoModelData.Add( new PullBoxInfoModel( pullBox.UniqueId, textNote.UniqueId ) );
      pullBoxInfoStorable.Save() ;
    }

    private static string GetPullBoxTextBox( int depth, int height, string text)
    {
      if ( depth == 0 || height == 0 ) return text ;
      Dictionary<int, (int, int)> defaultDimensions = new() ;
      defaultDimensions.Add(1, (150, 100));
      defaultDimensions.Add(2, ( 200, 200 ));
      defaultDimensions.Add(3, (300, 300));
      defaultDimensions.Add(4, (400, 300));
      defaultDimensions.Add(5, (500, 400));
      defaultDimensions.Add(6, (600, 400));
      defaultDimensions.Add(8, (800, 400));
      defaultDimensions.Add(10, (1000, 400));
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
          else if ( Math.Abs( routeDirection.Y + 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
          }
          else if ( Math.Abs( routeDirection.Z - 1 ) == 0 ) {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
          }
          else {
            pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
            pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
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
      DetailSymbolStorable detailSymbolStorable, List<ConduitsModel> conduitsModelData, List<HiroiMasterModel> hiroiMasterModels )
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
        AddWiringInformationCommandBase.CreateDetailSymbolModel( document, conduit, csvStorable, detailSymbolStorable,
          pullBoxElement.UniqueId ) ;

      var elementIds = conduitsFromPullBox.Select( c => c.UniqueId ).ToList() ;
      var (detailTableModels, _, _) = CreateDetailTableCommandBase.CreateDetailTableAddWiringInfo( document, csvStorable,
        detailSymbolStorable, conduitsFromPullBox, elementIds, false ) ;

      var newDetailTableModels = DetailTableViewModel.SummarizePlumbing( detailTableModels, conduitsModelData,
        detailSymbolStorable, new List<DetailTableModel>(), false, new Dictionary<string, string>() ) ;

      var plumbingSizes = newDetailTableModels.Where( p => int.TryParse( p.PlumbingSize, out _ ) )
        .Select( p => Convert.ToInt32( p.PlumbingSize ) ).ToArray() ;
      var (depth, width, height) = GetPullBoxDimension( plumbingSizes, isStraightDirection ) ;

      if ( depth == 0 || width == 0 || height == 0 )
        return null ;
      var minPullBoxModelDepth = hiroiMasterModels.Where( p => p.Tani == TaniOfPullBox && p.Hinmei.Contains( HinmeiOfPullBox ) )
        .Where( p =>
        {
          var (d, w, h) = ParseKikaku( p.Kikaku ) ;
          return d >= depth && w >= width && h >= height ;
        } ).Min( x =>
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

    public static IEnumerable<TextNote> GetTextNotesOfAutomaticCalculatedDimensionPullBox( Document document )
    {
      var pullBoxUniqueIds = document.GetAllElements<FamilyInstance>()
        .OfCategory( BuiltInCategory.OST_ElectricalFixtures )
        .Where( e => e.GetConnectorFamilyType() == ConnectorFamilyType.PullBox )
        .Where( e => Convert.ToBoolean( e.ParametersMap.get_Item( PullBoxRouteManager.IsAutoCalculatePullBoxSizeParameter ).AsString() ) )
        .Select( e => e.UniqueId )
        .ToList() ;
      var pullBoxInfoStorable = document.GetPullBoxInfoStorable() ;
      var pullBoxInfoModels = pullBoxInfoStorable.PullBoxInfoModelData.Where( p => pullBoxUniqueIds.Contains(p.PullBoxUniqueId) ) ;
      var textNote = document.GetAllElements<TextNote>().Where( t => pullBoxInfoModels.Any(p => p.TextNoteUniqueId == t.UniqueId) ) ;
      return textNote ;
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