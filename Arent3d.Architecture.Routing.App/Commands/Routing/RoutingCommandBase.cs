using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  public abstract class RoutingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      var executor = new RoutingExecutor( document, commandData.View ) ;

      IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? segments ;
      try {
        segments = GetRouteSegmentsBeforeTransaction( uiDocument ) ;
        if ( null == segments ) return Result.Cancelled ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }

      using var transactionGroup = new TransactionGroup( document ) ;
      try {
        transactionGroup.Start( GetTransactionNameKey().GetAppStringByKeyOrDefault( "Routing" ) ) ;

        var result = GenerateRoutes( uiDocument, executor, segments ) ;
        if ( RoutingExecutionResultType.Cancel == result.Type ) {
          transactionGroup.RollBack() ;
          return Result.Cancelled ;
        }
        else if ( RoutingExecutionResultType.Failure == result.Type ) {
          transactionGroup.RollBack() ;
          return Result.Failed ;
        }

        AppendParametersToRevitGeneratedElements( uiDocument, executor, result ) ;

        transactionGroup.Commit() ;

        if ( executor.HasDeletedElements ) {
          CommandUtils.AlertDeletedElements() ;
        }
        if ( executor.HasBadConnectors ) {
          CommandUtils.AlertBadConnectors( executor.GetBadConnectorSet() ) ;
        }

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        transactionGroup.RollBack() ;
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        TaskDialog.Show( "error", e.ToString() ) ;
        transactionGroup.RollBack() ;
        return Result.Failed ;
      }
    }

    private RoutingExecutionResult GenerateRoutes( UIDocument uiDocument, RoutingExecutor executor, IAsyncEnumerable<(string RouteName, RouteSegment Segment)> segments )
    {
      using var transaction = new Transaction( uiDocument.Document ) ;
      SetupFailureHandlingOptions( transaction, executor ) ;
      try {
        transaction.Start( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ) ) ;

        segments = segments.Concat( GetRouteSegmentsInTransaction( uiDocument ) ) ;

        var tokenSource = new CancellationTokenSource() ;
        using var progress = ProgressBar.Show( tokenSource ) ;

        var task = Task.Run( () => executor.Run( segments, progress ), tokenSource.Token ) ;
        task.ConfigureAwait( false ) ;
        ThreadDispatcher.WaitWithDoEvents( task ) ;

        if ( task.IsCanceled ) {
          transaction.RollBack() ;
          return RoutingExecutionResult.Cancel ;
        }

        var result = task.Result ;
        if ( RoutingExecutionResultType.Success != result.Type ) {
          transaction.RollBack() ;
          return result ;
        }

        transaction.Commit() ;
        return result ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }

    private void AppendParametersToRevitGeneratedElements( UIDocument uiDocument, RoutingExecutor executor, RoutingExecutionResult result )
    {
      using var transaction = new Transaction( uiDocument.Document ) ;
      try {
        transaction.Start( "TransactionName.Commands.Routing.Common.SetupParameters".GetAppStringByKeyOrDefault( "Setup Parameter" ) ) ;

        executor.RunPostProcess( result ) ;

        transaction.Commit() ;
      }
      catch{
        transaction.RollBack() ;
        throw ;
      }
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
    /// Collects route segments to be auto-routed (before transaction).
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegmentsBeforeTransaction( UIDocument uiDocument )
    {
      return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
    }

    /// <summary>
    /// Collects route segments to be auto-routed (in transaction).
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      return AsyncEnumerable.Empty<(string RouteName, RouteSegment Segment)>() ;
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