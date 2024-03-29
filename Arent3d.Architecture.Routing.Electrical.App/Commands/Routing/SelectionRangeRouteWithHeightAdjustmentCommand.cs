﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Electrical.App.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.SelectionRangeRouteWithHeightAdjustmentCommand", DefaultString = "Selection Range \nRoute With Height Adjustment" )]
  [Image( "resources/RerouteAll.png" )]
  public class SelectionRangeRouteWithHeightAdjustmentCommand : RoutingCommandBase<SelectionRangeRouteWithHeightAdjustmentCommand.SelectState>
  {
    private const string ErrorMessageNoPowerAndPassAndSensorConnector = "No power, pass and sensor connectors are selected." ;
    private const string ErrorMessageNoPowerAndPassConnector = "No power, pass connectors are selected." ;
    private const string ErrorMessageNoPowerAndSensorConnector = "No power, sensor connectors are selected." ;
    private const string ErrorMessageNoPassAndSensorConnector = "No pass, sensor connectors are selected." ;
    private const string ErrorMessageNoPowerConnector = "No power connector is selected." ;
    private const string ErrorMessageNoPassConnector = "No pass connector is selected." ;
    private const string ErrorMessageTwoOrMorePowerConnector = "Two or more power connectors are selected." ;
    private const string ErrorMessageTwoOrMorePassConnector = "Two or more pass connectors are selected." ;
    private const string ErrorMessageNoSensorConnector = "No sensor connectors are selected on the power connector level." ;
    private const string ErrorMessageSensorConnector = "At least two sensor connectors on the power connector level must be selected." ;
    private const string ErrorMessageCannotDetermineSensorConnectorArrayDirection = "Couldn't determine sensor array direction" ;
    private const string ConfirmMessageHeightSettingNotGood = "The height from Power to Pass need to be larger than height of Pass\nAnd height of Pass need to be larger than height from Pass to Sensors\nDo you still want to connect with these height settings?" ;
    private const string ConfirmCaptionHeightSettingNotGood = "Height Settings Are Not Good Confirmation" ;

    protected override OperationResult<SelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var routingExecutor = GetRoutingExecutor() ;

      var (powerConnector, passConnector, sensorConnectors, sensorDirection, errorMessage) = SelectionRangeRoute( uiDocument ) ;
      if ( null != errorMessage ) return OperationResult<SelectState>.FailWithMessage( errorMessage ) ;

      var farthestSensorConnector = sensorConnectors.Last() ;
      var property = ShowPropertyDialog( uiDocument.Document, powerConnector!, passConnector!, farthestSensorConnector ) ;
      if ( true != property?.DialogResult ) return OperationResult<SelectState>.Cancelled ;
      if ( ! IsGoodHeightSettings( property, passConnector! ) ) {
        MessageBoxResult messageBoxResult = MessageBox.Show( ConfirmMessageHeightSettingNotGood, ConfirmCaptionHeightSettingNotGood, MessageBoxButton.YesNo ) ;
        if ( messageBoxResult == MessageBoxResult.No ) return OperationResult<SelectState>.Cancelled ;
      }

      if ( GetMepSystemClassificationInfo( powerConnector!, farthestSensorConnector ) is not { } classificationInfo ) return OperationResult<SelectState>.Failed ;

      var pipeSpec = new MEPSystemPipeSpec( new RouteMEPSystem( uiDocument.Document, property.GetSystemType(), property.GetCurveType() ), routingExecutor.FittingSizeCalculator ) ;
      using Transaction transaction = new Transaction( uiDocument.Document, "ReverseRotate" ) ;
      transaction.Start() ;

      transaction.Commit() ;
      return new OperationResult<SelectState>( new SelectState( powerConnector!, passConnector!, sensorConnectors, sensorDirection, property, classificationInfo, pipeSpec ) ) ;
    }

    private static bool IsGoodHeightSettings( RangeRangeRouteWithHeightAdjustmentPropertyDialog property, FamilyInstance passConnector )
    {
      var heightFromPowerToPass = property.GetPowerToPassFromFixedHeight() ;
      var heightFromPassToSensors = property.GetFromFixedHeight() ;
      bool isHeightFromPowerToPassLargerThanPassHeight = false, isPassHeightLargerThanFromPassToSensorsHeight = false ;
      var level = passConnector.Document.GetElement( passConnector.LevelId ) as Level ;
      var passTopHeight = passConnector.GetTopConnectorOfConnectorFamily().Origin.Z - level?.Elevation ;
      var passBottomHeight = passConnector.GetBottomConnectorOfConnectorFamily().Origin.Z - level?.Elevation ;
      if ( heightFromPowerToPass != null ) {
        isHeightFromPowerToPassLargerThanPassHeight = heightFromPowerToPass.Value.Height > passTopHeight ;
      }

      if ( heightFromPassToSensors != null ) {
        isPassHeightLargerThanFromPassToSensorsHeight = heightFromPassToSensors.Value.Height < passBottomHeight ;
      }

      return isHeightFromPowerToPassLargerThanPassHeight && isPassHeightLargerThanFromPassToSensorsHeight ;
    }

    private MEPSystemClassificationInfo GetMepSystemClassificationInfo( Element fromPickElement, Element toPickElement )
    {
      if ( ( fromPickElement.GetConnectors().FirstOrDefault() ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType() ;
    }

    public record SelectState( FamilyInstance PowerConnector, FamilyInstance PassConnector, IReadOnlyList<FamilyInstance> SensorConnectors, SelectionRangeRouteManager.SensorArrayDirection SensorDirection, IRangeRouteWithHeightAdjustmentProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, MEPSystemPipeSpec PipeSpec ) ;

    private RangeRangeRouteWithHeightAdjustmentPropertyDialog? ShowPropertyDialog( Document document, Element powerElement, Element passElement, Element sensorElement )
    {
      var fromLevelId = powerElement.LevelId ;
      var toLevelId = sensorElement.LevelId ;

      if ( ( powerElement.GetConnectors().FirstOrDefault() ?? passElement.GetConnectors().FirstOrDefault() ?? sensorElement.GetConnectors().FirstOrDefault() ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    private static RangeRangeRouteWithHeightAdjustmentPropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, toLevelId ) ;
      var sv = new RangeRangeRouteWithHeightAdjustmentPropertyDialog( document, routeChoiceSpec, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType, routeChoiceSpec.StandardTypes?.FirstOrDefault(), initValues.Diameter ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }

    private static RangeRangeRouteWithHeightAdjustmentPropertyDialog ShowDialog( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var sv = new RangeRangeRouteWithHeightAdjustmentPropertyDialog( document, routeChoiceSpec, new RouteProperties( document, routeChoiceSpec, addInType ) ) ;
      sv.ShowDialog() ;

      return sv ;
    }

    private static ( FamilyInstance? PowerConnector, FamilyInstance? PassConnector, IReadOnlyList<FamilyInstance> SensorConnectors, SelectionRangeRouteManager.SensorArrayDirection SensorDirection, string? ErrorMessage ) SelectionRangeRoute( UIDocument iuDocument )
    {
      var selectedElements = iuDocument.Selection.PickElementsByRectangle( ConnectorFamilySelectionFilter.Instance, "ドラックで複数コネクタを選択して下さい。" ).OfType<FamilyInstance>() ;

      FamilyInstance? powerConnector = null, passConnector = null ;
      var sensorConnectors = new List<FamilyInstance>() ;
      foreach ( var element in selectedElements ) {
        if ( element.GetConnectorFamilyType() is not { } connectorFamilyType ) continue ;

        if ( connectorFamilyType == ConnectorFamilyType.Power ) {
          if ( null != powerConnector ) return ( null!, null!, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageTwoOrMorePowerConnector ) ;
          powerConnector = element ;
        }
        else if ( connectorFamilyType == ConnectorFamilyType.Pass ) {
          if ( null != passConnector ) return ( null!, null!, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageTwoOrMorePassConnector ) ;
          passConnector = element ;
        }
        else if ( connectorFamilyType == ConnectorFamilyType.Sensor ) {
          sensorConnectors.Add( element ) ;
        }
      }

      if ( powerConnector == null && passConnector == null && 0 == sensorConnectors.Count ) return ( null, null, Array.Empty<FamilyInstance>(), default, ErrorMessageNoPowerAndPassAndSensorConnector ) ;
      if ( powerConnector == null && passConnector == null ) return ( null, null, Array.Empty<FamilyInstance>(), default, ErrorMessageNoPowerAndPassConnector ) ;
      if ( powerConnector == null && 0 == sensorConnectors.Count ) return ( null, null, Array.Empty<FamilyInstance>(), default, ErrorMessageNoPowerAndSensorConnector ) ;
      if ( passConnector == null && 0 == sensorConnectors.Count ) return ( null, null, Array.Empty<FamilyInstance>(), default, ErrorMessageNoPassAndSensorConnector ) ;
      if ( powerConnector == null ) return ( null, null, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageNoPowerConnector ) ;
      if ( passConnector == null ) return ( null, null, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageNoPassConnector ) ;
      var powerLevel = powerConnector.LevelId ;
      sensorConnectors.RemoveAll( fi => fi.LevelId != powerLevel ) ;
      if ( 0 == sensorConnectors.Count ) return ( null, null, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageNoSensorConnector ) ;
      if ( 1 == sensorConnectors.Count ) return ( null, null, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageSensorConnector ) ;
      var sensorDirection = SortSensorConnectors( passConnector, sensorConnectors ) ;
      if ( SelectionRangeRouteManager.SensorArrayDirection.Invalid == sensorDirection ) return ( null, null, Array.Empty<FamilyInstance>(), SelectionRangeRouteManager.SensorArrayDirection.Invalid, ErrorMessageCannotDetermineSensorConnectorArrayDirection ) ;
      return ( powerConnector, passConnector, sensorConnectors, sensorDirection, null ) ;
    }

    private static SelectionRangeRouteManager.SensorArrayDirection SortSensorConnectors( FamilyInstance passConnector, List<FamilyInstance> sensorConnectors )
    {
      var passConnectorPoint = passConnector.GetBottomConnectorOfConnectorFamily().Origin ;
      double minX = double.MaxValue, minY = double.MaxValue ;
      double maxX = -double.MaxValue, maxY = -double.MaxValue ;
      var sensorToOrigin = new Dictionary<FamilyInstance, XYZ>( sensorConnectors.Count ) ;
      foreach ( var sensor in sensorConnectors ) {
        var origin = sensor.GetBottomConnectorOfConnectorFamily().Origin ;
        sensorToOrigin.Add( sensor, origin ) ;
        var (x, y, _) = origin ;
        if ( x < minX ) minX = x ;
        if ( y < minY ) minY = y ;
        if ( maxX < x ) maxX = x ;
        if ( maxY < y ) maxY = y ;
      }

      var (powerX, powerY, _) = passConnectorPoint ;
      var xRange = GetRange( minX, maxX, powerX ) ;
      var yRange = GetRange( minY, maxY, powerY ) ;
      if ( xRange < 0 && yRange < 0 ) return SelectionRangeRouteManager.SensorArrayDirection.Invalid ;
      SelectionRangeRouteManager.SensorArrayDirection dir ;
      if ( yRange <= xRange ) {
        dir = ( maxX < powerX ) ? SelectionRangeRouteManager.SensorArrayDirection.XMinus : SelectionRangeRouteManager.SensorArrayDirection.XPlus ;
      }
      else {
        dir = ( maxY < powerY ) ? SelectionRangeRouteManager.SensorArrayDirection.YMinus : SelectionRangeRouteManager.SensorArrayDirection.YPlus ;
      }

      sensorConnectors.Sort( ( a, b ) => Compare( sensorToOrigin, a, b, dir ) ) ;
      return dir ;

      static double GetRange( double min, double max, double refPos )
      {
        if ( min <= refPos && refPos <= max ) return -1.0 ; // cannot use
        return max - min ;
      }

      static int Compare( Dictionary<FamilyInstance, XYZ> sensorToOrigin, FamilyInstance a, FamilyInstance b, SelectionRangeRouteManager.SensorArrayDirection dir ) =>
        dir switch
        {
          SelectionRangeRouteManager.SensorArrayDirection.XMinus => sensorToOrigin[ b ].X.CompareTo( sensorToOrigin[ a ].X ),
          SelectionRangeRouteManager.SensorArrayDirection.YMinus => sensorToOrigin[ b ].Y.CompareTo( sensorToOrigin[ a ].Y ),
          SelectionRangeRouteManager.SensorArrayDirection.XPlus => sensorToOrigin[ a ].X.CompareTo( sensorToOrigin[ b ].X ),
          SelectionRangeRouteManager.SensorArrayDirection.YPlus => sensorToOrigin[ a ].Y.CompareTo( sensorToOrigin[ b ].Y ),
          _ => throw new ArgumentOutOfRangeException( nameof( dir ), dir, null )
        } ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.SelectionRangeRouteWithHeightAdjustmentCommand" ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, SelectState selectState )
    {
      var (powerConnector, passConnector, sensorConnectors, sensorDirection, routeProperty, classificationInfo, pipeSpec) = selectState ;
      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;
      var passToSensorsFromFixedHeight = routeProperty.GetFromFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var diameter = routeProperty.GetDiameter() ;
      var radius = diameter * 0.5 ;
      var nameBase = GetNameBase( curveType ) ;
      var nextIndex = GetRouteNameIndex( RouteCache.Get( DocumentKey.Get( document ) ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;
      var (footPassPoint, passPoints) = SelectionRangeRouteManager.CreatePassPoints( routeName, passConnector, sensorConnectors, sensorDirection, routeProperty, pipeSpec, passConnector.GetBottomConnectorOfConnectorFamily().Origin ) ;
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter
      var result = new List<(string RouteName, RouteSegment Segment)>( passPoints.Count * 2 + 2 ) ;

      // main route

      var passConnectorEndPoint = new ConnectorEndPoint( passConnector.GetBottomConnectorOfConnectorFamily(), radius ) ;
      var passConnectorEndPointKey = passConnectorEndPoint.Key ;
      var secondFromEndPoints = EliminateSamePassPoints( footPassPoint, passPoints ).Select( pp => (IEndPoint) new PassPointEndPoint( pp ) ).ToList() ;
      var secondToEndPoints = secondFromEndPoints.Skip( 1 ).Append( new ConnectorEndPoint( IsUpDirection( passConnector, sensorConnectors.Last(), routeProperty ) ? sensorConnectors.Last().GetTopConnectorOfConnectorFamily() : sensorConnectors.Last().GetBottomConnectorOfConnectorFamily(), radius ) ) ;
      var firstToEndPoint = secondFromEndPoints[ 0 ] ;
      result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, passConnectorEndPoint, firstToEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), passToSensorsFromFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;
      result.AddRange( secondFromEndPoints.Zip( secondToEndPoints, ( f, t ) =>
      {
        var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, false, passToSensorsFromFixedHeight, passToSensorsFromFixedHeight, avoidType, null ) ;
        return ( routeName, segment ) ;
      } ) ) ;

      // branch routes
      result.AddRange( passPoints.Zip( sensorConnectors.Take( passPoints.Count ), ( pp, sensor ) =>
      {
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new PassPointBranchEndPoint( document, pp.UniqueId, radius, passConnectorEndPointKey ) ;
        var connectorEndPoint = new ConnectorEndPoint( IsUpDirection( passConnector, sensor, routeProperty ) ? sensor.GetTopConnectorOfConnectorFamily() : sensor.GetBottomConnectorOfConnectorFamily(), radius ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameter, false, passToSensorsFromFixedHeight, passToSensorsFromFixedHeight, avoidType, null ) ;
        return ( subRouteName, segment ) ;
      } ) ) ;

      //power to pass route
      routeName = nameBase + "_" + ( ++nextIndex ) ;
      var powerConnectorEndPoint = new ConnectorEndPoint( powerConnector.GetBottomConnectorOfConnectorFamily(), radius ) ;
      var passConnectorUpEndPoint = new ConnectorEndPoint( passConnector.GetTopConnectorOfConnectorFamily(), radius ) ;
      result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, powerConnectorEndPoint, passConnectorUpEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetPowerToPassFromFixedHeight(), passToSensorsFromFixedHeight, avoidType, routeProperty.GetShaft()?.UniqueId ) ) ) ;

      // change color connectors
      var allConnectors = new List<FamilyInstance> { powerConnector } ;
      allConnectors.AddRange( sensorConnectors ) ;
      ConfirmUnsetCommandBase.ResetElementColor( allConnectors ) ;
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

    private bool IsUpDirection( FamilyInstance passConnector, FamilyInstance sensorConnector, IRouteProperty property )
    {
      var firstConnectThroughHeightFromPassToSensors = property.GetFromFixedHeight() ;
      Level? level = passConnector.Document.GetElement( passConnector.LevelId ) as Level ;
      var passBottomHeight = passConnector.GetBottomConnectorOfConnectorFamily().Origin.Z - level?.Elevation ;
      var connectorHeight = sensorConnector.GetTopConnectorOfConnectorFamily().Origin.Z - level?.Elevation ;
      if ( connectorHeight < firstConnectThroughHeightFromPassToSensors!.Value.Height && passBottomHeight > firstConnectThroughHeightFromPassToSensors.Value.Height ) {
        return true ;
      }

      if ( connectorHeight > firstConnectThroughHeightFromPassToSensors.Value.Height ) {
        return false ;
      }

      return true ;
    }

    private AddInType GetAddInType()
    {
      return AppCommandSettings.AddInType ;
    }

    protected DialogInitValues CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }

    private MEPSystemClassificationInfo GetMEPSystemClassificationInfoFromSystemType()
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }

    private string GetNameBase( MEPCurveType curveType )
    {
      return curveType.Category.Name ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, SelectState selectState )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
    }
  }
}