using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.SelectionRangeRouteCommand", DefaultString = "Selection Range\nRoute" )]
  [Image( "resources/RerouteAll.png" )]
  public class SelectionRangeRouteCommand : SelectionRangeRouteCommandBase
  {
    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.SelectionRangeRoute" ;
    }

    protected override AddInType GetAddInType()
    {
      return AppCommandSettings.AddInType ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    protected override DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType )
    {
      return curveType.Category.Name ;
    }

    protected override MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
    {
      return MEPSystemClassificationInfo.CableTrayConduit ;
    }

    protected override (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom )
    {
      return PickCommandUtil.CreateBranchingRouteEndPoint( newPickResult, anotherPickResult, routeProperty, classificationInfo, AppCommandSettings.FittingSizeCalculator, newPickIsFrom ) ;
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, SelectState selectState )
    {
      ElectricalCommandUtil.SetPropertyForCable( document, executeResultValue ) ;
    }
    
    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue )
    {
      using var progress = ShowProgressBar( "Routing...", false ) ;
      List<string> boards = new() ;
      List<XYZ> pullBoxPositions = new() ;
      List<(FamilyInstance, XYZ)> pullBoxElements = new List<(FamilyInstance, XYZ)>() ;
      while ( true ) {
        var segments = PullBoxRouteManager.GetSegmentsWithPullBox( document, executeResultValue, boards, pullBoxPositions, pullBoxElements) ;
        if ( ! segments.Any() ) break ;
        using Transaction transaction = new( document ) ;
        transaction.Start( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ) ) ;
        var failureOptions = transaction.GetFailureHandlingOptions() ;
        failureOptions.SetFailuresPreprocessor( new PullBoxRouteManager.FailurePreprocessor() ) ;
        transaction.SetFailureHandlingOptions( failureOptions ) ;
        try {
          var newRouteNames = segments.Select( s => s.RouteName ).Distinct().ToHashSet() ;
          var oldRoutes = executeResultValue.Where( r => ! newRouteNames.Contains( r.RouteName ) ) ;
          var result = executor.Run( segments, progress ) ;
          executeResultValue = result.Value ;
          foreach ( var oldRoute in oldRoutes ) {
            executeResultValue.ToList().Add( oldRoute ) ;
          }
        }
        catch {
          break ;
        }

        transaction.Commit( failureOptions ) ;
      }
      
      #region Change dimension of pullbox and set new label
      foreach ( var pullBoxElement in pullBoxElements ) {
        var (pullBox, position) = pullBoxElement ;
        var detailSymbolStorable = document.GetDetailSymbolStorable() ;
      
        string buzaiCd = string.Empty ;
        string textLabel = PullBoxRouteManager.DefaultPullBoxLabel ;
        var csvStorable = document.GetCsvStorable() ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        var pullBoxModel = PullBoxRouteManager.GetPullBoxWithAutoCalculatedDimension( document, pullBox, csvStorable, detailSymbolStorable, conduitsModelData, hiroiMasterModels ) ;
        if ( pullBoxModel != null ) {
          buzaiCd = pullBoxModel.Buzaicd;
          var (depth, width, _) =  PullBoxRouteManager.ParseKikaku( pullBoxModel.Kikaku );
          textLabel = PullBoxRouteManager.GetPullBoxTextBox( depth, width, PullBoxRouteManager.DefaultPullBoxLabel ) ;
        }
      
        if ( ! string.IsNullOrEmpty( buzaiCd ) ) {
          using Transaction t1 = new(document, "Update dimension of pull box") ;
          t1.Start() ;
          pullBox.ParametersMap.get_Item( PickUpViewModel.MaterialCodeParameter )?.Set( buzaiCd ) ;
          detailSymbolStorable.DetailSymbolModelData.RemoveAll( _ => true) ;
          t1.Commit() ;
          
          using Transaction t2 = new( document, "Create text note" ) ;
          t2.Start() ;
          XYZ? positionLabel = new XYZ( position.X + 0.2, position.Y + 0.5, position.Z ) ;
          
          PullBoxRouteManager.CreateTextNoteAndGroupWithPullBox( document, positionLabel , pullBox, textLabel );
          t2.Commit() ;
        }
      }
      #endregion

      return executeResultValue ;
    }
  }
}