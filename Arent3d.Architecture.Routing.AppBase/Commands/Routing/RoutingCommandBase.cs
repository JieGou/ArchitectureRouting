using System ;
using System.Collections.Generic ;
using System.Threading ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class RoutingCommandBase : IExternalCommand
  {
    public record SelectRangeState( SelectionRangeRouteCommandBase.SelectState SelectState, IReadOnlyCollection<Route> Routes) ;
    
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

          AppendParametersToRevitGeneratedElements( uiDocument, executor, executionResult ) ;

          // var selectState = state as SelectionRangeRouteCommandBase.SelectState ?? throw new InvalidOperationException() ;
          // if ( selectState != null ) {
          //   var routes = executionResult.GeneratedRoutes ;
          //   var selectRangeState = new SelectRangeState( selectState, routes ) ;
          //   var routingResult = GenerateRoutes( document, executor, selectRangeState ) ;
          //
          //   AppendParametersToRevitGeneratedElements( uiDocument, executor, routingResult ) ;
          // }
          
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

    private void AppendParametersToRevitGeneratedElements( UIDocument uiDocument, RoutingExecutor executor, RoutingExecutionResult result )
    {
      uiDocument.Document.Transaction( "TransactionName.Commands.Routing.Common.SetupParameters".GetAppStringByKeyOrDefault( "Setup Parameter" ), _ =>
      {
        executor.RunPostProcess( result ) ;
        return Result.Succeeded ;
      } ) ;
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
  }
}