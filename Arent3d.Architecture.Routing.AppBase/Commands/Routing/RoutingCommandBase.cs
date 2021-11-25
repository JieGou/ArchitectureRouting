using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
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

      var executor = CreateRoutingExecutor( document, commandData.View ) ;

      object? state ;
      try {
        bool success ;
        ( success, state ) = OperateUI( uiDocument, executor ) ;
        if ( false == success && state is string mes ) {
          message = mes ;
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

          // Avoid Revit bugs about reducer insertion.
          FixReducers( document, executor, executionResult.GeneratedRoutes ) ;

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

    private static void FixReducers( Document document, RoutingExecutor executor, IEnumerable<Route> routes )
    {
      document.Transaction( "TransactionName.Commands.Routing.Common.Routing".GetAppStringByKeyOrDefault( "Routing" ), transaction =>
      {
        using var _ = FromToTreeManager.SuppressUpdate() ;

        SetupFailureHandlingOptions( transaction, executor ) ;

        var routeNames = routes.Select( route => route.Name ).ToHashSet() ;
        foreach ( var curve in document.GetAllElements<MEPCurve>().Where( curve => curve.GetRouteName() is { } routeName && routeNames.Contains( routeName ) ) ) {
          FixCurveReducers( curve ) ;
        }

        return ( Result.Succeeded, null! ) ;
      }, (RoutingExecutionResult)null! ) ;

      static void FixCurveReducers( MEPCurve curve )
      {
        // Avoid Revit bugs about reducer insertion.
        foreach ( var connector in curve.GetConnectors().OfEnd() ) {
          var anotherConnectors = connector.GetConnectedConnectors().OfEnd().EnumerateAll() ;
          if ( 1 != anotherConnectors.Count ) continue ;

          var anotherConnector = anotherConnectors.First() ;
          if ( connector.HasSameShapeAndParameters( anotherConnector ) ) continue ;

          if ( false == ShakeShape( connector, anotherConnector ) ) continue ;
          return ;  // done
        }
      }

      static bool ShakeShape( Connector connector, Connector anotherConnector )
      {
        switch ( connector.Shape ) {
          case ConnectorProfileType.Oval :
          case ConnectorProfileType.Round :
          {
            var orgRadius = connector.Radius ;
            connector.Radius = anotherConnector.Radius ;
            connector.Radius = orgRadius ;
            return true ;
          }
          case ConnectorProfileType.Rectangular :
          {
            var orgWidth = connector.Width ;
            var orgHeight = connector.Height ;
            connector.Width = anotherConnector.Width ;
            connector.Height = anotherConnector.Height ;
            connector.Width = orgWidth ;
            connector.Height = orgHeight ;
            return true ;
          }
          default : return false ;
        }
      }
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