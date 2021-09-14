using System ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewRackFromToCommandBase : IExternalCommand
  {
    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private readonly double maxDistanceTolerance = ( 100.0 ).MillimetersToRevitUnits() ;

    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
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

            var rackConnectors = new List<Connector>() ;
            foreach ( var connector in connectors ) {
              var element = connector.Owner ;
              if ( element is Conduit ) // element is straight conduit
              {
                var conduit = ( element as Conduit )! ;

                var location = ( element.Location as LocationCurve )! ;
                var line = ( location.Curve as Line )! ;
                Connector firstConnector = GetFirstConnector( element.GetConnectorManager()!.Connectors )! ;

                var length = conduit.ParametersMap
                  .get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document,
                    "Length" ) ).AsDouble() ;
                var diameter = conduit.ParametersMap
                  .get_Item( "Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault( document,
                    "Outside Diameter" ) ).AsDouble() ;

                var symbol =
                  uiDocument.Document.GetFamilySymbol( RoutingFamilyType.CableTray )! ; // TODO may change in the future

                // Create cable tray
                var instance = symbol.Instantiate(
                  new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                  uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;

                // set cable rack length
                SetParameter( instance,
                  "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ),
                  length ) ; // TODO may be must change when FamilyType change

                // move cable rack to under conduit
                instance.Location.Move( new XYZ( 0, 0, -diameter ) ) ; // TODO may be must change when FamilyType change

                // set cable tray direction
                if ( 1.0 == line.Direction.Y ) {
                  ElementTransformUtils.RotateElement( document, instance.Id,
                    Line.CreateBound(
                      new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                      new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z + 1 ) ),
                    Math.PI / 2 ) ;
                }

                // save connectors of cable rack
                foreach ( Connector rackConnector in instance.GetConnectorManager()!.Connectors ) {
                  rackConnectors.Add( connector ) ;
                }
              }
              else // element is conduit fitting
              {
                var conduit = ( element as FamilyInstance )! ;

                var location = ( element.Location as LocationPoint )! ;

                var length = conduit.ParametersMap
                  .get_Item(
                    "Revit.Property.Builtin.NominalRadius".GetDocumentStringByKeyOrDefault( document, "呼び半径" ) )
                  .AsDouble() ;
                var diameter = conduit.ParametersMap
                  .get_Item(
                    "Revit.Property.Builtin.NominalDiameter".GetDocumentStringByKeyOrDefault( document, "呼び径" ) )
                  .AsDouble() ;
                var bendRadius = conduit.ParametersMap
                  .get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document,
                    "Bend Radius" ) ).AsDouble() ;

                var symbol =
                  uiDocument.Document.GetFamilySymbol( RoutingFamilyType
                    .CableTrayFitting )! ; // TODO may change in the future

                var instance = symbol.Instantiate( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ),
                  uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;

                // set cable tray Bend Radius
                SetParameter( instance,
                  "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ),
                  bendRadius / 2 ) ; // TODO may be must change when FamilyType change

                // set cable tray fitting direction
                if ( 1.0 == conduit.FacingOrientation.X ) {
                  instance.Location.Rotate(
                    Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ),
                      new XYZ( location.Point.X, location.Point.Y, location.Point.Z - 1 ) ), Math.PI / 2 ) ;
                }
                else if ( -1.0 == conduit.FacingOrientation.X ) {
                  instance.Location.Rotate(
                    Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ),
                      new XYZ( location.Point.X, location.Point.Y, location.Point.Z + 1 ) ), Math.PI / 2 ) ;
                }
                else if ( -1.0 == conduit.FacingOrientation.Y ) {
                  instance.Location.Rotate(
                    Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ),
                      new XYZ( location.Point.X, location.Point.Y, location.Point.Z + 1 ) ), Math.PI ) ;
                }

                // move cable rack to under conduit
                instance.Location.Move( new XYZ( 0, 0, -diameter ) ) ; // TODO may be must change when FamilyType change

                // save connectors of cable rack
                rackConnectors.AddRange( instance.GetConnectors() ) ;
              }
            }

            // connect all rack connectors
            foreach ( Connector connector in rackConnectors ) {
              if ( ! connector.IsConnected ) {
                var otherConnectors =
                  rackConnectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
                if ( otherConnectors != null ) {
                  var connectTo = GetConnectorClosestTo( otherConnectors, connector.Origin, maxDistanceTolerance ) ;
                  if ( connectTo != null ) {
                    connector.ConnectTo( connectTo ) ;
                  }
                }
              }
            }

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

    private static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }

    /// <summary>
    /// Return the connector in the set
    /// closest to the given point.
    /// </summary>
    /// <param name="connectors"></param>
    /// <param name="point"></param>
    /// <param name="minDist"></param>
    /// <returns></returns>
    private static Connector? GetConnectorClosestTo( List<Connector> connectors, XYZ point,
      double maxDistance = double.MaxValue )
    {
      double minDistance = double.MaxValue ;
      Connector? targetConnector = null ;

      foreach ( Connector connector in connectors ) {
        double distance = connector.Origin.DistanceTo( point ) ;

        if ( distance < minDistance && distance <= maxDistance ) {
          targetConnector = connector ;
          minDistance = distance ;
        }
      }

      return targetConnector ;
    }

    /// <summary>
    /// Return the first connector.
    /// </summary>
    /// <param name="connectors"></param>
    /// <returns></returns>
    private static Connector? GetFirstConnector( ConnectorSet connectors )
    {
      foreach ( Connector connector in connectors ) {
        if ( 0 == connector.Id ) {
          return connector ;
        }
      }

      return null ;
    }

    /// <summary>
    /// Return the last connector.
    /// </summary>
    /// <param name="connectors"></param>
    /// <returns></returns>
    private static Connector? GetLastConnector( ConnectorSet connectors )
    {
      int maxId = -1 ;
      Connector? targetConnector = null ;
      foreach ( Connector connector in connectors ) {
        if ( connector.Id > maxId ) {
          maxId = connector.Id ;
          targetConnector = connector ;
        }
      }

      return targetConnector ;
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