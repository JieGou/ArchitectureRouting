﻿using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
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

    protected override DialogInitValues CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo )
    {
      var curveType = RouteMEPSystem.GetMEPCurveType( document, new[] { connector }, null ) ;

      return new DialogInitValues( classificationInfo, RouteMEPSystem.GetSystemType( document, connector ), curveType, connector.GetDiameter() ) ;
    }

    protected override string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType )
    {
      return curveType.Category.Name ;
    }

    protected override MEPSystemClassificationInfo GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType )
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
    
    protected override IReadOnlyCollection<Route> CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue, SelectState selectState )
    {
      if ( ! PullBoxRouteManager.IsPullBoxDisPlaySettingEnabled( document ) ) return executeResultValue ;
      
      using var progress = ShowProgressBar( "Routing...", false ) ;
      List<string> boards = new() ;
      List<XYZ> pullBoxPositions = new() ;
      List<(FamilyInstance, XYZ?)> pullBoxElements = new() ;
      var resultRoute = executeResultValue.ToList() ;
      var parentIndex = 1 ;
      Dictionary<string, List<string>> parentAndChildRoute = new() ;
      for ( int i = 0 ; i < 50 ; i++ ) {
        var segments = PullBoxRouteManager.GetSegmentsWithPullBox( document, resultRoute, boards, pullBoxPositions, pullBoxElements, ref parentIndex, ref parentAndChildRoute ) ;
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
      }
      
      PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, pullBoxElements ) ;

      #region Change Representative Route Name

      if ( ! parentAndChildRoute.Any() ) return resultRoute ;
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

      return resultRoute ;
    }
  }
}