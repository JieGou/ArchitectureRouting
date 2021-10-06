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
    private readonly double maxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;

    private readonly int minNumberOfMultiplicity = 5 ;
    private readonly double minLengthOfConduit = ( 3.0 ).MetersToRevitUnits() ;
    private readonly double cableTrayDefaultBendRadius = ( 16.0 ).MillimetersToRevitUnits() ;

    private readonly double[] cableTrayWidthMapping = { 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0, 1200.0 } ;

    private Dictionary<ElementId, List<Connector>> elbowsToCreate = new Dictionary<ElementId, List<Connector>>() ;

    private Dictionary<string, double> routeLengthCache = new Dictionary<string, double>() ;

    private Dictionary<string, Dictionary<int, double>> routeMaxWidthCache =
      new Dictionary<string, Dictionary<int, double>>() ;

    protected abstract AddInType GetAddInType() ;

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

            foreach ( var elbow in elbowsToCreate ) {
              CreateElbow( uiDocument, elbow.Key, elbow.Value ) ;
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

          Connector firstConnector =
            NewRackCommandBase.GetFirstConnector( conduit.GetConnectorManager()!.Connectors )! ;

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
          if ( false == symbol.IsActive ) symbol.Activate() ;
          // Create cable tray
          var instance =
            document.Create.NewFamilyInstance( firstConnector.Origin, symbol, null, StructuralType.NonStructural ) ;

          // set cable rack length
          SetParameter( instance,
            "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ),
            length ) ; // TODO may be must change when FamilyType change

          // set cable rack length
          SetParameter( instance,
            "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ),
            cableRackWidth.MillimetersToRevitUnits() ) ; // TODO may be must change when FamilyType change

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

          // check cable tray exists
          if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
            transaction.RollBack() ;
            return ;
          }

          if ( 1.0 != line.Direction.Z && -1.0 != line.Direction.Z ) {
            var elbows = conduit.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd()
              .Select( c => c.Owner ).OfType<FamilyInstance>() ;
            foreach ( var elbow in elbows ) {
              if ( elbowsToCreate.ContainsKey( elbow.Id ) ) {
                elbowsToCreate[ elbow.Id ]
                  .Add( NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(),
                    ( elbow.Location as LocationPoint )!.Point )! ) ;
              }
              else {
                elbowsToCreate.Add( elbow.Id,
                  new List<Connector>()
                  {
                    NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(),
                      ( elbow.Location as LocationPoint )!.Point )!
                  } ) ;
              }
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
    /// Create elbow for 2 cable rack
    /// </summary>
    /// <param name="uiDocument"></param>
    /// <param name="elementId"></param>
    /// <param name="connectors"></param>
    private void CreateElbow( UIDocument uiDocument, ElementId elementId, List<Connector> connectors )
    {
      var document = uiDocument.Document ;
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var conduit = document.GetElementById<FamilyInstance>( elementId )! ;

        // Ignore the case of vertical conduits in the oz direction
        if ( 1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.HandOrientation.Z || 1.0 == conduit.HandOrientation.Z) {
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
        if ( false == symbol.IsActive ) symbol.Activate() ;

        var instance = document.Create.NewFamilyInstance( location.Point, symbol, uiDocument.ActiveView.GenLevel,
          StructuralType.NonStructural ) ;

        // set cable rack length
        SetParameter( instance,
          "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ),
          length ) ; // TODO may be must change when FamilyType change

        // set cable tray Bend Radius
        SetParameter( instance,
          "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( document, "Bend Radius" ),
          cableTrayDefaultBendRadius ) ; // TODO may be must change when FamilyType change

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
        if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
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
        var secondCableRackWidth = secondCableRack.ParametersMap
          .get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) )
          .AsDouble() ; // TODO may be must change when FamilyType change

        // set cable rack length
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ),
          firstCableRackWidth >= secondCableRackWidth
            ? firstCableRackWidth
            : secondCableRackWidth ) ; // TODO may be must change when FamilyType change

        foreach ( var connector in instance.GetConnectors() ) {
          var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
          if ( null != otherConnectors ) {
            var connectTo =
              NewRackCommandBase.GetConnectorClosestTo( otherConnectors, connector.Origin, maxDistanceTolerance ) ;
            if ( connectTo != null ) {
              connector.ConnectTo( connectTo ) ;
            }
          }
        }

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }

    /// <summary>
    /// Calculate cable rack width base on sum diameter of route
    /// </summary>
    /// <param name="document"></param>
    /// <param name="subRoute"></param>
    /// <returns></returns>
    private double CalcCableRackWidth( Document document, SubRoute subRoute )
    {
      var routes = RouteCache.Get( document ) ;
      var sumDiameter = subRoute.GetSubRouteGroup()
        .Sum( s => routes.GetSubRoute( s )?.GetDiameter().RevitUnitsToMillimeters() + 10 ) + 120 ;
      var cableTraywidth = 0.6 * sumDiameter ;
      foreach ( var width in cableTrayWidthMapping ) {
        if ( cableTraywidth <= width ) {
          cableTraywidth = width ;
          return cableTraywidth!.Value ;
        }
      }

      return cableTraywidth!.Value ;
    }

    /// <summary>
    /// Calculate cable rack max width
    /// </summary>
    /// <param name="element"></param>
    /// <param name="elements"></param>
    /// <param name="document"></param>
    /// <returns></returns>
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
        if ( elbowsConnected.Any() && straightsConnected.Any() && null != element.Item2.PreviousSubRoute &&
             straightsConnected.First().GetSubRouteIndex()!.Value == element.Item2.PreviousSubRoute!.SubRouteIndex ) {
          var key = routeMaxWidthCache[ routeName ].Keys
            .Where( x => x <= element.Item2.PreviousSubRoute!.SubRouteIndex ).Max() ;
          return routeMaxWidthCache[ routeName ][ key ] ;
        }
        else if ( elbowsConnected.Any() &&
                  ( null == element.Item2.PreviousSubRoute || ( null != element.Item2.PreviousSubRoute &&
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

    /// <summary>
    /// Calculate cable rack length
    /// </summary>
    /// <param name="routeName"></param>
    /// <param name="elements"></param>
    /// <param name="document"></param>
    /// <returns></returns>
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