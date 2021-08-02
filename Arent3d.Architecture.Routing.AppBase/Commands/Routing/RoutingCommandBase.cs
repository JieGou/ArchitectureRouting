using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class RoutingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var executor = new RoutingExecutor( document, GetFittingSizeCalculator(), commandData.View ) ;

      IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? segments ;
      try {
        segments = GetRouteSegmentsParallelToTransaction( uiDocument ) ;
        if ( null == segments ) return Result.Cancelled ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }

      try {
        var result = document.TransactionGroup( GetTransactionNameKey().GetAppStringByKeyOrDefault( "Routing" ), _ =>
        {
          var executionResult = GenerateRoutes( uiDocument, executor, segments ) ;
          if ( RoutingExecutionResultType.Cancel == executionResult.Type ) return Result.Cancelled ;
          if ( RoutingExecutionResultType.Failure == executionResult.Type ) return Result.Failed ;

          AppendParametersToRevitGeneratedElements( uiDocument, executor, executionResult ) ;

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

    protected virtual IFittingSizeCalculator GetFittingSizeCalculator() => DefaultFittingSizeCalculator.Instance ;

    private RoutingExecutionResult GenerateRoutes( UIDocument uiDocument, RoutingExecutor executor, IAsyncEnumerable<(string RouteName, RouteSegment Segment)> segments )
    {
      return uiDocument.Document.Transaction( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ), transaction =>
      {
        SetupFailureHandlingOptions( transaction, executor ) ;

        segments = segments.Concat( GetRouteSegmentsInTransaction( uiDocument ).EnumerateAll().ToAsyncEnumerable() ) ;

        var tokenSource = new CancellationTokenSource() ;
        var task = Task.Run( async () =>
        {
          using var progress = ProgressBar.ShowWithNewThread( tokenSource ) ;
          progress.Message = "Routing..." ;
          return await executor.Run( segments, progress ) ;
        }, tokenSource.Token ) ;
        task.ConfigureAwait( false ) ;
        ThreadDispatcher.WaitWithDoEvents( task ) ;

        if ( task.IsCanceled ) return ( Result.Cancelled, RoutingExecutionResult.Cancel ) ;

        var result = task.Result ;
        return result.Type switch
        {
          RoutingExecutionResultType.Success => ( Result.Succeeded, result ),
          RoutingExecutionResultType.Cancel => ( Result.Cancelled, result ),
          _ => ( Result.Failed, result ),
        } ;
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
      transaction.SetFailureHandlingOptions( ModifyFailureHandlingOptions( transaction.GetFailureHandlingOptions(), executor ) ) ;
    }

    private static FailureHandlingOptions ModifyFailureHandlingOptions( FailureHandlingOptions handlingOptions, RoutingExecutor executor )
    {
      var failuresPreprocessor = new RoutingFailuresPreprocessor( executor ) ;

      handlingOptions = handlingOptions.SetFailuresPreprocessor( failuresPreprocessor ) ;

      return handlingOptions ;
    }

    protected abstract string GetTransactionNameKey() ;

    /// <summary>
    /// Collects route segments to be auto-routed (parallel to transaction).
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
    }

    /// <summary>
    /// Collects route segments to be auto-routed (in transaction).
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      return Enumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
    }


    private class RoutingFailuresPreprocessor : IFailuresPreprocessor
    {
      private readonly RoutingExecutor _executor ;
      
      public RoutingFailuresPreprocessor( RoutingExecutor executor )
      {
        _executor = executor ;
      }

      public FailureProcessingResult PreprocessFailures( FailuresAccessor failuresAccessor )
      {
        var document = failuresAccessor.GetDocument() ;

        var elementsToDelete = new HashSet<ElementId>() ;
        foreach ( var failure in failuresAccessor.GetFailureMessages() ) {
          foreach ( var elmId in failure.GetFailingElementIds() ) {
            if ( document.GetElementById<MEPCurve>( elmId ) is null ) continue ;
            elementsToDelete.Add( elmId ) ;
          }
        }

        if ( 0 < elementsToDelete.Count ) {
          elementsToDelete.ForEach( _executor.RegisterDeletedPipe ) ;
          failuresAccessor.DeleteElements( elementsToDelete.ToList() ) ;

          return FailureProcessingResult.ProceedWithCommit ;
        }
        else {
          return FailureProcessingResult.Continue ;
        }
      }
    }
  }
}