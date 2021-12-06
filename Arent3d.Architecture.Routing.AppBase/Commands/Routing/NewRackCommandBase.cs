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

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
  public abstract class NewRackCommandBase : IExternalCommand
  {
    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private static readonly double maxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;
    private const double BendRadiusSettingForStandardFamilyType = 20.5 ;
    private const double RATIO_BEND_RADIUS = 3.45 ;

    public static IReadOnlyDictionary<byte, string> RackTypes { get ; } = new Dictionary<byte, string> { { 0, "Normal Rack" }, { 1, "Limit Rack" } } ;

    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var result = document.Transaction(
          "TransactionName.Commands.Rack.CreateCableRackFroAllRoute".GetAppStringByKeyOrDefault(
            "Create Cable Rack For All Route" ), _ =>
          {
            var parameterName = document.GetParameterName( RoutingParameter.RouteName ) ;
            if ( null == parameterName ) return Result.Failed ;

            var filter =
              new ElementParameterFilter(
                ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

            // get all route names
            var routeNames = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits )
              .OfNotElementType().Where( filter ).OfType<Element>()
              .Select( x => x.GetRouteName() ).Distinct() ;

            // create cable rack for each route
            foreach ( var routeName in routeNames ) {
              CreateCableRackForRoute( uiDocument, routeName ) ;
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

    public static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }
    
    private static void SetParameter( FamilyInstance instance, string parameterName, string value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }

    /// <summary>
    /// Creat cable rack for route
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="routeName"></param>
    private void CreateCableRackForRoute( UIDocument uiDocument, string? routeName )
    {
      if ( routeName != null ) {
        var document = uiDocument.Document ;
        // get all elements in route
        var allElementsInRoute = document.GetAllElementsOfRouteName<Element>( routeName ) ;
        CreateRackForConduit( uiDocument, allElementsInRoute ) ;
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
    public static bool ExistsCableTray( Document document, FamilyInstance familyInstance )
    {
      return document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).OfNotElementType()
        .Any( x => IsSameLocation( x.Location, familyInstance.Location ) && x.Id != familyInstance.Id &&
                     x.FacingOrientation.IsAlmostEqualTo( familyInstance.FacingOrientation ) &&
                     IsSameConnectors( x.GetConnectors(), familyInstance.GetConnectors() )) ;
    }

    /// <summary>
    /// compare 2 locations
    /// </summary>
    /// <param name="location"></param>
    /// <param name="otherLocation"></param>
    /// <returns></returns>
    public static bool IsSameLocation( Location location, Location otherLocation )
    {
      if ( location is LocationPoint ) {
        if ( ! ( otherLocation is LocationPoint ) ) {
          return false ;
        }

        var locationPoint = ( location as LocationPoint )! ;
        var otherLocationPoint = ( otherLocation as LocationPoint )! ;
        return locationPoint.Point.DistanceTo( otherLocationPoint.Point) <= maxDistanceTolerance &&
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

    public static void CreateRackForConduit( UIDocument uiDocument, IEnumerable<Element> allElementsInRoute )
    {
      var document = uiDocument.Document ;
      var connectors = new List<Connector>() ;
      foreach ( var element in allElementsInRoute ) {
        using var transaction = new SubTransaction( uiDocument.Document ) ;
        try {
          transaction.Start() ;
          if ( element is Conduit ) // element is straight conduit
          {
            var instance = CreateRackForStraightConduit( uiDocument, element ) ;

            // check cable tray exists
            if ( ExistsCableTray( document, instance ) ) {
              transaction.RollBack() ;
              continue ;
            }

            // save connectors of cable rack
            foreach ( Connector connector in instance.GetConnectorManager()!.Connectors ) {
              connectors.Add( connector ) ;
            }
          }
          else // element is conduit fitting
          {
            var conduit = ( element as FamilyInstance )! ;

            // Ignore the case of vertical conduits in the oz direction
            if ( 1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.HandOrientation.Z || 1.0 == conduit.HandOrientation.Z) {
              continue ;
            }

            var location = ( element.Location as LocationPoint )! ;

            var instance = CreateRackForFittingConduit( uiDocument, conduit, location ) ;

            // check cable tray exists
            if ( ExistsCableTray( document, instance ) ) {
              transaction.RollBack() ;
              continue ;
            }

            // save connectors of cable rack
            connectors.AddRange( instance.GetConnectors() ) ;
          }

          transaction.Commit() ;
        }
        catch {
          transaction.RollBack() ;
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
    
    public static FamilyInstance CreateRackForStraightConduit( UIDocument uiDocument, Element element, double cableRackWidth = 0 )
    {
      var document = uiDocument.Document ;
      var conduit = ( element as Conduit )! ;

      var location = ( element.Location as LocationCurve )! ;
      var line = ( location.Curve as Line )! ;
      
      Connector firstConnector = GetFirstConnector( element.GetConnectorManager()!.Connectors )! ;

      var length = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ;
      var diameter = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault( document, "Outside Diameter" ) ).AsDouble() ;

      var symbol = document.GetFamilySymbols( RoutingFamilyType.CableTray ).FirstOrDefault() ?? throw new InvalidOperationException() ; // TODO may change in the future

      // Create cable tray
      if (false == symbol.IsActive) symbol.Activate();
      var instance = document.Create.NewFamilyInstance(firstConnector.Origin, symbol, null, StructuralType.NonStructural);

      // set cable rack length
      SetParameter( instance, "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ), length ) ; // TODO may be must change when FamilyType change

      // set cable rack length
      if ( cableRackWidth > 0 )
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ), cableRackWidth.MillimetersToRevitUnits() ) ;

      // set cable rack comments
      SetParameter( instance, "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), cableRackWidth == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      // set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = GetToConnectorId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( document, "To-Side Connector Id" ), toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( document, "From-Side Connector Id" ), toConnectorId ) ;

      // set cable tray direction
      if ( 1.0 == line.Direction.Y ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z + 1 ) ), Math.PI / 2 ) ;
      }
      else if ( -1.0 == line.Direction.Y ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z - 1 ) ), Math.PI / 2 ) ;
      }
      else if ( -1.0 == line.Direction.X ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z - 1 ) ), Math.PI ) ;
      }
      else if ( 1.0 == line.Direction.Z ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y - 1, firstConnector.Origin.Z ) ), Math.PI / 2 ) ;
      }
      else if ( -1.0 == line.Direction.Z ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y + 1, firstConnector.Origin.Z ) ), Math.PI / 2 ) ;
      }
      
      if ( 1.0 == line.Direction.Z || -1.0 == line.Direction.Z ) {
        // move cable rack to right of conduit
        instance.Location.Move( new XYZ( 0, diameter, 0 ) ) ;
      }
      else {
        // move cable rack to under conduit
        instance.Location.Move( new XYZ( 0, 0, -diameter ) ) ; // TODO may be must change when FamilyType change
      }

      return instance ;
    }
    
    public static FamilyInstance CreateRackForFittingConduit( UIDocument uiDocument, FamilyInstance conduit, LocationPoint location, double cableTrayDefaultBendRadius = 0)
    {
      var document = uiDocument.Document ;
      
      var length = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( document, "電線管長さ" ) ).AsDouble() ;
      var diameter = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.NominalDiameter".GetDocumentStringByKeyOrDefault( document, "呼び径" ) ).AsDouble() ;
      var bendRadius = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) ).AsDouble() ;

      var symbol = uiDocument.Document.GetFamilySymbols( RoutingFamilyType.CableTrayFitting ).FirstOrDefault() ?? throw new InvalidOperationException() ; // TODO may change in the future

      if (false == symbol.IsActive) symbol.Activate();
      var instance = document.Create.NewFamilyInstance(location.Point, symbol, null, StructuralType.NonStructural);

      // set cable tray Bend Radius
      bendRadius = cableTrayDefaultBendRadius == 0 ? ( RATIO_BEND_RADIUS * diameter.RevitUnitsToMillimeters() + BendRadiusSettingForStandardFamilyType ).MillimetersToRevitUnits() : cableTrayDefaultBendRadius ;
      SetParameter( instance, "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ), bendRadius ) ; // TODO may be must change when FamilyType change

      // set cable rack length
      SetParameter( instance, "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ), length ) ; // TODO may be must change when FamilyType change

      // set cable rack comments
      SetParameter( instance, "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), cableTrayDefaultBendRadius == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      // set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = GetToConnectorId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( document, "To-Side Connector Id" ), toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( document, "From-Side Connector Id" ), toConnectorId ) ;

      // set cable tray fitting direction
      if ( 1.0 == conduit.FacingOrientation.X ) {
        instance.Location.Rotate( Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ), new XYZ( location.Point.X, location.Point.Y, location.Point.Z - 1 ) ), Math.PI / 2 ) ;
      }
      else if ( -1.0 == conduit.FacingOrientation.X ) {
        instance.Location.Rotate( Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ), new XYZ( location.Point.X , location.Point.Y, location.Point.Z + 1 ) ), Math.PI / 2 ) ;
      }
      else if ( -1.0 == conduit.FacingOrientation.Y ) {
        instance.Location.Rotate( Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ), new XYZ( location.Point.X, location.Point.Y, location.Point.Z + 1 ) ), Math.PI ) ;
      }
      
      // move cable rack to under conduit
      instance.Location.Move( new XYZ( 0, 0, -diameter ) ) ; // TODO may be must change when FamilyType change

      return instance ;
    }

    public static bool IsSameConnectors( IEnumerable<Connector> connectors, IEnumerable<Connector> otherConnectors )
    {
      var isSameConnectors = true ;
      foreach ( var connector in connectors ) {
        if ( ! otherConnectors.Any( x => x.Origin.IsAlmostEqualTo( connector.Origin ) ) ) {
          return false ;
        }
      }

      return isSameConnectors ;
    }

    private static ( string, string ) GetToConnectorId( Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      var fromConnectorId = fromEndPointKey!.GetElementId() ;

      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      var toConnectorId = toEndPointKey!.GetElementId() ;

      return ( fromConnectorId, toConnectorId ) ;
    }
  }
}