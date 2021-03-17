using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI.Forms ;
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

      using var transaction = new Transaction( document, GetTransactionNameKey().GetAppStringByKeyOrDefault( "Routing" ) ) ;
      SetupFailureHandlingOptions( transaction ) ;
      try {
        transaction.Start() ;

        segments = segments.Concat( GetRouteSegmentsInTransaction( uiDocument ) ) ;

        var tokenSource = new CancellationTokenSource() ;
        using var progress = ProgressBar.Show( tokenSource ) ;

        var task = Task.Run( () => executor.Run( segments, progress ), tokenSource.Token ) ;
        task.ConfigureAwait( false ) ;
        ThreadDispatcher.WaitWithDoEvents( task ) ;

        if ( task.IsCanceled || RoutingExecutionResult.Cancel == task.Result ) {
          transaction.RollBack() ;
          return Result.Cancelled ;
        }
        else if ( RoutingExecutionResult.Success == task.Result ) {
          transaction.Commit() ;

          if ( executor.HasBadConnectors ) {
            CommandUtils.AlertBadConnectors( executor.GetBadConnectorSet() ) ;
          }

          return Result.Succeeded ;
        }
        else {
          transaction.RollBack() ;
          return Result.Failed ;
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        transaction.RollBack() ;
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        TaskDialog.Show( "error", e.ToString() ) ;
        transaction.RollBack() ;
        return Result.Failed ;
      }
    }

    private static void SetupFailureHandlingOptions( Transaction transaction )
    {
      transaction.SetFailureHandlingOptions( ModifyFailureHandlingOptions( transaction.GetFailureHandlingOptions() ) ) ;
    }

    private static FailureHandlingOptions ModifyFailureHandlingOptions( FailureHandlingOptions handlingOptions )
    {
      handlingOptions = handlingOptions.SetFailuresPreprocessor( new RoutingFailuresPreprocessor() ) ;
      
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
          failuresAccessor.DeleteElements( elementsToDelete.ToList() ) ;
          TaskDialog.Show( "Dialog.Commands.Routing.Dialog.Title.Error".GetAppStringByKeyOrDefault( null ), "Dialog.Commands.Routing.Common.Dialog.Body.Error.DeletedSomeFailedElements".GetAppStringByKeyOrDefault( null ) ) ;

          return FailureProcessingResult.ProceedWithCommit ;
        }
        else {
          return FailureProcessingResult.Continue ;
        }
      }
    }
  }
}