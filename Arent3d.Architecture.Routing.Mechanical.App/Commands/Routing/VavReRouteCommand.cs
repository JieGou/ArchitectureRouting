using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MathLib ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.VavReRouteCommand", DefaultString = "VavReroute\nSelected" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class VavReRouteCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickAndReRoute" ;

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      return ( true, SelectRoutes( uiDocument ) ) ;
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      return new[] { pickInfo.Route } ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var routes = state as IReadOnlyCollection<Route> ?? throw new InvalidOperationException() ;
      RouteGenerator.CorrectEnvelopes( document ) ;

      var segments = Route.GetAllRelatedBranches( routes ).ToSegmentsWithName().EnumerateAll() ;
      var tees = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_DuctFitting ).Where( tee => tee.Symbol.FamilyName == "022_丸型 T 型" ) ;

      foreach ( var tee in tees ) {
        foreach ( var (routeName, segment) in segments ) {
          var teeBehindConnector = tee.GetConnectors().Where( conn => conn.Id == 1 || conn.Id == 2 ).MaxItemOrDefault( conn => ( Vector2d.Distance( conn.Origin.To3dPoint().To2d(), segments.First().Segment.FromEndPoint.RoutingStartPosition.To3dPoint().To2d() ) ) ) ;
          if ( teeBehindConnector == null ) continue ;
          if ( tee.GetNearestEndPoints( false ).First().Key == segment.ToEndPoint.Key ) {
            var passPointDir = tee.HandOrientation ;
            var passpoint = document.AddPassPoint( routeName, teeBehindConnector.Origin, passPointDir, segment.PreferredNominalDiameter, segment.FromEndPoint.GetLevelId( document ) ) ;
            break ;
          }
        }
      }

      return segments ;
    }

    public enum TeeConnectorType
    {
      Connector1,
      Connector2
    }
  }
}