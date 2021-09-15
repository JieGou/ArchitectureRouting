using System ;
using System.Linq;
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
    private readonly BuiltInCategory[] ConduitBuiltInCategories =
    {
      BuiltInCategory.OST_Conduit, BuiltInCategory.OST_ConduitFitting, BuiltInCategory.OST_ConduitRun
    };

        protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var result = document.Transaction(
          "TransactionName.Commands.Rack.CreateCableRackFroAllRoute".GetAppStringByKeyOrDefault( "Create Cable Rack For All Route" ), _ =>
          {
              var parameterName = document.GetParameterName(RoutingParameter.RouteName);
              if (null == parameterName) return Result.Failed;

              var filter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateSharedParameterApplicableRule(parameterName));

              // get all route names
              var routeNames = document.GetAllElements<Element>()
                              .OfCategory(ConduitBuiltInCategories)
                              .OfNotElementType()
                              .Where(filter).OfType<Element>().Select(x => RoutingElementExtensions.GetRouteName(x))
                              .Distinct();

              // create cable rack for each route
              foreach(var routeName in routeNames)
              {
                  CreateCableRackForRoute(uiDocument, routeName);
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
        /// Creat cable rack for route
        /// </summary>
        /// <param name="uiDocument"></param>
        /// <param name="routeName"></param>
    private void CreateCableRackForRoute(UIDocument uiDocument, string? routeName)
        {
            if (routeName != null)
            {
                var document = uiDocument.Document;
                // get all elements in route
                var allElementsInRoute = document.GetAllElementsOfRouteName<Element>(routeName);

                var connectors = new List<Connector>();
                // Browse each conduits and draw the cable tray below
                foreach (var element in allElementsInRoute)
                {
                    if (element is Conduit) // element is straight conduit
                    {
                        var conduit = (element as Conduit)!;

                        var location = (element.Location as LocationCurve)!;
                        var line = (location.Curve as Line)!;
                        Connector firstConnector = GetFirstConnector(element.GetConnectorManager()!.Connectors)!;

                        var length = conduit.ParametersMap.get_Item("Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault(document, "Length")).AsDouble();
                        var diameter = conduit.ParametersMap.get_Item("Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault(document, "Outside Diameter")).AsDouble();

                        var symbol =
                          uiDocument.Document.GetFamilySymbol(RoutingFamilyType
                            .CableTray)!; // TODO may change in the future

                        // Create cable tray
                        var instance = symbol.Instantiate(
                          new XYZ(firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z),
                          uiDocument.ActiveView.GenLevel, StructuralType.NonStructural);

                        // set cable rack length
                        SetParameter(instance, "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault(document, "トレイ長さ"), length); // TODO may be must change when FamilyType change

                        // move cable rack to under conduit
                        instance.Location.Move(new XYZ(0, 0,
                          -diameter)); // TODO may be must change when FamilyType change

                        // set cable tray direction
                        if (1.0 == line.Direction.Y)
                        {
                            ElementTransformUtils.RotateElement(document, instance.Id,
                              Line.CreateBound(
                                new XYZ(firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z),
                                new XYZ(firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z + 1)),
                              Math.PI / 2);
                        }

                        // save connectors of cable rack
                        foreach (Connector connector in instance.GetConnectorManager()!.Connectors)
                        {
                            connectors.Add(connector);
                        }
                    }
                    else // element is conduit fitting
                    {
                        var conduit = (element as FamilyInstance)!;

                        var location = (element.Location as LocationPoint)!;

                        var length = conduit.ParametersMap.get_Item("Revit.Property.Builtin.NominalRadius".GetDocumentStringByKeyOrDefault(document, "呼び半径")).AsDouble();
                        var diameter = conduit.ParametersMap.get_Item("Revit.Property.Builtin.NominalDiameter".GetDocumentStringByKeyOrDefault(document, "呼び径")).AsDouble();
                        var bendRadius = conduit.ParametersMap.get_Item("Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault(document, "Bend Radius")).AsDouble();

                        var symbol =
                          uiDocument.Document.GetFamilySymbol(RoutingFamilyType
                            .CableTrayFitting)!; // TODO may change in the future

                        var instance = symbol.Instantiate(new XYZ(location.Point.X, location.Point.Y, location.Point.Z),
                          uiDocument.ActiveView.GenLevel, StructuralType.NonStructural);

                        // set cable tray Bend Radius
                        SetParameter(instance, "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault(document, "Bend Radius"),
                          bendRadius / 2); // TODO may be must change when FamilyType change

                        // set cable tray fitting direction
                        if (1.0 == conduit.FacingOrientation.X)
                        {
                            instance.Location.Rotate(
                              Line.CreateBound(new XYZ(location.Point.X, location.Point.Y, location.Point.Z),
                                new XYZ(location.Point.X, location.Point.Y, location.Point.Z - 1)), Math.PI / 2);
                        }
                        else if (-1.0 == conduit.FacingOrientation.X)
                        {
                            instance.Location.Rotate(
                              Line.CreateBound(new XYZ(location.Point.X, location.Point.Y, location.Point.Z),
                                new XYZ(location.Point.X, location.Point.Y, location.Point.Z + 1)), Math.PI / 2);
                        }
                        else if (-1.0 == conduit.FacingOrientation.Y)
                        {
                            instance.Location.Rotate(
                              Line.CreateBound(new XYZ(location.Point.X, location.Point.Y, location.Point.Z),
                                new XYZ(location.Point.X, location.Point.Y, location.Point.Z + 1)), Math.PI);
                        }

                        // move cable rack to under conduit
                        instance.Location.Move(new XYZ(0, 0,
                          -diameter)); // TODO may be must change when FamilyType change

                        // save connectors of cable rack
                        connectors.AddRange(instance.GetConnectors());
                    }
                }

                // connect all connectors
                foreach (Connector connector in connectors)
                {
                    if (!connector.IsConnected)
                    {
                        var otherConnectors = connectors.FindAll(x => !x.IsConnected && x.Owner.Id != connector.Owner.Id);
                        if (otherConnectors != null)
                        {
                            var connectTo = GetConnectorClosestTo(otherConnectors, connector.Origin, maxDistanceTolerance);
                            if (connectTo != null)
                            {
                                connector.ConnectTo(connectTo);
                            }
                        }
                    }
                }
            }

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