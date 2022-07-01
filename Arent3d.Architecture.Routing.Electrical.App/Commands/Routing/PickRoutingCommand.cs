using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

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
      using var progress = ShowProgressBar( "Routing...", false ) ;
      List<string> boards = new() ;
      List<XYZ> pullBoxPositions = new() ;
      List<(FamilyInstance, XYZ)> pullBoxElements = new() ;
      var resultRoute = executeResultValue.ToList() ;
      var parentIndex = 1 ;
      while ( true ) {
        var isPassedShaft = executeResultValue.SingleOrDefault( e => e.UniqueShaftElementUniqueId != null ) != null ;
        var segments = isPassedShaft ? PullBoxRouteManager.GetSegmentsWithPullBoxShaft( document, executeResultValue, pullBoxPositions, pullBoxElements, ref parentIndex ) : PullBoxRouteManager.GetSegmentsWithPullBox( document, resultRoute, boards, pullBoxPositions, pullBoxElements, ref parentIndex ) ;
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
        if ( isPassedShaft ) break ;
      }
      
      #region Change dimension of pullbox and set new label
      
      var detailSymbolStorable = document.GetDetailSymbolStorable() ;
      var pullBoxInfoStorable = document.GetPullBoxInfoStorable() ;
      var csvStorable = document.GetCsvStorable() ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
      var scale = ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;

      foreach ( var pullBoxElement in pullBoxElements ) {
        var (pullBox, position) = pullBoxElement ;
        var positionLabel = new XYZ( position.X + 0.2, position.Y + 0.5, position.Z ) ;
        PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, pullBox, csvStorable, detailSymbolStorable, pullBoxInfoStorable,
          conduitsModelData, hiroiMasterModels, scale, PullBoxRouteManager.DefaultPullBoxLabel, positionLabel, true ) ;
      }

      #endregion
      
      // Reroute after calculate new dimension for pull box
      // executeResultValue = PullBoxRoutingCommandBase.ExecuteReRoute( document, executor, progress, resultRoute ) ;

      return executeResultValue ;
    }
  }
}