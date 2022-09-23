using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.Exceptions ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PickRoutingCommandBase : RoutingCommandBase<PickRoutingCommandBase.PickState>
  {
    // Todo: 斜めルーティングの場合はPullBoxを無視する
    public record PickState( ConnectorPicker.IPickResult FromPickResult, ConnectorPicker.IPickResult ToPickResult, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, XYZ? PassPointPosition, XYZ? PassPointDirection, XYZ? SecondPassPointPosition, XYZ? SecondPassPointDirection, bool IsNeedCreatePullBox ) ;

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;

    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;


    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty,
      MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;

    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      try {
        var routingExecutor = GetRoutingExecutor() ;
        var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
        ConnectorPicker.IPickResult toPickResult ;

        using ( uiDocument.SetTempColor( fromPickResult ) ) {
          toPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, false, "Dialog.Commands.Routing.PickRouting.PickSecond".GetAppStringByKeyOrDefault( null ), fromPickResult, GetAddInType() ) ;
        }

        var property = ShowPropertyDialog( uiDocument.Document, fromPickResult, toPickResult ) ;
        if ( true != property?.DialogResult ) return OperationResult<PickState>.Cancelled ;

        if ( property.GetShaft() != null ) {
          var isShaftThroughFromAndToLevel = CheckShaft( document, property.GetShaft()!.UniqueId, fromPickResult.GetLevelId(), toPickResult.GetLevelId() ) ;
          if ( ! isShaftThroughFromAndToLevel ) {
            MessageBox.Show( "The shaft does not go through the levels of connectors", "Arent" ) ;
            return OperationResult<PickState>.Cancelled ;
          }
        }

        if ( GetMEPSystemClassificationInfo( fromPickResult, toPickResult, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<PickState>.Cancelled ;

        double passPointPositionDistance ;
        using ( var trans = new Transaction( document, "Create MEP System Pipe Spec" ) ) {
          trans.Start() ;
          var pipeSpec = new MEPSystemPipeSpec( new RouteMEPSystem( document, property.GetSystemType(), property.GetCurveType() ), routingExecutor.FittingSizeCalculator ) ;
          var bendRadius = pipeSpec.GetLongElbowSize( property.GetDiameter().DiameterValueToPipeDiameter() ) ;
          passPointPositionDistance = bendRadius + property.GetDiameter() * 6 ;
          trans.Commit() ;
        }

        XYZ? passPointPosition = null ;
        XYZ? passPointDirection = null ;
        XYZ? secondPassPointPosition = null ;
        XYZ? secondPassPointDirection = null ;
        
        // Todo: 斜めルーティングの場合はPullBoxを無視する
        var isNeedCreatePullBox = ! PickCommandUtil.IsAnyConnectorNotDirectionAsXOrY( fromPickResult, toPickResult ) ;
        
        if ( GetAddInType() == AddInType.Electrical && document.ActiveView is ViewPlan && fromPickResult.GetLevelId() == toPickResult.GetLevelId() ) {
          ( passPointPosition, passPointDirection, secondPassPointPosition, secondPassPointDirection ) = ShowPreviewLines( uiDocument, fromPickResult, toPickResult, passPointPositionDistance ) ;
        }

        // Todo: 斜めルーティングの場合はPullBoxを無視する
        return new OperationResult<PickState>( new PickState( fromPickResult, toPickResult, property, classificationInfo, passPointPosition, passPointDirection, secondPassPointPosition, secondPassPointDirection, isNeedCreatePullBox ) ) ;
      }
      catch ( OperationCanceledException ) {
        return OperationResult<PickState>.Cancelled ;
      }
    }
    
    private ( XYZ?, XYZ?, XYZ?, XYZ? ) ShowPreviewLines( UIDocument uiDocument, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, double passPointPositionDistance )
    {
      XYZ? passPointPosition = null ;
      XYZ? passPointDirection = null ;
      XYZ? secondPassPointPosition = null ;
      XYZ? secondPassPointDirection = null ;      
      var document = uiDocument.Document ;
      if ( document.ActiveView is ViewPlan ) {
        var allRouteLines = PickCommandUtil.CreatePreviewLines( uiDocument.Document, fromPickResult, toPickResult ) ;

        var previewLineIds = new List<ElementId>() ;
        var allLineIds = new List<ElementId>() ;

        allRouteLines.ForEach( rl =>
        {
          previewLineIds.AddRange( rl.previewLineIds ) ;
          allLineIds.AddRange( rl.allLineIds ) ;
        } ) ;

        if ( previewLineIds.Any() && allLineIds.Any() ) {
          if ( allRouteLines.Any( rl => rl.allowedTiltedPiping ) && allRouteLines.Count() == 1 ) {
            var routeLines = allRouteLines.First( rl => rl.allowedTiltedPiping ) ;
            var firstPasPoint = GetPassPointPositionAndDirection( routeLines.previewLineIds[ 0 ], document, true, true, passPointPositionDistance ) ;
            passPointPosition = firstPasPoint.passPointPosition ;
            passPointDirection = firstPasPoint.passPointDirection ;

            var secondPassPoint = GetPassPointPositionAndDirection( routeLines.previewLineIds[ 1 ], document, true, true, passPointPositionDistance ) ;
            secondPassPointPosition = secondPassPoint.passPointPosition ;
            secondPassPointDirection = secondPassPoint.passPointDirection ;
          }
          else {
            PreviewLineSelectionFilter previewLineSelectionFilter = new(previewLineIds) ;
            var selectedRoute = uiDocument.Selection.PickObject( ObjectType.Element, previewLineSelectionFilter, "Select preview line." ) ;
            var routeLines = allRouteLines.First( rl => rl.previewLineIds.Any( lineId => lineId == selectedRoute.ElementId ) ) ;
            var isCreatePullBoxAtMiddlePoint = ! PickCommandUtil.IsAnyConnectorNotDirectionAsXOrY( fromPickResult, toPickResult ) ;
            var passPoint = GetPassPointPositionAndDirection( routeLines.previewLineIds[ 0 ], document, isCreatePullBoxAtMiddlePoint, true, passPointPositionDistance ) ;
            passPointPosition = passPoint.passPointPosition ;
            passPointDirection = passPoint.passPointDirection ;

            if ( PickCommandUtil.IsAnyConnectorNotDirectionAsXOrY( fromPickResult, toPickResult ) ) {
              var secondPassPoint = GetPassPointPositionAndDirection( routeLines.previewLineIds[ 1 ], document, isPassPointInMiddle: false, isFromStartPoint: false, passPointPositionDistance ) ;
              secondPassPointPosition = secondPassPoint.passPointPosition ;
              secondPassPointDirection = secondPassPoint.passPointDirection ;
            }
          }

          PickCommandUtil.RemovePreviewLines( document, allLineIds ) ;
        }
      }

      return ( passPointPosition, passPointDirection, secondPassPointPosition, secondPassPointDirection ) ;
    }

    private static (XYZ? passPointPosition, XYZ? passPointDirection) GetPassPointPositionAndDirection(ElementId lineId,Document document, bool isPassPointInMiddle = true,bool isFromStartPoint = true,double passPointPositionDistance = 0 )
    {
      var passPointLine = ( (DetailLine)document.GetElement( lineId ) ).GeometryCurve as Line ;
      var p1 = passPointLine!.GetEndPoint( 0 ) ;
      var p2 = passPointLine!.GetEndPoint( 1 ) ;
      
      var passPointDirection = passPointLine.Direction ;
      if ( isPassPointInMiddle ) {
        var passPointPosition = ( p1 + p2 ) / 2 ;
        return ( passPointPosition, passPointDirection ) ;
      }
      
      var distance = p1.DistanceTo( p2 ) - passPointPositionDistance ;
      {
        var passPointPosition = isFromStartPoint
          ? p1 + ( p2 - p1 ).Normalize() * distance
          : p2 + ( p1 - p2 ).Normalize() * distance ;
        return ( passPointPosition, passPointDirection ) ;
      }
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, MEPSystemType? systemType )
    {
      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute )?.Route.GetSystemClassificationInfo() is { } routeSystemClassificationInfo ) return routeSystemClassificationInfo ;

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private IRoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var fromLevelId = GetTrueLevelId( document, fromPickResult ) ;
      var toLevelId = GetTrueLevelId( document, toPickResult ) ;

      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute ) is { } subRoute ) {
        var route = subRoute.Route ;

        return ShowDialog( document, new DialogInitValues( route.GetSystemClassificationInfo(), route.GetMEPSystemType(), route.GetDefaultCurveType(), subRoute.GetDiameter() ), fromLevelId, toLevelId ) ;
      }

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }

    protected virtual IRoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType, routeChoiceSpec.StandardTypes?.FirstOrDefault(), initValues.Diameter ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, routeChoiceSpec, addInType ) ) ;
      sv.ShowDialog() ;

      return sv ;
    }

    private static bool CheckShaft( Document document, string shaftElementUniqueId, ElementId fromLevelId, ElementId toLevelId )
    {
      var shaft = document.GetElementById<Opening>( shaftElementUniqueId ) ;
      if ( shaft == null ) return false ;
      var baseLevel = (Level) document.GetElement( shaft.get_Parameter( BuiltInParameter.WALL_BASE_CONSTRAINT ).AsElementId() ) ;
      var topLevel = (Level) document.GetElement( shaft.get_Parameter( BuiltInParameter.WALL_HEIGHT_TYPE ).AsElementId() ) ;
      var fromLevel = (Level) document.GetElement( fromLevelId ) ;
      var toLevel = (Level) document.GetElement( toLevelId ) ;
      return fromLevel.Elevation >= baseLevel.Elevation && fromLevel.Elevation <= topLevel.Elevation && toLevel.Elevation >= baseLevel.Elevation && toLevel.Elevation <= topLevel.Elevation ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      // Todo: 斜めルーティングの場合はPullBoxを無視する
      var (fromPickResult, toPickResult, routeProperty, classificationInfo, passPointPosition, passPointDirection, secondPassPointPosition, secondPassPointDirection, _) = pickState ;

      RouteGenerator.CorrectEnvelopes( document ) ;
      ChangeFromConnectorAndToConnectorColor( document, fromPickResult, toPickResult ) ;

      if ( null != fromPickResult.SubRoute ) {
        return CreateNewSegmentListForRoutePick( fromPickResult, toPickResult, false, routeProperty, classificationInfo ) ;
      }

      if ( null != toPickResult.SubRoute ) {
        return CreateNewSegmentListForRoutePick( toPickResult, fromPickResult, true, routeProperty, classificationInfo ) ;
      }

      return CreateNewSegmentList( document, fromPickResult, toPickResult, routeProperty, classificationInfo, passPointPosition, passPointDirection, secondPassPointPosition, secondPassPointDirection ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, XYZ? passPointPosition, XYZ? passPointDirection , XYZ? secondPassPointPosition, XYZ? secondPassPointDirection)
    {
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, toPickResult, useConnectorDiameter ) ;
      var toEndPoint = PickCommandUtil.GetEndPoint( toPickResult, fromPickResult, useConnectorDiameter ) ;

      if ( passPointPosition == null ) {
        var (name, segment) = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo ) ;

        return new[] { ( name, segment ) } ;
      }

      if ( secondPassPointPosition == null ) {
        return CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo, passPointPosition, passPointDirection! ) ;
      }

      var allowedTiltedPiping = GetAddInType() == AddInType.Electrical ;
      return CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo, passPointPosition, passPointDirection!, secondPassPointPosition, secondPassPointDirection, allowedTiltedPiping ) ;

    }

    private (string RouteName, RouteSegment Segment) CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
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

      return ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ;
    }
    
    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, XYZ passPointPosition, XYZ passPointDirection )
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

      var results = new List<(string RouteName, RouteSegment Segment)>() ;
      var passPointPositionZ = PassPointEndPoint.GetForcedFixedHeight( document, fromFixedHeight, fromEndPoint.GetLevelId( document ) ) ?? passPointPosition.Z ;
      passPointPosition = new XYZ( passPointPosition.X, passPointPosition.Y, passPointPositionZ ) ;
      var passPoint = document.AddPassPoint( name, passPointPosition, passPointDirection!, diameter * 0.5, fromEndPoint.GetLevelId( document ) ) ;
      passPoint.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toEndPoint.Key.GetElementUniqueId() ) ;
      passPoint.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromEndPoint.Key.GetElementUniqueId() ) ;
      var passPointEndPoint = new PassPointEndPoint( passPoint ) ;
      results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, passPointEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
      results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, passPointEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;

      return results ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, IEndPoint fromEndPoint, IEndPoint toEndPoint, IRouteProperty routeProperty,
      MEPSystemClassificationInfo classificationInfo, XYZ passPointPosition, XYZ passPointDirection, XYZ? secondPassPointPosition, XYZ? secondPassPointDirection, bool allowedTiltedPiping )
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

      var results = new List<(string RouteName, RouteSegment Segment)>() ;
      var passPointPositionZ = PassPointEndPoint.GetForcedFixedHeight( document, fromFixedHeight, fromEndPoint.GetLevelId( document ) ) ?? passPointPosition.Z ;
      passPointPosition = new XYZ( passPointPosition.X, passPointPosition.Y, passPointPositionZ ) ;
      var passPoint = document.AddPassPoint( name, passPointPosition, passPointDirection!, diameter * 0.5, fromEndPoint.GetLevelId( document ) ) ;
      passPoint.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toEndPoint.Key.GetElementUniqueId() ) ;
      passPoint.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromEndPoint.Key.GetElementUniqueId() ) ;
      var passPointEndPoint = new PassPointEndPoint( passPoint ) ;

      if ( secondPassPointPosition != null ) {
        var secondPassPointPositionZ = PassPointEndPoint.GetForcedFixedHeight( document, fromFixedHeight, fromEndPoint.GetLevelId( document ) ) ?? secondPassPointPosition.Z ;
        secondPassPointPosition = new XYZ( secondPassPointPosition.X, secondPassPointPosition.Y, secondPassPointPositionZ ) ;
        var secondPassPoint = document.AddPassPoint( name, secondPassPointPosition, secondPassPointDirection!, diameter * 0.5, fromEndPoint.GetLevelId( document ) ) ;
        secondPassPoint.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toEndPoint.Key.GetElementUniqueId() ) ;
        secondPassPoint.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromEndPoint.Key.GetElementUniqueId() ) ;
        var secondPassPointEndPoint = new PassPointEndPoint( secondPassPoint ) ;
      
        results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, passPointEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping ) ) ) ;
        results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, passPointEndPoint, secondPassPointEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping ) ) ) ;
        results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, secondPassPointEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping ) ) ) ;
      }
      else {
        results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, passPointEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping ) ) ) ;
        results.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, passPointEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, allowedTiltedPiping ) ) ) ;
      }

      return results ;
    }
    private static Level? GetLevel( Document document, IEndPoint endPoint )
    {
      if ( endPoint.GetReferenceConnector()?.Owner.GetLevelId() is not { } levelId ) return null ;

      return document.GetElementById<Level>( levelId ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentListForRoutePick( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool anotherIndicatorIsFromSide, IRouteProperty routeProperty,
      MEPSystemClassificationInfo classificationInfo )
    {
      //return AppendNewSegmentIntoPickedRoute( routePickResult, anotherPickResult, anotherIndicatorIsFromSide ) ;  // Use this, when a branch is to be merged into the parent from-to.
      return CreateSubBranchRoute( routePickResult, anotherPickResult, anotherIndicatorIsFromSide, routeProperty, classificationInfo ).EnumerateAll() ;
    }

    private IEnumerable<(string RouteName, RouteSegment Segment)> CreateSubBranchRoute( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool anotherIndicatorIsFromSide, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var affectedRoutes = new List<Route>() ;
      var (routeEndPoint, otherSegments1) = CreateEndPointOnSubRoute( routePickResult, anotherPickResult, routeProperty, classificationInfo, true ) ;

      IEndPoint anotherEndPoint ;
      IReadOnlyCollection<( string RouteName, RouteSegment Segment )>? otherSegments2 = null ;
      if ( null != anotherPickResult.SubRoute ) {
        ( anotherEndPoint, otherSegments2 ) = CreateEndPointOnSubRoute( anotherPickResult, routePickResult, routeProperty, classificationInfo, false ) ;
      }
      else {
        anotherEndPoint = PickCommandUtil.GetEndPoint( anotherPickResult, routePickResult, UseConnectorDiameter() ) ;
      }

      var fromEndPoint = anotherIndicatorIsFromSide ? anotherEndPoint : routeEndPoint ;
      var toEndPoint = anotherIndicatorIsFromSide ? routeEndPoint : anotherEndPoint ;

      var document = routePickResult.SubRoute!.Route.Document ;
      var (name, segment) = CreateSegmentOfNewRoute( document, fromEndPoint, toEndPoint, routeProperty, classificationInfo ) ;

      // Inserted segment
      yield return ( name, segment ) ;

      // Routes where pass points are inserted
      var routes = RouteCache.Get( DocumentKey.Get( routePickResult.SubRoute!.Route.Document ) ) ;
      var changedRoutes = new HashSet<Route>() ;
      if ( null != otherSegments1 ) {
        foreach ( var tuple in otherSegments1 ) {
          yield return tuple ;

          if ( routes.TryGetValue( tuple.RouteName, out var route ) ) {
            changedRoutes.Add( route ) ;
          }
        }
      }

      if ( null != otherSegments2 ) {
        foreach ( var tuple in otherSegments2 ) {
          yield return tuple ;

          if ( routes.TryGetValue( tuple.RouteName, out var route ) ) {
            changedRoutes.Add( route ) ;
          }
        }
      }

      // Affected routes
      if ( 0 != affectedRoutes.Count ) {
        var affectedRouteSet = new HashSet<Route>() ;
        foreach ( var route in affectedRoutes ) {
          affectedRouteSet.Add( route ) ;
          affectedRouteSet.UnionWith( route.CollectAllDescendantBranches() ) ;
        }

        affectedRouteSet.ExceptWith( changedRoutes ) ;

        foreach ( var tuple in affectedRouteSet.ToSegmentsWithName() ) {
          yield return tuple ;
        }
      }
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> AppendNewSegmentIntoPickedRoute( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool anotherIndicatorIsFromSide )
    {
      var route = routePickResult.SubRoute!.Route ;
      var segments = route.ToSegmentsWithNameList() ;
      var anotherEndPoint = PickCommandUtil.GetEndPoint( anotherPickResult, routePickResult, UseConnectorDiameter() ) ;
      var segment = CreateNewSegment( routePickResult.SubRoute!, routePickResult.EndPointOverSubRoute, routePickResult, anotherEndPoint, anotherIndicatorIsFromSide ) ;
      segment.ApplyRealNominalDiameter() ;
      segments.Add( ( route.RouteName, segment ) ) ;
      return segments ;
    }

    private static RouteSegment CreateNewSegment( SubRoute subRoute, EndPointKey? endPointOverSubRoute, ConnectorPicker.IPickResult pickResult, IEndPoint newEndPoint, bool newEndPointIndicatorIsFromSide )
    {
      var document = subRoute.Route.Document ;
      var detector = new RouteSegmentDetector( subRoute, pickResult.PickedElement ) ;
      var classificationInfo = subRoute.Route.GetSystemClassificationInfo() ;
      var systemType = subRoute.Route.GetMEPSystemType() ;
      var curveType = subRoute.Route.GetDefaultCurveType() ;

      if ( null != endPointOverSubRoute && subRoute.AllEndPoints.FirstOrDefault( ep => ep.Key == endPointOverSubRoute ) is { } overSubRoute ) {
        var shaft = ( newEndPoint.GetLevelId( document ) != overSubRoute.GetLevelId( document ) ) ? subRoute.ShaftElementUniqueId : null ;
        if ( newEndPointIndicatorIsFromSide ) {
          return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, overSubRoute, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FromFixedHeight, subRoute.ToFixedHeight, subRoute.AvoidType, shaft ) ;
        }
        else {
          return new RouteSegment( classificationInfo, systemType, curveType, overSubRoute, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FromFixedHeight, subRoute.ToFixedHeight, subRoute.AvoidType, shaft ) ;
        }
      }

      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( false == detector.IsPassingThrough( segment ) ) continue ;

        if ( newEndPointIndicatorIsFromSide ) {
          var shaft = ( newEndPoint.GetLevelId( document ) != segment.ToEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementUniqueId : null ;
          return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, segment.ToEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FromFixedHeight, subRoute.ToFixedHeight, subRoute.AvoidType, shaft ) ;
        }
        else {
          var shaft = ( segment.FromEndPoint.GetLevelId( document ) != newEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementUniqueId : null ;
          return new RouteSegment( classificationInfo, systemType, curveType, segment.FromEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FromFixedHeight, subRoute.ToFixedHeight, subRoute.AvoidType, shaft ) ;
        }
      }

      // fall through: add terminate end point.
      if ( newEndPointIndicatorIsFromSide ) {
        var terminateEndPoint = new TerminatePointEndPoint( document, string.Empty, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( false ), newEndPoint.GetDiameter(), string.Empty ) ;
        var shaft = ( newEndPoint.GetLevelId( document ) != terminateEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementUniqueId : null ;
        return new RouteSegment( classificationInfo, systemType, curveType, newEndPoint, terminateEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FromFixedHeight, subRoute.ToFixedHeight, subRoute.AvoidType, shaft ) ;
      }
      else {
        var terminateEndPoint = new TerminatePointEndPoint( document, string.Empty, newEndPoint.RoutingStartPosition, newEndPoint.GetRoutingDirection( true ), newEndPoint.GetDiameter(), string.Empty ) ;
        var shaft = ( terminateEndPoint.GetLevelId( document ) != newEndPoint.GetLevelId( document ) ) ? subRoute.ShaftElementUniqueId : null ;
        return new RouteSegment( classificationInfo, systemType, curveType, terminateEndPoint, newEndPoint, subRoute.GetDiameter(), subRoute.IsRoutingOnPipeSpace, subRoute.FromFixedHeight, subRoute.ToFixedHeight, subRoute.AvoidType, shaft ) ;
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, PickState state )
    {
      if ( GetAddInType() == AddInType.Electrical )
        ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
    }

    private static void ChangeFromConnectorAndToConnectorColor( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var connectors = new List<Element>() ;
      var fromConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).FirstOrDefault( c => c.Id == fromPickResult.PickedElement.Id ) ;
      if ( fromConnector != null )
        connectors.Add( fromConnector ) ;

      var toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).FirstOrDefault( c => c.Id == toPickResult.PickedElement.Id ) ;
      if ( toConnector != null )
        connectors.Add( toConnector ) ;

      if ( ! connectors.Any() ) return ;
      ConfirmUnsetCommandBase.ResetElementColor( connectors ) ;
    }
  }
}