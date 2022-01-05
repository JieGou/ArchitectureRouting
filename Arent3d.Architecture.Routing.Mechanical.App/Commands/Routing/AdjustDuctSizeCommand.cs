using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AdjustDuctSizeCommand", DefaultString = "Adjust\nDuctSize" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AdjustDuctSizeCommand : RoutingCommandBase<IReadOnlyCollection<Route>>
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.PickAndAdjustDuctSize" ;

    private AddInType GetAddInType() => AppCommandSettings.AddInType ;

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view ) => AppCommandSettings.CreateRoutingExecutor( document, view ) ;

    protected override OperationResult<IReadOnlyCollection<Route>> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      return new OperationResult<IReadOnlyCollection<Route>>( SelectRoutes( commandData.Application.ActiveUIDocument ) ) ;
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      if ( 0 < list.Count ) return list ;

      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.PickAndReRoute.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      return new[] { pickInfo.Route } ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, IReadOnlyCollection<Route> state )
    {
      var topRoute = GetTopRoute( state.First() ) ;
      RouteGenerator.CorrectEnvelopes( document ) ;

      var adjuster = new DuctSizeAdjusterForTTE() ;
      adjuster.Setup( document, topRoute, 10.0 ) ;
      return adjuster.Execute().EnumerateAll() ;
    }

    private static Route GetTopRoute( Route route )
    {
      var targetRoutes = route.GetParentBranches() ;
      if ( ! targetRoutes.Any() ) return route ;
      return GetTopRoute( targetRoutes.First() ) ;
    }
  }
}