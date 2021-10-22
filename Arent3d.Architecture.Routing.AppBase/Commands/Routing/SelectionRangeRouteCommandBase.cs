using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class SelectionRangeRouteCommandBase : RoutingCommandBase
  {
    private record PickState( Element PowerConnector, List<Element> SensorConnectors, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;
    
    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;
    
    protected abstract AddInType GetAddInType() ;
    
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var (powerConnector, sensorConnectors, sectionRange) = SelectionRangeRoute( uiDocument ) ;
      if (powerConnector == null || sensorConnectors.Count < 1 ) return ( false, null ) ;
      
      var property = ShowPropertyDialog( uiDocument.Document, powerConnector, sensorConnectors[0] ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;

      if ( GetMEPSystemClassificationInfo( powerConnector, sensorConnectors[0], property.GetSystemType() ) is not { } classificationInfo ) return ( false, null ) ;

      return ( true, new PickState( powerConnector, sensorConnectors, property, classificationInfo ) ) ;
    }

    private (Element? powerConnector, List<Element> sensorConnectors, List<double> sectionRange) SelectionRangeRoute( UIDocument iuDocument )
    {
      var selectedElements = iuDocument.Selection.PickElementsByRectangle( "ドラックで複数コネクタを選択して下さい。" ) ;

      List<Element> sensorConnectors = new List<Element>() ;
      foreach ( var element in selectedElements ) {
        if ( element.Category.Name == "Electrical Fixtures" )
          sensorConnectors.Add( element ) ;
      }

      Element? powerConnector = null;
      if ( sensorConnectors.Count > 0 ) {
        foreach ( var element in sensorConnectors ) {
          if ( element.ParametersMap.get_Item( "Revit.Property.Builtin.Connector Type".GetDocumentStringByKeyOrDefault( iuDocument.Document, "Connector Type" ) ).AsString() == RoutingElementExtensions.RouteConnectorType[0] ) {
            powerConnector = element ;
            sensorConnectors.Remove( element ) ;
            break ;
          }
        }
      }

      List<double> sectionRange = new List<double>() ;
      if ( powerConnector != null ) {
        var powerPoint = powerConnector!.GetConnectors().First().Origin ;
        var maxXPoint = powerPoint.X ;
        var minXPoint = powerPoint.X ;
        var maxYPoint = powerPoint.Y ;
        var minYPoint = powerPoint.Y ;
        if ( sensorConnectors.Count > 0 ) {
          foreach ( var element in sensorConnectors ) {
            var sensorPoint = element.GetConnectors().First().Origin ;
            if ( sensorPoint.X > maxXPoint) {
              maxXPoint = sensorPoint.X ;
            }
            if ( sensorPoint.X < minXPoint) {
              minXPoint = sensorPoint.X ;
            }
            if ( sensorPoint.Y > maxYPoint) {
              maxYPoint = sensorPoint.Y ;
            }
            if ( sensorPoint.Y < minYPoint) {
              minYPoint = sensorPoint.Y ;
            }
          }
        }
        sectionRange.AddRange( new List<double>() {maxXPoint, minXPoint, maxYPoint, minYPoint} );
      }
      
      return ( powerConnector, sensorConnectors, sectionRange ) ;
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
      var pickState = state as PickState ?? throw new InvalidOperationException() ;
      var (fromPickResult, toPickResult, routeProperty, classificationInfo) = pickState ;

      return CreateNewSegmentList( document, fromPickResult, toPickResult, routeProperty, classificationInfo ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, Element fromPickElement, List<Element> toPickElements, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      List<(string RouteName, RouteSegment Segment)> listSegment = new List<(string RouteName, RouteSegment Segment)>() ;
      foreach ( var toPickElement in toPickElements ) {
        var fromEndPoint = PickCommandUtil.GetEndPointConnector( fromPickElement, toPickElement ) ;
        var toEndPoint = PickCommandUtil.GetEndPointConnector( toPickElement, fromPickElement ) ;

        var (name, segment) = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo ) ;
        listSegment.Add( ( name, segment ) );
      }
      return listSegment ;
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
  }
}