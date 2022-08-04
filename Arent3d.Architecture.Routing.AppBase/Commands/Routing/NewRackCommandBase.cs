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
using System.Globalization ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.ApplicationServices ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;
using Transform = Autodesk.Revit.DB.Transform ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
  public abstract class NewRackCommandBase : IExternalCommand
  {
    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private static readonly double MaxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;
    private const double BendRadiusSettingForStandardFamilyType = 20.5 ;
    private const double RatioBendRadius = 3.45 ;
    private const string Notation = "CR (W:{0})" ;
    private const char XChar = 'x' ;

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
        return locationPoint.Point.DistanceTo( otherLocationPoint.Point) <= MaxDistanceTolerance &&
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

        return line.Origin.IsAlmostEqualTo( otherLine.Origin, MaxDistanceTolerance ) &&
               line.Direction == otherLine.Direction && line.Length == otherLine.Length ;
      }

      return location == otherLocation ;
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
            
            racks.Add( instance );
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
            var connectTo = GetConnectorClosestTo( otherConnectors, connector.Origin, MaxDistanceTolerance ) ;
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
      var scaleRatio = uiDocument.Document.ActiveView.Scale / 100.0 ;

      var location = ( element.Location as LocationCurve )! ;
      var line = ( location.Curve as Line )! ;
      
      Connector firstConnector = GetFirstConnector( element.GetConnectorManager()!.Connectors )! ;

      var length = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ;
      var diameter = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.OutsideDiameter".GetDocumentStringByKeyOrDefault( document, "Outside Diameter" ) ).AsDouble() ;

      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.CableTray ).FirstOrDefault() ?? throw new InvalidOperationException() ; // TODO may change in the future

      // Create cable tray
      if (false == symbol.IsActive) symbol.Activate();
      var instance = document.Create.NewFamilyInstance(new XYZ(firstConnector.Origin.X, firstConnector.Origin.Y, line.Origin.Z), symbol, null, StructuralType.NonStructural);

      // set cable rack length
      SetParameter( instance, "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ), length ) ; // TODO may be must change when FamilyType change

      // set cable rack length
      if ( cableRackWidth > 0 )
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ),
          ( cableRackWidth * scaleRatio ).MillimetersToRevitUnits() ) ;

      // set cable rack comments
      SetParameter( instance, "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), cableRackWidth == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      // set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = GetFromAndToConnectorUniqueId( conduit ) ;
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
        instance.Location.Move( new XYZ( 0, 0, -30d.MillimetersToRevitUnits() ) ) ; // TODO may be must change when FamilyType change
      }

      return instance ;
    }
    
    public static FamilyInstance CreateRackForFittingConduit( UIDocument uiDocument, FamilyInstance conduit, LocationPoint location, double cableTrayDefaultBendRadius = 0)
    {
      var document = uiDocument.Document ;
      
      var length = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.ConduitFitting.Length".GetDocumentStringByKeyOrDefault( document, "電線管長さ" ) ).AsDouble() ;
      var diameter = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.NominalDiameter".GetDocumentStringByKeyOrDefault( document, "継手外径" ) ).AsDouble() ;
      var bendRadius = conduit.ParametersMap.get_Item( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) ).AsDouble() ;

      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.CableTrayFitting ).FirstOrDefault() ?? throw new InvalidOperationException() ; // TODO may change in the future

      if (false == symbol.IsActive) symbol.Activate();
      var instance = document.Create.NewFamilyInstance(location.Point, symbol, null, StructuralType.NonStructural);

      // set cable tray Bend Radius
      bendRadius = cableTrayDefaultBendRadius == 0 ? ( RatioBendRadius * diameter.RevitUnitsToMillimeters() + BendRadiusSettingForStandardFamilyType ).MillimetersToRevitUnits() : cableTrayDefaultBendRadius ;
      SetParameter( instance, "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ), bendRadius ) ; // TODO may be must change when FamilyType change

      // set cable rack length
      SetParameter( instance, "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ), length ) ; // TODO may be must change when FamilyType change

      // set cable rack comments
      SetParameter( instance, "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), cableTrayDefaultBendRadius == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      // set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = GetFromAndToConnectorUniqueId( conduit ) ;
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
      instance.Location.Move( new XYZ( 0, 0, -30d.MillimetersToRevitUnits() ) ) ; // TODO may be must change when FamilyType change

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

    public static ( string, string ) GetFromAndToConnectorUniqueId( Element conduit )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ?? throw new NullReferenceException() ;
      var fromConnectorId = fromEndPointKey.GetElementUniqueId() ;

      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ?? throw new NullReferenceException() ;
      var toConnectorId = toEndPointKey.GetElementUniqueId() ;

      return ( fromConnectorId, toConnectorId ) ;
    }

    public static void CreateNotationForRack(Document doc, Application app, IEnumerable<FamilyInstance> racks )
    {
      var rackNotationStorable = doc.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? doc.GetRackNotationStorable() ;
      RemoveNotationUnused( doc, rackNotationStorable ) ;
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

      var defaultSymbolMagnification = ImportDwgMappingModel.GetDefaultSymbolMagnification( doc ) ;
      
      if ( directionXRacks.Any() ) {
        foreach ( var (key, value) in directionXRacks ) {
          foreach ( var xRack in value ) {
            CreateNotation( doc, rackNotationStorable, xRack.Value, key, true, defaultSymbolMagnification ) ;
          }
        }
      }

      if ( directionYRacks.Any() ) {
        foreach ( var (key, value) in directionYRacks ) {
          foreach ( var yRack in value ) {
            CreateNotation( doc, rackNotationStorable, yRack.Value, key, false, defaultSymbolMagnification ) ;
          }
        }
      }

      rackNotationStorable.Save() ;
    }

    private static string GetFromConnectorId(Document doc, Element rack )
    {
      var fromElementId = rack.ParametersMap.get_Item( "Revit.Property.Builtin.FromSideConnectorId".GetDocumentStringByKeyOrDefault( doc, "From-Side Connector Id" ) ).AsString() ;
      if ( string.IsNullOrEmpty( fromElementId ) ) return string.Empty ;
      var fromConnector = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == fromElementId ) ;
      if ( ! fromConnector!.IsTerminatePoint() && ! fromConnector!.IsPassPoint() ) return fromElementId ;
      fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorId ) ;
      fromElementId = fromConnectorId! ;

      return fromElementId ;
    }

    private static void CreateNotation( Document doc, RackNotationStorable rackNotationStorable, IReadOnlyCollection<FamilyInstance> racks, string fromConnectorId, bool isDirectionX, double scale )
    {
      const string xSymbol = " x " ;
      var count = racks.Count ;
      var rack = racks.OrderByDescending( x => x.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ) ).AsDouble() ).FirstOrDefault() ;
      if ( rack != null ) {
        var widthCableTray = rack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( doc, "トレイ幅" ) ).AsDouble() ;
        var notationModel = rackNotationStorable.RackNotationModelData.FirstOrDefault( n => n.FromConnectorId == fromConnectorId && n.IsDirectionX == isDirectionX && Math.Abs( n.RackWidth - Math.Round( widthCableTray, 4 ) ) == 0 ) ;
        if ( notationModel == null ) {
          if ( doc.ActiveView is ViewPlan viewPlan ) {
            var point = ( rack.Location as LocationPoint )!.Point ;
            var connectors = rack.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ;
            point = new XYZ( 0.5 * ( connectors[ 0 ].Origin.X + connectors[ 1 ].Origin.X ), 0.5 * ( connectors[ 0 ].Origin.Y + connectors[ 1 ].Origin.Y ), point.Z ) ;
            var notationDistance = widthCableTray.RevitUnitsToMillimeters() ;
            var notation = count > 1 ? string.Format( Notation, notationDistance.ToString( CultureInfo.CurrentCulture ) ) + xSymbol + racks.Count : string.Format( Notation, notationDistance.ToString( CultureInfo.CurrentCulture ) ) ;
            var textNoteType = TextNoteHelper.FindOrCreateTextNoteType( doc ) ;
            if ( null == textNoteType ) return ;
            TextNote textNote ;

            const double multiple = 3 ;
            var heightText = TextNoteHelper.TotalHeight.MillimetersToRevitUnits() ;
            if ( isDirectionX ) {
              var vector = (XYZ.BasisX * heightText * multiple + XYZ.BasisY * heightText * multiple + XYZ.BasisY * heightText )* scale ;
              var transform = Transform.CreateTranslation( vector ) ;
              textNote = TextNote.Create( doc, doc.ActiveView.Id, transform.OfPoint( point ), notation, textNoteType.Id ) ;
            }
            else {
              var vector = (XYZ.BasisX.Negate() * heightText * multiple + XYZ.BasisY * heightText * multiple + XYZ.BasisY * heightText)* scale ;
              var transform = Transform.CreateTranslation( vector ) ;
              textNote = TextNote.Create( doc, doc.ActiveView.Id, transform.OfPoint( point ), notation, textNoteType.Id ) ;
              ElementTransformUtils.MirrorElements( doc, new List<ElementId> { textNote.Id }, Plane.CreateByNormalAndOrigin( XYZ.BasisX, textNote.Coord ), false ) ;
            }

            doc.Regenerate() ;

            var underLineTextNote = CreateUnderLineText( textNote, viewPlan.GenLevel.Elevation ) ;
            var nearestPoint = underLineTextNote.GetEndPoint( 0 ).DistanceTo( point ) > underLineTextNote.GetEndPoint( 1 ).DistanceTo( point ) ? underLineTextNote.GetEndPoint( 1 ) : underLineTextNote.GetEndPoint( 0 ) ;
            var curves = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { Line.CreateBound( nearestPoint, new XYZ( point.X, point.Y, viewPlan.GenLevel.Elevation ) ) }, new List<Type> { typeof( CableTray ) } ) ;
            curves.Add( underLineTextNote ) ;

            var detailCurves = NotationHelper.CreateDetailCurve( doc.ActiveView, curves ) ;
            var curveClosestPoint = GeometryHelper.GetCurveClosestPoint( detailCurves, point ) ;

            (string? endLineUniqueId, int? endPoint) endLineLeader = ( curveClosestPoint.DetailCurve?.UniqueId, endPoint: curveClosestPoint.EndPoint ) ;
            var ortherLineId = detailCurves.Select( x => x.UniqueId ).Where( x => x != endLineLeader.endLineUniqueId ).ToList() ;

            foreach ( var item in racks ) {
              var rackNotationModel = new RackNotationModel( item.UniqueId, textNote.UniqueId, rack.UniqueId, fromConnectorId, isDirectionX, Math.Round( widthCableTray, 4 ), endLineLeader.endLineUniqueId, endLineLeader.endPoint, ortherLineId ) ;
              rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
            }
          }
        }
        else {
          var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( t => t.UniqueId == notationModel.NotationId ) ;
          if ( textElement == null ) return ;
          var textNote = textElement as TextNote ;
          var text = textNote!.Text ;
          if ( text.Contains( XChar ) ) {
            var number = text.Substring( text.IndexOf( XChar ) + 1 ).Trim( '\r' ) ;
            textNote.Text = text.Substring( 0, text.IndexOf( XChar ) + 2 ) + ( Convert.ToInt16( number ) + count ) ;
          }
          else {
            textNote.Text = text.Trim( '\r' ) + xSymbol + ( 1 + count ) ;
          }

          foreach ( var item in racks ) {
            var rackNotationModel = new RackNotationModel( item.UniqueId, notationModel.NotationId, notationModel.RackNotationId, fromConnectorId, isDirectionX, notationModel.RackWidth, 
              notationModel.EndLineLeaderId, notationModel.EndPoint, notationModel.OtherLineIds ) ;
            rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
          }
        }
      }
    }

    public static Line CreateUnderLineText( TextNote textNote, double elevation )
    {
      var offset = textNote.TextNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).AsDouble() ;
      var height = ( textNote.Height + offset ) * textNote.Document.ActiveView.Scale ;
      var coord = Transform.CreateTranslation( textNote.UpDirection.Negate() * height ).OfPoint( textNote.Coord ) ;
      coord = Transform.CreateTranslation( ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? 1 : -1 ) * offset * textNote.Document.ActiveView.Scale * textNote.BaseDirection ).OfPoint( coord ) ;
      var width = ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? -1 : 1 ) * ( textNote.Width + 2 * offset ) * textNote.Document.ActiveView.Scale ;
      var middle = Transform.CreateTranslation( textNote.BaseDirection * width ).OfPoint( coord ) ;

      return Line.CreateBound( new XYZ( coord.X, coord.Y, elevation ), new XYZ( middle.X, middle.Y, elevation ) ) ;
    }

    private static void RemoveNotationUnused( Document doc, RackNotationStorable rackNotationStorable )
    {
      var notationUnused = new List<RackNotationModel>() ;
      if ( ! rackNotationStorable.RackNotationModelData.Any() ) return ;
      foreach ( var notationModel in rackNotationStorable.RackNotationModelData ) {
        var rack = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.RackTypeElements ).FirstOrDefault( c => c.UniqueId == notationModel.RackId ) ;
        if ( rack != null ) continue ;
        notationUnused.Add( notationModel ) ;
        var textElement = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).FirstOrDefault( e => e.UniqueId == notationModel.NotationId ) ;
        if ( textElement == null ) continue ;
        var textNote = textElement as TextNote ;
        var text = textNote!.Text ;
        if ( text.Contains( XChar ) ) {
          var number = text.Substring( text.IndexOf( XChar ) + 1 ).Trim( '\r' ) ;
          textNote.Text = Convert.ToInt16( number ) - 1 == 1 ? text.Substring( 0, text.IndexOf( XChar ) - 1 ) : text.Substring( 0, text.IndexOf( XChar ) + 2 ) + ( Convert.ToInt16( number ) - 1 ) ;
        }
        else {
          doc.Delete( textElement.Id ) ;
        }
      }

      if ( ! notationUnused.Any() ) return ;
      foreach ( var notationModel in notationUnused ) {
        rackNotationStorable.RackNotationModelData.Remove( notationModel ) ;
      }

      rackNotationStorable.Save() ;
    }

    private static void CreateNewTextNoteType( Document doc, TextNote textNote )
    {
      //Create new text type
      string strStyleName = "Rack Notation Type" ;

      var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( strStyleName, tt.Name ) ) ;
      if ( textNoteType == null ) {
        // Create new Note type
        Element ele = textNote.TextNoteType.Duplicate( strStyleName ) ;
        textNoteType = ( ele as TextNoteType ) ! ;
        textNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).Set( ( 1.0 / 32.0 ) * ( 1.0 / 12.0 ) ) ;
        textNoteType.get_Parameter( BuiltInParameter.LEADER_ARROWHEAD ).Set( ElementId.InvalidElementId ) ;
      }

      // Change the text notes type to the new type
      textNote.ChangeTypeId( textNoteType!.Id ) ;
    }
  }
}