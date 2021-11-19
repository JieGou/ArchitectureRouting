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
  public abstract class PickFASUAndVAVAutomaticallyCommandBase : IExternalCommand
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
        if ( state is string mes ) {
          message = mes ;
        }

        if ( true == success ) {
          return Result.Succeeded ;
        }
        else {
          return Result.Failed ;
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
    }

    protected abstract RoutingExecutor CreateRoutingExecutor( Document document, View view ) ;

    private RoutingExecutionResult GenerateRoutes( Document document, RoutingExecutor executor, object? state )
    {
      return document.Transaction(
        "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ), transaction =>
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

    private static void SetupFailureHandlingOptions( Transaction transaction, RoutingExecutor executor )
    {
      if ( executor.CreateFailuresPreprocessor() is not { } failuresPreprocessor ) return ;

      transaction.SetFailureHandlingOptions( ModifyFailureHandlingOptions( transaction.GetFailureHandlingOptions(),
        failuresPreprocessor ) ) ;
    }

    private static FailureHandlingOptions ModifyFailureHandlingOptions( FailureHandlingOptions handlingOptions,
      IFailuresPreprocessor failuresPreprocessor )
    {
      return handlingOptions.SetFailuresPreprocessor( failuresPreprocessor ) ;
    }

    /// <summary>
    /// Collects UI states.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual (bool Result, object? State)
      OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor ) =>
      ( true, null ) ;

    /// <summary>
    /// Generate route segments to be auto-routed from UI state.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected virtual IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document,
      object? state )
    {
      return Array.Empty<(string RouteName, RouteSegment Segment)>() ;
    }
  }
}