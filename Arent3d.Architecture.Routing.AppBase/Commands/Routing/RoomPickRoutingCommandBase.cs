using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class RoomPickRoutingCommandBase : RoutingCommandBase<RoomPickRoutingCommandBase.RoomPickState>
  {
    public record RoomPickState( ConnectorPicker.IPickResult FromPickResult, ConnectorPicker.IPickResult ToPickResult, Reference? RoomPick, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<RoomPickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var routingExecutor = GetRoutingExecutor() ;
      var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
      ConnectorPicker.IPickResult toPickResult ;

      using ( uiDocument.SetTempColor( fromPickResult ) ) {
        toPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, false, "Dialog.Commands.Routing.PickRouting.PickSecond".GetAppStringByKeyOrDefault( null ), fromPickResult, GetAddInType() ) ;
      }

      var room = PickRoom( uiDocument ) ;

      var property = ShowPropertyDialog( uiDocument.Document, fromPickResult, toPickResult ) ;
      if ( true != property?.DialogResult ) return OperationResult<RoomPickState>.Cancelled ;

      if ( GetMEPSystemClassificationInfo( fromPickResult, toPickResult, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<RoomPickState>.Cancelled ;

      return new OperationResult<RoomPickState>( new RoomPickState( fromPickResult, toPickResult, room, property, classificationInfo ) ) ;
    }

    private Reference? PickRoom( UIDocument uiDocument )
    {
      RoomPickFilter roomPickFilter = new RoomPickFilter() ;
      Reference? element = null ;
      FamilyInstance? room = null ;
      while ( room == null ) {
        MessageBox.Show( "Please select point in room's wall", "Message" ) ;
        element = uiDocument.Selection.PickObject( ObjectType.Element, roomPickFilter, "Select room." ) ;
        room = uiDocument.Document.GetAllFamilyInstances( RoutingFamilyType.Room ).FirstOrDefault( r => r.Id == element.ElementId ) ;
      }

      return element ;
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, MEPSystemType? systemType )
    {
      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute )?.Route.GetSystemClassificationInfo() is { } routeSystemClassificationInfo ) return routeSystemClassificationInfo ;

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private IRoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var fromLevelId = GetTrueLevelId( document, fromPickResult ) ;
      var toLevelId = GetTrueLevelId( document, toPickResult ) ;

      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute ) is { } subRoute ) {
        var route = subRoute.Route ;

        return ShowDialog( document, new DialogInitValues( route.GetSystemClassificationInfo(), route.GetMEPSystemType(), route.GetDefaultCurveType(), subRoute.GetDiameter() ), fromLevelId, toLevelId ) ;
      }

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }

    private static ElementId GetTrueLevelId( Document document, Element element )
    {
      var levelId = element.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      var locationPoint = ( element.Location as LocationPoint ) ! ;
      var origin = locationPoint.Point ;

      return document.GuessLevel( origin! ).Id ;
    }

    protected IRoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType, routeChoiceSpec.StandardTypes?.FirstOrDefault(), initValues.Diameter ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, routeChoiceSpec ) ) ;
      sv.ShowDialog() ;

      return sv ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, RoomPickState roomPickState )
    {
      var (fromPickResult, toPickResult, room, routeProperty, classificationInfo) = roomPickState ;

      RouteGenerator.CorrectEnvelopes( document ) ;

      if ( null != fromPickResult.SubRoute ) {
        return CreateNewSegmentListForRoutePick( fromPickResult, toPickResult, room, false, routeProperty, classificationInfo ) ;
      }

      if ( null != toPickResult.SubRoute ) {
        return CreateNewSegmentListForRoutePick( toPickResult, fromPickResult, room, true, routeProperty, classificationInfo ) ;
      }

      return CreateNewSegmentList( document, fromPickResult, toPickResult, room, routeProperty, classificationInfo ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, Reference? room, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;
      var fromOrigin = fromPickResult.GetOrigin() ;
      var fromConnectorId = fromPickResult.PickedElement.Id.IntegerValue.ToString() ;
      var toConnectorId = toPickResult.PickedElement.Id.IntegerValue.ToString() ;

      var routeSegments = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, room, fromOrigin, fromConnectorId, toConnectorId, routeProperty, classificationInfo ) ;

      return routeSegments ;
    }

    private enum RoomEdge
    {
      Left,
      Right,
      Front,
      Back
    }

    private static FamilyInstance InsertPassPointElement( Document document, string routeName, ElementId? levelId, double radius, Reference room, FixedHeight? fromFixedHeight, bool isOut, string fromConnectorId, string toConnectorId )
    {
      IList<Element> levels = new FilteredElementCollector( document ).OfClass( typeof( Level ) ).ToElements() ;
      if ( levels.FirstOrDefault( l => l.Id == levelId ) == null ) throw new InvalidOperationException() ;
      var level = levels.FirstOrDefault( l => l.Id == levelId ) as Level ;
      var height = fromFixedHeight?.Height ?? 0 ;
      height += level!.Elevation ;
      XYZ position = new XYZ( room.GlobalPoint.X, room.GlobalPoint.Y, height ) ;
      Vector3d direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
      var edgeRoom = GetRoomEdgeInsertPassPoint( document, room ) ;
      switch ( edgeRoom ) {
        case RoomEdge.Left :
          direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          break ;
        case RoomEdge.Right :
          direction = isOut ? new Vector3d( -1, 0, 0 ) : new Vector3d( 1, 0, 0 ) ;
          break ;
        case RoomEdge.Front :
          direction = isOut ? new Vector3d( 0, 1, 0 ) : new Vector3d( 0, -1, 0 ) ;
          break ;
        case RoomEdge.Back :
          direction = isOut ? new Vector3d( 0, -1, 0 ) : new Vector3d( 0, 1, 0 ) ;
          break ;
      }

      var passPoint = document.AddPassPoint( routeName, position, direction.normalized.ToXYZRaw(), radius, levelId! ) ;
      passPoint.SetProperty( PassPointParameter.RelatedConnectorId, toConnectorId ) ;
      passPoint.SetProperty( PassPointParameter.RelatedFromConnectorId, fromConnectorId ) ;
      return passPoint ;
    }

    private static RoomEdge GetRoomEdgeInsertPassPoint( Document document, Reference element )
    {
      var passPoint = element.GlobalPoint ;
      var room = document.GetElement( element.ElementId ) ;
      if ( room == null ) return RoomEdge.Left ;
      var locationPoint = ( room.Location as LocationPoint ) ! ;
      var lenght = room.ParametersMap.get_Item( "Lenght" ).AsDouble() ;
      var width = room.ParametersMap.get_Item( "Width" ).AsDouble() ;
      var p1 = locationPoint.Point ;
      var p2 = new XYZ( p1.X + lenght, p1.Y, p1.Z ) ;
      var p3 = new XYZ( p2.X, p2.Y - width, p2.Z ) ;
      var p4 = new XYZ( p1.X, p1.Y - width, p1.Z ) ;
      if ( Math.Abs( passPoint.X - p1.X ) < 0.01 && p4.Y < passPoint.Y && passPoint.Y < p1.Y )
        return RoomEdge.Left ;
      if ( Math.Abs( passPoint.X - p2.X ) < 0.01 && p3.Y < passPoint.Y && passPoint.Y < p2.Y )
        return RoomEdge.Right ;
      if ( Math.Abs( passPoint.Y - p4.Y ) < 0.01 && p4.X < passPoint.X && passPoint.X < p3.X )
        return RoomEdge.Front ;
      if ( Math.Abs( passPoint.Y - p1.Y ) < 0.01 && p1.X < passPoint.X && passPoint.X < p2.X )
        return RoomEdge.Back ;

      return RoomEdge.Left ;
    }

    private bool CheckFromPickElementIsInOrOutRoom( Document document, Reference element, XYZ fromEndPoint )
    {
      var room = document.GetElement( element.ElementId ) ;
      if ( room == null ) return true ;
      var locationPoint = ( room.Location as LocationPoint ) ! ;
      var lenght = room.ParametersMap.get_Item( "Lenght" ).AsDouble() ;
      var width = room.ParametersMap.get_Item( "Width" ).AsDouble() ;
      var p1 = locationPoint.Point ;
      var p2 = new XYZ( p1.X + lenght, p1.Y, p1.Z ) ;
      var p3 = new XYZ( p2.X, p2.Y - width, p2.Z ) ;
      return ! (fromEndPoint.X > p1.X) || ! (fromEndPoint.X < p2.X) || ! (fromEndPoint.Y < p1.Y) || ! (fromEndPoint.Y > p3.Y) ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, Reference? room, XYZ fromOrigin, string fromConnectorId, string toConnectorId, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;

      var routes = RouteCache.Get( document ) ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = routeProperty.GetDiameter() ;
      var isRoutingOnPipeSpace = routeProperty.GetRouteOnPipeSpace() ;
      var fromFixedHeight = routeProperty.GetFromFixedHeight() ;
      var toFixedHeight = routeProperty.GetToFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var shaftElementId = routeProperty.GetShaft()?.Id ?? ElementId.InvalidElementId ;

      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      if ( room != null ) {
        var pickRoom = document.GetElement( room.ElementId ) ;
        ElementId levelId = GetTrueLevelId( document, pickRoom ) ;
        var isOut = CheckFromPickElementIsInOrOutRoom( document, room, fromOrigin ) ;
        var passPoint = new PassPointEndPoint( InsertPassPointElement( document, name, levelId, diameter / 2, room, fromFixedHeight, isOut, fromConnectorId, toConnectorId ) ) ;
        routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, passPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ) ;
        routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, passPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ) ;
      }
      else {
        routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ) ;
      }

      return routeSegments ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, Reference? room, bool anotherIndicatorIsFromSide, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      return CreateSubBranchRoute( routePickResult, anotherPickResult, room, anotherIndicatorIsFromSide, routeProperty, classificationInfo ).EnumerateAll() ;
    }

    private IEnumerable<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, Reference? room, bool anotherIndicatorIsFromSide, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var affectedRoutes = new List<Route>() ;
      var (routeEndPoint, otherSegments1) = CreateEndPointOnSubRoute( routePickResult, anotherPickResult, routeProperty, classificationInfo, true ) ;

      IEndPoint anotherEndPoint ;
      IReadOnlyCollection<( string RouteName, RouteSegment Segment )>? otherSegments2 = null ;
      if ( null != anotherPickResult.SubRoute ) {
        ( anotherEndPoint, otherSegments2 ) = CreateEndPointOnSubRoute( anotherPickResult, routePickResult, routeProperty, classificationInfo, false ) ;
      }
      else {
        anotherEndPoint = PickCommandUtil.GetEndPoint( anotherPickResult, routePickResult, UseConnectorDiameter() ) ;
      }

      var fromEndPoint = anotherIndicatorIsFromSide ? anotherEndPoint : routeEndPoint ;
      var toEndPoint = anotherIndicatorIsFromSide ? routeEndPoint : anotherEndPoint ;

      var document = routePickResult.SubRoute!.Route.Document ;
      var fromOrigin = anotherIndicatorIsFromSide ? anotherPickResult.GetOrigin() : routePickResult.GetOrigin() ;
      var fromConnectorId = anotherIndicatorIsFromSide ? anotherPickResult.PickedElement.Id.IntegerValue.ToString() : routePickResult.EndPointOverSubRoute!.GetElementId() ;
      string toConnectorId ;
      if ( anotherIndicatorIsFromSide )
        toConnectorId = routePickResult.EndPointOverSubRoute!.GetElementId() ;
      else
        toConnectorId = null != anotherPickResult.SubRoute ? anotherPickResult.EndPointOverSubRoute!.GetElementId() : anotherPickResult.PickedElement.Id.IntegerValue.ToString() ;

      var routeSegments = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, room, fromOrigin, fromConnectorId, toConnectorId, routeProperty, classificationInfo ) ;

      // Inserted segment
      foreach ( var (name, segment) in routeSegments ) {
        yield return ( name, segment ) ;
      }

      // Routes where pass points are inserted
      var routes = RouteCache.Get( routePickResult.SubRoute!.Route.Document ) ;
      var changedRoutes = new HashSet<Route>() ;
      if ( null != otherSegments1 ) {
        foreach ( var tuple in otherSegments1 ) {
          yield return tuple ;

          if ( routes.TryGetValue( tuple.RouteName, out var route ) ) {
            changedRoutes.Add( route ) ;
          }
        }
      }

      if ( null != otherSegments2 ) {
        foreach ( var tuple in otherSegments2 ) {
          yield return tuple ;

          if ( routes.TryGetValue( tuple.RouteName, out var route ) ) {
            changedRoutes.Add( route ) ;
          }
        }
      }

      // Affected routes
      if ( 0 != affectedRoutes.Count ) {
        var affectedRouteSet = new HashSet<Route>() ;
        foreach ( var route in affectedRoutes ) {
          affectedRouteSet.Add( route ) ;
          affectedRouteSet.UnionWith( route.CollectAllDescendantBranches() ) ;
        }

        affectedRouteSet.ExceptWith( changedRoutes ) ;

        foreach ( var tuple in affectedRouteSet.ToSegmentsWithName() ) {
          yield return tuple ;
        }
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    private class RoomPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return ( e.Category.Id.IntegerValue.Equals( (int) BuiltInCategory.OST_GenericModel ) ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
  }
}