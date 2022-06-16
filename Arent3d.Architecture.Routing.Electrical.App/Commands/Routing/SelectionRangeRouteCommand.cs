﻿using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.EndPoints ;
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
    
    protected override void CreatePullBoxAfterRouteGenerated( Document document, RoutingExecutor executor, IReadOnlyCollection<Route> executeResultValue )
    {
      using var progress = ShowProgressBar( "Routing...", false ) ;
      var routeNames = executeResultValue.Select( r => r.RouteName ).Distinct().ToHashSet() ;
      while ( true ) {
        var segments = PullBoxRouteManager.GetSegmentsWithPullBox( document, executeResultValue ) ;
        if ( ! segments.Any() ) break ;
        using Transaction transaction = new( document ) ;
        transaction.Start( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ) ) ;
        try {
          var result = executor.Run( segments, progress ) ;
          executeResultValue = result.Value.Where( r => routeNames.Contains( r.RouteName ) ).ToList() ;
        }
        catch {
          break ;
        }
        transaction.Commit() ;
      }
    }
  }
}