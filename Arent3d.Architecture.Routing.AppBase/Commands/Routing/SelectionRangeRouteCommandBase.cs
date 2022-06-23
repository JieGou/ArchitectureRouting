using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class SelectionRangeRouteCommandBase : RoutingCommandBase<SelectionRangeRouteCommandBase.SelectState>
  {
    public record SelectState( IReadOnlyList<FamilyInstance> PowerConnectors, IReadOnlyList<FamilyInstance> SensorConnectors, SelectionRangeRouteManager.SensorArrayDirection SensorDirection, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, MEPSystemPipeSpec PipeSpec ) ;

    public record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;

    protected override OperationResult<SelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var routingExecutor = GetRoutingExecutor() ;

      var (powerConnectors, sensorConnectors, sensorDirection, errorMessage) = SelectionRangeRouteManager.SelectionRangeRoute( uiDocument ) ;
      if ( powerConnectors.Count() > 1 ) {
        if ( null != errorMessage ) return OperationResult<SelectState>.FailWithMessage( errorMessage ) ;
      }
      

      var powerConnector = powerConnectors.First() ;
      var farthestSensorConnector = sensorConnectors.Last() ;
      var property = ShowPropertyDialog( uiDocument.Document, powerConnector!, farthestSensorConnector ) ;
      if ( true != property?.DialogResult ) return OperationResult<SelectState>.Cancelled ;

      if ( GetMEPSystemClassificationInfo( powerConnector!, farthestSensorConnector, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<SelectState>.Failed ;

      var pipeSpec = new MEPSystemPipeSpec( new RouteMEPSystem( uiDocument.Document, property.GetSystemType(), property.GetCurveType() ), routingExecutor.FittingSizeCalculator ) ;

      return new OperationResult<SelectState>( new SelectState( powerConnectors, sensorConnectors, sensorDirection, property, classificationInfo, pipeSpec ) ) ;
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

        return SelectionRangeRouteManager.ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return SelectionRangeRouteManager.ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, SelectState selectState )
    {
      return CreateRouteSegments(document, selectState) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateRouteSegments(Document document, SelectState selectState )
    {
      var (powerConnectors, sensorConnectors, sensorDirection, routeProperty, classificationInfo, pipeSpec) = selectState ;
      
      var powerConnector = powerConnectors.First() ;
      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;
      var sensorFixedHeight = routeProperty.GetFromFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var diameter = routeProperty.GetDiameter() ;
      var radius = diameter * 0.5 ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = SelectionRangeRouteManager.GetRouteNameIndex( RouteCache.Get( DocumentKey.Get( document ) ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;
      
       var (footPassPoint, passPoints) = SelectionRangeRouteManager.CreatePassPoints( routeName, powerConnector, sensorConnectors, sensorDirection, routeProperty, pipeSpec, powerConnector.GetTopConnectorOfConnectorFamily().Origin ) ;
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter

      var allPassPoints = new List<FamilyInstance>() ;
      if ( footPassPoint != null ) allPassPoints.Add( footPassPoint ) ;
      allPassPoints.AddRange( passPoints ) ;
      if ( IsAnyPassPointsInsideEnvelope( document, allPassPoints ) ) {
        var allPassPointIds = allPassPoints.Select( p => p.UniqueId ).ToList() ;
        document.Delete( allPassPointIds ) ;
        MessageBox.Show( "Message.AppBase.Commands.Routing.SelectionRangeRouteCommandBase.ErrorMessageUnableToRouteDueToEnvelope".GetDocumentStringByKeyOrDefault( document, "選択範囲はenvelopeの干渉回避が不可能なため、envelopeの位置又はコネクタの位置を再調整してください。" ), "Error" ) ;
        return new List<(string RouteName, RouteSegment Segment)>() ;
      }

      var result = new List<(string RouteName, RouteSegment Segment)>( passPoints.Count * 2 + 1 ) ;

      // main route
      var powerConnectorEndPoint = new ConnectorEndPoint( powerConnector.GetTopConnectorOfConnectorFamily(), radius ) ;
      var powerConnectorEndPointKey = powerConnectorEndPoint.Key ;
      {
        var secondFromEndPoints = EliminateSamePassPoints( footPassPoint, passPoints ).Select( pp => (IEndPoint) new PassPointEndPoint( pp ) ).ToList() ;
        var secondToEndPoints = secondFromEndPoints.Skip( 1 ).Append( new ConnectorEndPoint( sensorConnectors.Last().GetTopConnectorOfConnectorFamily(), radius ) ) ;
        var firstToEndPoint = secondFromEndPoints[ 0 ] ;

        result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, powerConnectorEndPoint, firstToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
        result.AddRange( secondFromEndPoints.Zip( secondToEndPoints, ( f, t ) =>
        {
          var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
          return ( routeName, segment ) ;
        } ) ) ;
      }

      // branch routes
      result.AddRange( passPoints.Zip( sensorConnectors.Take( passPoints.Count ), ( pp, sensor ) =>
      {
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new PassPointBranchEndPoint( document, pp.UniqueId, radius, powerConnectorEndPointKey ) ;
        var connectorEndPoint = new ConnectorEndPoint( sensor.GetTopConnectorOfConnectorFamily(), radius ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, null ) ;
        return ( subRouteName, segment ) ;
      } ) ) ;

      // change color connectors
      var allConnectors = new List<FamilyInstance> { powerConnector } ;
      allConnectors.AddRange( sensorConnectors ) ;
      ConfirmUnsetCommandBase.ResetElementColor( document, allConnectors ) ;

      return result ;

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

    private static bool IsAnyPassPointsInsideEnvelope( Document document, IReadOnlyCollection<FamilyInstance> passPoints )
    {
      var envelopes = document.GetAllFamilyInstances( RoutingFamilyType.Envelope ).ToList() ;
      if ( ! envelopes.Any() ) return false ;
      foreach ( var envelope in envelopes ) {
        var envelopLocation = ( envelope.Location as LocationPoint ) ! ;
        var (xEnvelope, yEnvelope, zEnvelope) = envelopLocation.Point ;
        var lenghtEnvelope = envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Length".GetDocumentStringByKeyOrDefault( document, "奥行き" ) ).AsDouble() ;
        var widthEnvelope = envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Width".GetDocumentStringByKeyOrDefault( document, "幅" ) ).AsDouble() ;
        var heightEnvelope = envelope.ParametersMap.get_Item( "Revit.Property.Builtin.Envelope.Height".GetDocumentStringByKeyOrDefault( document, "高さ" ) ).AsDouble() ;
        var center = new Vector3d( xEnvelope, yEnvelope, zEnvelope + heightEnvelope / 2 ) ;
        var size = new Vector3d( widthEnvelope, lenghtEnvelope, heightEnvelope ) ;
        var envelopeBox = Box3d.ConstructFromCenterSize( center, size ) ;
        foreach ( var passPoint in passPoints ) {
          var passPointLocation = ( passPoint.Location as LocationPoint ) ! ;
          var (xPassPoint, yPassPoint, zPassPoint) = passPointLocation.Point ;
          if ( envelopeBox.Contains( new Vector3d( xPassPoint, yPassPoint, zPassPoint ), 0 ) ) {
            return true ;
          }
        }
      }

      return false ;
    }
  }
}