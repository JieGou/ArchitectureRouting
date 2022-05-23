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
using Line = Autodesk.Revit.DB.Line ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PressureGuidingTubeCommandBase : RoutingCommandBase<PressureGuidingTubeCommandBase.PressureGuidingTubePickState>
  {
    private double _height ;
    private string _annotationContent = string.Empty ; 

    //public record PressureGuidingTubePickState( ConnectorPicker.IPickResult FromPickResult, ConnectorPicker.IPickResult ToPickResult, List<Element> ListSelectedPoint, RouteProperties RouteProperties, MEPSystemClassificationInfo ClassificationInfo ) ;
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
      if ( true == result ) {
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

          // var selectedElementList = new List<Element>() ;
          // for ( var i = 0 ; i < selectedPointList.Count - 1 ; i++ ) {
          //   selectedElementList.Add( GeneratePressureConnector( document, level, selectedPointList[ i ], true ) );
          // }
          
          return new OperationResult<PressureGuidingTubePickState>( new PressureGuidingTubePickState( fromPickResult, toPickResult, selectedPointList, properties, classificationInfo ) ) ;
        }
        catch {
          MessageBox.Show( "Generate pressure guiding tube failed.", "Error Message" ) ;
          return OperationResult<PressureGuidingTubePickState>.Failed ;
        }
      }

      return OperationResult<PressureGuidingTubePickState>.Cancelled ;
    }

    /// <summary>
    /// Create route segments
    /// </summary>
    /// <param name="document"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PressureGuidingTubePickState state )
    {
      var (fromPickResult, toPickResult, selectedPointList, routeProperty, classificationInfo) = state ;
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;
      //var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      //var toConnectorId = toPickResult.PickedElement.UniqueId ;
      return CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, selectedPointList, routeProperty, classificationInfo) ;
      //return CreateSegmentOfNewRouteUseTempConnector( document, fromPickResult, toPickResult, selectedPointList, routeProperty, classificationInfo) ;
    }
    
    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, List<XYZ> toPoints, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
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
        var passPointEndPoints = GetPassPoint( document, fromEndPoint, toEndPointsWithoutLast, level, diameter / 2, height ) ;
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
     
    /// <summary>
    /// Create segment of new route
    /// </summary>
    /// <param name="document"></param>
    /// <param name="fromEndPoint"></param>
    /// <param name="toEndPoint"></param>
    /// <param name="toPoints"></param>
    /// <param name="routeProperty"></param>
    /// <param name="classificationInfo"></param> 
    /// <returns></returns>
    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRouteUseTempConnector( Document document, ConnectorPicker.IPickResult fromIPick, ConnectorPicker.IPickResult toIPick, List<Element> toPoints, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
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

      var useConnectorDiameter = UseConnectorDiameter() ; 
      var routeFromEndPoints = new List<IEndPoint>() { } ;
      var routeToEndPoints = new List<IEndPoint>() ;
      
      ConnectorPicker.IPickResult fromTempIPick = fromIPick;
      ConnectorPicker.IPickResult toTempIPick = toIPick;
      
      if ( toPoints.Count >= 1 ) {  
        for ( var i = 0 ; i <= toPoints.Count - 1 ; i++ ) { 
          
          toTempIPick = ConnectorPicker.CreatePressureConnector( toPoints[ i ], fromTempIPick.GetOrigin(), null ) ;
          routeFromEndPoints.Add( PickCommandUtil.GetEndPoint( fromTempIPick, toTempIPick, useConnectorDiameter )  );
          routeToEndPoints.Add( PickCommandUtil.GetEndPoint( toTempIPick, fromTempIPick, useConnectorDiameter )  );
          
          if(toPoints.Count > i + 1)
            fromTempIPick = ConnectorPicker.CreatePressureConnector( toPoints[ i ], null, (toPoints[ i + 1 ].Location as LocationPoint )!.Point) ;
          else {
            fromTempIPick = ConnectorPicker.CreatePressureConnector( toPoints[ i ], null, toIPick.GetOrigin()) ;
            
          }
        }
        routeFromEndPoints.Add( PickCommandUtil.GetEndPoint( fromTempIPick, toIPick, useConnectorDiameter )  );
        routeToEndPoints.Add( PickCommandUtil.GetEndPoint( toIPick, fromTempIPick, useConnectorDiameter )  );

      }
      else {
        routeFromEndPoints.Add( PickCommandUtil.GetEndPoint( fromTempIPick, toTempIPick, useConnectorDiameter )  );
        routeToEndPoints.Add( PickCommandUtil.GetEndPoint( toTempIPick, fromTempIPick, useConnectorDiameter )  );
      }
 
      List<(string RouteName, RouteSegment Segment)> result = new() ;
      result.AddRange( routeFromEndPoints.Zip( routeToEndPoints, ( f, t ) =>
      { 
        var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
        return ( name, segment ) ;
      } ) ) ;

      return result ;
    }
    
    private List<PassPointEndPoint> GetPassPoint(Document document, IEndPoint fromEndPoint, List<XYZ> toPoints, Level level, double radius, double height )
    {
      var passPoints = new List<PassPointEndPoint>() ;
      var fromPoint = fromEndPoint.RoutingStartPosition ;
      foreach ( var toPoint in toPoints ) {
        var newToPoint = new XYZ( toPoint.X, toPoint.Y, height ) ;
        var dir = GetPreferredDirection( fromPoint, newToPoint ) ; 
        passPoints.Add( new PassPointEndPoint( document, string.Empty, newToPoint, dir, radius, level )) ;
        fromPoint = newToPoint ;
      }

      return passPoints ;
    } 
    
    private static XYZ GetPreferredDirection( XYZ pos, XYZ anotherPos )
    {
      var dir = anotherPos - pos ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        return ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        return ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }
    } 

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    /// <summary>
    /// Create connector type pressure (X symbol)
    /// </summary>
    /// <param name="document"></param>
    /// <param name="level"></param>
    /// <param name="xyz"></param>
    /// <param name="isTemp"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private FamilyInstance GeneratePressureConnector( Document document, Level level, XYZ xyz, bool isTemp = false )
    {
      var symbol = isTemp ?  document.GetFamilySymbols( ElectricalRoutingFamilyType.PressureTempConnector ).FirstOrDefault() : document.GetFamilySymbols( ElectricalRoutingFamilyType.PressureConnector ).FirstOrDefault() ?? throw new Exception() ;
      using Transaction t = new Transaction( document, "Create end point mark trans" ) ;
      t.Start() ;
      var result = symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
      t.Commit() ;
      return result ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue )
    {
      using Transaction t = new Transaction( document, "Change conduit color" ) ;
      t.Start() ;
       
      //Change conduit color to yellow RGB(255,255,0)
      Element? conduitUseToAddAnnotation = null ;
      OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
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

    /// <summary>
    /// Initial all property of rout
    /// </summary>
    /// <param name="document"></param>
    /// <param name="elementId"></param>
    /// <returns></returns>
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