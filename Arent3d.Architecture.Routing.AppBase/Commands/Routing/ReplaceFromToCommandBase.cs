using System.Collections.Generic ;
using System.Linq ;
using System.Threading.Tasks ;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.ReplaceFromToCommand", DefaultString = "Replace\nFrom-To" )]
  [Image( "resources/ReplaceFromTo.png" )]
  public abstract class ReplaceFromToCommandBase : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.ReplaceFromTo" ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsParallelToTransaction( UIDocument uiDocument )
    {
      var route = GetReplacingRoute( uiDocument ) ;

      var oldEndPoint = GetChangingEndPoint( uiDocument, route ) ;
      var newEndPoint = PickNewEndPoint( uiDocument, route, oldEndPoint ) ;

      return GetReplacedEndPoints( route, oldEndPoint, newEndPoint ) ;
    }

    private static async IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetReplacedEndPoints( Route route, IEndPoint oldEndPoint, IEndPoint newEndPoint )
    {
      await Task.Yield() ;
      
      oldEndPoint.EraseInstance() ;
      newEndPoint.GenerateInstance( route.RouteName ) ;

      foreach ( var (routeName, segment) in route.CollectAllDescendantBranches().ToSegmentsWithName().EnumerateAll() ) {
        segment.ReplaceEndPoint( oldEndPoint, newEndPoint ) ;
        yield return ( routeName, segment ) ;
      }
    }

    private static IEndPoint GetChangingEndPoint( UIDocument uiDocument, Route route )
    {
      var message = "Dialog.Commands.Routing.ReplaceFromTo.SelectFromTo".GetAppStringByKeyOrDefault( "Select which end is to be changed." ) ;

      var array = route.RouteSegments.SelectMany( GetReplaceableEndPoints ).ToArray() ;
            // TODO: selection ui

      var sv = new SelectEndPoint( array ) { Title = message };
      sv.ShowDialog();

      uiDocument.ClearSelection();
      uiDocument.GetActiveUIView()?.ZoomToFit();

      if ( true != sv.DialogResult )
          return array[0];

      return sv.GetSelectedEndPoint();
    }

    private static IEnumerable<IEndPoint> GetReplaceableEndPoints( RouteSegment segment )
    {
      if ( segment.FromEndPoint.IsReplaceable ) yield return segment.FromEndPoint ;
      if ( segment.ToEndPoint.IsReplaceable ) yield return segment.ToEndPoint ;
    }

    private IEndPoint PickNewEndPoint( UIDocument uiDocument, Route route, IEndPoint endPoint )
    {
      var fromPickResult = PickCommandUtil.PickResultFromAnother( route, endPoint ) ;
      var toPickResult = ConnectorPicker.GetConnector( uiDocument, "Dialog.Commands.Routing.ReplaceFromTo.SelectEndPoint".GetAppStringByKeyOrDefault( null ), fromPickResult ) ;
      
      return PickCommandUtil.GetEndPoint( toPickResult, fromPickResult ) ;
    }

    private static Route GetReplacingRoute( UIDocument uiDocument )
    {
      return GetReplacingRouteFromCurrentSelection( uiDocument ) ?? PickReplacingRoute( uiDocument ) ;
    }

    private static Route? GetReplacingRouteFromCurrentSelection( UIDocument uiDocument )
    {
      return PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).UniqueOrDefault() ;
    }

    private static Route PickReplacingRoute( UIDocument uiDocument )
    {
      return PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.ReplaceFromTo.Pick".GetAppStringByKeyOrDefault( null ) ).Route ;
    }
  }
}