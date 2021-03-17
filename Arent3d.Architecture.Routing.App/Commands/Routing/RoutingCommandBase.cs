using System ;
using System.Collections.Generic ;
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
      var document = commandData.Application.ActiveUIDocument.Document ;

      var executor = new RoutingExecutor( document, commandData.View ) ;

      IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? segments ;
      try {
        segments = GetRouteSegments( commandData.Application.ActiveUIDocument ) ;
        if ( null == segments ) return Result.Cancelled ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }

      using var transaction = new Transaction( document, GetTransactionNameKey().GetAppStringByKeyOrDefault( "Routing" ) ) ;
      try {
        transaction.Start() ;

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

    protected abstract string GetTransactionNameKey() ;

    /// <summary>
    /// Collects route segments to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected abstract IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegments( UIDocument uiDocument ) ;
  }
}