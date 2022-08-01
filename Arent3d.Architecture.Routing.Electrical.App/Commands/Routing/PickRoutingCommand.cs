using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.PickRoutingCommand", DefaultString = "Pick\nFrom-To" )]
  [Image( "resources/PickFrom-To.png" )]
  public class PickRoutingCommand : PickRoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickRouting" ;

    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom )
    {
      return PickCommandUtil.CreateBranchingRouteEndPoint( newPickResult, anotherPickResult, routeProperty, classificationInfo, AppCommandSettings.FittingSizeCalculator, newPickIsFrom ) ;
    }

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) => curveType.Category.Name ;

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, PickState state )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
    }
    
    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue, PickState state )
    {
      if ( ! PullBoxRouteManager.IsGradeUnderThree( document ) ) return executeResultValue ;

      var registrationOfBoardDataModels = document.GetRegistrationOfBoardDataStorable().RegistrationOfBoardData ;
      var (fromPickResult, toPickResult, _, _, _, _) = state ;
      var listConnectors = new List<Element>() { fromPickResult.PickedElement, toPickResult.PickedElement } ;
      var isRouteBetweenPowerConnectors = IsRouteBetweenPowerConnectors( listConnectors, registrationOfBoardDataModels ) ;
      if ( isRouteBetweenPowerConnectors ) return executeResultValue ;
      
      using var progress = ShowProgressBar( "Routing...", false ) ;
      List<string> boards = new() ;
      List<XYZ> pullBoxPositions = new() ;
      List<(FamilyInstance, XYZ?)> pullBoxElements = new() ;
      var resultRoute = executeResultValue.ToList() ;
      var parentIndex = 1 ;
      var isPassedShaft = executeResultValue.SingleOrDefault( e => e.UniqueShaftElementUniqueId != null ) != null ;
      var isWireEnteredShaft = false ;
      var isPickedFromBottomToTop = fromPickResult.PickedConnector!.Origin.Z < toPickResult.PickedConnector!.Origin.Z ;
      Dictionary<string, List<string>> parentAndChildRoute = new() ;
      for ( int i = 0 ; i < 50 ; i++ ) {
        var segments = isPassedShaft ? PullBoxRouteManager.GetSegmentsWithPullBoxShaft( document, resultRoute, pullBoxPositions, pullBoxElements, ref parentIndex, ref parentAndChildRoute, isWireEnteredShaft, isPickedFromBottomToTop ) : PullBoxRouteManager.GetSegmentsWithPullBox( document, resultRoute, boards, pullBoxPositions, pullBoxElements, ref parentIndex, ref parentAndChildRoute ) ;
        if ( ! segments.Any() ) break ;
        using Transaction transaction = new( document ) ;
        transaction.Start( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ) ) ;
        var failureOptions = transaction.GetFailureHandlingOptions() ;
        failureOptions.SetFailuresPreprocessor( new PullBoxRouteManager.FailurePreprocessor() ) ;
        transaction.SetFailureHandlingOptions( failureOptions ) ;
        try {
          var newRouteNames = segments.Select( s => s.RouteName ).Distinct().ToHashSet() ;
          var oldRoutes = resultRoute.Where( r => ! newRouteNames.Contains( r.RouteName ) ) ;
          var result = executor.Run( segments, progress ) ;
          resultRoute = new List<Route>() ;
          resultRoute.AddRange( oldRoutes ) ;
          resultRoute.AddRange( result.Value.ToList() ) ;
        }
        catch {
          break ;
        }

        transaction.Commit( failureOptions ) ;

        if ( ! isPassedShaft ) continue ;
        if ( isWireEnteredShaft ) break ;
        isWireEnteredShaft = true ;
      }
      
      #region Change dimension of pullbox and set new label
      
      var detailSymbolStorable = document.GetDetailSymbolStorable() ;
      var pullBoxInfoStorable = document.GetPullBoxInfoStorable() ;
      var csvStorable = document.GetCsvStorable() ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      var scale = ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;

      foreach ( var pullBoxElement in pullBoxElements ) {
        var (pullBox, position) = pullBoxElement ;
        var positionLabel = position != null ? new XYZ( position.X + 0.4 * baseLengthOfLine, position.Y + 0.7 * baseLengthOfLine, position.Z ) : null ;
        PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, pullBox, csvStorable, detailSymbolStorable, pullBoxInfoStorable,
          conduitsModelData, hiroiMasterModels, scale, PullBoxRouteManager.DefaultPullBoxLabel, positionLabel, true ) ;
      }

      #endregion

      #region Change Representative Route Name

      if ( ! parentAndChildRoute.Any() ) return executeResultValue ;
      using Transaction transactionChangeRepresentativeRouteName = new( document ) ;
      transactionChangeRepresentativeRouteName.Start( "Change Representative Route Name" ) ;
      foreach ( var (parentRouteName, childRouteNames ) in parentAndChildRoute ) {
        var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => childRouteNames.Contains(c.GetRouteName()! ) ).ToList() ;
        foreach ( var conduit in conduits ) {
          conduit.TrySetProperty( RoutingParameter.RepresentativeRouteName, parentRouteName ) ;
        }
      }

      transactionChangeRepresentativeRouteName.Commit() ;

      #endregion
      
      return executeResultValue ;
    }
    
    private static bool IsRouteBetweenPowerConnectors( IEnumerable<Element> listConnectors, IReadOnlyCollection<RegistrationOfBoardDataModel> registrationOfBoardDataModels )
    {
      var powerConnectors = listConnectors.Where( IsPowerConnector ).ToList() ;

      if ( powerConnectors.Count <= 1 ) return false ;

      var boardConnectors = new List<Element>() ;
      foreach ( var element in powerConnectors ) {
        element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCodeOfConnector ) ;
        if ( string.IsNullOrEmpty( ceedCodeOfConnector ) ) continue ;
        var registrationOfBoardDataModel = registrationOfBoardDataModels.FirstOrDefault( x =>
          x.AutoControlPanel == ceedCodeOfConnector || x.SignalDestination == ceedCodeOfConnector ) ;
        if ( registrationOfBoardDataModel == null ) continue ;
        boardConnectors.Add( element ) ;
      }

      return boardConnectors.Count > 1 ;
    }

    private static bool IsPowerConnector( Element element )
    {
      if ( element is not FamilyInstance familyInstance ) return false ;
      if ( familyInstance.GetConnectorFamilyType() is not { } connectorFamilyType ) return false ;
      return connectorFamilyType == ConnectorFamilyType.Power ;
    }
  }
}