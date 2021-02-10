using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.Linq ;
using System.Threading ;
using System.Threading.Tasks ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.App.Commands
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

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, "Pick a point on a route." ) ;

      var executor = new RoutingExecutor( document, commandData.View ) ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Insert Pass Point" ) ;
      try {
        var elm = InsertPassPointElement( document, pickInfo ) ;
        var newRecords = CreateNewRouteRecords( pickInfo.SubRoute, pickInfo.Element, elm.Id.IntegerValue ) ;

        var tokenSource = new CancellationTokenSource() ;
        using var progress = Forms.ProgressBar.Show( tokenSource ) ;

        var task = Task.Run( () => executor.Run( newRecords.ToAsyncEnumerable(), progress ), tokenSource.Token ) ;
        task.ConfigureAwait( false ) ;
        ThreadDispatcher.WaitWithDoEvents( task ) ;

        if ( task.IsCanceled || RoutingExecutionResult.Cancel == task.Result ) {
          transaction.RollBack() ;
          return Result.Cancelled ;
        }
        else if ( RoutingExecutionResult.Success == task.Result ) {
          transaction.Commit() ;

          if ( executor.HasBadConnectors ) {
            CommandUtils.AlertBadConnectors( executor.GetBadConnectors() ) ;
          }

          return Result.Succeeded ;
        }
        else {
          transaction.RollBack() ;
          return Result.Failed ;
        }
      }
      catch ( Exception ) {
        transaction.RollBack() ;
        throw ;
      }
    }

    private static FamilyInstance InsertPassPointElement( Document document, PointOnRoutePicker.PickInfo pickInfo )
    {
      var symbol = document.GetFamilySymbol( RoutingFamilyType.PassPoint )! ;
      if ( false == symbol.IsActive ) symbol.Activate() ;

      var position = pickInfo.Position ;
      var direction = pickInfo.RouteDirection ;

      var instance = document.Create.NewFamilyInstance( position, symbol, StructuralType.NonStructural ) ;
      instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;
      instance.LookupParameter( "Arent-RoundDuct-Diameter" ).Set( pickInfo.Radius * 2.0 ) ;

      var elevationAngle = Math.Atan2( direction.Z, Math.Sqrt( direction.X * direction.X + direction.Y * direction.Y ) ) ;
      var rotationAngle = Math.Atan2( direction.Y, direction.X ) ;

      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisY ), -elevationAngle ) ;
      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( position, position + XYZ.BasisZ ), rotationAngle ) ;

      return instance ;
    }

    private static IEnumerable<RouteRecord> CreateNewRouteRecords( SubRoute subRoute, Element insertingElement, int passPointId )
    {
      var inserter = new PassPointInserter( subRoute, insertingElement, passPointId ) ;

      return subRoute.Route.RouteInfos.Select( inserter.CreateInsertedRouteRecord ) ;
    }


    private class PassPointInserter
    {
      private readonly RouteInfoDetector _detector ;
      private readonly int _insertedPassPointId ;

      public PassPointInserter( SubRoute subRoute, Element insertingElement, int passPointId )
      {
        _detector = new RouteInfoDetector( subRoute, insertingElement ) ;
        _insertedPassPointId = passPointId ;
      }

      public RouteRecord CreateInsertedRouteRecord( RouteInfo info )
      {
        var index = _detector.GetPassedThroughPassPointIndex( info ) ;
        if ( index < 0 ) return new RouteRecord( _detector.RouteName, info ) ;

        var newPassPoints = CreateInsertedArray( info.PassPoints, index, _insertedPassPointId ) ;

        return new RouteRecord( _detector.RouteName, info.FromId, info.ToId, newPassPoints ) ;
      }

      private static int[] CreateInsertedArray( int[] passPoints, int index, int insertedPassPointId )
      {
        var result = new int[ passPoints.Length + 1 ] ;

        Array.Copy( passPoints, 0, result, 0, index ) ;
        result[ index ] = insertedPassPointId ;
        Array.Copy( passPoints, index, result, index + 1, passPoints.Length - index ) ;

        return result ;
      }
    }
  }
}