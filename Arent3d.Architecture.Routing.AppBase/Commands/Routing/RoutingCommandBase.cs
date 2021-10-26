using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
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
        if ( false == success ) return Result.Cancelled ;
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
            var conduits = document.GetAllElementsOfRouteName<Conduit>( routes.First().RouteName )  ;
            var index = SelectCenterConduitIndex( conduits, selectState.SensorConnectors.ElementAt( 0 ) ) ;

            foreach ( var sensorConnector in selectState.SensorConnectors ) {
              toPickResult = ConnectorPicker.GetConnector( uiDocument, executor, sensorConnector, false ) ;
              
              conduits = document.GetAllElementsOfRouteName<Conduit>( routes.First().RouteName )  ;
              fromPickResult = ConnectorPicker.GetConnector( uiDocument, executor, conduits.ElementAt( index ), false ) ;
              
              var pickState = new PickRoutingCommandBase.PickState( fromPickResult, toPickResult, selectState.PropertyDialog, selectState.ClassificationInfo ) ;
              var routingResult = GenerateRoutes( document, executor, pickState ) ;
              if ( RoutingExecutionResultType.Cancel == routingResult.Type ) return Result.Cancelled ;
              if ( RoutingExecutionResultType.Failure == routingResult.Type ) return Result.Failed ;
              
              index += 1 ;
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

    private int SelectCenterConduitIndex( IEnumerable<Conduit> conduits, Element? sensorConnector )
    {
      double minDistance = 1000 ;
      var index = 0 ;
      var count = 0 ;
      foreach ( var conduit in conduits ) {
        var location = ( conduit.Location as LocationCurve )! ;
        var line = ( location.Curve as Line )! ;
        var distance = sensorConnector!.GetTopConnectors().Origin.DistanceTo( line.Origin ) ;
        if ( distance < minDistance ) {
          minDistance = distance ;
          index = count ;
        }

        count++ ;
      }

      return index ;
    }
  }
}