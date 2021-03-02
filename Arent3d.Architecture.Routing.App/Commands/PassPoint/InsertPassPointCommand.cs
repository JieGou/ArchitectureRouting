using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.App.Commands.PassPoint
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Insert Pass Point" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  public class InsertPassPointCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      PointOnRoutePicker.PickInfo pickInfo ;
      try {
        pickInfo = PointOnRoutePicker.PickRoute( uiDocument, true, "Pick a point on a route." ) ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }

      var executor = new RoutingExecutor( document, commandData.View ) ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Insert Pass Point" ) ;
      try {
        var elm = InsertPassPointElement( document, pickInfo ) ;
        var routeRecords = GetNewSegmentList( pickInfo.SubRoute, pickInfo.Element, elm.Id.IntegerValue ).ToSegmentsWithName( pickInfo.SubRoute.Route.RouteName ) ;

        var tokenSource = new CancellationTokenSource() ;
        using var progress = ProgressBar.Show( tokenSource ) ;

        var task = Task.Run( () => executor.Run( routeRecords.ToAsyncEnumerable(), progress ), tokenSource.Token ) ;
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
          return Result.Failed ;
        }
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      finally {
        if ( transaction.HasStarted() ) {
          transaction.RollBack() ;
        }
      }
    }

    private static FamilyInstance InsertPassPointElement( Document document, PointOnRoutePicker.PickInfo pickInfo )
    {
      return document.AddPassPoint( pickInfo.Route.RouteName, pickInfo.Position, pickInfo.RouteDirection, pickInfo.Radius ) ;
    }

    private static IEnumerable<RouteSegment> GetNewSegmentList( SubRoute subRoute, Element insertingElement, int passPointId )
    {
      var detector = new RouteSegmentDetector( subRoute, insertingElement ) ;
      var passPoint = new PassPointEndIndicator( passPointId ) ;
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( detector.IsPassingThrough( segment ) ) {
          // split segment
          yield return new RouteSegment( segment.FromId, passPoint, segment.PreferredNominalDiameter ) ;
          yield return new RouteSegment( passPoint, segment.ToId, segment.PreferredNominalDiameter ) ;
        }
        else {
          yield return segment ;
        }
      }
    }
  }
}