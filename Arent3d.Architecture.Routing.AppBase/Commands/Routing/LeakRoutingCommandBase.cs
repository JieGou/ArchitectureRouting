using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MathLib ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class LeakRoutingCommandBase : RoutingCommandBase<LeakRoutingCommandBase.LeakState>
  {
    private static readonly double MaxDistanceTolerance = ( 300.0 ).MillimetersToRevitUnits() ;
    private static readonly double DefaultWidthJBoxConnector = ( 200.0 ).MillimetersToRevitUnits() ;
    private const double MinDistancePoints = 0.1 ;
    private const string JBoxConnectorType = "JB" ;
    private const string ErrorMessageIsNotJBoxConnector = "Selected connector isn't JBOX connector." ;

    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;

    public record LeakState( ConnectorPicker.IPickResult fromConnectorResult, ConnectorPicker.IPickResult toConnectorResult, List<XYZ> PickPoints, int ConduitType, RouteProperties RouteProperty, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<LeakState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      UIApplication uiApp = commandData.Application;
      UIDocument uiDocument = commandData.Application.ActiveUIDocument ;
      Document document = uiDocument.Document ;
      try {
        var routingExecutor = GetRoutingExecutor() ;

        var sv = new LeakRouteDialog() ;
        sv.ShowDialog() ;
        if ( true != sv.DialogResult ) return OperationResult<LeakState>.Cancelled ;

        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType(), true ) ;
        var fromConnector = fromPickResult.PickedElement ;
        if ( fromConnector is not FamilyInstance || false == fromConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) 
                                                 || string.IsNullOrEmpty( ceedCode ) || ! ceedCode!.Contains( JBoxConnectorType ) ) {
          MessageBox.Show( ErrorMessageIsNotJBoxConnector, "Message" ) ;
          return OperationResult<LeakState>.Cancelled ;
        }

        XYZ fromPoint = fromPickResult.GetOrigin() ;
        var pickPoints = new List<XYZ>() ;
        
        if ( sv.CreateMode == 0 ) {
          LineExternal lineExternal = new( uiApp ) ;
          XYZ prevPoint = fromPickResult.GetOrigin() ;
          try {
            lineExternal.PickedPoints.Add( prevPoint ) ;
            while ( true ) {
              lineExternal.DrawingServer.BasePoint = prevPoint ;
              lineExternal.DrawExternal() ;
              var nextPoint = uiDocument.Selection.PickPoint( "Pick next point" ) ;
              if ( prevPoint.DistanceTo( nextPoint ) > MinDistancePoints )
                pickPoints.Add( nextPoint ) ;
              else {
                // when prevPoint = nextPoint, to exits from the while loop
                break ;
              }

              prevPoint = nextPoint ;
            }
          }
          catch ( OperationCanceledException ) {
            // when the user hits ESC, to exits from the while loop
          }
          finally {
            lineExternal.Dispose() ;
          }
        }
        else {
          var isHasParameterWidth = fromConnector.HasParameter( "W" ) ;
          var fromConnectorWidth = ( isHasParameterWidth ? fromConnector.ParametersMap.get_Item( "W" ).AsDouble() : DefaultWidthJBoxConnector ) * 1.5 ;
          if ( !DrawingPreviewRectangleWhenLeakingInRectangleMode( uiApp, fromPoint, out var secondPoint ) ) return OperationResult<LeakState>.Cancelled ;
          var mpt = ( fromPoint + secondPoint ) * 0.5 ;
          var currView = document.ActiveView ;
          var plane = Plane.CreateByNormalAndOrigin( currView.RightDirection, mpt ) ;
          var mirrorMat = Transform.CreateReflection( plane ) ;
          var firstPoint = mirrorMat.OfPoint( fromPoint ) ;
          var thirdPoint = mirrorMat.OfPoint( secondPoint ) ;
          var lastPoint = fromPoint.Y > secondPoint?.Y ? new XYZ( fromPoint.X, fromPoint.Y - fromConnectorWidth, fromPoint.Z ) : new XYZ( fromPoint.X, fromPoint.Y + fromConnectorWidth, fromPoint.Z ) ;
          pickPoints = new List<XYZ>() { firstPoint, secondPoint!, thirdPoint, lastPoint } ;
        }

        var count = pickPoints.Count() ;
        if ( count < 1 ) return OperationResult<LeakState>.Cancelled ;
        var level = document.GetElementById<Level>( fromPickResult.GetLevelId() ) ;
        var height = fromPoint.Z - level!.Elevation + sv.RouteHeight.MillimetersToRevitUnits() ;

        var prevToPoint = count > 1 ? pickPoints[ count - 2 ] : fromPoint ;
        var toPick = CreateLeakEndPoint( document, level!, new XYZ( pickPoints[ count - 1 ].X, pickPoints[ count - 1 ].Y, height ) ) ;

        var toPickResult = ConnectorPicker.GetConnectorPickResult( toPick, prevToPoint ) ;

        var property = CreateRouteProperties( document, fromPickResult, toPickResult, height ) ;

        if ( property == null ) return OperationResult<LeakState>.Cancelled ;

        if ( GetMEPSystemClassificationInfo( fromPickResult, property.SystemType ) is not { } classificationInfo ) return OperationResult<LeakState>.Cancelled ;

        return new OperationResult<LeakState>( new LeakState( fromPickResult, toPickResult, pickPoints, sv.ConduitType, property, classificationInfo ) ) ;
      }
      catch {
        return OperationResult<LeakState>.Cancelled ;
      }
    }

    private static bool DrawingPreviewRectangleWhenLeakingInRectangleMode(UIApplication uiApp, XYZ firstPoint,out XYZ? secondPoint)
    {
      var uiDocument = uiApp.ActiveUIDocument ;
      var selection = uiDocument.Selection ;
      const double minDistance = 0.01 ;

      // This is the object to render the guide line
      var rectangleExternal = new RectangleExternal( uiApp ) ;
      try {
        // Add first point to list picked points
        rectangleExternal.PickedPoints.Add( firstPoint ) ;
        // Assign first point
        rectangleExternal.DrawingServer.BasePoint = firstPoint ;
        // Render the guide line
        rectangleExternal.DrawExternal() ;
        // Pick next point 
        var lastPoint = selection.PickPoint( "Pick point" ) ;
        if ( firstPoint.DistanceTo( lastPoint ) < minDistance ) {
          secondPoint = null ;
          return false ;
        }
        else {
          secondPoint = lastPoint ;
          return true ;
        }
      }
      catch ( OperationCanceledException ) {
        secondPoint = null ;
        return false;
      }
      finally {
        // End to render guide line
        rectangleExternal.Dispose() ;
      }
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( ConnectorPicker.IPickResult fromPickResult, MEPSystemType? systemType )
    {
      if ( fromPickResult.PickedConnector is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, LeakState leakStates )
    {
      return CreateNewSegmentList( document, leakStates ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, LeakState leakStates )
    {
      var (fromPickResult, toPickResult, toPickPoints, conduitType, routeProperty, classificationInfo) = leakStates ;
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;
      var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      var toConnectorId = toPickResult.PickedElement.UniqueId ;

      var routeSegment = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, toPickPoints, routeProperty, classificationInfo, fromConnectorId, toConnectorId ) ;

      return routeSegment ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, List<XYZ> toPoints, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo, string fromConnectorId, string toConnectorId )
    {
      var systemType = routeProperty.SystemType ;
      var curveType = routeProperty.CurveType ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = routeProperty.Diameter ?? 16 ;
      var isRoutingOnPipeSpace = routeProperty.IsRouteOnPipeSpace ?? false ;
      var fromFixedHeight = routeProperty.FromFixedHeight ;
      var toFixedHeight = routeProperty.ToFixedHeight ;
      var avoidType = routeProperty.AvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = routeProperty.Shaft?.UniqueId ;
      var level = document.ActiveView.GenLevel ;

      var routeFromEndPoints = new List<IEndPoint>() { fromEndPoint } ;
      var routeToEndPoints = new List<IEndPoint>() ;

      if ( toPoints.Count > 1 ) {
        var height = toEndPoint.RoutingStartPosition.Z ;
        var toEndPointsWithoutLast = toPoints.Take( toPoints.Count() - 1 ).ToList() ;
        var passPointEndPoints = InsertPassPointElement( document, fromEndPoint, toEndPointsWithoutLast, name, level, diameter / 2, fromConnectorId, toConnectorId, height ) ;
        routeToEndPoints.AddRange( passPointEndPoints ) ;
        routeFromEndPoints.AddRange( passPointEndPoints ) ;
      }

      routeToEndPoints.Add( toEndPoint ) ;
      List<(string RouteName, RouteSegment Segment)> result = new() ;
      result.AddRange( routeFromEndPoints.Zip( routeToEndPoints, ( f, t ) =>
      {
        var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
        return ( name, segment ) ;
      } ) ) ;

      return result ;
    }

    private static List<PassPointEndPoint> InsertPassPointElement( Document document, IEndPoint fromEndPoint, List<XYZ> toPoints, string routeName, Level level, double radius, string fromConnectorId, string toConnectorId, double height )
    {
      var passPoints = new List<PassPointEndPoint>() ;
      XYZ fromPoint = fromEndPoint.RoutingStartPosition ;
      foreach ( var toPoint in toPoints ) {
        XYZ toPosition ;
        Vector3d direction ;
        var distanceX = Math.Abs( fromPoint.X - toPoint.X ) ;
        var distanceY = Math.Abs( fromPoint.Y - toPoint.Y ) ;
        if ( distanceX == 0 ) {
          if ( distanceY <= MaxDistanceTolerance ) continue ;
          direction = fromPoint.Y < toPoint.Y ? new Vector3d( 0, 1, 0 ) : new Vector3d( 0, -1, 0 ) ;
          toPosition = fromPoint.Y < toPoint.Y ? new XYZ( toPoint.X, toPoint.Y - MaxDistanceTolerance, height ) : new XYZ( toPoint.X, toPoint.Y + MaxDistanceTolerance, height ) ;
        }
        else if ( distanceY == 0 ) {
          if ( distanceX <= MaxDistanceTolerance ) continue ;
          direction = fromPoint.X < toPoint.X ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          toPosition = fromPoint.X < toPoint.X ? new XYZ( toPoint.X - MaxDistanceTolerance, toPoint.Y, height ) : new XYZ( toPoint.X + MaxDistanceTolerance, toPoint.Y, height ) ;
        }
        else {
          if ( distanceX > distanceY ) {
            if ( distanceX <= MaxDistanceTolerance ) continue ;
            direction = fromPoint.X < toPoint.X ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
            toPosition = fromPoint.X < toPoint.X ? new XYZ( toPoint.X - MaxDistanceTolerance, toPoint.Y, height ) : new XYZ( toPoint.X + MaxDistanceTolerance, toPoint.Y, height ) ;
          }
          else {
            if ( distanceY <= MaxDistanceTolerance ) continue ;
            direction = fromPoint.Y < toPoint.Y ? new Vector3d( 0, 1, 0 ) : new Vector3d( 0, -1, 0 ) ;
            toPosition = fromPoint.Y < toPoint.Y ? new XYZ( toPoint.X, toPoint.Y - MaxDistanceTolerance, height ) : new XYZ( toPoint.X, toPoint.Y + MaxDistanceTolerance, height ) ;
          }
        }

        var passPoint = document.AddPassPoint( routeName, toPosition, direction.normalized.ToXYZRaw(), radius, level.Id ) ;
        passPoint.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromConnectorId ) ;
        passPoint.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toConnectorId ) ;
        passPoints.Add( new PassPointEndPoint( passPoint ) ) ;
        fromPoint = toPoint ;
      }

      return passPoints ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    private RouteProperties? CreateRouteProperties( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, double height )
    {
      var fromLevelId = GetTrueLevelId( document, fromPickResult ) ;
      var toLevelId = GetTrueLevelId( document, toPickResult ) ;

      if ( ( fromPickResult.PickedConnector ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return CreateRouteProperties( document, initValues, height ) ;
      }

      return CreateRouteProperties( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    protected RouteProperties CreateRouteProperties( Document document, DialogInitValues initValues, double height )
    {
      var routeProperty = new RouteProperties( document, initValues.SystemType, initValues.CurveType, initValues.Diameter, false, true, FixedHeight.CreateOrNull( FixedHeightType.Ceiling, height ), null, null, AvoidType.Whichever, null ) ;

      return routeProperty ;
    }

    private static RouteProperties CreateRouteProperties( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var routeProperty = new RouteProperties( document, routeChoiceSpec, addInType ) ;

      return routeProperty ;
    }

    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }

    private FamilyInstance CreateLeakEndPoint( Document document, Level level, XYZ position )
    {
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.ToJboxConnector ).FirstOrDefault() ?? throw new Exception() ;
      using Transaction t = new( document, "Create to JBOX connector" ) ;
      t.Start() ;
      var familyInstance = symbol.Instantiate( position, level, StructuralType.NonStructural ) ;
      t.Commit() ;
      return familyInstance ;
    }
    
    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, LeakState leakState )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
      var wireTypeName = ChangeWireTypeCommand.WireSymbolOptions.Values.ElementAt( leakState.ConduitType ) ;
      if ( string.IsNullOrEmpty( wireTypeName ) ) return ;
      var routeNames = executeResultValue.Select( r => r.RouteName ).Distinct().ToHashSet() ;
      ChangeWireTypeCommand.ChangeWireType( document, routeNames, wireTypeName, true ) ;
    }
  }
}