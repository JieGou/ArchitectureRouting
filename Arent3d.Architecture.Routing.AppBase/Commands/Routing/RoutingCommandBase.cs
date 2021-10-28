using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class RoutingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var executor = CreateRoutingExecutor( document, commandData.View ) ;

      object? state ;
      try {
        bool success ;
        ( success, state ) = OperateUI( uiDocument, executor ) ;
        if ( false == success && state is string ) {
          TaskDialog.Show( "Error Message", state as string ) ;
          return Result.Cancelled ;
        }
        else if ( false == success ) {
          return Result.Cancelled ;
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }

      try {
        var result = document.TransactionGroup( GetTransactionNameKey().GetAppStringByKeyOrDefault( "Routing" ), _ =>
        {
          var executionResult = GenerateRoutes( document, executor, state ) ;
          if ( RoutingExecutionResultType.Cancel == executionResult.Type ) return Result.Cancelled ;
          if ( RoutingExecutionResultType.Failure == executionResult.Type ) return Result.Failed ;

          var selectState = state as SelectionRangeRouteCommandBase.SelectState ;
          if ( selectState != null ) {
            ConnectorPicker.IPickResult fromPickResult ;
            ConnectorPicker.IPickResult toPickResult ;
            var routes = executionResult.GeneratedRoutes ;
            var routeName = routes.First().RouteName ;
            List<XYZ> passPoints = new List<XYZ>() ;
            foreach ( var route in routes.ToSegmentsWithName() ) {
              passPoints.Add( route.Segment.ToEndPoint.RoutingStartPosition ) ;
            }
            var passPoint = passPoints.First() ;

            foreach ( var sensorConnector in selectState.SensorConnectors ) {
              toPickResult = ConnectorPicker.GetConnector( uiDocument, executor, sensorConnector, false ) ;

              var conduit = SelectCenterConduitIndex( document, routeName, passPoint ) ;
              fromPickResult = ConnectorPicker.GetConnector( uiDocument, executor, conduit, false, sensorConnector ) ;

              var pickState = new PickRoutingCommandBase.PickState( fromPickResult, toPickResult, selectState.PropertyDialog, selectState.ClassificationInfo ) ;
              var routingResult = GenerateRoutes( document, executor, pickState ) ;
              if ( RoutingExecutionResultType.Cancel == routingResult.Type ) return Result.Cancelled ;
              if ( RoutingExecutionResultType.Failure == routingResult.Type ) return Result.Failed ;

              routes = routingResult.GeneratedRoutes ;
              passPoint = FindPassPoint( routes, routeName, passPoints ) ;
            }
          }

          return Result.Succeeded ;
        } ) ;

        if ( Result.Succeeded == result ) {
          if ( executor.HasDeletedElements ) {
            CommandUtils.AlertDeletedElements() ;
          }

          if ( executor.HasBadConnectors ) {
            CommandUtils.AlertBadConnectors( executor.GetBadConnectorSet() ) ;
          }
        }

        return result ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract RoutingExecutor CreateRoutingExecutor( Document document, View view ) ;

    private RoutingExecutionResult GenerateRoutes( Document document, RoutingExecutor executor, object? state )
    {
      return document.Transaction( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ), transaction =>
      {
        using var _ = FromToTreeManager.SuppressUpdate() ;

        SetupFailureHandlingOptions( transaction, executor ) ;

        try {
          using var progress = ProgressBar.ShowWithNewThread( new CancellationTokenSource() ) ;
          progress.Message = "Routing..." ;

          var segments = GetRouteSegments( document, state ) ;
          var result = executor.Run( segments, progress ) ;

          if ( RoutingExecutionResultType.Success == result.Type ) {
            executor.RunPostProcess( result ) ;
          }

          return result.Type switch
          {
            RoutingExecutionResultType.Success => ( Result.Succeeded, result ),
            RoutingExecutionResultType.Cancel => ( Result.Cancelled, result ),
            _ => ( Result.Failed, result ),
          } ;
        }
        catch ( OperationCanceledException ) {
          return ( Result.Cancelled, RoutingExecutionResult.Cancel ) ;
        }
      }, RoutingExecutionResult.Cancel ) ;
    }


    private static void SetupFailureHandlingOptions( Transaction transaction, RoutingExecutor executor )
    {
      if ( executor.CreateFailuresPreprocessor() is not { } failuresPreprocessor ) return ;
      
      transaction.SetFailureHandlingOptions( ModifyFailureHandlingOptions( transaction.GetFailureHandlingOptions(), failuresPreprocessor ) ) ;
    }

    private static FailureHandlingOptions ModifyFailureHandlingOptions( FailureHandlingOptions handlingOptions, IFailuresPreprocessor failuresPreprocessor )
    {
      return handlingOptions.SetFailuresPreprocessor( failuresPreprocessor ) ;
    }

    protected abstract string GetTransactionNameKey() ;

    /// <summary>
    /// Collects UI states.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor ) => ( true, null ) ;

    /// <summary>
    /// Generate route segments to be auto-routed from UI state.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      return Array.Empty<(string RouteName, RouteSegment Segment)>() ;
    }

    private Conduit SelectCenterConduitIndex( Document document, string routeName, XYZ passPoint )
    {
      var conduits = document.GetAllElementsOfRouteName<Conduit>( routeName ) ;
      var centerConduit = conduits.First() ;
      foreach ( var conduit in conduits ) {
        var location = ( conduit.Location as LocationCurve )! ;
        var line = ( location.Curve as Line )! ;
        var conduitEndPoint = line.GetEndPoint( 0 ) ;
        if ( passPoint.DistanceTo( conduitEndPoint ) == 0 ) {
          centerConduit = conduit ; 
          break ;
        }
      }

      return centerConduit ;
    }

    private XYZ FindPassPoint( IReadOnlyCollection<Route> routes, string routeName, List<XYZ> passPoints )
    {
      XYZ passPoint = XYZ.Zero ;
      var routeSegment = routes.ToSegmentsWithName().Where( x => x.RouteName == routeName ) ;
      foreach ( var route in routeSegment ) {
        var toEndPoint = route.Segment.ToEndPoint ;
        var position = toEndPoint.RoutingStartPosition ;
        if ( toEndPoint is PassPointEndPoint && passPoints.FirstOrDefault( point => point.X == position.X && point.Y == position.Y && point.Z == position.Z ) == null ) {
          passPoints.Add( position ) ;
          passPoint = position ;
        }
      }

      return passPoint ;
    }
  }
}