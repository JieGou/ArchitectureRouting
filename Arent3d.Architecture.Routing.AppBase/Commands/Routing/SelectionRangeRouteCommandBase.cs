using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class SelectionRangeRouteCommandBase : RoutingCommandBase
  {
    private const string errorMessNoPowerAndSensorConnector = "No power connector and sensor connector selected." ;
    
    private const string errorMessNoPowerConnector = "No power connector selected." ;
    
    private const string errorMessNoSensorConnector = "No sensor connector selected." ;
    
    private const string errorMessSensorConnector = "At least 2 sensor connectors must be selected." ;
    
    public record SelectState( Element PowerConnector, Element FirstSensorConnector, Element LastSensorConnector, List<Element> SensorConnectors, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, Element FarthestSensorConnector ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var (powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, farthestSensorConnector) = SelectionRangeRoute( uiDocument ) ;
      if ( powerConnector == null && ( firstSensorConnector == null || lastSensorConnector == null || farthestSensorConnector == null ) ) return ( false, errorMessNoPowerAndSensorConnector ) ;
      if ( powerConnector == null ) return ( false, errorMessNoPowerConnector ) ;
      if ( firstSensorConnector == null || lastSensorConnector == null || farthestSensorConnector == null ) return ( false, errorMessNoSensorConnector ) ;
      if ( sensorConnectors.Count < 1 ) return ( false, errorMessSensorConnector ) ;

      var property = ShowPropertyDialog( uiDocument.Document, powerConnector, lastSensorConnector ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;

      if ( GetMEPSystemClassificationInfo( powerConnector, lastSensorConnector, property.GetSystemType() ) is not { } classificationInfo ) return ( false, null ) ;

      return ( true, new SelectState( powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, property, classificationInfo, farthestSensorConnector ) ) ;
    }

    private ( Element? powerConnector, Element? firstSensorConnector, Element? lastSensorConnector, List<Element> sensorConnectors, Element? farthestSensorConnector ) SelectionRangeRoute( UIDocument iuDocument )
    {
      var selectedElements = iuDocument.Selection.PickElementsByRectangle( "ドラックで複数コネクタを選択して下さい。" ) ;

      Element? powerConnector = null ;
      List<Element> sensorConnectors = new List<Element>() ;
      foreach ( var element in selectedElements ) {
        if ( !element.ParametersMap.Contains( "Revit.Property.Builtin.Connector Type".GetDocumentStringByKeyOrDefault( iuDocument.Document, "Connector Type" ) )) continue ;
        if ( element.ParametersMap.get_Item( "Revit.Property.Builtin.Connector Type".GetDocumentStringByKeyOrDefault( iuDocument.Document, "Connector Type" ) ).AsString() == RoutingElementExtensions.RouteConnectorType[ 0 ] ) {
          powerConnector = element ;
        }
        else if ( element.ParametersMap.get_Item( "Revit.Property.Builtin.Connector Type".GetDocumentStringByKeyOrDefault( iuDocument.Document, "Connector Type" ) ).AsString() == RoutingElementExtensions.RouteConnectorType[ 1 ] ) {
          sensorConnectors.Add( element ) ;
        }
      }

      Element? lastSensorConnector = sensorConnectors.Count < 1 ? null : sensorConnectors.First() ;
      Element? firstSensorConnector = sensorConnectors.Count < 1 ? null : sensorConnectors.First() ;
      Element? farthestSensorConnector = sensorConnectors.Count < 1 ? null : sensorConnectors.First() ;
      if ( powerConnector == null || sensorConnectors.Count < 1 ) return ( powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, farthestSensorConnector ) ;
      var powerPoint = powerConnector!.GetTopConnectors().Origin ;
      var maxDistance = sensorConnectors[ 0 ].GetTopConnectors().Origin.DistanceTo( powerPoint ) ;
      var minDistance = maxDistance ;
      if ( sensorConnectors.Count > 0 ) {
        foreach ( var element in sensorConnectors ) {
          var distance = element.GetTopConnectors().Origin.DistanceTo( powerPoint ) ;
          
          // 一番遠いコネクタ
          if ( distance > maxDistance ) {
            farthestSensorConnector = element ;
            maxDistance = distance ;            
          }
          
          // 一番近いコネクタ
          if ( ! ( distance < minDistance ) ) continue ;
          firstSensorConnector = element ;
          minDistance = distance ;
        }
      }

      var sensorConnectorList = from sensorConnector in sensorConnectors orderby sensorConnector.GetTopConnectors().Origin.Y ascending select sensorConnector ;
      sensorConnectors = sensorConnectorList.ToList() ;
      lastSensorConnector = sensorConnectors.First().GetTopConnectors().Origin.Y <= farthestSensorConnector!.GetTopConnectors().Origin.Y ? sensorConnectors.First() : farthestSensorConnector ; 
      sensorConnectors.Remove( lastSensorConnector! ) ;

      return ( powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, farthestSensorConnector ) ;
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( Element fromPickElement, Element toPickElement, MEPSystemType? systemType )
    {
      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private RoutePropertyDialog? ShowPropertyDialog( Document document, Element fromPickElement, Element toPickElement )
    {
      var fromLevelId = fromPickElement.LevelId ;
      var toLevelId = toPickElement.LevelId ;

      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
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

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      if ( state is PickRoutingCommandBase.PickState ) {
        var pickState = state as PickRoutingCommandBase.PickState ?? throw new InvalidOperationException() ;
        var (fromPickResult, toPickResult, routeProperty, classificationInfo) = pickState ;
        return CreateNewSegmentListForRoutePick( fromPickResult, toPickResult, true, routeProperty, classificationInfo ) ;
      }
      else {
        var selectState = state as SelectState ?? throw new InvalidOperationException() ;
        var (powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, routeProperty, classificationInfo, fasthestSensorConnector) = selectState ;
        return CreateNewSegmentList( document, powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, routeProperty, classificationInfo, fasthestSensorConnector ) ;
      }
    }

    private static FamilyInstance InsertPassPointElement( Document document, string routeName, Element fromPickElement, Element firstSensor, Element toPickElement, bool isFirst, FixedHeight? fromFixedHeight, int countSensorConnector, Element farthestSensor )
    {
      const double plusYOneSensor = 0.5 ;
      const double plusY = 1.2 ;
      var fromConnector = fromPickElement.GetTopConnectors() ;
      var toConnector = toPickElement.GetTopConnectors() ;
      var firstConnector = firstSensor.GetTopConnectors() ;
      var farthestConnector = farthestSensor.GetTopConnectors() ;
      IList<Element> levels = new FilteredElementCollector( document ).OfClass( typeof( Level ) ).ToElements() ;
      if ( levels.FirstOrDefault( level => level.Id == fromPickElement.GetLevelId() ) == null ) throw new InvalidOperationException() ;
      var level = levels.FirstOrDefault( level => level.Id == fromPickElement.GetLevelId() ) as Level ;
      var height = fromFixedHeight?.Height ?? 0 ;
      height += level!.Elevation ;
      XYZ position ;
      Vector3d direction ;
      if ( isFirst ) {
        var xPoint = ( fromConnector.Origin.X + farthestConnector.Origin.X ) * 0.5 ;
        var yPoint = ( fromConnector.Origin.Y + firstConnector.Origin.Y ) * 0.5 ;

        var cornerPointRight = new Vector2d( fromConnector.Origin.X, yPoint ) ;
        var cornerPointLeft = new Vector2d( farthestConnector.Origin.X, yPoint ) ;

        position = new XYZ( xPoint, yPoint, height ) ;
        direction = new Vector3d( cornerPointLeft.y - cornerPointRight.y, cornerPointRight.x - cornerPointLeft.x, height ) ;
      }
      else {
        var xPoint = ( firstConnector.Origin.X + farthestConnector.Origin.X ) * 0.5 ;
        var yPoint = countSensorConnector == 1 ? toConnector.Origin.Y + plusYOneSensor : toConnector.Origin.Y + plusY ;

        var cornerPointBack = new Vector2d( xPoint, firstConnector.Origin.Y ) ;
        var cornerPointFront = new Vector2d( xPoint, farthestConnector.Origin.Y ) ;
        position = new XYZ( xPoint, yPoint, height ) ;
        direction = new Vector3d( cornerPointBack.y - cornerPointFront.y, cornerPointFront.x - cornerPointBack.x, height ) ;
      }

      return document.AddPassPointSelectRange( routeName, position, direction.normalized.ToXYZRaw(), fromConnector.Radius, fromPickElement.GetLevelId() ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, Element powerConnector, Element firstSensorConnector, Element lastSensorConnector, List<Element> sensorConnectors, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, Element farthestSensorConnector )
    {
      List<(string RouteName, RouteSegment Segment)> listSegment = CreateSegmentOfNewRoute( document, powerConnector, firstSensorConnector, lastSensorConnector, sensorConnectors, routeProperty, classificationInfo, farthestSensorConnector ) ;
      return listSegment ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, Element powerConnector, Element firstSensorConnector, Element lastSensorConnector, List<Element> sensorConnectors, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, Element farthestSensorConnector )
    {
      var fromEndPoint = PickCommandUtil.GetEndPointConnector( powerConnector ) ;
      var toEndPoint = PickCommandUtil.GetEndPointConnector( lastSensorConnector ) ;

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
      var firstPassPoint = new PassPointEndPoint( InsertPassPointElement( document, name, powerConnector, firstSensorConnector, lastSensorConnector, true, fromFixedHeight, sensorConnectors.Count, farthestSensorConnector ) ) ;
      var secondPassPoint = new PassPointEndPoint( InsertPassPointElement( document, name, powerConnector, firstSensorConnector, lastSensorConnector, false, fromFixedHeight, sensorConnectors.Count, farthestSensorConnector ) ) ;
      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, toEndPoint, secondPassPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ) ;
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, secondPassPoint, firstPassPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ) ;
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, firstPassPoint, fromEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ) ;

      return routeSegments ;
    }

    private (string RouteName, RouteSegment Segment) CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
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

      return ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    public IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult sensorConnectorPickResult, bool anotherIndicatorIsFromSide, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      return CreateSubBranchRoute( routePickResult, sensorConnectorPickResult, anotherIndicatorIsFromSide, routeProperty, classificationInfo ).EnumerateAll() ;
    }

    private IEnumerable<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult sensorConnectorPickResult, bool anotherIndicatorIsFromSide, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var affectedRoutes = new List<Route>() ;
      var (routeEndPoint, otherSegments1) = CreateEndPointOnSubRoute( routePickResult, sensorConnectorPickResult, routeProperty, classificationInfo, true ) ;

      IEndPoint sensorConnectorEndPoint ;
      IReadOnlyCollection<( string RouteName, RouteSegment Segment )>? otherSegments2 = null ;
      if ( null != sensorConnectorPickResult.SubRoute ) {
        ( sensorConnectorEndPoint, otherSegments2 ) = CreateEndPointOnSubRoute( sensorConnectorPickResult, routePickResult, routeProperty, classificationInfo, false ) ;
      }
      else {
        sensorConnectorEndPoint = PickCommandUtil.GetEndPoint( sensorConnectorPickResult, routePickResult ) ;
      }

      var fromEndPoint = anotherIndicatorIsFromSide ? sensorConnectorEndPoint : routeEndPoint ;
      var toEndPoint = anotherIndicatorIsFromSide ? routeEndPoint : sensorConnectorEndPoint ;

      var document = routePickResult.SubRoute!.Route.Document ;
      var (name, segment) = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo ) ;

      // Inserted segment
      yield return ( name, segment ) ;

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
  }
}