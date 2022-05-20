using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PressureGuidingTubeCommandBase : RoutingCommandBase<PressureGuidingTubeCommandBase.PressureGuidingTubePickState>
  {
    private double _height = 0 ;
    private Dictionary<XYZ, FamilyInstance> _dictTempElement = new Dictionary<XYZ, FamilyInstance>() ;

    public record PressureGuidingTubePickState( ConnectorPicker.IPickResult FromPickResult, List<XYZ> ListSelectedPoint, RouteProperties RouteProperties, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<PressureGuidingTubePickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
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
          _height = pressureSettingViewModel.PressureGuidingTube.Height.MillimetersToRevitUnits() ;
          //var selectedPointList = new List<ConnectorPicker.IPickResult>() ;
          var selectedPointList = new List<XYZ>() ;
          var numberOfPoint = 0 ;

          using ( uiDocument.SetTempColor( fromPickResult ) ) {
            //Automatic: Select one point only
            if ( pressureSettingViewModel.SelectedCreationMode == CreationModeEnum.自動モード ) {
              var xyz = uiDocument.Selection.PickPoint( "Click the end point" ) ;
              selectedPointList.Add( xyz ) ;
              var element = GeneratePressureConnector( document, level!, new XYZ( xyz.X, xyz.Y, _height ) ) ;
              _dictTempElement.Add( xyz, element ) ;
              //selectedPointList.Add( ConnectorPicker.CreatePressureConnector( uiDocument, element, fromPickResult.PickedConnector, AddInType.Electrical ) ) ;
            }
            //Manual: Select many point
            else {
              try {
                while ( true ) {
                  numberOfPoint++ ;
                  var xyz = uiDocument.Selection.PickPoint( "Click the end point number " + numberOfPoint ) ;
                  selectedPointList.Add( xyz ) ;
                  var element = GeneratePressureConnector( document, level!, new XYZ( xyz.X, xyz.Y, _height ) ) ;
                  _dictTempElement.Add( xyz, element ) ;
                  //selectedPointList.Add( ConnectorPicker.CreatePressureConnector( uiDocument, element, fromPickResult.PickedConnector, AddInType.Electrical ) ) ;
                }
              }
              catch ( OperationCanceledException ) {
                //end select point 
              }
            }
          }

          var properties = InitRoutProperties( document, _height, fromPickResult.PickedConnector?.GetDiameter() ) ;
          MEPSystemClassificationInfo classificationInfo = MEPSystemClassificationInfo.Undefined ;
          if ( ( fromPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo )
            classificationInfo = connectorClassificationInfo ;

          return new OperationResult<PressureGuidingTubePickState>( new PressureGuidingTubePickState( fromPickResult, selectedPointList, properties, classificationInfo! ) ) ;
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
      var (fromPickResult, selectedPointList, routeProperty, classificationInfo) = state ;
      return CreateNewSegmentList( document, fromPickResult, selectedPointList, routeProperty, classificationInfo ) ;
    }

    /// <summary>
    /// Create list segment
    /// </summary>
    /// <param name="document"></param>
    /// <param name="fromPickResult"></param>
    /// <param name="selectedPointList"></param>
    /// <param name="routeProperty"></param>
    /// <param name="classificationInfo"></param>
    /// <returns></returns>
    //private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, List<ConnectorPicker.IPickResult> selectedPointList, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, List<XYZ> selectedPointList, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      var preferredRadius = fromPickResult.PickedConnector?.Radius ;
      for ( var i = 0 ; i < selectedPointList.Count ; i++ ) {
        routeSegments.Add( i == 0 ? CreateNewSegment( document, fromPickResult, selectedPointList[ 0 ], routeProperty, classificationInfo ) : CreateNewSegment( document, selectedPointList[ i - 1 ], selectedPointList[ i ], routeProperty, classificationInfo ) ) ;
      }

      return routeSegments ;
    }

    private (string RouteName, RouteSegment Segment) CreateNewSegment( Document document, ConnectorPicker.IPickResult fromPickResult, XYZ selectedPoint, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var element = _dictTempElement[ selectedPoint ] ;
      var toPicked = ConnectorPicker.CreatePressureConnector( element, fromPickResult.PickedConnector?.Origin, null, GetAddInType() ) ;
      return CreateNewSegment( document, fromPickResult, toPicked, routeProperty, classificationInfo ) ;
    }

    private (string RouteName, RouteSegment Segment) CreateNewSegment( Document document, XYZ fromPoint, XYZ toPoint, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var elementFrom = _dictTempElement[ fromPoint ] ;
      var fromPick = ConnectorPicker.CreatePressureConnector( elementFrom, null, toPoint, GetAddInType() ) ;

      var elementTo = _dictTempElement[ toPoint ] ;
      var toPick = ConnectorPicker.CreatePressureConnector( elementTo, fromPoint, null, GetAddInType() ) ;

      return CreateNewSegment( document, fromPick, toPick, routeProperty, classificationInfo ) ;
    }

    /// <summary>
    /// Create new segment
    /// </summary>
    /// <param name="document"></param>
    /// <param name="fromPickResult"></param>
    /// <param name="selectedPoint"></param>
    /// <param name="routeProperty"></param>
    /// <param name="classificationInfo"></param>
    /// <returns></returns>
    private (string RouteName, RouteSegment Segment) CreateNewSegment( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult selectedPoint, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, selectedPoint, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( selectedPoint, fromPickResult, useConnectorDiameter ) ;

      return CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo ) ;
    }

    /// <summary>
    /// Create segment of new route
    /// </summary>
    /// <param name="document"></param>
    /// <param name="fromEndPoint"></param>
    /// <param name="toEndPoint"></param>
    /// <param name="routeProperty"></param>
    /// <param name="classificationInfo"></param>
    /// <returns></returns>
    private (string RouteName, RouteSegment Segment) CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, RouteProperties routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var systemType = routeProperty.SystemType ;
      var curveType = routeProperty.CurveType ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = routeProperty.Diameter ;
      bool isRoutingOnPipeSpace = routeProperty.IsRouteOnPipeSpace ?? false ;
      var fromFixedHeight = routeProperty.FromFixedHeight ;
      var toFixedHeight = routeProperty.ToFixedHeight ;
      var avoidType = routeProperty.AvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = routeProperty.Shaft?.UniqueId ;

      return ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ;
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
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private FamilyInstance GeneratePressureConnector( Document document, Level level, XYZ xyz )
    {
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.PressureConnector ).FirstOrDefault() ?? throw new Exception() ;
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
      OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
      ogs.SetProjectionLineColor( new Color( 255, 255, 0 ) ) ;
      foreach ( var route in executeResultValue ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        foreach ( var conduit in conduits ) {
          document.ActiveView.SetElementOverrides( conduit.Id, ogs ) ;
          //conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem! ) ;
          //conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
        }
      }

      //Delete template element of selected points except the last one.
      var listDel = _dictTempElement.Values.Select( x => x.Id ).ToList() ;
      listDel.RemoveAt( listDel.Count - 1 ) ;
      document.Delete( listDel ) ;

      t.Commit() ;
    }

    /// <summary>
    /// Initial all property of rout
    /// </summary>
    /// <param name="document"></param>
    /// <param name="height"></param>
    /// <param name="diameter"></param>
    /// <returns></returns>
    private RouteProperties InitRoutProperties( Document document, double height, double? diameter )
    {
      var curveType = document.GetAllElements<MEPCurveType>().FirstOrDefault( x => x.FamilyName.ToLower().Contains( "conduit" ) ) ;
      return new RouteProperties( document, null, curveType, diameter, false, true, FixedHeight.CreateOrNull( FixedHeightType.Ceiling, height ), null, null, AvoidType.Whichever, null ) ;
    }
  }
}