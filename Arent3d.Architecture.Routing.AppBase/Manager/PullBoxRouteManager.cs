using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
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
    private static readonly double PullBoxHeight = ( 15.0 ).MillimetersToRevitUnits() ;
    
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

      var isBeforeSegment = true ;
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var fromSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      var isPassingThrough = parentRoute.RouteSegments.FirstOrDefault( s => detector.IsPassingThrough( s ) ) != null ;
      if ( isPassingThrough ) {
        foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
          if ( detector.IsPassingThrough( segment ) ) {
            isBeforeSegment = false ;
            var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
            result.Add( ( name, newSegment ) ) ;
            result.Add( ( parentRoute.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            if ( segment.FromEndPoint.DisplayTypeName != PassPointBranchEndPoint.Type ) {
              fromSegments.Add( ( name, newSegment ) ) ;
            }

            withoutRouteNames.Add( name ) ;
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
            if ( isBeforeSegment ) {
              fromSegments.Add( ( name, newSegment ) ) ;
            }
          }
        }

        if ( ! routeRecords.Any() ) return result ;
        {
          foreach ( var (routeName, segment) in routeRecords ) {
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
          
          // var routeCount = routeRecords.Select( r => r.RouteName ).Distinct().Count() ;
          // if ( ! fromSegments.Any() ) return result ;
          // for ( var i = 0 ; i < routeCount ; i++ ) {
          //   nextIndex++ ;
          //   name = nameBase + "_" + nextIndex ;
          //
          //   foreach ( var (_, mainSegment) in fromSegments ) {
          //     var newSegment = new RouteSegment( mainSegment.SystemClassificationInfo, mainSegment.SystemType, mainSegment.CurveType, mainSegment.FromEndPoint, mainSegment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
          //     result.Add( ( name, newSegment ) ) ;
          //     withoutRouteNames.Add( name ) ;
          //   }
          // }
        }
      }
      else {
        result.AddRange( from segment in parentRoute.RouteSegments.EnumerateAll() select ( parentRoute.RouteName, segment ) ) ;

        foreach ( var (routeName, segment) in routeRecords ) {
          if ( detector.IsPassingThrough( segment ) ) {
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            result.Add( ( name, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
            withoutRouteNames.Add( name ) ;
          }
          else {
            result.Add( ( routeName, segment ) ) ;
          }
        }
      }

      return result ;
    }

    // private static List<(string RouteName, RouteSegment Segment)> GetMainSegments( Document document, List<(string RouteName, RouteSegment Segment)> fromSegments, List<string> withoutRouteNames, FamilyInstance pullBox, Route parentRoute, RouteSegmentDetector detector, string name,
    //   double diameter, bool isRoutingOnPipeSpace, FixedHeight? fromFixedHeightFirst, FixedHeight? fromFixedHeightSecond, FixedHeight? toFixedHeight, AvoidType avoidType, string shaftElementUniqueId,  )
    // {
    //   var isBeforeSegment = true ;
    //   var result = new List<(string RouteName, RouteSegment Segment)>() ;
    //   var isPassingThrough = parentRoute.RouteSegments.FirstOrDefault( detector.IsPassingThroughFrom ) != null ;
    //   if ( ! isPassingThrough ) return result ;
    //   foreach ( var segment in parentRoute.RouteSegments.EnumerateAll() ) {
    //     if ( detector.IsPassingThroughFrom( segment ) ) {
    //       isBeforeSegment = false ;
    //       ConnectorEndPoint pullBoxFromEndPoint ;
    //   ConnectorEndPoint pullBoxToEndPoint ;
    //   if ( isCreatePullBoxWithoutSettingHeight ) {
    //     if ( fromDirection != null && toDirection != null ) {
    //       if ( fromDirection.X is 1 or -1 ) {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.X is 1 ? RoutingElementExtensions.ConnectorPosition.Left : RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
    //       }
    //       else if ( fromDirection.Y is 1 or -1 ) {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.Y is 1 ? RoutingElementExtensions.ConnectorPosition.Front : RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
    //       }
    //       else {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( fromDirection.Z is 1 ? RoutingElementExtensions.ConnectorPosition.Bottom : RoutingElementExtensions.ConnectorPosition.Top ), radius ) ;
    //       }
    //       
    //       if ( toDirection.X is 1 or -1 ) {
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.X is 1 ? RoutingElementExtensions.ConnectorPosition.Right : RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
    //       }
    //       else if ( toDirection.Y is 1 or -1 ) {
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Y is 1 ? RoutingElementExtensions.ConnectorPosition.Back : RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
    //       }
    //       else {
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( toDirection.Z is 1 ? RoutingElementExtensions.ConnectorPosition.Top : RoutingElementExtensions.ConnectorPosition.Bottom ), radius ) ;
    //       }
    //     }
    //     else {
    //       if ( Math.Abs( routeDirection.X - 1 ) == 0 ) {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
    //       }
    //       else if ( Math.Abs( routeDirection.X + 1 ) == 0 ) {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Right ), radius ) ;
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Left ), radius ) ;
    //       }
    //       else if ( Math.Abs( routeDirection.Y - 1 ) == 0 ) {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
    //       }
    //       else {
    //         pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Back ), radius ) ;
    //         pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily( RoutingElementExtensions.ConnectorPosition.Front ), radius ) ;
    //       }
    //     }
    //   }
    //   else {
    //     pullBoxFromEndPoint = new ConnectorEndPoint( pullBox.GetTopConnectorOfConnectorFamily(), radius ) ;
    //     pullBoxToEndPoint = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily(), radius ) ;
    //   }
    //       var newSegment = new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, pullBoxFromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
    //       result.Add( ( name, newSegment ) ) ;
    //       result.Add( ( parentRoute.RouteName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, pullBoxToEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
    //       if ( segment.FromEndPoint.DisplayTypeName != PassPointBranchEndPoint.Type ) {
    //         fromSegments.Add( ( name, newSegment ) ) ;
    //       }
    //
    //       withoutRouteNames.Add( name ) ;
    //     }
    //     else {
    //       if ( isBeforeSegment ) {
    //         if ( segment.FromEndPoint.TypeName == PassPointEndPoint.Type ) {
    //           var passPoint = document.GetElement( segment.FromEndPoint.Key.GetElementUniqueId() ) ;
    //           passPoint.TrySetProperty( RoutingParameter.RouteName, name ) ;
    //         }
    //
    //         if ( segment.ToEndPoint.TypeName == PassPointEndPoint.Type ) {
    //           var passPoint = document.GetElement( segment.ToEndPoint.Key.GetElementUniqueId() ) ;
    //           passPoint.TrySetProperty( RoutingParameter.RouteName, name ) ;
    //         }
    //       }
    //
    //       var newSegment = isBeforeSegment ? new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) : new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ;
    //       result.Add( isBeforeSegment ? ( name, newSegment ) : ( parentRoute.RouteName, newSegment ) ) ;
    //       if ( isBeforeSegment ) {
    //         fromSegments.Add( ( name, newSegment ) ) ;
    //       }
    //     }
    //   }
    //
    //   return result ;
    // }

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

    public static IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSegmentsWithPullBox ( Document document, IReadOnlyCollection<Route> executeResultValue, List<string> boardUniqueIds, List<XYZ> pullBoxPositions, List<string> withoutRouteNames )
    {
      const string angleParameter = "角度" ;
      const double maxAngle = 270 ;
      var maxLength = ( 30.0 ).MetersToRevitUnits() ;

      string conduitFittingLengthParam = "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( document, "電線管長さ" ) ;
      string conduitLengthParam = "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ;
      var registrationOfBoardDataModels = document.GetRegistrationOfBoardDataStorable().RegistrationOfBoardData ;
      
      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var beforeResult = executeResultValue.ToList().Where( r => ! withoutRouteNames.Contains( r.RouteName ) ) ;
      foreach ( var route in beforeResult ) {
        var allConduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        var conduitFittingsOfRoute = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        
        double sumLength = 0 ;
        double sumAngle = 0 ;
        FamilyInstance? selectedConduitFitting = null ;
        for ( var i = conduitFittingsOfRoute.Count - 1; i > -1; i-- ) {
          if ( conduitFittingsOfRoute[i].HasParameter( angleParameter ) ) {
            var angle = conduitFittingsOfRoute[i].ParametersMap.get_Item( angleParameter ).AsDouble() ;
            sumAngle += angle ;
          }
          
          if ( sumAngle < maxAngle ) continue ;
          selectedConduitFitting = conduitFittingsOfRoute[i] ;
          break;
        }

        Element? selectedConduit = null ;
        for ( var i = allConduitsOfRoute.Count - 1; i > -1; i-- ) {
          if ( allConduitsOfRoute[i].HasParameter( conduitLengthParam ) ) {
            var length = allConduitsOfRoute[i].ParametersMap.get_Item( conduitLengthParam ).AsDouble() ;
            sumLength += length ;
          }

          if ( allConduitsOfRoute[i].HasParameter( conduitFittingLengthParam ) ) {
            var length = allConduitsOfRoute[i].ParametersMap.get_Item( conduitFittingLengthParam ).AsDouble() ;
            sumLength += length ;
          }

          if ( sumLength < maxLength ) continue ;
          selectedConduit = allConduitsOfRoute[ i ] ;
          break;
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
          var conduitFitting = conduitFittingsOfRoute.ElementAt( 2 ) ;
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
          double originX = 0, originY = 0, originZ, height = 0 ;
          Level? level = null ;
          XYZ? direction = null ;
          XYZ? fromDirection = null ;
          XYZ? toDirection = null ;
          if ( selectedConduit is Conduit ) {
            level = document.GetAllElements<Level>().SingleOrDefault( l => l.Id == selectedConduit.GetLevelId() ) ;
            var location = ( selectedConduit.Location as LocationCurve ) ! ;
            var line = ( location.Curve as Line ) ! ;
            ( originX, originY, originZ) = line.GetEndPoint( 1 ) ;
            direction = line.Direction ;
            height = originZ - level!.Elevation ;
            var conduitLength = selectedConduit.ParametersMap.get_Item( conduitLengthParam ).AsDouble() ;
            var length = sumLength - maxLength ;
            if ( direction.X is 1 or -1 ) {
              originX -= length > conduitLength ? direction.X * length : direction.X * PullBoxWidth ;
            }
            else if ( direction.Y is 1 or -1 ) { 
              originY -= length > conduitLength ? direction.Y * length : direction.Y * PullBoxLenght ;
            }
          }
          else if ( selectedConduit is FamilyInstance conduitFitting ) {
            var pullBoxInfo = GetPullBoxInfo( document, route.RouteName, conduitFitting ) ;
            var isSamePullBoxPositions = ComparePullBoxPosition( pullBoxPositions, pullBoxInfo.Position ) ;
            if ( isSamePullBoxPositions ) continue ;
          
            ( originX, originY, originZ) = pullBoxInfo.Position ;
            level = pullBoxInfo.Level ;
            height = originZ - level.Elevation ;
            direction = pullBoxInfo.FromDirection ;
            fromDirection = pullBoxInfo.FromDirection ;
            toDirection = pullBoxInfo.ToDirection ;
          }
          
          result = CreatePullBoxAndGetSegments( document, route, selectedConduit, originX, originY, height, level, direction, nameBase!, withoutRouteNames, fromDirection, toDirection ).ToList() ;
          pullBoxPositions.Add( new XYZ( originX, originY, height ) ) ;
          return result ;
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
        var pullBox = GenerateConnector( document, ElectricalRoutingFamilyType.PullBox, ConnectorFamilyType.PullBox, originX, originY, originZ - PullBoxHeight, level!, route.RouteName ) ;
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
    
    private static Element? GetConduit( List<Element> allConduits, Element conduitFitting )
    {
      var conduitFittingLocation = ( conduitFitting.Location as LocationPoint ) ! ;
      var conduitFittingPoint = conduitFittingLocation.Point ;
      var conduits = allConduits.Where( c => c.GetBuiltInCategory() == BuiltInCategory.OST_Conduit ).ToList() ;
      var minDistance = double.MaxValue ;
      var prevConduit = conduits.First() ;
      foreach ( var conduit in conduits ) {
        var fromConduitLocation = ( conduit.Location as LocationCurve ) ! ;
        var fromConduitLine = ( fromConduitLocation.Curve as Line ) ! ;
        var fromConduitPoint = fromConduitLine.GetEndPoint( 1 ) ;
        var distance = fromConduitPoint.DistanceTo( conduitFittingPoint ) ;
        if ( distance > minDistance ) continue ;
        minDistance = distance ;
        prevConduit = conduit ;
      }

      return prevConduit ;
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
  }
}