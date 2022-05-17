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
    public record PressureGuidingTubePickState( ConnectorPicker.IPickResult FromPickResult, List<FamilyInstance> ToPickResult, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty,
      MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;

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
          var height = document.GetHeightSettingStorable()[ level! ].HeightOfConnectors.MillimetersToRevitUnits() + pressureSettingViewModel.PressureGuidingTube.Height.MillimetersToRevitUnits() ;
          List<FamilyInstance> toPickResult = new List<FamilyInstance>() ;
          int numberOfPoint = 0 ;

          using ( uiDocument.SetTempColor( fromPickResult ) ) {
            //Automatic: Select one point only
            if ( pressureSettingViewModel.SelectedCreationMode == CreationModeEnum.自動モード ) {
              var xyz = uiDocument.Selection.PickPoint( "Click the end point" ) ;
              toPickResult.Add( CreatePressureEndPoint( document, level!, new XYZ( xyz.X, xyz.Y, height ) ) ) ;
            }
            //Manual: Select many point
            else {
              try {
                while ( true ) {
                  numberOfPoint++ ;
                  var xyz = uiDocument.Selection.PickPoint( "Click the end point number " + numberOfPoint.ToString() ) ;
                  toPickResult.Add( CreatePressureEndPoint( document, level!, new XYZ( xyz.X, xyz.Y, height ) ) ) ;
                }
              }
              catch ( OperationCanceledException ) {
                //end select point 
              }
            }
          }

          var property = ShowPropertyDialog( uiDocument.Document, fromPickResult, toPickResult ) ;
          if ( true != property?.DialogResult ) return OperationResult<PressureGuidingTubePickState>.Cancelled ;
 
          var classificationInfo = GetMEPSystemClassificationInfoFromSystemType( property.GetSystemType() ) ;
          if ( ( fromPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo )
            classificationInfo = connectorClassificationInfo ;

          return new OperationResult<PressureGuidingTubePickState>( new PressureGuidingTubePickState( fromPickResult, toPickResult, property, classificationInfo! ) ) ;
        }
        catch {
          MessageBox.Show( "Generate pressure guiding tube failed.", "Error Message" ) ;
          return OperationResult<PressureGuidingTubePickState>.Failed ;
        }
      }

      return OperationResult<PressureGuidingTubePickState>.Cancelled ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PressureGuidingTubePickState state )
    {
      var (fromPickResult, toPickResult, routeProperty, classificationInfo) = state ;

      RouteGenerator.CorrectEnvelopes( document ) ;

      return CreateNewSegmentList( document, fromPickResult, toPickResult, routeProperty, classificationInfo ) ;
    }
 
    private IRoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult, List<FamilyInstance> toPickResult )
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

    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }

    protected IRoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
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

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, List<FamilyInstance> toPickResultList, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      var useConnectorDiameter = UseConnectorDiameter() ;
      var preferredRadius = fromPickResult.PickedConnector?.Radius ;
      for ( var i = 0 ; i < toPickResultList.Count ; i++ ) {
        routeSegments.AddRange( i == 0 ? CreateNewSegmentList( document, fromPickResult, toPickResultList[ 0 ], routeProperty, classificationInfo ) : CreateNewSegmentList( document, toPickResultList[ i - 1 ], toPickResultList[ i ], routeProperty, classificationInfo, preferredRadius ?? 1.0 ) ) ;
      }
       
      return routeSegments ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, FamilyInstance toPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;
      var fromOrigin = fromPickResult.GetOrigin() ;
      var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      var toConnectorId = toPickResult.UniqueId ;

      var routeSegments = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, fromOrigin, fromConnectorId, toConnectorId, routeProperty, classificationInfo ) ;

      return routeSegments ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, FamilyInstance fromPickResult, FamilyInstance toPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, double preferredRadius )
    {
      var useConnectorDiameter = UseConnectorDiameter() ; 
      var fromEndPoint = PickCommandUtil.GetEndPoint( document, fromPickResult, toPickResult, preferredRadius, useConnectorDiameter, false ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( document, toPickResult, fromPickResult, preferredRadius, useConnectorDiameter, true  ) ;
      var fromOrigin = ( fromPickResult.Location as LocationPoint )!.Point ;
      var fromConnectorId = fromPickResult.UniqueId ;
      var toConnectorId = toPickResult.UniqueId ;

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

    private FamilyInstance CreatePressureEndPoint( Document document, Level level, XYZ xyz )
    {
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.PressureEndPoint ).FirstOrDefault() ?? throw new Exception() ;
      using Transaction t = new Transaction( document, "Create end point mark trans" ) ;
      t.Start() ;
      var result = symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
      t.Commit() ;
      return result ;
    }
    
    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue )
    {
      //Change conduit color
      using Transaction t = new Transaction( document, "Change conduit color" ) ;
      t.Start() ;
      OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
      ogs.SetProjectionLineColor( new Color(255, 255, 0) ) ;
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