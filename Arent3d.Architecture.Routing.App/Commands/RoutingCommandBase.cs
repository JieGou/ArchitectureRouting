using System ;
using System.Collections.Generic ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  public abstract class RoutingCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      CollectRacks( document, commandData.View ) ;

      var executor = new RoutingExecutor( document ) ;

      IAsyncEnumerable<RouteRecord>? routeRecords ;
      try {
        routeRecords = ReadRouteRecords( commandData.Application.ActiveUIDocument ) ;
        if ( null == routeRecords ) return Result.Cancelled ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }

      using var transaction = new Transaction( document, "Routing Assist" ) ;
      try {
        transaction.Start() ;

        var tokenSource = new CancellationTokenSource() ;
        using var progress = Forms.ProgressBar.Show( tokenSource ) ;

        var task = Task.Run( () => executor.Run( routeRecords, progress ), tokenSource.Token ) ;
        task.ConfigureAwait( false ) ;
        ThreadDispatcher.WaitWithDoEvents( task ) ;

        if ( task.IsCanceled || RoutingExecutionResult.Cancel == task.Result ) {
          transaction.RollBack() ;
          return Result.Cancelled ;
        }
        else if ( RoutingExecutionResult.Success == task.Result ) {
          transaction.Commit() ;

          if ( executor.HasBadConnectors ) {
            AlertBadConnectors( executor.GetBadConnectors() ) ;
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

    private void CollectRacks( Document document, View view )
    {
      var racks = DocumentMapper.Get( document ).RackCollection ;

      racks.Clear() ;
      {
        var connector = document.FindConnector( 17299721, 3 )! ;
        var z = connector.Origin.Z - connector.Radius ;

        foreach ( var familyInstance in GetRackInstances( document ).NonNull() ) {
          var (min, max) = familyInstance.get_BoundingBox( view ).To3d() ;
          min.z = max.z = z ;

          racks.AddRack( new Rack.Rack { Box = new Box3d( min, max ), IsMainRack = true, BeamInterval = 5 } ) ;
        }
      }

      racks.CreateLinkages() ;
    }

    private IEnumerable<FamilyInstance> GetRackInstances( Document document )
    {
      var familySymbol = document.GetFamilySymbol( RoutingFamilyType.RackGuide ) ;
      if ( null == familySymbol ) return Array.Empty<FamilyInstance>() ;

      return document.GetAllFamilyInstances( familySymbol ) ;
    }

    private void AlertBadConnectors( IReadOnlyCollection<Connector> badConnectors )
    {
      TaskDialog.Show( "Connection error", "Some elbows, tees and/or connectors could not be inserted.\n\n・" + string.Join( "\n・", badConnectors.Select( GetConnectorInfo ) ) ) ;
    }

    private static string GetConnectorInfo( Connector connector )
    {
      var count = connector.ConnectorManager.Connectors.Size ;
      return $"[{count switch { 2 => "Elbow", 3 => "Tee", 4 => "Cross", _ => throw new ArgumentException() }}] {connector.Origin}" ;
    }

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected abstract IAsyncEnumerable<RouteRecord>? ReadRouteRecords( UIDocument uiDocument ) ;
  }
}