using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class RoomSelectionRangeRouteCommandBase : RoutingCommandBase<RoomSelectionRangeRouteCommandBase.RoomSelectState>
  {
    public record RoomSelectState( FamilyInstance PowerConnector, IReadOnlyList<FamilyInstance> SensorConnectors, Reference? RoomPick, SelectionRangeRouteManager.SensorArrayDirection SensorDirection, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, MEPSystemPipeSpec PipeSpec ) ;

    protected abstract AddInType GetAddInType() ;

    protected abstract SelectionRangeRouteCommandBase.DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType() ;

    protected abstract string GetNameBase( MEPCurveType curveType ) ;

    protected override OperationResult<RoomSelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var routingExecutor = GetRoutingExecutor() ;

      var (powerConnectors, sensorConnectors, sensorDirection, errorMessage) = SelectionRangeRouteManager.SelectionRangeRoute( uiDocument ) ;
      if ( null != errorMessage ) return OperationResult<RoomSelectState>.FailWithMessage( errorMessage ) ;

      var room = RoomRouteManager.PickRoom( uiDocument ) ;

      var powerConnector = powerConnectors.FirstOrDefault() ;
      var farthestSensorConnector = sensorConnectors.Last() ;
      var property = ShowPropertyDialog( uiDocument.Document, powerConnector!, farthestSensorConnector ) ;
      if ( true != property?.DialogResult ) return OperationResult<RoomSelectState>.Cancelled ;

      if ( GetMEPSystemClassificationInfo( powerConnector!, farthestSensorConnector, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<RoomSelectState>.Failed ;

      var pipeSpec = new MEPSystemPipeSpec( new RouteMEPSystem( uiDocument.Document, property.GetSystemType(), property.GetCurveType() ), routingExecutor.FittingSizeCalculator ) ;

      return new OperationResult<RoomSelectState>( new RoomSelectState( powerConnector!, sensorConnectors, room, sensorDirection, property, classificationInfo, pipeSpec ) ) ;
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( Element fromPickElement, Element toPickElement, MEPSystemType? systemType )
    {
      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType() ;
    }

    private RoutePropertyDialog? ShowPropertyDialog( Document document, Element fromPickElement, Element toPickElement )
    {
      var fromLevelId = fromPickElement.LevelId ;
      var toLevelId = toPickElement.LevelId ;

      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return SelectionRangeRouteManager.ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return SelectionRangeRouteManager.ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, RoomSelectState selectState )
    {
      var (powerConnector, sensorConnectors, room, sensorDirection, routeProperty, classificationInfo, pipeSpec) = selectState ;

      if ( room == null ) return new List<(string RouteName, RouteSegment Segment)>() ;

      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;
      var sensorFixedHeight = routeProperty.GetFromFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var diameter = routeProperty.GetDiameter() ;
      var radius = diameter * 0.5 ;
      var nameBase = GetNameBase( curveType ) ;
      var nextIndex = SelectionRangeRouteManager.GetRouteNameIndex( RouteCache.Get( DocumentKey.Get( document ) ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;

      var result = new List<(string RouteName, RouteSegment Segment)>() ;
      var isOutFromConnector = RoomRouteManager.IsPickedElementOutsideOfRoom( document, room, powerConnector.GetTopConnectorOfConnectorFamily().Origin ) ;
      var (insideSensorConnectors, outsideSensorConnectors) = ClassifySensorConnectorsInsideOrOutsideRoom( document, sensorConnectors, room ) ;
      if ( isOutFromConnector ) {
        CreateRouteSegment( result, document, isOutFromConnector, room, powerConnector, outsideSensorConnectors, insideSensorConnectors, classificationInfo, systemType, curveType, routeProperty, sensorDirection, pipeSpec, routeName, radius, diameter, sensorFixedHeight, avoidType, nameBase, nextIndex ) ;
      }
      else {
        CreateRouteSegment( result, document, isOutFromConnector, room, powerConnector, insideSensorConnectors, outsideSensorConnectors, classificationInfo, systemType, curveType, routeProperty, sensorDirection, pipeSpec, routeName, radius, diameter, sensorFixedHeight, avoidType, nameBase, nextIndex ) ;
      }

      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter

      // change color connectors
      var allConnectors = new List<FamilyInstance> { powerConnector } ;
      allConnectors.AddRange( sensorConnectors ) ;
      ConfirmUnsetCommandBase.ResetElementColor( allConnectors ) ;

      return result ;
    }

    private void CreateRouteSegment( List<(string RouteName, RouteSegment Segment)> result, Document document, bool isOutFromConnector, Reference? room, FamilyInstance powerConnector, IReadOnlyList<FamilyInstance> startSensorConnectors, IReadOnlyList<FamilyInstance> endSensorConnectors, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, IRouteProperty routeProperty, SelectionRangeRouteManager.SensorArrayDirection sensorDirection, MEPSystemPipeSpec pipeSpec, string routeName, double radius, double diameter, FixedHeight? sensorFixedHeight, AvoidType avoidType, string nameBase, int nextIndex )
    {
      IReadOnlyList<FamilyInstance> startPassPoints = new List<FamilyInstance>() ;
      FamilyInstance? startFootPassPoint = null ;
      IReadOnlyList<FamilyInstance> endPassPoints = new List<FamilyInstance>() ;
      FamilyInstance? endFootPassPoint = null ;
      IReadOnlyList<FamilyInstance> passPointsOnWallRoom = new List<FamilyInstance>() ;
      var sortEndSensorConnectors = new List<FamilyInstance>() ;
      var startPowerPosition = powerConnector.GetTopConnectorOfConnectorFamily().Origin ;
      var endPowerPosition = powerConnector.GetTopConnectorOfConnectorFamily().Origin ;
      if ( endSensorConnectors.Any() ) {
        XYZ passPointPosition ;
        ( passPointsOnWallRoom, passPointPosition ) = CreatePassPointOnRoomWall( document, room, isOutFromConnector, routeName, diameter, routeProperty.GetFromFixedHeight(), powerConnector.UniqueId, endSensorConnectors.Last().UniqueId ) ;
        if ( passPointsOnWallRoom.Any() ) {
          endPowerPosition = passPointPosition ;
        }
      }

      if ( startSensorConnectors.Any() ) {
        ( startFootPassPoint, startPassPoints ) = SelectionRangeRouteManager.CreatePassPoints( routeName, powerConnector, startSensorConnectors, sensorDirection, routeProperty, pipeSpec, startPowerPosition ) ;
      }

      if ( endSensorConnectors.Any() ) {
        var firstSubRouteName = nameBase + "_" + ( nextIndex + 1 ) ;
        sortEndSensorConnectors = endSensorConnectors.ToList() ;
        var endSensorDirection = SelectionRangeRouteManager.SortSensorConnectors( endPowerPosition, ref sortEndSensorConnectors ) ;
        ( endFootPassPoint, endPassPoints ) = SelectionRangeRouteManager.CreatePassPoints( firstSubRouteName, powerConnector, sortEndSensorConnectors, endSensorDirection, routeProperty, pipeSpec, endPowerPosition ) ;
      }

      CreateRouteSegment( result, document, powerConnector, startSensorConnectors, sortEndSensorConnectors, startFootPassPoint, startPassPoints, endFootPassPoint, endPassPoints, passPointsOnWallRoom, classificationInfo, systemType, curveType, routeProperty, routeName, radius, diameter, sensorFixedHeight, avoidType, nameBase, nextIndex, room!, isOutFromConnector, pipeSpec ) ;
    }

    private void CreateRouteSegment( List<(string RouteName, RouteSegment Segment)> result, Document document, FamilyInstance powerConnector, IReadOnlyList<FamilyInstance> startSensorConnectors, IReadOnlyList<FamilyInstance> endSensorConnectors, FamilyInstance? startFootPassPoint, IReadOnlyList<FamilyInstance> startPassPoints, FamilyInstance? endFootPassPoint, IReadOnlyList<FamilyInstance> endPassPoints, IReadOnlyList<FamilyInstance> passPointsOnWallRoom, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, IRouteProperty routeProperty, string routeName, double radius, double diameter, FixedHeight? sensorFixedHeight, AvoidType avoidType, string nameBase, int nextIndex, Reference room, bool isOutFromConnector, MEPSystemPipeSpec pipeSpec )
    {
      // main route
      var powerConnectorEndPoint = new ConnectorEndPoint( powerConnector.GetTopConnectorOfConnectorFamily(), radius ) ;
      var lastStartRouteFromEndPoints = (IEndPoint) powerConnectorEndPoint ;
      var powerConnectorEndPointKey = powerConnectorEndPoint.Key ;
      {
        if ( startPassPoints.Any() ) {
          var secondStartRouteFromEndPoints = EliminateSamePassPoints( startFootPassPoint, startPassPoints ).Select( pp => (IEndPoint) new PassPointEndPoint( pp ) ).ToList() ;
          var secondStartRouteToEndPoints = secondStartRouteFromEndPoints.Skip( 1 ).Append( new ConnectorEndPoint( startSensorConnectors.Last().GetTopConnectorOfConnectorFamily(), radius ) ) ;
          var firstStartRouteToEndPoint = secondStartRouteFromEndPoints[ 0 ] ;
          result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, powerConnectorEndPoint, firstStartRouteToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
          result.AddRange( secondStartRouteFromEndPoints.Zip( secondStartRouteToEndPoints, ( f, t ) =>
          {
            var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
            return ( routeName, segment ) ;
          } ) ) ;

          lastStartRouteFromEndPoints = new PassPointBranchEndPoint( document, startFootPassPoint != null ? startFootPassPoint.UniqueId : startPassPoints.First().UniqueId, radius, powerConnectorEndPointKey ) ;
        }
      }

      // branch routes
      var endPowerConnectorEndPointKey = powerConnectorEndPoint.Key ;
      {
        if ( endPassPoints.Any() ) {
          var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
          if ( endSensorConnectors.Count == 1 ) {
            var firstEndRouteToEndPoint = new ConnectorEndPoint( endSensorConnectors.Last().GetTopConnectorOfConnectorFamily(), radius ) ;
            if ( passPointsOnWallRoom.Any() ) {
              CreateSegmentThroughPassPointOnWall( result, passPointsOnWallRoom, lastStartRouteFromEndPoints, firstEndRouteToEndPoint, classificationInfo, systemType, curveType, routeProperty, subRouteName, diameter, sensorFixedHeight, avoidType ) ;
            }

            document.Delete( endPassPoints.First().UniqueId ) ;
            if ( endFootPassPoint != null ) document.Delete( endFootPassPoint.UniqueId ) ;
          }
          else {
            var passPoints = EliminateSamePassPoints( endFootPassPoint, endPassPoints ).ToList() ;
            var bendingRadius = pipeSpec.GetLongElbowSize( diameter.DiameterValueToPipeDiameter() ) ;
            var passPointOffset = 3 * ( bendingRadius + MEPSystemPipeSpec.MinimumShortCurveLength ) ;
            MovePassPoint( document, passPoints, room, isOutFromConnector, passPointOffset ) ;
            var secondEndRouteFromEndPoints = passPoints.Select( pp => (IEndPoint) new PassPointEndPoint( pp ) ).ToList() ;
            var secondEndRouteToEndPoints = secondEndRouteFromEndPoints.Skip( 1 ).Append( new ConnectorEndPoint( endSensorConnectors.Last().GetTopConnectorOfConnectorFamily(), radius ) ) ;
            var firstEndRouteToEndPoint = secondEndRouteFromEndPoints[ 0 ] ;

            if ( passPointsOnWallRoom.Any() ) {
              CreateSegmentThroughPassPointOnWall( result, passPointsOnWallRoom, lastStartRouteFromEndPoints, firstEndRouteToEndPoint, classificationInfo, systemType, curveType, routeProperty, subRouteName, diameter, sensorFixedHeight, avoidType ) ;
            }
            else {
              result.Add( ( subRouteName, new RouteSegment( classificationInfo, systemType, curveType, lastStartRouteFromEndPoints, firstEndRouteToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, null ) ) ) ;
            }

            result.AddRange( secondEndRouteFromEndPoints.Zip( secondEndRouteToEndPoints, ( f, t ) =>
            {
              var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
              return ( subRouteName, segment ) ;
            } ) ) ;
          }
        }
      }

      if ( startPassPoints.Any() ) {
        result.AddRange( startPassPoints.Zip( startSensorConnectors.Take( startPassPoints.Count ), ( pp, sensor ) =>
        {
          var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
          var branchEndPoint = new PassPointBranchEndPoint( document, pp.UniqueId, radius, powerConnectorEndPointKey ) ;
          var connectorEndPoint = new ConnectorEndPoint( sensor.GetTopConnectorOfConnectorFamily(), radius ) ;
          var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
          return ( subRouteName, segment ) ;
        } ) ) ;
      }

      if ( endPassPoints.Any() && endSensorConnectors.Count > 1 ) {
        result.AddRange( endPassPoints.Zip( endSensorConnectors.Take( endPassPoints.Count ), ( pp, sensor ) =>
        {
          var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
          var branchEndPoint = new PassPointBranchEndPoint( document, pp.UniqueId, radius, endPowerConnectorEndPointKey ) ;
          var connectorEndPoint = new ConnectorEndPoint( sensor.GetTopConnectorOfConnectorFamily(), radius ) ;
          var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
          return ( subRouteName, segment ) ;
        } ) ) ;
      }

      static IEnumerable<FamilyInstance> EliminateSamePassPoints( FamilyInstance? firstPassPoint, IEnumerable<FamilyInstance> passPoints )
      {
        if ( null != firstPassPoint ) yield return firstPassPoint ;

        var lastId = firstPassPoint?.Id ?? ElementId.InvalidElementId ;
        foreach ( var passPoint in passPoints ) {
          if ( passPoint.Id == lastId ) continue ;
          lastId = passPoint.Id ;
          yield return passPoint ;
        }
      }
    }

    private void MovePassPoint( Document document, IReadOnlyCollection<Element> passPoints, Reference reference, bool isOutFromConnector, double minDistanceByBendingRadius )
    {
      const double minDistanceBetweenPassPoints = 0.2 ;
      var minDistanceBetweenPassPointAndWall = Math.Max( 0.8, minDistanceByBendingRadius ) ;
      const double minOutDistanceBetweenPassPointAndWall = 0.4 ;
      var room = document.GetElement( reference.ElementId ) ;
      var lenght = room.ParametersMap.get_Item( "Lenght" ).AsDouble() ;
      var width = room.ParametersMap.get_Item( "Width" ).AsDouble() ;
      var thickness = isOutFromConnector ? room.ParametersMap.get_Item( "Thickness" ).AsDouble() : 0 ;
      var roomLocationPoint = ( room.Location as LocationPoint )!.Point ;
      var p1 = new XYZ( roomLocationPoint.X + thickness, roomLocationPoint.Y - thickness, roomLocationPoint.Z ) ;
      var p2 = new XYZ( roomLocationPoint.X + lenght - thickness, p1.Y, p1.Z ) ;
      var p4 = new XYZ( p1.X, roomLocationPoint.Y - width + thickness, p1.Z ) ;
      var firstPassPoint = passPoints.First() ;
      var firstPassPointLocationPoint = ( firstPassPoint.Location as LocationPoint ) ! ;
      var firstPassPointOrigin = firstPassPointLocationPoint.Point ;
      double xDistance = 0 ;
      double yDistance = 0 ;
      var xDistanceBetweenP1AndFirstPassPointOrigin = Math.Abs( p1.X - firstPassPointOrigin.X ) ;
      var xDistanceBetweenP2AndFirstPassPointOrigin = Math.Abs( p2.X - firstPassPointOrigin.X ) ;
      var yDistanceBetweenP1AndFirstPassPointOrigin = Math.Abs( p1.Y - firstPassPointOrigin.Y ) ;
      var yDistanceBetweenP4AndFirstPassPointOrigin = Math.Abs( p4.Y - firstPassPointOrigin.Y ) ;

      if ( isOutFromConnector ) {
        if ( p1.X > firstPassPointOrigin.X ) {
          xDistance = xDistanceBetweenP1AndFirstPassPointOrigin + minDistanceBetweenPassPointAndWall ;
        }
        else if ( xDistanceBetweenP1AndFirstPassPointOrigin < minDistanceBetweenPassPointAndWall ) {
          xDistance = minDistanceBetweenPassPointAndWall - xDistanceBetweenP1AndFirstPassPointOrigin ;
        }
        else if ( p2.X < firstPassPointOrigin.X ) {
          xDistance = -xDistanceBetweenP2AndFirstPassPointOrigin - minDistanceBetweenPassPointAndWall ;
        }
        else if ( xDistanceBetweenP2AndFirstPassPointOrigin < minDistanceBetweenPassPointAndWall ) {
          xDistance = xDistanceBetweenP2AndFirstPassPointOrigin - minDistanceBetweenPassPointAndWall ;
        }

        if ( p1.Y < firstPassPointOrigin.Y ) {
          yDistance = -yDistanceBetweenP1AndFirstPassPointOrigin - minDistanceBetweenPassPointAndWall ;
        }
        else if ( yDistanceBetweenP1AndFirstPassPointOrigin < minDistanceBetweenPassPointAndWall ) {
          yDistance = yDistanceBetweenP1AndFirstPassPointOrigin - minDistanceBetweenPassPointAndWall ;
        }
        else if ( p4.Y > firstPassPointOrigin.Y ) {
          yDistance = yDistanceBetweenP4AndFirstPassPointOrigin + minDistanceBetweenPassPointAndWall ;
        }
        else if ( yDistanceBetweenP4AndFirstPassPointOrigin < minDistanceBetweenPassPointAndWall ) {
          yDistance = minDistanceBetweenPassPointAndWall - yDistanceBetweenP4AndFirstPassPointOrigin ;
        }
      }
      else {
        var secondPassPoint = passPoints.ElementAt( 1 ) ;
        var secondPassPointLocationPoint = ( secondPassPoint.Location as LocationPoint ) ! ;
        var secondPassPointOrigin = secondPassPointLocationPoint.Point ;
        var firstPassPointPosition = RoomRouteManager.GetPassPointPositionOutRoom( firstPassPointOrigin, p1, p2, p4 ) ;
        var secondPassPointPosition = RoomRouteManager.GetPassPointPositionOutRoom( secondPassPointOrigin, p1, p2, p4 ) ;
        if ( p1.X < firstPassPointOrigin.X && firstPassPointOrigin.X < p2.X ) {
          if ( secondPassPointPosition == RoomRouteManager.RoomEdge.Left ) {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Right ) {
              xDistance = xDistanceBetweenP2AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
            else {
              xDistance = -xDistanceBetweenP1AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
          }
          else if ( secondPassPointPosition == RoomRouteManager.RoomEdge.Right ) {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Left ) {
              xDistance = -xDistanceBetweenP1AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
            else {
              xDistance = xDistanceBetweenP2AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
          }
          else if ( secondPassPointPosition == RoomRouteManager.RoomEdge.Back ) {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Left ) {
              xDistance = -xDistanceBetweenP1AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Right ) {
              xDistance = xDistanceBetweenP2AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Front ) {
            }
          }
          else {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Left ) {
              xDistance = -xDistanceBetweenP1AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Right ) {
              xDistance = xDistanceBetweenP2AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Back ) {
            }
          }
        }
        else if ( xDistanceBetweenP1AndFirstPassPointOrigin < minOutDistanceBetweenPassPointAndWall ) {
          xDistance = xDistanceBetweenP1AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
        }
        else if ( xDistanceBetweenP2AndFirstPassPointOrigin < minOutDistanceBetweenPassPointAndWall ) {
          xDistance = minDistanceBetweenPassPointAndWall - minOutDistanceBetweenPassPointAndWall ;
        }

        if ( p1.Y > firstPassPointOrigin.Y && firstPassPointOrigin.Y > p4.Y && firstPassPointPosition != secondPassPointPosition ) {
          if ( secondPassPointPosition == RoomRouteManager.RoomEdge.Left ) {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Back ) {
              yDistance = yDistanceBetweenP1AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Front ) {
              yDistance = -yDistanceBetweenP4AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Right ) {
            }
          }
          else if ( secondPassPointPosition == RoomRouteManager.RoomEdge.Right ) {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Back ) {
              yDistance = yDistanceBetweenP1AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Front ) {
              yDistance = -yDistanceBetweenP4AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
            else if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Left ) {
            }
          }
          else if ( secondPassPointPosition == RoomRouteManager.RoomEdge.Back ) {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Front ) {
              yDistance = -yDistanceBetweenP4AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
            else {
              yDistance = yDistanceBetweenP1AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
          }
          else {
            if ( firstPassPointPosition == RoomRouteManager.RoomEdge.Back ) {
              yDistance = yDistanceBetweenP1AndFirstPassPointOrigin + minOutDistanceBetweenPassPointAndWall ;
            }
            else {
              yDistance = -yDistanceBetweenP4AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
            }
          }
        }
        else if ( yDistanceBetweenP1AndFirstPassPointOrigin < minOutDistanceBetweenPassPointAndWall ) {
          yDistance = minOutDistanceBetweenPassPointAndWall - yDistanceBetweenP1AndFirstPassPointOrigin ;
        }
        else if ( yDistanceBetweenP4AndFirstPassPointOrigin < minOutDistanceBetweenPassPointAndWall ) {
          yDistance = yDistanceBetweenP4AndFirstPassPointOrigin - minOutDistanceBetweenPassPointAndWall ;
        }
      }

      if ( xDistance == 0 && yDistance == 0 ) return ;
      firstPassPoint.Location.Move( new XYZ( xDistance, yDistance, 0 ) ) ;
      if ( passPoints.Count <= 1 ) return ;
      var passPointsWithoutFirst = passPoints.ToList().GetRange( 1, passPoints.Count - 1 ) ;
      var prevPassPointOrigin = ( firstPassPoint.Location as LocationPoint )!.Point ;
      foreach ( var passPoint in passPointsWithoutFirst ) {
        double xDistanceBetweenPassPoint = 0 ;
        double yDistanceBetweenPassPoint = 0 ;
        var (xNextPassPoint, yNextPassPoint, _) = ( passPoint.Location as LocationPoint )!.Point ;
        var xDistanceBetweenPrevAndNextPassPoint = Math.Abs( xNextPassPoint - prevPassPointOrigin.X ) ;
        var yDistanceBetweenPrevAndNextPassPoint = Math.Abs( yNextPassPoint - prevPassPointOrigin.Y ) ;
        if ( xDistance > 0 ) {
          if ( xNextPassPoint <= prevPassPointOrigin.X ) {
            xDistanceBetweenPassPoint = minDistanceBetweenPassPoints + xDistanceBetweenPrevAndNextPassPoint ;
          }
          else if ( xDistanceBetweenPrevAndNextPassPoint < minDistanceBetweenPassPoints && Math.Abs( yNextPassPoint - prevPassPointOrigin.Y ) < minDistanceBetweenPassPoints ) {
            xDistanceBetweenPassPoint = minDistanceBetweenPassPoints - xDistanceBetweenPrevAndNextPassPoint ;
          }
        }
        else if ( xDistance < 0 ) {
          if ( xNextPassPoint >= prevPassPointOrigin.X ) {
            xDistanceBetweenPassPoint = -minDistanceBetweenPassPoints - xDistanceBetweenPrevAndNextPassPoint ;
          }
          else if ( xDistanceBetweenPrevAndNextPassPoint < minDistanceBetweenPassPoints && Math.Abs( yNextPassPoint - prevPassPointOrigin.Y ) < minDistanceBetweenPassPoints ) {
            xDistanceBetweenPassPoint = xDistanceBetweenPrevAndNextPassPoint - minDistanceBetweenPassPoints ;
          }
        }

        if ( yDistance > 0 ) {
          if ( yNextPassPoint <= prevPassPointOrigin.Y ) {
            yDistanceBetweenPassPoint = minDistanceBetweenPassPoints + yDistanceBetweenPrevAndNextPassPoint ;
          }
          else if ( Math.Abs( yNextPassPoint - prevPassPointOrigin.Y ) < minDistanceBetweenPassPoints && Math.Abs( xNextPassPoint - prevPassPointOrigin.X ) < minDistanceBetweenPassPoints ) {
            yDistanceBetweenPassPoint = minDistanceBetweenPassPoints - yDistanceBetweenPrevAndNextPassPoint ;
          }
        }
        else if ( yDistance < 0 ) {
          if ( yNextPassPoint >= prevPassPointOrigin.Y ) {
            yDistanceBetweenPassPoint = -minDistanceBetweenPassPoints - yDistanceBetweenPrevAndNextPassPoint ;
          }
          else if ( yDistanceBetweenPrevAndNextPassPoint < minDistanceBetweenPassPoints && Math.Abs( xNextPassPoint - prevPassPointOrigin.X ) < minDistanceBetweenPassPoints ) {
            yDistanceBetweenPassPoint = yDistanceBetweenPrevAndNextPassPoint - minDistanceBetweenPassPoints ;
          }
        }

        if ( xDistanceBetweenPassPoint != 0 || yDistanceBetweenPassPoint != 0 ) {
          passPoint.Location.Move( new XYZ( xDistanceBetweenPassPoint, yDistanceBetweenPassPoint, 0 ) ) ;
          prevPassPointOrigin = ( passPoint.Location as LocationPoint )!.Point ;
        }
        else {
          return ;
        }
      }
    }

    private void CreateSegmentThroughPassPointOnWall( List<(string RouteName, RouteSegment Segment)> result, IReadOnlyCollection<FamilyInstance> passPointsOnWallRoom, IEndPoint lastStartRouteFromEndPoints, IEndPoint firstEndRouteToEndPoint, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, IRouteProperty routeProperty, string routeName, double diameter, FixedHeight? sensorFixedHeight, AvoidType avoidType )
    {
      if ( passPointsOnWallRoom.Count > 1 ) {
        var passPoint = new PassPointEndPoint( passPointsOnWallRoom.FirstOrDefault()! ) ;
        var passPoint2 = new PassPointEndPoint( passPointsOnWallRoom.LastOrDefault()! ) ;
        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, lastStartRouteFromEndPoints, passPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, passPoint, passPoint2, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, passPoint2, firstEndRouteToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
      }
      else {
        var passPoint = new PassPointEndPoint( passPointsOnWallRoom.FirstOrDefault()! ) ;
        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, lastStartRouteFromEndPoints, passPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, passPoint, firstEndRouteToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
      }
    }

    private ( List<FamilyInstance>, List<FamilyInstance> ) ClassifySensorConnectorsInsideOrOutsideRoom( Document document, IEnumerable<FamilyInstance> sensorConnectors, Reference room )
    {
      var insideRoomConnectors = new List<FamilyInstance>() ;
      var outsideRoomConnectors = new List<FamilyInstance>() ;
      foreach ( var sensorConnector in sensorConnectors ) {
        var isOut = RoomRouteManager.IsPickedElementOutsideOfRoom( document, room, sensorConnector.GetTopConnectorOfConnectorFamily().Origin ) ;
        if ( isOut ) outsideRoomConnectors.Add( sensorConnector ) ;
        else insideRoomConnectors.Add( sensorConnector ) ;
      }

      return ( insideRoomConnectors, outsideRoomConnectors ) ;
    }

    private ( List<FamilyInstance>, XYZ ) CreatePassPointOnRoomWall( Document document, Reference? room, bool isOutFromConnector, string name, double diameter, FixedHeight? fromFixedHeight, string fromConnectorId, string toConnectorId )
    {
      if ( room == null ) return ( new List<FamilyInstance>(), new XYZ() ) ;
      var pickRoom = document.GetElement( room.ElementId ) ;
      ElementId levelId = SelectionRangeRouteManager.GetTrueLevelId( document, pickRoom ) ;
      return RoomRouteManager.InsertPassPointElement( document, name, levelId, diameter, room, fromFixedHeight, isOutFromConnector, fromConnectorId, toConnectorId ) ;
    }
  }
}