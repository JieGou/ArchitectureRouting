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
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class LeakRoutingCommandBase : RoutingCommandBase<LeakRoutingCommandBase.LeakState>
  {
    private static readonly double MaxDistanceTolerance = ( 500.0 ).MillimetersToRevitUnits() ;
    private const string JBoxConnectorType = "JBOX" ;
    private const string ErrorMessageIsNotJBoxConnector = "Selected connector isn't JBOX connector." ;
    private const string ErrorMessageOneJBoxConnector = "One JBOX connector are selected." ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;

    public record LeakState( ConnectorPicker.IPickResult fromConnectorResult, ConnectorPicker.IPickResult toConnectorResult, List<XYZ> PickPoints, double height, int routeMode, RouteProperties RouteProperty, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;

    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<LeakState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      UIDocument uiDocument = commandData.Application.ActiveUIDocument ;
      Document document = uiDocument.Document ;
      try {
        var routingExecutor = GetRoutingExecutor() ;

        var sv = new LeakRouteDialog() ;
        sv.ShowDialog() ;
        if ( true != sv.DialogResult ) return OperationResult<LeakState>.Cancelled ;

        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
        var fromConnector = fromPickResult.PickedElement ;
        if ( fromConnector is not FamilyInstance || false == fromConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) 
                                                 || string.IsNullOrEmpty( ceedCode ) || ! ceedCode!.Contains( JBoxConnectorType ) ) {
          MessageBox.Show( ErrorMessageIsNotJBoxConnector, "Message" ) ;
          return OperationResult<LeakState>.Cancelled ;
        }

        XYZ fromPoint = fromPickResult.GetOrigin() ;
        var pickPoints = new List<XYZ>() ;
        if ( sv.CreateMode == 0 ) {
          while ( true ) {
            try {
              XYZ point = uiDocument.Selection.PickPoint( "Pick points then press escape to cause an exception ahem...exit selection" ) ;
              pickPoints.Add( point ) ;
            }
            catch {
              break ; // TODO: might not be correct. Was : Exit While
            }
          }
        }
        else {
          // var selectedElement = uiDocument.Selection.PickElementsByRectangle( ConnectorFamilySelectionFilter.Instance, "Select JBOX connector" ).Where( p => p is FamilyInstance ).ToList() ;
          // var jBoxConnectors = selectedElement.Where( e => e.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) && ! string.IsNullOrEmpty( ceedCode ) && ceedCode!.Contains( JBoxConnectorType ) ).ToList() ;
          // if ( jBoxConnectors.Count == 1 ) {
          //   var fromFamilyInstance = ( jBoxConnectors.First() as FamilyInstance ) ! ;
          //   fromPickResult = ConnectorPicker.GetConnectorPickResult( document, GetAddInType(), fromFamilyInstance ) ;
          // }
          // else {
          //   MessageBox.Show( ErrorMessageOneJBoxConnector, "Message" ) ;
          //   return OperationResult<LeakState>.Cancelled ;
          // }
          var fromConnectorWidth = fromConnector.ParametersMap.get_Item( "W" ).AsDouble() * 1.5 ;
          XYZ secondPoint = uiDocument.Selection.PickPoint( "Pick points then press escape to cause an exception ahem...exit selection" ) ;
          var mpt = ( fromPoint + secondPoint ) * 0.5 ;
          var currView = document.ActiveView ;
          var plane = Plane.CreateByNormalAndOrigin( currView.RightDirection, mpt ) ;
          var mirrorMat = Transform.CreateReflection( plane ) ;
          var firstPoint = mirrorMat.OfPoint( fromPoint ) ;
          var thirdPoint = mirrorMat.OfPoint( secondPoint ) ;
          var lastPoint = fromPoint.Y > secondPoint.Y ? new XYZ( fromPoint.X, fromPoint.Y - fromConnectorWidth, fromPoint.Z ) : new XYZ( fromPoint.X, fromPoint.Y + fromConnectorWidth, fromPoint.Z ) ;
          pickPoints = new List<XYZ>() { firstPoint, secondPoint, thirdPoint, lastPoint } ;
        }

        var count = pickPoints.Count() ;
        var level = document.GetElementById<Level>( fromPickResult.GetLevelId() ) ;
        var height = fromPoint.Z - level!.Elevation + sv.RouteHeight.MillimetersToRevitUnits() ;

        var prevYPoint = count > 1 ? pickPoints[ count - 2 ].Y : fromPoint.Y ;
        var toPick = CreateLeakEndPoint( document, level!, new XYZ( pickPoints[ count - 1 ].X, pickPoints[ count - 1 ].Y, height ), prevYPoint ) ;

        var toPickResult = ConnectorPicker.GetConnectorPickResult( document, GetAddInType(), toPick ) ;

        var property = CreateRouteProperties( document, fromPickResult, toPickResult, height ) ;

        if ( property == null ) return OperationResult<LeakState>.Cancelled ;

        if ( GetMEPSystemClassificationInfo( fromPickResult, property.SystemType ) is not { } classificationInfo ) return OperationResult<LeakState>.Cancelled ;

        return new OperationResult<LeakState>( new LeakState( fromPickResult, toPickResult, pickPoints, height, sv.CreateMode, property, classificationInfo ) ) ;
      }
      catch {
        return OperationResult<LeakState>.Cancelled ;
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
      var (fromPickResult, toPickResult, toPickPoints, height, routeMode, routeProperty, classificationInfo) = leakStates ;
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;
      var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      var toConnectorId = toPickResult.PickedElement.UniqueId ;

      var routeSegment = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, toPickPoints, routeMode, routeProperty, classificationInfo, fromConnectorId, toConnectorId ) ;

      return routeSegment ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, List<XYZ> toPoints, int routeMode, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo, string fromConnectorId, string toConnectorId )
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
        routeFromEndPoints.AddRange( routeToEndPoints ) ;
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
          direction = fromPoint.Y < toPoint.Y ? new Vector3d( 0, 1, 0 ) : new Vector3d( 0, -1, 0 ) ;
          toPosition = fromPoint.Y < toPoint.Y ? new XYZ( toPoint.X, toPoint.Y - MaxDistanceTolerance, height ) : new XYZ( toPoint.X, toPoint.Y + MaxDistanceTolerance, height ) ;
        }
        else if ( distanceY == 0 ) {
          direction = fromPoint.X < toPoint.X ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          toPosition = fromPoint.X < toPoint.X ? new XYZ( toPoint.X - MaxDistanceTolerance, toPoint.Y, height ) : new XYZ( toPoint.X + MaxDistanceTolerance, toPoint.Y, height ) ;
        }
        else {
          if ( distanceX > distanceY ) {
            direction = fromPoint.X < toPoint.X ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
            toPosition = fromPoint.X < toPoint.X ? new XYZ( toPoint.X - MaxDistanceTolerance, toPoint.Y, height ) : new XYZ( toPoint.X + MaxDistanceTolerance, toPoint.Y, height ) ;
          }
          else {
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

    private static List<PassPointEndPoint> InsertPassPointOfRectangleMode( Document document, IEndPoint fromEndPoint, List<XYZ> toPoints, string routeName, Level level, double radius, string fromConnectorId, string toConnectorId, double height )
    {
      var passPoints = new List<PassPointEndPoint>() ;
      XYZ fromPoint = fromEndPoint.RoutingStartPosition ;
      foreach ( var toPoint in toPoints ) {
        XYZ toPosition ;
        Vector3d direction ;
        var distanceX = Math.Abs( fromPoint.X - toPoint.X ) ;
        if ( distanceX == 0 ) {
          direction = fromPoint.Y < toPoint.Y ? new Vector3d( 0, 1, 0 ) : new Vector3d( 0, -1, 0 ) ;
          toPosition = fromPoint.Y < toPoint.Y ? new XYZ( toPoint.X, toPoint.Y - MaxDistanceTolerance, height ) : new XYZ( toPoint.X, toPoint.Y + MaxDistanceTolerance, height ) ;
        }
        else {
          direction = fromPoint.X < toPoint.X ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          toPosition = fromPoint.X < toPoint.X ? new XYZ( toPoint.X - MaxDistanceTolerance, toPoint.Y, height ) : new XYZ( toPoint.X + MaxDistanceTolerance, toPoint.Y, height ) ;
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

        return CreateRouteProperties( document, initValues, fromLevelId, toLevelId, height ) ;
      }

      return CreateRouteProperties( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    protected RouteProperties CreateRouteProperties( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId, double height )
    {
      var routeProperty = new RouteProperties( document, initValues.SystemType, initValues.CurveType, initValues.Diameter, true, true, FixedHeight.CreateOrNull( FixedHeightType.Ceiling, height ), null, null, AvoidType.Whichever, null ) ;

      return routeProperty ;
    }

    private static RouteProperties CreateRouteProperties( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var routeProperty = new RouteProperties( document, routeChoiceSpec ) ;

      return routeProperty ;
    }

    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }

    private FamilyInstance CreateLeakEndPoint( Document document, Level level, XYZ position, double prevYPoint )
    {
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.ToJboxConnector ).FirstOrDefault() ?? throw new Exception() ;
      using Transaction t = new Transaction( document, "Create to JBOX connector" ) ;
      t.Start() ;
      var familyInstance = symbol.Instantiate( position, level, StructuralType.NonStructural ) ;
      // var locationPoint = ( familyInstance.Location as LocationPoint )!.Point ;
      // var angleRotate = prevYPoint > locationPoint.Y ? Math.PI / 2 : -Math.PI / 2 ;
      // ElementTransformUtils.RotateElement( document, familyInstance.Id, Line.CreateBound( locationPoint, new XYZ( locationPoint.X, locationPoint.Y, locationPoint.Z + 1 ) ), angleRotate ) ;
      t.Commit() ;
      return familyInstance ;
    }
  }
}