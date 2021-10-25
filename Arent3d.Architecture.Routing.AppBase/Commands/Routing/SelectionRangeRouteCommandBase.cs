using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class SelectionRangeRouteCommandBase : RoutingCommandBase
  {
    private readonly double distanceToPower = ( 600.0 ).MillimetersToRevitUnits() ;
    private readonly double xDistanceToSensor = ( 600.0 ).MillimetersToRevitUnits() ;
    private readonly double yDistanceToSensor = ( 600.0 ).MillimetersToRevitUnits() ;
    
    public record SelectState( Element PowerConnector, Element LastSensorConnector, List<Element> SensorConnectors, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;
    
    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;
    
    protected abstract AddInType GetAddInType() ;
    
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var (powerConnector, lastSensorConnector, sensorConnectors) = SelectionRangeRoute( uiDocument ) ;
      if (powerConnector == null || lastSensorConnector == null || sensorConnectors.Count < 1 ) return ( false, null ) ;
      
      var property = ShowPropertyDialog( uiDocument.Document, powerConnector, lastSensorConnector ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;

      if ( GetMEPSystemClassificationInfo( powerConnector, lastSensorConnector, property.GetSystemType() ) is not { } classificationInfo ) return ( false, null ) ;

      return ( true, new SelectState( powerConnector, lastSensorConnector, sensorConnectors, property, classificationInfo ) ) ;
    }

    private ( Element? powerConnector, Element? lastSensorConnector, List<Element> sensorConnectors ) SelectionRangeRoute( UIDocument iuDocument )
    {
      var selectedElements = iuDocument.Selection.PickElementsByRectangle( "ドラックで複数コネクタを選択して下さい。" ) ;

      Element? powerConnector = null;
      List<Element> sensorConnectors = new List<Element>() ;
      foreach ( var element in selectedElements ) {
        if ( element.Category.Name != "Electrical Fixtures" ) continue ;
        if ( element.ParametersMap.get_Item( "Revit.Property.Builtin.Connector Type".GetDocumentStringByKeyOrDefault( iuDocument.Document, "Connector Type" ) ).AsString() == RoutingElementExtensions.RouteConnectorType[ 0 ] ) {
          powerConnector = element ;
        }
        else {
          sensorConnectors.Add( element ) ;
        }
      }

      Element? lastSensorConnector = null ;
      if ( powerConnector == null || sensorConnectors.Count <= 0 ) return ( powerConnector, lastSensorConnector, sensorConnectors ) ;
      var powerPoint = powerConnector!.GetTopConnectors().Origin ;
      var maxDistance = sensorConnectors[ 0 ].GetTopConnectors().Origin.DistanceTo( powerPoint ) ;
      if ( sensorConnectors.Count > 0 ) {
        foreach ( var element in sensorConnectors ) {
          var distance = element.GetTopConnectors().Origin.DistanceTo( powerPoint ) ;
          if ( ! ( distance > maxDistance ) ) continue ;
          lastSensorConnector = element ;
          maxDistance = distance ;
        }
      }

      sensorConnectors.Remove( lastSensorConnector! ) ;

      return ( powerConnector, lastSensorConnector, sensorConnectors ) ;
    }
    
    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( Element fromPickElement, Element toPickElement, MEPSystemType? systemType )
    {
      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private RoutePropertyDialog? ShowPropertyDialog( Document document, Element fromPickElement, Element toPickElement )
    {
      var fromLevelId = fromPickElement.LevelId;
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
      var selectState = state as SelectState ?? throw new InvalidOperationException() ;
      var (powerConnector, lastSensorConnector, sensorConnectors, routeProperty, classificationInfo) = selectState ;
      var segmentList = CreateNewSegmentList( document, powerConnector, lastSensorConnector, sensorConnectors, routeProperty, classificationInfo ) ;

      return segmentList ;
    }
    
    private FamilyInstance InsertPassPointElement( Document document, string RouteName, Element fromPickElement, Element toPickElement, double Xdistance, double Ydistance, bool isFrom )
    {
      var fromConnector = fromPickElement.GetTopConnectors() ;
      var toConnector = toPickElement.GetTopConnectors() ;
      var firstXCoordinate = isFrom ? fromConnector.Origin.X : toConnector.Origin.X ;
      var firstYCoordinate = isFrom ? fromConnector.Origin.Y : toConnector.Origin.Y ;
      var xPoint = fromConnector.Origin.X - toConnector.Origin.X > 0 ? firstXCoordinate - Xdistance : firstXCoordinate + Xdistance ;
      var yPoint = fromConnector.Origin.Y - toConnector.Origin.Y > 0 ? firstYCoordinate - Ydistance : firstYCoordinate + Ydistance ;
      var position = new XYZ( xPoint, yPoint, fromConnector.Origin.Z);
      var direction = toConnector.Origin.To3dRaw() - fromConnector.Origin.To3dRaw() ;  
      return document.AddPassPointSelectRange( RouteName, position, direction.normalized.ToXYZRaw(), fromConnector.Radius, fromPickElement.GetLevelId() ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, Element powerConnector, Element lastSensorConnector, List<Element> sensorConnectors, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      List<(string RouteName, RouteSegment Segment)> listSegment = CreateSegmentOfNewRoute( document, powerConnector, lastSensorConnector, sensorConnectors, routeProperty, classificationInfo ) ;
      return listSegment ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, Element powerConnector, Element lastSensorConnector, List<Element> sensorConnectors, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var fromEndPoint = PickCommandUtil.GetEndPointConnector( powerConnector, lastSensorConnector ) ;
      var toEndPoint = PickCommandUtil.GetEndPointConnector( lastSensorConnector, powerConnector ) ;
      
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
      
      var firstPassPoint = new PassPointEndPoint ( InsertPassPointElement( document, name, powerConnector, lastSensorConnector, distanceToPower, distanceToPower, true ) ) ;
      var secondPassPoint = new PassPointEndPoint ( InsertPassPointElement( document, name, powerConnector, lastSensorConnector, -xDistanceToSensor, -yDistanceToSensor,false ) ) ;
      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, firstPassPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) );
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, firstPassPoint, secondPassPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) );
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, secondPassPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementId ) ) );

      return routeSegments ;
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