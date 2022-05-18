using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Plumbing ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class LeakRoutingCommandBase : RoutingCommandBase<LeakRoutingCommandBase.LeakState>
  {
    public record LeakState(ConnectorPicker.IPickResult PickConnectorResult,List<FamilyInstance> PickPoints, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;
    
    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty,
      MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override OperationResult<LeakState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      // UIDocument  uiDocument = commandData.Application.ActiveUIDocument ;
      // Document document = uiDocument.Document ;
      // uiDoc = uiDocument ;
      // var routingExecutor = GetRoutingExecutor() ;
      //
      // var sv = new LeakRouteDialog() ;
      // sv.ShowDialog() ;
      // if ( true != sv?.DialogResult ) return OperationResult<LeakState>.Cancelled ;
      //
      // var pickConnectorResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
      //
      // bool endWhile = false;
      // var pickPoints = new List<XYZ>() ;
      //
      // while (endWhile == false)
      // {
      //   try
      //   {
      //     XYZ XYZ = uiDocument.Selection.PickPoint( "Pick points then press escape to cause an exception ahem...exit selection" ) ;
      //     pickPoints.Add( XYZ ) ;
      //   }
      //   catch
      //   {
      //     endWhile = true;
      //     break; // TODO: might not be correct. Was : Exit While
      //   }
      // }
      // var level = document.GetElementById<Level>( pickConnectorResult.GetLevelId() ) ;
      // var height = document.GetHeightSettingStorable()[ level! ].HeightOfConnectors.MillimetersToRevitUnits() + sv.height ;
      // var creationMode = sv.createMode ;
      //
      //
      // var property = ShowPropertyDialog( uiDocument.Document, pickConnectorResult ) ;
      //
      // if ( true != property?.DialogResult ) return OperationResult<LeakState>.Cancelled ;
      //
      // if ( GetMEPSystemClassificationInfo( pickConnectorResult, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<LeakState>.Cancelled ;
      //
      // return new OperationResult<LeakState>( new LeakState( pickConnectorResult, pickPoints, height,creationMode, property, classificationInfo) ) ;
      
      UIDocument uiDocument = commandData.Application.ActiveUIDocument ;
      Document document = uiDocument.Document ;

    
      var sv = new LeakRouteDialog() ;
      var result = sv.ShowDialog() ;
      if ( true == result ) {
        try {
          
          //Generate segments
          var routingExecutor = GetRoutingExecutor() ;
          var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
          var level = document.GetElementById<Level>( fromPickResult.GetLevelId() ) ;
          var height = document.GetHeightSettingStorable()[ level! ].HeightOfConnectors.MillimetersToRevitUnits() + sv.Height.MillimetersToRevitUnits() ;
          List<FamilyInstance> selectedPointList = new List<FamilyInstance>() ;


          //Automatic: Select one point only
          if ( sv.CmbCreationMode.SelectedIndex != 0 ) {
           
          }
          //Manual: Select many point
          else {
            int numberOfPoint = 0 ;
            while ( true ) {
              numberOfPoint++ ;
              try {
                var xyz = uiDocument.Selection.PickPoint( "Click the end point number " + numberOfPoint.ToString() ) ;
                selectedPointList.Add( CreateLeakEndPoint( document, level!, new XYZ( xyz.X, xyz.Y, height ) ) ) ;
              }
              catch {
                break ; // TODO: might not be correct. Was : Exit While
              }
            }
          }
          

          var property = ShowPropertyDialog( uiDocument.Document, fromPickResult ) ;
          if ( true != property?.DialogResult ) return OperationResult<LeakState>.Cancelled ;

          var classificationInfo = GetMEPSystemClassificationInfoFromSystemType( property.GetSystemType() ) ;
          if ( ( fromPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo )
            classificationInfo = connectorClassificationInfo ;

          return new OperationResult<LeakState>( new LeakState( fromPickResult, selectedPointList, property, classificationInfo! ) ) ;
        }
        catch {
          MessageBox.Show( "Generate leak routing failed.", "Error Message" ) ;
          return OperationResult<LeakState>.Failed ;
        }
      }

      return OperationResult<LeakState>.Cancelled ;
    }
    
    private FamilyInstance CreateLeakEndPoint( Document document, Level level, XYZ xyz )
    {
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.ToJboxConnector ).FirstOrDefault() ?? throw new Exception() ;
      using Transaction t = new Transaction( document, "Create end point mark trans" ) ;
      t.Start() ;
      var result = symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
      t.Commit() ;
      return result ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, LeakState leakState )
    {
      var (fromPickResult, selectedPointList, routeProperty, classificationInfo) = leakState ;

      RouteGenerator.CorrectEnvelopes( document ) ;

      return CreateNewSegmentList( document, fromPickResult, selectedPointList, routeProperty, classificationInfo ) ;
    }
    
    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, List<FamilyInstance> selectedPointList, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      var preferredRadius = fromPickResult.PickedConnector?.Radius ;
      for ( var i = 0 ; i < selectedPointList.Count ; i++ ) {
        routeSegments.AddRange( i == 0 ? CreateNewSegmentList( document, fromPickResult, selectedPointList[ 0 ], routeProperty, classificationInfo ) : CreateNewSegmentList( document, selectedPointList[ i - 1 ], selectedPointList[ i ], routeProperty, classificationInfo, preferredRadius ?? 0.05 ) ) ;
      }

      return routeSegments ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, FamilyInstance selectedPoint, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, selectedPoint, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( selectedPoint, fromPickResult ) ;
      var fromOrigin = fromPickResult.GetOrigin() ;
      var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      var toConnectorId = selectedPoint.UniqueId ;

      var routeSegments = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, fromOrigin, fromConnectorId, toConnectorId, routeProperty, classificationInfo ) ;

      return routeSegments ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, FamilyInstance fromPoint, FamilyInstance toPoint, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, double preferredRadius )
    {
      var useConnectorDiameter = UseConnectorDiameter() ; 
      var fromEndPoint = PickCommandUtil.GetEndPoint( document, fromPoint, toPoint, preferredRadius ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( document, toPoint, fromPoint, preferredRadius  ) ;
      var fromOrigin = ( fromPoint.Location as LocationPoint )!.Point ;
      var fromConnectorId = fromPoint.UniqueId ;
      var toConnectorId = toPoint.UniqueId ;

      var routeSegments = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, fromOrigin, fromConnectorId, toConnectorId, routeProperty, classificationInfo ) ;

      return routeSegments ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, XYZ fromOrigin, string fromConnectorId, string toConnectorId, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = routeProperty.GetDiameter() ;
      var isRoutingOnPipeSpace = routeProperty.GetRouteOnPipeSpace() ;
      var fromFixedHeight = routeProperty.GetFromFixedHeight() ;
      var toFixedHeight = routeProperty.GetToFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var shaftElementUniqueId = routeProperty.GetShaft()?.UniqueId ;

      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;

      return routeSegments ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    private IRoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult )
    {
      var fromLevelId = GetTrueLevelId( document, fromPickResult ) ;

      if ( fromPickResult.SubRoute is { } subRoute ) {
        var route = subRoute.Route ;

        return ShowDialog( document, new DialogInitValues( route.GetSystemClassificationInfo(), route.GetMEPSystemType(), route.GetDefaultCurveType(), subRoute.GetDiameter() ), fromLevelId, fromLevelId ) ;
      }
      
      if ( fromPickResult.PickedConnector is not { } connector ) return ShowDialog( document, GetAddInType(), fromLevelId, fromLevelId ) ;
      if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

      if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

      return ShowDialog( document, initValues, fromLevelId, fromLevelId ) ;
    }

    protected virtual IRoutePropertyDialog ShowDialog( Document document, LeakRoutingCommandBase.DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
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
    
    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }
    
    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue )
    {
      //Change conduit color to yellow RGB(255,255,0)
      using Transaction t = new Transaction( document, "Change conduit color" ) ;
      t.Start() ;
      OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
      ogs.SetProjectionLineColor( new Color(0, 0, 0) ) ;
      foreach ( var route in executeResultValue ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
        foreach ( var conduit in conduits ) {
          document.ActiveView.SetElementOverrides( conduit.Id, ogs ) ;
          //conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem! ) ;
          //conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
        } 
      }
      t.Commit() ;
    }
  }
}