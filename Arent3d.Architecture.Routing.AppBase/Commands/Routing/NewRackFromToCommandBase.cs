using System ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.ApplicationServices ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewRackFromToCommandBase : IExternalCommand
  {
    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private readonly double maxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;

    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      UIApplication uiApp = commandData.Application ;
      Application app = uiApp.Application ;
      try {
        // 線クリックのui設定＿Setting UI of wire click
        var pickFrom = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route.", GetAddInType() ) ;
        var pickTo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route.", GetAddInType() ) ;

        // TODO 電動二方弁でコネクタ設定時エラーが出る（おそらくコネクタタイプの問題）＿Error occurs when setting the connector with an motor two-way valve (probably connector type problem)

        if ( null == pickFrom.Position || null == pickTo.Position || null == pickFrom.RouteDirection ||
             null == pickTo.RouteDirection ) {
          return Result.Failed ;
        }

        var result = document.Transaction(
          "TransactionName.Commands.Rack.NewRackFromTo".GetAppStringByKeyOrDefault( "New Rack From-To" ), _ =>
          {
            var fromElement = pickFrom.Element ;
            var toElement = pickTo.Element ;
            var routeName = RoutingElementExtensions.GetRouteName( pickFrom.Element )! ;

            var connectors = FindAllConnectorsBetweenTwoElement( fromElement, toElement ) ;
            if ( null == connectors || ! connectors.Any() ) {
              TaskDialog.Show( "Dialog.Commands.NewRackFromTo.Dialog.Title.Error".GetAppStringByKeyOrDefault( null ),
                "Dialog.Commands.NewRackFromTo.Dialog.Body.Error.CanNotConnectTwoPiles"
                  .GetAppStringByKeyOrDefault( null ) ) ;
              return Result.Failed ;
            }

            var racks = new List<FamilyInstance>() ;
            var allElementsInRoute = new List<Element>() ;
            foreach ( var connector in connectors ) {
              allElementsInRoute.Add( connector.Owner );
            }
            NewRackCommandBase.CreateRackForConduit( uiDocument, allElementsInRoute, racks );
            
            // insert notation for racks
            NewRackCommandBase.CreateNotationForRack( document, app, racks ) ;

            return Result.Succeeded ;
          } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    /// <summary>
    /// Return all connectors between fromElement and toElement. Using DFS algorithm with stack
    /// </summary>
    /// <param name="fromElement"></param>
    /// <param name="toElement"></param>
    /// <returns></returns>
    private static IEnumerable<Connector> FindAllConnectorsBetweenTwoElement( Element fromElement, Element toElement )
    {
      bool existRouting = false ;
      var visited = new HashSet<Connector>() ;
      var correctPath = new HashSet<Connector>() ;
      Stack<Connector> stack = new Stack<Connector>() ;
      foreach ( var item in fromElement.GetConnectors() ) {
        stack.Push( item ) ;
      }

      while ( stack.Count > 0 ) {
        var current = stack.Pop() ;
        if ( fromElement.GetConnectors().Any( x => x.Owner.Id == current.Owner.Id ) ) {
          correctPath = new HashSet<Connector>() ;
        }

        if ( toElement.Id == current.Owner.Id ) {
          visited.Add( current ) ;
          correctPath.Add( current ) ;
          existRouting = true ;
          break ;
        }

        if ( visited.Any( x => x.Owner.Id == current.Owner.Id && x.Id == current.Id ) ) {
          continue ;
        }
        else {
          visited.Add( current ) ;
          correctPath.Add( current ) ;
        }

        var neighbours = new List<Connector>() ;
        var connecToConnectors = current.GetConnectedConnectors()
          .Where( x => ! visited.Any( y => y.Owner.Id == x.Owner.Id && y.Id == x.Id ) ) ;

        foreach ( var connecToConnector in connecToConnectors ) {
          neighbours.AddRange( connecToConnector.GetOtherConnectorsInOwner()
            .Where( x => ! visited.Any( y => y.Owner.Id == x.Owner.Id && y.Id == x.Id ) ) ) ;
        }

        foreach ( var neighbour in neighbours )
          stack.Push( neighbour ) ;
      }

      if ( ! existRouting ) {
        correctPath = new HashSet<Connector>() ;
      }

      return correctPath ;
    }
  }
}