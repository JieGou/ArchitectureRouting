using System ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewRackCommandBase : IExternalCommand
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

        // TODO 電動二方弁でコネクタ設定時エラーが出る（おそらくコネクタタイプの問題）＿Error occurs when setting the connector with an motor two-way valve (probably connector type problem)

        if ( null == pickFrom.Position || null == pickFrom.RouteDirection ) {
          return Result.Failed ;
        }

        var result = document.Transaction(
          "TransactionName.Commands.Rack.NewRack".GetAppStringByKeyOrDefault( "New Rack" ), _ =>
          {
            var routeName = RoutingElementExtensions.GetRouteName( pickFrom.Element ) ;
            if ( routeName != null ) {
              // get all elements in route
              var allElementsInRoute = document.GetAllElementsOfRouteName<Element>( routeName ) ;
              var connectors = new List<Connector>() ;
              // Browse each conduits and draw the cable tray below
              foreach ( var element in allElementsInRoute ) {
                if ( element is Conduit ) // element is straight conduit
                {
                  var conduit = ( element as Conduit )! ;

                  var location = ( element.Location as LocationCurve )! ;
                  var line = ( location.Curve as Line )! ;
                  Connector firstConnector = GetFirstConnector( element.GetConnectorManager()!.Connectors )! ;

                  var length = conduit.ParametersMap.get_Item( "Length" ).AsDouble() ;
                  var diameter = conduit.ParametersMap.get_Item( "Outside Diameter" ).AsDouble() ;

                  var symbol =
                    uiDocument.Document.GetFamilySymbol( RoutingFamilyType
                      .CableTray )! ; // TODO may change in the future

                  // Create cable tray
                  var instance = symbol.Instantiate(
                    new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                    uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;

                  // set cable rack length
                  SetParameter( instance, "トレイ長さ", length ) ; // TODO may be must change when FamilyType change

                  // move cable rack to under conduit
                  instance.Location.Move( new XYZ( 0, 0,
                    -diameter ) ) ; // TODO may be must change when FamilyType change

                  // set cable tray direction
                  if ( 1.0 == line.Direction.Y ) {
                    ElementTransformUtils.RotateElement( document, instance.Id,
                      Line.CreateBound(
                        new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                        new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z + 1 ) ),
                      Math.PI / 2 ) ;
                  }

                  // save connectors of cable rack
                  foreach ( Connector connector in instance.GetConnectorManager()!.Connectors ) {
                    connectors.Add( connector ) ;
                  }
                }
                else // element is conduit fitting
                {
                  var conduit = ( element as FamilyInstance )! ;

                  var location = ( element.Location as LocationPoint )! ;

                  var length = conduit.ParametersMap.get_Item( "呼び半径" ).AsDouble() ;
                  var diameter = conduit.ParametersMap.get_Item( "呼び径" ).AsDouble() ;
                  var bendRadius = conduit.ParametersMap.get_Item( "Bend Radius" ).AsDouble() ;

                  var symbol =
                    uiDocument.Document.GetFamilySymbol( RoutingFamilyType
                      .CableTrayFitting )! ; // TODO may change in the future

                  var instance = symbol.Instantiate( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ),
                    uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;

                  // set cable tray Bend Radius
                  SetParameter( instance, "Bend Radius",
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
                  instance.Location.Move( new XYZ( 0, 0,
                    -diameter ) ) ; // TODO may be must change when FamilyType change

                  // save connectors of cable rack
                  connectors.AddRange( instance.GetConnectors() ) ;
                }
              }

              // connect all connectors
              foreach ( Connector connector in connectors ) {
                if ( ! connector.IsConnected ) {
                  var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
                  if ( otherConnectors != null ) {
                    var connectTo = GetConnectorClosestTo( otherConnectors, connector.Origin, maxDistanceTolerance ) ;
                    if ( connectTo != null ) {
                      connector.ConnectTo( connectTo ) ;
                    }
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
    /// <param name="maxDistance"></param>
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
  }
}