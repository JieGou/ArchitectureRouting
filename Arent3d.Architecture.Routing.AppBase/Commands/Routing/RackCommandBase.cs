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
using Arent3d.Architecture.Routing.AppBase.Utils;
using Arent3d.Utility ;
using ImportDwgMappingModel = Arent3d.Architecture.Routing.AppBase.Model.ImportDwgMappingModel ;
using Transform = Autodesk.Revit.DB.Transform ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class RackCommandBase : IExternalCommand
  {

    private static readonly double MaxDistanceTolerance = 20d.MillimetersToRevitUnits() ; // Max Distance Tolerance when find Connector Closest
    private const double BendRadiusSettingForStandardFamilyType = 20.5 ;
    private const double RatioBendRadius = 3.45 ;
    private const string Notation = "CR (W:{0})" ;
    private const char XChar = 'x' ;
    public const string NotationSelectionName = "ARENT_RACK_NOTATION" ;

    public static IReadOnlyDictionary<byte, string> RackTypes { get ; } = new Dictionary<byte, string> { { 0, "Normal Rack" }, { 1, "Limit Rack" } } ;

    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      try {
        var result = uiDocument.Document.Transaction( "TransactionName.Commands.Rack.CreateCableRackFroAllRoute".GetAppStringByKeyOrDefault( "Create Cable Rack For All Route" ), _ =>
          {
            var parameterName = uiDocument.Document.GetParameterName( RoutingParameter.RouteName ) ;
            if ( null == parameterName ) 
              return Result.Failed ;

            var filter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateSharedParameterApplicableRule( parameterName ) ) ;

            // Get all route names
            var routeNames = uiDocument.Document.GetAllElements<Element>()
              .OfCategory( BuiltInCategorySets.Conduits )
              .OfNotElementType()
              .Where( filter )
              .Select( x => x.GetRouteName() )
              .Distinct() ;

            // Create cable rack for each route
            var racks = new List<FamilyInstance>() ;
            foreach ( var routeName in routeNames ) {
              CreateCableRackForRoute( uiDocument, routeName, racks ) ;
            }
            
            // Insert notation for racks
            CreateNotationForRack( uiDocument.Document, racks ) ;

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

    private static void CreateCableRackForRoute( UIDocument uiDocument, string? routeName, List<FamilyInstance> racks )
    {
      if ( routeName == null ) 
        return ;
      
      var document = uiDocument.Document ;
      // Get all elements in route
      var allElementsInRoute = document.GetAllElementsOfRouteName<Element>( routeName ) ;
      CreateRackForConduit( uiDocument, allElementsInRoute, racks ) ;
    }
    
    public static Connector? GetConnectorClosestTo( List<Connector> connectors, XYZ point, double maxDistance = double.MaxValue )
    {
      var minDistance = double.MaxValue ;
      Connector? targetConnector = null ;

      foreach ( var connector in connectors ) {
        var distance = connector.Origin.DistanceTo( point ) ;

        if ( ! ( distance < minDistance ) || ! ( distance <= maxDistance ) ) 
          continue ;
        
        targetConnector = connector ;
        minDistance = distance ;
      }

      return targetConnector ;
    }
    
    private static Connector? GetFirstConnector( ConnectorSet connectors )
    {
      foreach ( Connector connector in connectors ) {
        if ( 0 == connector.Id ) 
          return connector ;
      }

      return null ;
    }
    
    public static bool ExistsCableTray( Document document, FamilyInstance familyInstance )
    {
      return document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.CableTrays ).OfNotElementType()
        .Any( x => IsSameLocation( x.Location, familyInstance.Location ) && x.Id != familyInstance.Id &&
                     x.FacingOrientation.IsAlmostEqualTo( familyInstance.FacingOrientation ) &&
                     IsSameConnectors( x.GetConnectors(), familyInstance.GetConnectors().ToList() )) ;
    }
    
    private static bool IsSameLocation( Location location, Location otherLocation )
    {
      if ( location is LocationPoint locationPoint ) {
        if ( otherLocation is not LocationPoint otherLocationPoint )
          return false ;

        return locationPoint.Point.DistanceTo( otherLocationPoint.Point) <= MaxDistanceTolerance &&
               Math.Abs( locationPoint.Rotation - otherLocationPoint.Rotation ) < GeometryHelper.Tolerance ;
      }

      if ( location is not LocationCurve locationCurve ) 
        return location == otherLocation ;
      
      if ( otherLocation is not LocationCurve otherLocationCurve )
        return false ;

      var line = ( locationCurve.Curve as Line )! ;

      var otherLine = ( otherLocationCurve.Curve as Line )! ;

      return line.Origin.IsAlmostEqualTo( otherLine.Origin, MaxDistanceTolerance ) &&
             line.Direction == otherLine.Direction && Math.Abs( line.Length - otherLine.Length ) < GeometryHelper.Tolerance ;
    }

    public static void CreateRackForConduit( UIDocument uiDocument, IEnumerable<Element> allElementsInRoute, List<FamilyInstance> racks )
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
            if ( conduit.FacingOrientation.Z is 1.0 or -1.0 || conduit.HandOrientation.Z is -1.0 or 1.0) {
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
      foreach ( var connector in connectors ) {
        if ( connector.IsConnected ) continue ;
        
        var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
        var toConnector = GetConnectorClosestTo( otherConnectors, connector.Origin, MaxDistanceTolerance ) ;
        if ( toConnector != null )
          connector.ConnectTo( toConnector ) ;
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

      // Set cable rack length
      instance.LookupParameter( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ) )?.Set( length ) ; // TODO may be must change when FamilyType change

      // Set cable rack width
      if ( cableRackWidth > 0 )
        instance.LookupParameter( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) )?.Set( ( cableRackWidth * scaleRatio ).MillimetersToRevitUnits() ) ;

      // Set cable rack comments
      instance.LookupParameter( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ) )?.Set( cableRackWidth == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      // Set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = GetFromAndToConnectorUniqueId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) && instance.HasParameter(  ElectricalRoutingElementParameter.ToSideConnectorId ) )
        instance.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) && instance.HasParameter(  ElectricalRoutingElementParameter.FromSideConnectorId ) )
        instance.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;

      // Set route name
      SetRouteNameForRack( conduit, instance ) ;

      // set cable tray direction
      if ( Math.Abs( 1.0 - line.Direction.Y ) < GeometryHelper.Tolerance ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z + 1 ) ), Math.PI / 2 ) ;
      }
      else if ( Math.Abs( -1.0 - line.Direction.Y ) < GeometryHelper.Tolerance ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z - 1 ) ), Math.PI / 2 ) ;
      }
      else if ( Math.Abs( -1.0 - line.Direction.X ) < GeometryHelper.Tolerance ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z - 1 ) ), Math.PI ) ;
      }
      else if ( Math.Abs( 1.0 - line.Direction.Z ) < GeometryHelper.Tolerance ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y - 1, firstConnector.Origin.Z ) ), Math.PI / 2 ) ;
      }
      else if ( Math.Abs( -1.0 - line.Direction.Z ) < GeometryHelper.Tolerance ) {
        ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y, firstConnector.Origin.Z ), new XYZ( firstConnector.Origin.X, firstConnector.Origin.Y + 1, firstConnector.Origin.Z ) ), Math.PI / 2 ) ;
      }
      
      if ( line.Direction.Z is 1.0 or -1.0 ) {
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

      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.CableTrayFitting ).FirstOrDefault() ?? throw new InvalidOperationException() ; // TODO may change in the future

      if (false == symbol.IsActive) symbol.Activate();
      var instance = document.Create.NewFamilyInstance(location.Point, symbol, null, StructuralType.NonStructural);

      // set cable tray Bend Radius
      var bendRadius = cableTrayDefaultBendRadius == 0 ? ( RatioBendRadius * diameter.RevitUnitsToMillimeters() + BendRadiusSettingForStandardFamilyType ).MillimetersToRevitUnits() : cableTrayDefaultBendRadius ;
      instance.LookupParameter( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ) )?.Set(bendRadius) ; // TODO may be must change when FamilyType change

      // set cable rack length
      instance.LookupParameter( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ) )?.Set( length ) ; // TODO may be must change when FamilyType change

      // set cable rack comments
      instance.LookupParameter( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ) )?.Set( cableTrayDefaultBendRadius == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      // set To-Side Connector Id
      var (fromConnectorId, toConnectorId) = GetFromAndToConnectorUniqueId( conduit ) ;
      if ( ! string.IsNullOrEmpty( toConnectorId ) && instance.HasParameter(  ElectricalRoutingElementParameter.ToSideConnectorId ) )
        instance.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
      if ( ! string.IsNullOrEmpty( fromConnectorId ) && instance.HasParameter(  ElectricalRoutingElementParameter.FromSideConnectorId ) )
        instance.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;

      // set route name
      SetRouteNameForRack( conduit, instance ) ;

      // set cable tray fitting direction
      if ( Math.Abs( 1.0 - conduit.FacingOrientation.X ) < GeometryHelper.Tolerance ) {
        instance.Location.Rotate( Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ), new XYZ( location.Point.X, location.Point.Y, location.Point.Z - 1 ) ), Math.PI / 2 ) ;
      }
      else if ( Math.Abs( -1.0 - conduit.FacingOrientation.X ) < GeometryHelper.Tolerance ) {
        instance.Location.Rotate( Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ), new XYZ( location.Point.X , location.Point.Y, location.Point.Z + 1 ) ), Math.PI / 2 ) ;
      }
      else if ( Math.Abs( -1.0 - conduit.FacingOrientation.Y ) < GeometryHelper.Tolerance ) {
        instance.Location.Rotate( Line.CreateBound( new XYZ( location.Point.X, location.Point.Y, location.Point.Z ), new XYZ( location.Point.X, location.Point.Y, location.Point.Z + 1 ) ), Math.PI ) ;
      }
      
      // move cable rack to under conduit
      instance.Location.Move( new XYZ( 0, 0, -30d.MillimetersToRevitUnits() ) ) ; // TODO may be must change when FamilyType change

      return instance ;
    }

    private static void SetRouteNameForRack( Element conduit, FamilyInstance instance )
    {
      var routeName = conduit.GetRouteName() ;
      if ( string.IsNullOrEmpty( routeName ) ) return ;
      
      var routeNameArray = routeName!.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      instance.SetProperty( RoutingParameter.RouteName, routeName ) ;
    }

    private static bool IsSameConnectors( IEnumerable<Connector> connectors, IList<Connector> otherConnectors )
    {
      const bool isSameConnectors = true ;
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

    public static void CreateNotationForRack( Document document, IEnumerable<FamilyInstance> instances, View? viewPlan = null )
    {
      viewPlan ??= document.ActiveView ;
      var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? document.GetRackNotationStorable() ;
      RemoveNotationUnused( document, rackNotationStorable ) ;

      var racks = instances.Where( fi => fi.IsRack() ) ;
      
      // group racks by rack route name, direction, width
      var directionXRacks = new Dictionary<string, Dictionary<double, List<FamilyInstance>>>() ;
      var directionYRacks = new Dictionary<string, Dictionary<double, List<FamilyInstance>>>() ;
      foreach ( var rack in racks ) {
        var widthRack = Math.Round( rack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble(), 4 ) ;
        var routeName = rack.GetPropertyString( RoutingParameter.RouteName ) ?? string.Empty ;
        if ( string.IsNullOrEmpty( routeName ) ) continue ;
        
        if ( rack.HandOrientation.X is 1.0 or -1.0 ) {
          if ( directionXRacks.ContainsKey( routeName ) ) {
            var xRacks = directionXRacks[ routeName ] ;
            if ( xRacks.ContainsKey( widthRack ) )
              xRacks[ widthRack ].Add( rack ) ;
            else {
              xRacks.Add( widthRack, new List<FamilyInstance>() { rack } ) ;
            }
          }
          else {
            var xRacks = new Dictionary<double, List<FamilyInstance>> { { widthRack, new List<FamilyInstance>() { rack } } } ;
            directionXRacks.Add( routeName, xRacks ) ;
          }
        }
        else if ( rack.HandOrientation.Y is 1.0 or -1.0 ) {
          if ( directionYRacks.ContainsKey( routeName ) ) {
            var yRacks = directionYRacks[ routeName ] ;
            if ( yRacks.ContainsKey( widthRack ) )
              yRacks[ widthRack ].Add( rack ) ;
            else {
              yRacks.Add( widthRack, new List<FamilyInstance>() { rack } ) ;
            }
          }
          else {
            var xRacks = new Dictionary<double, List<FamilyInstance>> { { widthRack, new List<FamilyInstance>() { rack } } } ;
            directionYRacks.Add( routeName, xRacks ) ;
          }
        }
      }

      // for each route, create one tag for the longest rack in array of racks with same width and direction
      if ( directionXRacks.Any() ) {
        foreach ( var (routeName, widthToRacksDictionary) in directionXRacks ) {
          foreach ( var widthRacks in widthToRacksDictionary ) {
            CreateNotation( document, rackNotationStorable, widthRacks.Value, routeName, true, viewPlan ) ;
          }
        }
      }

      if ( directionYRacks.Any() ) {
        foreach ( var (routeName, widthToRacksDictionary) in directionYRacks ) {
          foreach ( var widthRacks in widthToRacksDictionary ) {
            CreateNotation( document, rackNotationStorable, widthRacks.Value, routeName, false, viewPlan ) ;
          }
        }
      }

      rackNotationStorable.Save() ;
    }

    private static void CreateNotation( Document doc, RackNotationStorable rackNotationStorable, IReadOnlyCollection<FamilyInstance> racks, string fromConnectorId, bool isDirectionX, View view )
    {
      var scale = view.Scale ;
      var magnification = ImportDwgMappingModel.GetMagnificationOfView( scale ) ;
      var longestRack = racks.MaxBy( x => x.TryGetRackLength(out var length) ? length : 0 ) ;
      if ( longestRack != null ) {
        var widthCableTray2D = CableRackUtils.RackWidthOnPlanView( scale );
        var widthCableTray = longestRack.TryGetRackWidth( out var w ) ? w : widthCableTray2D ;
        var notationModel = rackNotationStorable.RackNotationModelData.FirstOrDefault( n => n.FromConnectorId == fromConnectorId && n.IsDirectionX == isDirectionX && Math.Abs( n.RackWidth - Math.Round( widthCableTray, 4 ) ) == 0 ) ;
        if ( notationModel == null ) {
          if ( view is ViewPlan viewPlan ) {
            
            // target point of leader line
            var point = ( longestRack.Location as LocationPoint )!.Point ;
            var connectors = longestRack.MEPModel.ConnectorManager.Connectors.OfType<Connector>().ToList() ;
            var rackWidthInMillimeters = widthCableTray.RevitUnitsToMillimeters() ;
            if ( connectors.Count == 2 )
              point = ( connectors[ 0 ].Origin + connectors[ 1 ].Origin ) * 0.5 + widthCableTray2D * 0.5 * ( isDirectionX ? XYZ.BasisY : -XYZ.BasisX ) ;

            // content to show
            var notation = rackWidthInMillimeters switch
            {
              > 600 => string.Format( Notation, ( rackWidthInMillimeters / 2 ).ToString( CultureInfo.CurrentCulture ) ) + " x 2",
              _ => string.Format( Notation, rackWidthInMillimeters.ToString( CultureInfo.CurrentCulture ) ),
            } ;
            if ( longestRack.TryGetProperty( ElectricalRoutingElementParameter.Cover, out string? text ) && text is not null && ! text.Equals( "無し" ) )
              notation += " カバー付" ;

            // create text note
            var textNoteType = TextNoteHelper.FindOrCreateTextNoteType( doc, TextNoteHelper.TextSize, false ) ;
            if ( null == textNoteType ) return ;
            TextNote textNote ;

            const double multiple = 3 ;
            var heightText = TextNoteHelper.TotalHeight.MillimetersToRevitUnits() ;
            if ( isDirectionX ) {
              var vector = ( XYZ.BasisX * heightText * multiple + XYZ.BasisY * heightText * multiple + XYZ.BasisY * heightText ) * magnification ;
              textNote = TextNote.Create( doc, view.Id, point + vector, notation, textNoteType.Id ) ;
            }
            else {
              var vector = ( XYZ.BasisX.Negate() * heightText * multiple + XYZ.BasisY * heightText * multiple + XYZ.BasisY * heightText ) * magnification ;
              textNote = TextNote.Create( doc, view.Id, point + vector, notation, textNoteType.Id ) ;
              ElementTransformUtils.MirrorElements( doc, new List<ElementId> { textNote.Id }, Plane.CreateByNormalAndOrigin( XYZ.BasisX, textNote.Coord ), false ) ;
            }

            doc.Regenerate() ;

            // create 2 detail lines
            var underLineTextNote = CreateUnderLineText( textNote, viewPlan.GenLevel.Elevation ) ;
            var nearestPoint = underLineTextNote.GetEndPoint( 0 ).DistanceTo( point ) > underLineTextNote.GetEndPoint( 1 ).DistanceTo( point ) ? underLineTextNote.GetEndPoint( 1 ) : underLineTextNote.GetEndPoint( 0 ) ;
            var notUsedForIntersect = new List<Element> { longestRack } ;
            var curves = GeometryHelper.GetCurvesAfterIntersection( viewPlan, new List<Curve> { Line.CreateBound( nearestPoint, new XYZ( point.X, point.Y, viewPlan.GenLevel.Elevation ) ) }, new List<Type> { typeof( CableTray ) }, notUsedForIntersect ) ;
            curves.Add( underLineTextNote ) ;

            var detailCurves = NotationHelper.CreateDetailCurve( view, curves ) ;
            var curveClosestPoint = GeometryHelper.GetCurveClosestPoint( detailCurves, point ) ;

            (string? endLineUniqueId, int? endPoint) endLineLeader = ( curveClosestPoint.DetailCurve?.UniqueId, endPoint: curveClosestPoint.EndPoint ) ;
            var otherLineId = detailCurves.Select( x => x.UniqueId ).Where( x => x != endLineLeader.endLineUniqueId ).ToList() ;
            
            FilterUtil.AddElementToSelectionFilter(NotationSelectionName, textNote);
            FilterUtil.AddElementsToSelectionFilter(doc, NotationSelectionName, detailCurves);

            foreach ( var item in racks ) {
              var rackNotationModel = new RackNotationModel( item.UniqueId, textNote.UniqueId, longestRack.UniqueId, fromConnectorId, isDirectionX, Math.Round( widthCableTray, 4 ), endLineLeader.endLineUniqueId, endLineLeader.endPoint, otherLineId ) ;
              rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
            }
          }
        }
        else {
          foreach ( var item in racks ) {
            var rackNotationModel = new RackNotationModel( item.UniqueId, notationModel.NotationId, notationModel.RackNotationId, fromConnectorId, isDirectionX, notationModel.RackWidth, notationModel.EndLineLeaderId, notationModel.EndPoint, notationModel.OtherLineIds ) ;
            rackNotationStorable.RackNotationModelData.Add( rackNotationModel ) ;
          }
        }
      }
    }

    public static Line CreateUnderLineText( TextNote textNote, double elevation )
    {
      var view = textNote.Document.GetElement(textNote.OwnerViewId) as View;
      var offset = textNote.TextNoteType.get_Parameter( BuiltInParameter.LEADER_OFFSET_SHEET ).AsDouble() ;
      var height = ( textNote.Height + offset ) * view!.Scale ;
      var coord = Transform.CreateTranslation( textNote.UpDirection.Negate() * height ).OfPoint( textNote.Coord ) ;
      coord = Transform.CreateTranslation( ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? 1 : -1 ) * offset * view.Scale * textNote.BaseDirection ).OfPoint( coord ) ;
      var width = ( textNote.HorizontalAlignment == HorizontalTextAlignment.Right ? -1 : 1 ) * ( textNote.Width + 2 * offset ) * view.Scale ;
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
  }
}