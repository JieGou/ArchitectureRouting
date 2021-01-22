using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using CsvHelper ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Routing Assist" )]
  [Image( "resources/MEP.ico" )]
  public class RouteCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var executor = new RoutingExecutor( document ) ;

      var routeRecords = ReadRouteRecords() ;
      if ( null == routeRecords ) return Result.Cancelled ;

      var collector = new TestCollisionTargetCollector( document ) ;

      using var transaction = new Transaction( document, "Routing Assist" ) ;
      try {
        transaction.Start() ;

        var tokenSource = new CancellationTokenSource() ;
        using var progress = Forms.ProgressBar.Show( tokenSource ) ;

        var task = Task.Run( () => executor.Run( routeRecords, collector, progress ), tokenSource.Token ) ;
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
      catch ( Exception e ) {
        TaskDialog.Show( "error", e.ToString() ) ;
        transaction.RollBack() ;
        return Result.Failed ;
      }
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
    private static IAsyncEnumerable<RouteRecord>? ReadRouteRecords()
    {
#if true
      return ReadRouteRecordsForDebug() ;
#else
      return ReadRouteRecordsFromFile() ;
#endif
    }

    /// <summary>
    /// Returns hard-coded sample from-to records.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    private static async IAsyncEnumerable<RouteRecord> ReadRouteRecordsForDebug()
    {
      await Task.Delay( 0 ) ; // allow AsyncEnumerable
      //yield return new RouteRecord( "TestRoute1", new ConnectorIds( 17299721, 3 ), new ConnectorIds( 17299722, 4 ) ) ;
      //yield return new RouteRecord( "TestRoute1", new ConnectorIds( 17299721, 3 ), new ConnectorIds( 17299684, 4 ) ) ;
      //yield return new RouteRecord( "TestRoute2", new ConnectorIds( 17299721, 2 ), new ConnectorIds( 17299722, 1 ) ) ;

      yield return new RouteRecord( "TestRoute3", new ConnectorIndicator( 17299723, 3 ), new ConnectorIndicator( 17299685, 4 ), 17299574 ) ;

      //yield return new RouteRecord( "Rectangular", new ConnectorIds( 18208920, 8 ), new ConnectorIds( 18208786, 8 ) ) ;
      //yield return new RouteRecord( "Rectangular", new ConnectorIds( 18208920, 8 ), new ConnectorIds( 18208786, 8 ) ) ;
    }

    /// <summary>
    /// Collects collision check targets for debug.
    /// </summary>
    private class TestCollisionTargetCollector : ICollisionCheckTargetCollector
    {
      private readonly Document _document ;

      public TestCollisionTargetCollector( Document document )
      {
        _document = document ;
      }

      public IEnumerable<FamilyInstance> GetCollisionCheckTargets()
      {
        foreach ( var instance in _document.GetAllElements<FamilyInstance>() ) {
          if ( instance.Id.IntegerValue == 18204914 || instance.Id.IntegerValue == 18205151 || instance.Id.IntegerValue == 17299574 ) continue ;

          yield return instance ;
        }
      }

      public bool IsTargetGeometryElement( GeometryElement gElm )
      {
        var (min, max) = gElm.GetBoundingBox().To3d() ;

        if ( min.z < 30 || 60 < max.z ) return false ;
        if ( min.x < -20 || 100 < max.x ) return false ;
        if ( min.y < -20 || 100 < max.y ) return false ;

        return true ;
      }
    }

    /// <summary>
    /// Reads from-to records from a CSV file which user selected.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    private static IAsyncEnumerable<RouteRecord>? ReadRouteRecordsFromFile()
    {
      var csvFileName = OpenFromToCsv() ;
      if ( null == csvFileName ) return null ;

      using var reader = new StreamReader( csvFileName, true ) ;
      using var csv = new CsvReader( reader, CultureInfo.CurrentCulture ) ;
      if ( false == csv.Read() ) return null ;
      csv.ReadHeader() ;

      return ReadRouteRecordsFromFile( csv ) ;
    }

    private static async IAsyncEnumerable<RouteRecord> ReadRouteRecordsFromFile( CsvReader csv )
    {
      while ( await csv.ReadAsync() ) {
        var routeFields = RouteParser.ParseFields( csv ) ;
        if ( null == routeFields ) continue ;

        yield return routeFields.Value ;
      }
    }

    private static string? OpenFromToCsv()
    {
      using var dlg = new FileOpenDialog( "Routing from-to list (*.csv)|*.csv" ) { Title = "Open from-to list file" } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return null ;

      return ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ;
    }
  }
}