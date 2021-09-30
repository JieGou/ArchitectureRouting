using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.StorableCaches ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewLimitRackCommandBase : IExternalCommand
  {
    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private readonly double maxDistanceTolerance = ( 10.0 ).MillimetersToRevitUnits() ;

    private readonly BuiltInCategory[] CableTrayBuiltInCategories =
    {
      BuiltInCategory.OST_CableTray, BuiltInCategory.OST_CableTrayFitting
    } ;

    private readonly int minNumberOfMultiplicity = 5 ;
    private readonly double minLengthOfConduit = ( 3.0 ).MetersToRevitUnits() ;

    private readonly double[] CableTrayWidthMapping = { 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0, 1200.0 } ;

    protected abstract AddInType GetAddInType() ;
    private Dictionary<ElementId, List<Connector>> elbowsToCreate = new Dictionary<ElementId, List<Connector>>() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var result = document.Transaction(
          "TransactionName.Commands.Rack.CreateLimitCableRack".GetAppStringByKeyOrDefault( "Create Limit Cable" ), _ =>
          {
            var elements = document.CollectAllMultipliedRoutingElements( minNumberOfMultiplicity ) ;
            foreach ( var element in elements ) {
              var (mepCurve, subRoute) = element ;
              if ( RouteLength( subRoute.Route.RouteName, elements, document ) >= minLengthOfConduit ) {
                var conduit = ( mepCurve as Conduit )! ;
                var cableRackWidth = CalcCableRackMaxWidth( element, elements, document ) ;

                CreateCableRackForConduit( uiDocument, conduit, cableRackWidth ) ;
              }
            }

            List<ElementId> ids = new List<ElementId>() ;
            foreach ( var elbow in elbowsToCreate ) {
              if ( elbow.Value.Count == 2 ) {
                //document.Create.NewElbowFitting(elbow.Value[0], elbow.Value[1]);
                //elbow.Value[0].ConnectTo(elbow.Value[1]);
              }

              //ids.AddRange(elbow.Value.Select(y => y.Owner.Id).ToList());
              CreateElbow( uiDocument, elbow.Key, elbow.Value ) ;
            }
            //uiDocument.Selection.SetElementIds(ids);
            //uiDocument.ShowElements(ids);

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
    /// Creat cable rack for Conduit
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="routeName"></param>
    private void CreateCableRackForConduit( UIDocument uiDocument, Conduit conduit, double cableRackWidth )
    {
      if ( conduit != null ) {
        var document = uiDocument.Document ;

        using var transaction = new SubTransaction( document ) ;
        try {
          transaction.Start() ;
          var location = ( conduit.Location as LocationCurve )! ;
          var line = ( location.Curve as Line )! ;

          // Ignore the case of vertical conduits in the oz direction
          if ( 1.0 == line.Direction.Z || -1.0 == line.Direction.Z ) {
            return ;
          }

          Connector firstConnector = GetFirstConnector( conduit.GetConnectorManager()!.Connectors )! ;

          var length = conduit.ParametersMap
            .get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) )
            .AsDouble() ;
          var diameter = conduit.ParametersMap
            .get_Item( "Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault( document,
              "Outside Diameter" ) ).AsDouble() ;

          // check length of conduit must be great than or equal minLengthOfConduit
          //if ( length < minLengthOfConduit ) {
          //  return ;
          //}

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

          // set cable rack length
          SetParameter( instance,
            "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ),
            cableRackWidth.MillimetersToRevitUnits() ) ; // TODO may be must change when FamilyType change

          // move cable rack to under conduit
          instance.Location.Move( new XYZ( 0, 0, -diameter ) ) ; // TODO may be must change when FamilyType change

          // set cable tray direction
          if ( 1.0 == line.Direction.Y ) {
            ElementTransformUtils.RotateElement( document, instance.Id,
              Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z + 1 ) ),
              Math.PI / 2 ) ;
          }
          else if ( -1.0 == line.Direction.Y ) {
            ElementTransformUtils.RotateElement( document, instance.Id,
              Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z - 1 ) ),
              Math.PI / 2 ) ;
          }
          else if ( -1.0 == line.Direction.X ) {
            ElementTransformUtils.RotateElement( document, instance.Id,
              Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ),
                new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z - 1 ) ), Math.PI ) ;
          }

          // check cable tray exists
          if ( ExistsCableTray( document, instance ) ) {
            transaction.RollBack() ;
            return ;
          }

          var elbows = conduit.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd()
            .Select( c => c.Owner ).OfType<FamilyInstance>() ;
          foreach ( var elbow in elbows ) {
            if ( elbowsToCreate.ContainsKey( elbow.Id ) ) {
              elbowsToCreate[ elbow.Id ].Add( GetConnectorClosestTo( instance.GetConnectors().ToList(),
                ( elbow.Location as LocationPoint )!.Point )! ) ;
            }
            else {
              elbowsToCreate.Add( elbow.Id,
                new List<Connector>()
                {
                  GetConnectorClosestTo( instance.GetConnectors().ToList(),
                    ( elbow.Location as LocationPoint )!.Point )!
                } ) ;
            }
          }

          transaction.Commit() ;
        }
        catch {
          transaction.RollBack() ;
        }
      }
    }

    /// <summary>
    /// Creat cable rack for route
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="routeName"></param>
    private void CreateElbow( UIDocument uiDocument, ElementId elementId, List<Connector> connectors )
    {
      var document = uiDocument.Document ;
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var conduit = document.GetElementById<FamilyInstance>( elementId )! ;

        // Ignore the case of vertical conduits in the oz direction
        if ( 1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.FacingOrientation.Z ) {
          return ;
        }

        var location = ( conduit.Location as LocationPoint )! ;

        var length = conduit.ParametersMap
          .get_Item(
            "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( document, "電線管長さ" ) )
          .AsDouble() ;
        var diameter = conduit.ParametersMap
          .get_Item( "Revit.Property.Builtin.NominalDiameter".GetDocumentStringByKeyOrDefault( document, "呼び径" ) )
          .AsDouble() ;
        var bendRadius = conduit.ParametersMap
          .get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) )
          .AsDouble() ;

        var symbol =
          uiDocument.Document.GetFamilySymbol( RoutingFamilyType.CableTrayFitting )! ; // TODO may change in the future

        var instance = symbol.Instantiate( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ),
          uiDocument.ActiveView.GenLevel, StructuralType.NonStructural ) ;

        // set cable rack length
        SetParameter( instance,
          "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ),
          length ) ; // TODO may be must change when FamilyType change

        // set cable tray Bend Radius
        SetParameter( instance,
          "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ),
          ( 16.0 ).MillimetersToRevitUnits() ) ; // TODO may be must change when FamilyType change

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

        // check cable tray exists
        if ( ExistsCableTray( document, instance ) ) {
          transaction.RollBack() ;
          return ;
        }

        var firstCableRack = connectors.First().Owner ;
        // get cable rack width
        var firstCableRackWidth = firstCableRack.ParametersMap
          .get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) )
          .AsDouble() ; // TODO may be must change when FamilyType change
        var secondCableRack = connectors.Last().Owner ;
        // get cable rack width
        var secondCableRackWidth = firstCableRack.ParametersMap
          .get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) )
          .AsDouble() ; // TODO may be must change when FamilyType change

        //if (firstCableRackWidth == secondCableRackWidth)
        //{
        // set cable rack length
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ),
          firstCableRackWidth ) ; // TODO may be must change when FamilyType change
        foreach ( var connector in instance.GetConnectors() ) {
          var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
          if ( otherConnectors != null ) {
            var connectTo = GetConnectorClosestTo( otherConnectors, connector.Origin, maxDistanceTolerance ) ;
            if ( connectTo != null ) {
              connector.ConnectTo( connectTo ) ;
            }
          }
        }
        //}

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
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
    public static Connector? GetConnectorClosestTo( List<Connector> connectors, XYZ point,
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
    public static Connector? GetFirstConnector( ConnectorSet connectors )
    {
      foreach ( Connector connector in connectors ) {
        if ( 0 == connector.Id ) {
          return connector ;
        }
      }

      return null ;
    }

    /// <summary>
    /// Check cable tray exists (same place)
    /// </summary>
    /// <param name="document"></param>
    /// <param name="familyInstance"></param>
    /// <returns></returns>
    private bool ExistsCableTray( Document document, FamilyInstance familyInstance )
    {
      return document.GetAllElements<FamilyInstance>().OfCategory( CableTrayBuiltInCategories ).OfNotElementType()
        .Where( x => IsSameLocation( x.Location, familyInstance.Location ) && x.Id != familyInstance.Id &&
                     x.FacingOrientation.IsAlmostEqualTo( familyInstance.FacingOrientation ) ).Any() ;
    }

    /// <summary>
    /// compare 2 locations
    /// </summary>
    /// <param name="location"></param>
    /// <param name="otherLocation"></param>
    /// <returns></returns>
    bool IsSameLocation( Location location, Location otherLocation )
    {
      if ( location is LocationPoint ) {
        if ( ! ( otherLocation is LocationPoint ) ) {
          return false ;
        }

        var locationPoint = ( location as LocationPoint )! ;
        var otherLocationPoint = ( otherLocation as LocationPoint )! ;
        return locationPoint.Point.DistanceTo( otherLocationPoint.Point ) <= maxDistanceTolerance &&
               locationPoint.Rotation == otherLocationPoint.Rotation ;
      }
      else if ( location is LocationCurve ) {
        if ( ! ( otherLocation is LocationCurve ) ) {
          return false ;
        }

        var locationCurve = ( location as LocationCurve )! ;
        var line = ( locationCurve.Curve as Line )! ;

        var otherLocationCurve = ( otherLocation as LocationCurve )! ;
        var otherLine = ( otherLocationCurve.Curve as Line )! ;

        return line.Origin.IsAlmostEqualTo( otherLine.Origin, maxDistanceTolerance ) &&
               line.Direction == otherLine.Direction && line.Length == otherLine.Length ;
      }

      return location.Equals( otherLocation ) ;
    }

    private double CalcCableRackWidth( Document document, SubRoute subRoute )
    {
      var routes = RouteCache.Get( document ) ;
      var sumDiameter = subRoute.GetSubRouteGroup()
        .Sum( s => routes.GetSubRoute( s )?.GetDiameter().RevitUnitsToMillimeters() + 10 ) + 120 ;
      var cableTraywidth = 0.6 * sumDiameter ;
      foreach ( var width in CableTrayWidthMapping ) {
        if ( cableTraywidth <= width ) {
          cableTraywidth = width ;
          return cableTraywidth!.Value ;
        }
      }

      return cableTraywidth!.Value ;
    }

    private double CalcCableRackMaxWidth( (MEPCurve, SubRoute) element, IEnumerable<(MEPCurve, SubRoute)> elements,
      Document document )
    {
      var routeName = element.Item2.Route.RouteName ;
      var routeElements = elements.Where( x => x.Item2.Route.RouteName == routeName ) ;
      var maxWidth = 0.0 ;
      if ( routeMaxWidthCache.ContainsKey( routeName ) ) {
        var elbowsConnected = element.Item1.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd()
          .Select( c => c.Owner ).OfType<FamilyInstance>() ;
        var straightsConnected = element.Item1.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd()
          .Select( c => c.Owner ).OfType<Conduit>() ;
        if ( elbowsConnected.Any() && straightsConnected.Any() && element.Item2.PreviousSubRoute != null &&
             straightsConnected.First().GetSubRouteIndex()!.Value == element.Item2.PreviousSubRoute!.SubRouteIndex ) {
          var key = routeMaxWidthCache[ routeName ].Keys
            .Where( x => x <= element.Item2.PreviousSubRoute!.SubRouteIndex ).Max() ;
          return routeMaxWidthCache[ routeName ][ key ] ;
        }
        else if ( elbowsConnected.Any() &&
                  ( element.Item2.PreviousSubRoute == null || ( element.Item2.PreviousSubRoute != null &&
                                                                straightsConnected.Any() &&
                                                                straightsConnected.First().GetSubRouteIndex()!.Value !=
                                                                element.Item2.PreviousSubRoute!.SubRouteIndex ) ) &&
                  ! routeMaxWidthCache[ routeName ].ContainsKey( element.Item2.SubRouteIndex ) ) {
          maxWidth = CalcCableRackWidth( document, element.Item2 ) ;
          routeMaxWidthCache[ routeName ].Add( element.Item2.SubRouteIndex, maxWidth ) ;
          return maxWidth ;
        }
        else {
          var key = routeMaxWidthCache[ routeName ].Keys.Where( x => x <= element.Item2.SubRouteIndex ).Max() ;
          return routeMaxWidthCache[ routeName ][ key ] ;
        }
      }
      else {
        foreach ( var (mepCurve, subRoute) in routeElements ) {
          var cableTraywidth = CalcCableRackWidth( document, subRoute ) ;
          if ( cableTraywidth > maxWidth ) {
            maxWidth = cableTraywidth ;
          }
        }

        Dictionary<int, double> routeWidths = new Dictionary<int, double>() ;
        routeWidths.Add( element.Item2.SubRouteIndex, maxWidth ) ;
        routeMaxWidthCache.Add( routeName, routeWidths ) ;
        return maxWidth ;
      }
    }

    private Dictionary<string, double> routeLengthCache = new Dictionary<string, double>() ;

    private Dictionary<string, Dictionary<int, double>> routeMaxWidthCache =
      new Dictionary<string, Dictionary<int, double>>() ;

    private double RouteLength( string routeName, IEnumerable<(MEPCurve, SubRoute)> elements, Document document )
    {
      if ( routeLengthCache.ContainsKey( routeName ) ) {
        return routeLengthCache[ routeName ] ;
      }

      var routeLength = elements.Where( x => x.Item2.Route.RouteName == routeName ).Sum( x =>
        ( x.Item1 as Conduit )!.ParametersMap
        .get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) )
        .AsDouble() ) ;

      routeLengthCache.Add( routeName, routeLength ) ;

      return routeLength ;
    }
  }
}