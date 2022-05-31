using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MathLib ;
using Line = Autodesk.Revit.DB.Line ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PressureGuidingTubeCommandBase : RoutingCommandBase<PressureGuidingTubeCommandBase.PressureGuidingTubePickState>
  {
    private double _height ;
    private string _annotationContent = string.Empty ; 
    private static readonly double MaxDistanceTolerance = ( 300.0 ).MillimetersToRevitUnits() ;
    private const string ErrorMessageIsNotValidConnector = "Selected connector isn't valid." ;

    public record PressureGuidingTubePickState( ConnectorPicker.IPickResult FromPickResult, ConnectorPicker.IPickResult ToPickResult, List<XYZ> ListSelectedPoint, RouteProperties RouteProperties, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<PressureGuidingTubePickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      UIApplication uiApp = commandData.Application;
      UIDocument uiDocument = commandData.Application.ActiveUIDocument ;
      Document document = uiDocument.Document ;

      var pressureGuidingTubeStorable = document.GetPressureGuidingTubeStorable() ;
      var pressureSettingViewModel = new PressureGuidingTubeSettingViewModel( pressureGuidingTubeStorable.PressureGuidingTubeModelData ) ;
      var dialog = new PressureGuidingTubeSettingDialog( pressureSettingViewModel ) ;

      var result = dialog.ShowDialog() ;
      if ( true != result ) return OperationResult<PressureGuidingTubePickState>.Cancelled ;
      
      try {
        //Save pressure guiding tube setting
        using Transaction t = new Transaction( document, "Create pressure guiding tubes" ) ;
        t.Start() ;
        pressureGuidingTubeStorable.PressureGuidingTubeModelData = pressureSettingViewModel.PressureGuidingTube ;
        pressureGuidingTubeStorable.Save() ;
        t.Commit() ;

        //Generate segments
        var routingExecutor = GetRoutingExecutor() ;
        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
        var fromConnector = fromPickResult.PickedElement ;
        if ( fromConnector is not FamilyInstance || false == fromConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) || string.IsNullOrEmpty( ceedCode ) ) {
          MessageBox.Show( ErrorMessageIsNotValidConnector, "Message" ) ;
          return OperationResult<PressureGuidingTubePickState>.Cancelled ;
        }

          
        var level = document.GetElementById<Level>( fromPickResult.GetLevelId() ) ;
        _height = fromPickResult.GetOrigin().Z - level!.Elevation + pressureSettingViewModel.PressureGuidingTube.Height.MillimetersToRevitUnits() ;
        _annotationContent = pressureSettingViewModel.SelectedTubeType.GetFieldName() ;
        var selectedPointList = new List<XYZ>() ;
        var numberOfPoint = 0 ;

        using ( uiDocument.SetTempColor( fromPickResult ) ) {
          LineExternal lineExternal = new( uiApp ) ;
          XYZ prevPoint = fromPickResult.GetOrigin() ;
          lineExternal.PickedPoints.Add( prevPoint ) ;
          var tmp = prevPoint ;
          //Automatic: Select one point only
          if ( pressureSettingViewModel.SelectedCreationMode == CreationModeEnum.自動モード ) {
            lineExternal.DrawingServer.BasePoint = tmp ;
            lineExternal.DrawExternal() ;
            var xyz = uiDocument.Selection.PickPoint( "Click the end point" ) ;
              
            selectedPointList.Add( new XYZ( xyz.X, xyz.Y, _height ) ) ;
            lineExternal.Dispose();
          }
          //Manual: Select many point
          else {
            try {
              while ( true ) {
                numberOfPoint++ ;
                lineExternal.DrawingServer.BasePoint = tmp ;
                lineExternal.DrawExternal() ;
                var xyz = uiDocument.Selection.PickPoint( "Click the end point number " + numberOfPoint + ". Press Esc to end command." ) ;
                selectedPointList.Add( new XYZ( xyz.X, xyz.Y, _height ) ) ;
                tmp = xyz ;
              }
            }
            catch ( OperationCanceledException ) {
              //end select point 
            }
            finally {
              lineExternal.Dispose();
            }
          }
        }

        numberOfPoint = selectedPointList.Count ;
        var endElement = GeneratePressureConnector( document, level, selectedPointList[ numberOfPoint - 1 ] ) ;
        var toPickResult = ConnectorPicker.CreatePressureConnector( endElement, numberOfPoint > 1 ? selectedPointList[ numberOfPoint - 2 ] : fromPickResult.GetOrigin(), null ) ;
        var properties = InitRoutProperties( document, level.Id ) ;
        var classificationInfo = MEPSystemClassificationInfo.Undefined ;
        if ( ( fromPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo )
          classificationInfo = connectorClassificationInfo ;
 
        return new OperationResult<PressureGuidingTubePickState>( new PressureGuidingTubePickState( fromPickResult, toPickResult, selectedPointList, properties, classificationInfo ) ) ;
      }
      catch ( OperationCanceledException ) {
        return OperationResult<PressureGuidingTubePickState>.Cancelled ;
      }
      catch {
        MessageBox.Show( "Generate pressure guiding tube failed.", "Error Message" ) ;
        return OperationResult<PressureGuidingTubePickState>.Failed ;
      }
    }
 
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PressureGuidingTubePickState state )
    {
      var (fromPickResult, toPickResult, selectedPointList, routeProperty, classificationInfo) = state ;
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;
      var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      var toConnectorId = toPickResult.PickedElement.UniqueId ;
      return CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, selectedPointList, routeProperty, classificationInfo, fromConnectorId, toConnectorId) ; 
    }
    
    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, List<XYZ> toPoints, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo, string fromConnectorId, string toConnectorId  )
    {
      var systemType = routeProperty.SystemType ;
      var curveType = routeProperty.CurveType ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = routeProperty.Diameter ?? ( 16.0 ).MillimetersToRevitUnits() ;
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
        var passPointEndPoints = InsertPassPointElement( document, fromEndPoint, toEndPointsWithoutLast, name, level, diameter / 10, fromConnectorId, toConnectorId, height ) ;
        //var passPointEndPoints = GetPassPoint( document, fromEndPoint, toEndPointsWithoutLast, level, diameter / 2, height ) ;
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
        //passPoints.Add( new PassPointEndPoint( document, string.Empty, toPosition, direction.ToXYZRaw(), radius, level )) ;
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
 
    private FamilyInstance GeneratePressureConnector( Document document, Level level, XYZ xyz, bool isTemp = false )
    {
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.PressureConnector ).FirstOrDefault() ?? throw new Exception() ;
      using Transaction t = new Transaction( document, "Create end point mark trans" ) ;
      t.Start() ;
      var result = symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
      t.Commit() ;
      return result ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, PressureGuidingTubePickState result )
    {
      using Transaction t = new Transaction( document, "Change conduit color" ) ;
      t.Start() ;
       
      //Change conduit color to yellow RGB(255,255,0)
      Element? conduitUseToAddAnnotation = null ;
      OverrideGraphicSettings ogs = new () ;
      ogs.SetProjectionLineColor( new Color( 255, 255, 0 ) ) ;
      foreach ( var route in executeResultValue ) { 
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        conduitUseToAddAnnotation = conduits.Count > 0 ? conduits.Count > 1 ? conduits[ 1 ] : conduits[ 0 ] : null ;
        foreach ( var conduit in conduits ) {
          document.ActiveView.SetElementOverrides( conduit.Id, ogs ) ; 
          //conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem! ) ;
          //conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
        }
        
        //Delete passpoint symbol
        var passpoints = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PassPoints ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        foreach ( var passpoint in passpoints ) {
          document.Delete( passpoint.Id ) ; 
        }
      }
    
      //Create annotation 
      if ( conduitUseToAddAnnotation != null ) {
        var data = document.GetSetupPrintStorable() ;
        var defaultSymbolMagnification = data.Scale * data.Ratio ;
        CreateNotation( document, conduitUseToAddAnnotation, _annotationContent, true, defaultSymbolMagnification ) ;
      }
    
      t.Commit() ;
    }

     
    private RouteProperties InitRoutProperties( Document document, ElementId elementId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, GetAddInType(), elementId, elementId ) ;
      var routeProperty = new RouteProperties( document, routeChoiceSpec ) ;

      return routeProperty ;
    }

    private static void CreateNotation( Document doc, Element element, string notation, bool isDirectionX, double scale )
    {
      if ( doc.ActiveView is not ViewPlan viewPlan ) return ;
      
      var curve = ( element.Location as LocationCurve )!.Curve ;
      var point = new XYZ( 0.5 * ( curve.GetEndPoint( 0 ).X + curve.GetEndPoint( 1 ).X ), 0.5 * ( curve.GetEndPoint( 0 ).Y + curve.GetEndPoint( 1 ).Y ), curve.GetEndPoint( 0 ).Z ) ;
      var textNoteType = TextNoteHelper.FindOrCreateTextNoteType( doc ) ;
      if ( null == textNoteType ) return ;
       
      const double multiple = 3 ;
      var heightText = TextNoteHelper.TotalHeight.MillimetersToRevitUnits() ;
      var vector = ( XYZ.BasisX * heightText * multiple + XYZ.BasisY * heightText * multiple + XYZ.BasisY * heightText ) * scale ;
      var transform = Transform.CreateTranslation( vector ) ;
      var textNote = TextNote.Create( doc, doc.ActiveView.Id, transform.OfPoint( point ), notation, textNoteType.Id ) ;

      doc.Regenerate() ;

      var underLineTextNote = NewRackCommandBase.CreateUnderLineText( textNote, viewPlan.GenLevel.Elevation ) ;
      var nearestPoint = underLineTextNote.GetEndPoint( 0 ).DistanceTo( point ) > underLineTextNote.GetEndPoint( 1 ).DistanceTo( point ) ? underLineTextNote.GetEndPoint( 1 ) : underLineTextNote.GetEndPoint( 0 ) ;
      var curves = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { Line.CreateBound( nearestPoint, new XYZ( point.X, point.Y, viewPlan.GenLevel.Elevation ) ) }, new List<Type> { typeof( CableTray ) } ) ;
      curves.Add( underLineTextNote ) ;
      
      var detailCurves = NotationHelper.CreateDetailCurve( doc.ActiveView, curves ) ;
      var curveClosestPoint = GeometryHelper.GetCurveClosestPoint( detailCurves, point ) ;
      
      (string? endLineUniqueId, int? endPoint) endLineLeader = ( curveClosestPoint.DetailCurve?.UniqueId, endPoint: curveClosestPoint.EndPoint ) ;
      var otherLineId = detailCurves.Select( x => x.UniqueId ).Where( x => x != endLineLeader.endLineUniqueId ).ToList() ;
      
      var rackNotationStorable = doc.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? doc.GetRackNotationStorable() ;
      
      var rackNotationModel = new RackNotationModel( element.UniqueId, textNote.UniqueId, element.UniqueId, string.Empty, isDirectionX, null, endLineLeader.endLineUniqueId, endLineLeader.endPoint, otherLineId ) ;
      rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
      rackNotationStorable.Save() ;
    }
  }
}