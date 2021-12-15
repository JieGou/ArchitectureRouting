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
using System.Windows.Media ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.ApplicationServices ;

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
    private const string Notation = "CR (W:400)" ;

    public static IReadOnlyDictionary<byte, string> RackTypes { get ; } = new Dictionary<byte, string> { { 0, "Normal Rack" }, { 1, "Limit Rack" } } ;

    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      UIApplication uiApp = commandData.Application ;
      Application app = uiApp.Application ;
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
            var racks = new List<FamilyInstance>() ;
            foreach ( var routeName in routeNames ) {
              CreateCableRackForRoute( uiDocument, app, routeName, racks ) ;
            }
            
            // insert notation for racks
            CreateNotationForRack( document, app, racks ) ;

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
    private void CreateCableRackForRoute( UIDocument uiDocument, Application app, string? routeName, List<FamilyInstance> racks )
    {
      if ( routeName != null ) {
        var document = uiDocument.Document ;
        // get all elements in route
        var allElementsInRoute = document.GetAllElementsOfRouteName<Element>( routeName ) ;
        CreateRackForConduit( uiDocument, app, allElementsInRoute, racks ) ;
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

    public static void CreateRackForConduit( UIDocument uiDocument, Application app, IEnumerable<Element> allElementsInRoute, List<FamilyInstance> racks )
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
            
            racks.Add( instance );
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
      var (fromConnectorId, toConnectorId) = GetFromConnectorIdAndToConnectorId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( document, "To-Side Connector Id" ), toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( document, "From-Side Connector Id" ), fromConnectorId ) ;

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
      var (fromConnectorId, toConnectorId) = GetFromConnectorIdAndToConnectorId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.ToSideConnectorId".GetDocumentStringByKeyOrDefault( document, "To-Side Connector Id" ), toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) )
        SetParameter( instance, "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( document, "From-Side Connector Id" ), fromConnectorId ) ;

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

    public static ( string, string ) GetFromConnectorIdAndToConnectorId( Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      var fromConnectorId = fromEndPointKey!.GetElementId() ;

      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      var toConnectorId = toEndPointKey!.GetElementId() ;

      return ( fromConnectorId, toConnectorId ) ;
    }

    public static void CreateNotationForRack(Document doc, Application app, IEnumerable<FamilyInstance> racks )
    {
      var rackNotationStorable = doc.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? doc.GetRackNotationStorable() ;
      Dictionary<string, Dictionary<double, List<FamilyInstance>>> directionXRacks = new Dictionary<string, Dictionary<double, List<FamilyInstance>>>() ;
      Dictionary<string, Dictionary<double, List<FamilyInstance>>> directionYRacks = new Dictionary<string, Dictionary<double, List<FamilyInstance>>>() ;
      foreach ( var rack in racks ) {
        var widthRack = Math.Round( rack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( doc, "トレイ幅" ) ).AsDouble(), 4 ) ;
        var fromConnectorId = GetFromConnectorId( doc, rack ) ;
        if ( rack.HandOrientation.X is 1.0 or -1.0 ) {
          if ( directionXRacks.ContainsKey( fromConnectorId ) ) {
            Dictionary<double, List<FamilyInstance>> xRacks = directionXRacks[ fromConnectorId ] ;
            if ( xRacks.ContainsKey( widthRack ))
              xRacks[ widthRack ].Add( rack );
            else {
              xRacks.Add( widthRack, new List<FamilyInstance>() { rack } );
            }
          }
          else {
            Dictionary<double, List<FamilyInstance>> xRacks = new Dictionary<double, List<FamilyInstance>> { { widthRack, new List<FamilyInstance>() { rack } } } ;
            directionXRacks.Add( fromConnectorId, xRacks ) ;
          }
        }
        else if ( rack.HandOrientation.Y is 1.0 or -1.0 ) {
          if ( directionYRacks.ContainsKey( fromConnectorId ) ) {
            Dictionary<double, List<FamilyInstance>> yRacks = directionYRacks[ fromConnectorId ] ;
            if ( yRacks.ContainsKey( widthRack ))
              yRacks[ widthRack ].Add( rack );
            else {
              yRacks.Add( widthRack, new List<FamilyInstance>() { rack } );
            }
          }
          else {
            Dictionary<double, List<FamilyInstance>> xRacks = new Dictionary<double, List<FamilyInstance>> { { widthRack, new List<FamilyInstance>() { rack } } } ;
            directionYRacks.Add( fromConnectorId, xRacks ) ;
          }
        }
      }

      if ( directionXRacks.Any() ) {
        foreach ( var (key, value) in directionXRacks ) {
          foreach ( var xRack in value ) {
            CreateNotation( doc, app, rackNotationStorable, xRack.Value, key, true ) ;
          }
        }
      }

      if ( directionYRacks.Any() ) {
        foreach ( var (key, value) in directionYRacks ) {
          foreach ( var yRack in value ) {
            CreateNotation( doc, app, rackNotationStorable, yRack.Value, key, false ) ;
          }
        }
      }

      rackNotationStorable.Save() ;
    }

    private static string GetFromConnectorId(Document doc, Element rack )
    {
      var fromElementId = rack.ParametersMap.get_Item( "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( doc, "From-Side Connector Id" ) ).AsString() ;
      if ( string.IsNullOrEmpty( fromElementId ) ) return string.Empty ;
      var fromConnector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.Id.IntegerValue.ToString() == fromElementId ) ;
      if ( ! fromConnector!.IsTerminatePoint() && ! fromConnector!.IsPassPoint() ) return fromElementId ;
      fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorId, out string? fromConnectorId ) ;
      fromElementId = fromConnectorId! ;

      return fromElementId ;
    }

    private static void CreateNotation( Document doc, Application app, RackNotationStorable rackNotationStorable, IReadOnlyCollection<FamilyInstance> racks, string fromConnectorId, bool isDirectionX )
    {
      var count = racks.Count ;
      var rack = racks.ElementAt( count / 2 ) ;
      var bendRadiusRack = rack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( doc, "トレイ幅" ) ).AsDouble() ;
      var notationModel = rackNotationStorable.RackNotationModelData.FirstOrDefault( n => n.FromConnectorId == fromConnectorId && n.IsDirectionX == isDirectionX && Math.Abs( n.RackWidth - Math.Round( bendRadiusRack, 4 ) ) == 0 ) ;
      if ( notationModel == null ) {
        const double dPlus = 0.5 ;
        List<XYZ> points = new List<XYZ>() ;
        List<string> lineIds = new List<string>() ;

        var lenghtRack = rack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ) ).AsDouble() / 2 ;
        var (x, y, z) = ( rack.Location as LocationPoint )!.Point ;
        var firstPoint = isDirectionX ? new XYZ( rack.HandOrientation.X is 1.0 ? x + lenghtRack : x - lenghtRack, y + bendRadiusRack / 2, z ) : new XYZ( x + bendRadiusRack / 2, rack.HandOrientation.Y is 1.0 ? y + lenghtRack : y - lenghtRack, z ) ;

        if ( ! isDirectionX ) {
          points.Add( new XYZ( firstPoint.X + dPlus * ( count > 1 ? 20 : 15 ), firstPoint.Y, firstPoint.Z ) ) ;
        }
        else {
          points.Add( new XYZ( firstPoint.X, firstPoint.Y + dPlus * 6, firstPoint.Z ) ) ;
          points.Add( new XYZ( firstPoint.X + dPlus * ( count > 1 ? 20 : 15 ), firstPoint.Y + dPlus * 6, firstPoint.Z ) ) ;
        }

        foreach ( var nextP in points ) {
          var curve = Line.CreateBound( firstPoint, nextP ) ;
          var detailCurve = doc.Create.NewDetailCurve( doc.ActiveView, curve ) ;
          lineIds.Add( detailCurve.Id.IntegerValue.ToString() ) ;
          firstPoint = nextP ;
        }

        ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
        var noteWidth = 0.1 ;

        // make sure note width works for the text type
        var minWidth = TextElement.GetMinimumAllowedWidth( doc, defaultTextTypeId ) ;
        var maxWidth = TextElement.GetMaximumAllowedWidth( doc, defaultTextTypeId ) ;
        if ( noteWidth < minWidth ) {
          noteWidth = minWidth ;
        }
        else if ( noteWidth > maxWidth ) {
          noteWidth = maxWidth ;
        }

        TextNoteOptions opts = new( defaultTextTypeId ) { HorizontalAlignment = HorizontalTextAlignment.Left } ;

        var notation = count > 1 ? Notation + " x " + racks.Count : Notation ;
        var txtPosition = new XYZ( firstPoint.X - dPlus * ( count > 1 ? 16 : 12 ), firstPoint.Y + dPlus * 3, firstPoint.Z ) ;
        var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, notation, opts ) ;

        foreach ( var item in racks ) {
          var rackNotationModel = new RackNotationModel( item.Id.IntegerValue.ToString(), textNote.Id.IntegerValue.ToString(), rack.Id.IntegerValue.ToString(), fromConnectorId, isDirectionX, Math.Round( bendRadiusRack, 4 ), string.Join( ",", lineIds ) ) ;
          rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
        }
      }
      else {
        var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.Id.IntegerValue.ToString() == notationModel.NotationId ) ;
        if ( textElement == null ) return ;
        var textNote = textElement as TextNote ;
        var text = textNote!.Text ;
        if ( text.Contains( 'x' ) ) {
          var number = text.Substring( text.IndexOf( 'x' ) + 1 ).Trim('\r') ;
          textNote.Text = text.Substring( 0, text.IndexOf( 'x' ) + 2 ) + ( Convert.ToInt16( number ) + count );
        }
        else {
          textNote.Text = text.Trim('\r') + " x " + ( 1 + count ) ;
        }
        foreach ( var item in racks ) {
          var rackNotationModel = new RackNotationModel( item.Id.IntegerValue.ToString(), notationModel.NotationId, notationModel.RackNotationId, fromConnectorId, isDirectionX, notationModel.RackWidth, notationModel.LineIds ) ;
          rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
        }
      }
    }
  }
}