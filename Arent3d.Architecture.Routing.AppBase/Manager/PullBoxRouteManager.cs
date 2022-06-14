using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class PullBoxRouteManager
  {
    private static readonly double DefaultDistanceHeight = ( 200.0 ).MillimetersToRevitUnits() ;
    private const string DefaultConstructionItem = "未設定" ;
    
    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, Route route, SubRoute subRoute, Element element, FamilyInstance pullBox, double heightConnector, double heightWire, XYZ routeDirection, bool isCreatePullBoxWithoutSettingHeight, string nameBase )
    {
      var routeRecords = GetRelatedBranchSegments( route ).ToList() ;

      var detector = new RouteSegmentDetector( subRoute, element ) ;
      var diameter = route.UniqueDiameter ;
      var radius = diameter * 0.5 ;
      var isRoutingOnPipeSpace = route.UniqueIsRoutingOnPipeSpace ?? false ;
      var toFixedHeight = route.UniqueToFixedHeight ;
      var avoidType = route.UniqueAvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = route.UniqueShaftElementUniqueId ;
      var fromFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, isCreatePullBoxWithoutSettingHeight ? heightConnector : heightConnector + DefaultDistanceHeight ) ;
      var fromFixedHeightSecond = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightWire ) ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      ConnectorEndPoint pullBoxFromEndPoint ;
      ConnectorEndPoint pullBoxToEndPoint ;
      if ( isCreatePullBoxWithoutSettingHeight ) {
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
      else {
        pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetTopConnectorOfConnectorFamily(), radius ) ;
        pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily(), radius ) ;
      }

      var isBeforeSegment = true ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      foreach ( var segment in route.RouteSegments.EnumerateAll() ) {
        if ( detector.IsPassingThrough( segment ) ) {
          isBeforeSegment = false ;
          result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
          result.Add( ( route.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
        }
        else {
          result.Add( isBeforeSegment 
            ? ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ) 
            : ( route.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
        }
      }

      foreach ( var ( routeName, segment ) in routeRecords ) {
        var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
        if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
          var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? pullBoxToEndPoint.Key ;
          var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
          result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
        }
        else {
          result.Add( ( routeName, segment ) ) ;
        }
      }

      return result ;
    }

    private static EndPointKey? GetFromEndPointKey( Document document, List<(string RouteName, RouteSegment Segment)> segments, string passPointEndPointUniqueId )
    {
      var fromRouteName = string.Empty ;
      foreach ( var ( routeName, segment ) in segments ) {
        if ( segment.FromEndPoint.Key.GetElementUniqueId() != passPointEndPointUniqueId && segment.FromEndPoint.Key.GetElementUniqueId() != passPointEndPointUniqueId ) continue ;
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

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRelatedBranchSegments( Route route )
    {
      // add all related branches
      var relatedBranches = route.GetAllRelatedBranches() ;
      relatedBranches.Remove( route ) ;
      return relatedBranches.ToSegmentsWithName() ;
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

      instance.SetConnectorFamilyType( connectorType ?? ConnectorFamilyType.Power ) ;

      return instance ;
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

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }
  }
}