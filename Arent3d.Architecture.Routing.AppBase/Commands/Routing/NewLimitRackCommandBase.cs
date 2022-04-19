using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Utility ;
using Autodesk.Revit.ApplicationServices ;

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
      UIApplication uiApp = commandData.Application ;
      Application app = uiApp.Application ;
      try {
        var result = document.Transaction(
          "TransactionName.Commands.Rack.CreateLimitCableRack".GetAppStringByKeyOrDefault( "Create Limit Cable" ), _ =>
          {
            var racks = new List<FamilyInstance>() ;
            var fittings = new List<FamilyInstance>() ;
            var elements = document.CollectAllMultipliedRoutingElements( minNumberOfMultiplicity ) ;
            foreach ( var element in elements ) {
              var (mepCurve, subRoute) = element ;
              if ( RouteLength( subRoute.Route.RouteName, elements, document ) >= minLengthOfConduit ) {
                var conduit = ( mepCurve as Conduit )! ;
                var cableRackWidth = CalcCableRackMaxWidth( element, elements, document ) ;

                CreateCableRackForConduit( uiDocument, conduit, cableRackWidth, racks ) ;
              }
            }

            foreach ( var elbow in elbowsToCreate ) {
              CreateElbow( uiDocument, elbow.Key, elbow.Value, fittings ) ;
            }

            var newRacks = ConnectedRacks( document, racks, fittings ) ;
            
            //insert notation for racks
            NewRackCommandBase.CreateNotationForRack( document, app, newRacks ) ;

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

    private static List<FamilyInstance> ConnectedRacks( Document document, List<FamilyInstance> racks, List<FamilyInstance> fittings )
    {
      racks = racks.Where( MEPModelOnPlan ).ToList() ;
      fittings = fittings.Where( MEPModelOnPlan ).ToList() ;
      if ( ! racks.Any() )
        return fittings ;

      var groupRacks = new List<List<FamilyInstance>>() ;
      while ( racks.Any() ) {
        var rack = racks[ 0 ] ;
        racks.RemoveAt( 0 ) ;
        var subRacks = new List<FamilyInstance> { rack } ;

        if ( ! racks.Any() ) {
          groupRacks.Add(subRacks);
        }
        else {
          int count ;
          do {
            count = subRacks.Count ;
            var flag = false ;
            
            for ( var i = 0 ; i < racks.Count ; i++ ) {
              foreach ( var con in GetConnector(racks[ i ]) ) {
                if ( GetConnector(subRacks.Last()).Any( c => con.Origin.DistanceTo( c.Origin ) < GeometryUtil.Tolerance ) ) {
                  subRacks.Add(racks[i]);
                  racks.RemoveAt( i ) ;
                  flag = true ;
                }

                if(flag)
                  break;
              }
              
              if ( flag )
                break ;
            }
          } while ( count != subRacks.Count ) ;
          
          groupRacks.Add(subRacks);
        }
      }

      var newRacks = new List<FamilyInstance>() ;
      foreach ( var groupRack in groupRacks ) {
        var line = GetMaxLength( document, groupRack ) ;
        if(null == line)
          continue;
        var rack = groupRack[ 0 ] ; 
        newRacks.Add(rack);
        rack.LookupParameter( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ) ).Set( line.Length ) ;

        groupRack.RemoveAt( 0 ) ;
        document.Delete( groupRack.Select( x => x.Id ).ToList() ) ;
      }
      
      newRacks.AddRange(fittings);
      return newRacks ;
    }

    private static Line? GetMaxLength(Document document, List<FamilyInstance> racks)
    {
      if ( ! racks.Any() )
        return null ;

      var lines = new List<Line>() ;
      var points = racks.Select( x => GetConnector( x ).Select( y => y.Origin ) ).SelectMany( x => x ).ToList() ;
      for ( var i = 0 ; i < points.Count - 1 ; i++ ) {
        for ( var j = i + 1 ; j < points.Count ; j++ ) {
          if ( points[ i ].DistanceTo( points[ j ] ) > document.Application.ShortCurveTolerance ) {
            lines.Add(Line.CreateBound(points[i], points[j]));
          }
        }
      }

      return lines.MaxBy( x => x.Length ) ;
    }

    private static bool MEPModelOnPlan( FamilyInstance familyInstance )
    {
      var connectors = GetConnector( familyInstance ) ;
      if ( connectors.Count != 2 )
        return false ;

      return Math.Abs( connectors[ 0 ].Origin.Z - connectors[ 1 ].Origin.Z ) < GeometryHelper.Tolerance ;
    }
    
    private static List<Connector> GetConnector( FamilyInstance familyInstance )
    {
      var connectorSet = familyInstance.MEPModel?.ConnectorManager?.Connectors ;
      return null == connectorSet ? new List<Connector>() : connectorSet.OfType<Connector>().ToList() ;
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
    private void CreateCableRackForConduit( UIDocument uiDocument, Conduit conduit, double cableRackWidth, List<FamilyInstance> racks )
    {
      if ( conduit != null ) {
        var document = uiDocument.Document ;

        using var transaction = new SubTransaction( document ) ;
        try {
          transaction.Start() ;
          var location = ( conduit.Location as LocationCurve )! ;
          var line = ( location.Curve as Line )! ;

          var instance = NewRackCommandBase.CreateRackForStraightConduit( uiDocument, conduit, cableRackWidth ) ;

          // check cable tray exists
          if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
            transaction.RollBack() ;
            return ;
          }
          
          racks.Add( instance );

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
    private void CreateElbow( UIDocument uiDocument, ElementId elementId, List<Connector> connectors, List<FamilyInstance> racks )
    {
      var document = uiDocument.Document ;
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var conduit = document.GetElementById<FamilyInstance>( elementId )! ;
        
        if ( 1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.FacingOrientation.Z || -1.0 == conduit.HandOrientation.Z || 1.0 == conduit.HandOrientation.Z) {
          return ;
        }

        var location = ( conduit.Location as LocationPoint )! ;
        var instance = NewRackCommandBase.CreateRackForFittingConduit( uiDocument, conduit, location, cableTrayDefaultBendRadius ) ;

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

        racks.Add( instance );
        
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
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
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