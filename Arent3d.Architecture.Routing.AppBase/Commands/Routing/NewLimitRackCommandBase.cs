using System ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.DB.Electrical ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewLimitRackCommandBase : IExternalCommand
  {
    
    #region Constants & Variables

    /// <summary>
    /// Max Distance Tolerance when find Connector Closest
    /// </summary>
    private static readonly double MaxDistanceTolerance = ( 20.0 ).MillimetersToRevitUnits() ;
    private const int MinNumberOfMultiplicity = 5 ;
    private static readonly double MinLengthOfConduit = ( 3.0 ).MetersToRevitUnits() ;
    private static readonly double CableTrayDefaultBendRadius = ( 16.0 ).MillimetersToRevitUnits() ;
    public static readonly double[] CableTrayWidthMapping = { 200.0, 300.0, 400.0, 500.0, 600.0, 800.0, 1000.0, 1200.0 } ;
    private readonly Dictionary<ElementId, List<Connector>> _elbowsToCreate = new() ;
    private readonly Dictionary<string, double> _routeLengthCache = new() ;
    private readonly Dictionary<string, double> _routeMaxWidthDictionary = new() ;
    private const string TransactionKey = "TransactionName.Commands.Rack.CreateLimitCableRack" ;
    private static readonly string TransactionName = TransactionKey.GetAppStringByKeyOrDefault( "Create Limit Cable" ) ;
    private static readonly double MinLengthConduit = 50d.MillimetersToRevitUnits() ;

    #endregion

    #region Properties

    protected abstract AddInType GetAddInType() ;
    protected abstract bool IsSelectionRange { get ; }

    #endregion

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      try {
        var result = uiDocument.Document.Transaction( TransactionName, _ =>
        {
          Dictionary<string, List<MEPCurve>> routingElementGroups ;
          
          if ( IsSelectionRange ) {
            List<Element> pickedObjects ;
            
            try {
              pickedObjects = uiDocument.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
            }
            catch ( OperationCanceledException ) {
              return Result.Cancelled ;
            }
            
            if ( ! pickedObjects.Any() ) 
              return Result.Cancelled ;

            var pickedMepCurves = new List<MEPCurve>() ;
            foreach ( var pickedObject in pickedObjects )
              if ( pickedObject is MEPCurve mepCurve )
                pickedMepCurves.Add( mepCurve ) ;

            routingElementGroups = uiDocument.Document.CollectAllMultipliedRoutingElements( pickedMepCurves, MinNumberOfMultiplicity ) ;
          }
          else {
            routingElementGroups = uiDocument.Document.CollectAllMultipliedRoutingElements( MinNumberOfMultiplicity ) ;
          }

          var representativeMepCurves = routingElementGroups.SelectMany( s => s.Value ).Where( p =>
          {
            if ( p.GetSubRouteInfo() is not { } subRouteInfo ) return false;
            return p.GetRepresentativeSubRoute() == subRouteInfo ;
          } ).OfType<Conduit>().EnumerateAll() ;

          var horizontalConduits = representativeMepCurves.Where( x => !x.IsVertical() && (x.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble() ?? 0) > MinLengthConduit ) ;
          var verticalConduits = representativeMepCurves.Where( x => x.IsVertical() && ( x.get_Parameter( BuiltInParameter.CURVE_ELEM_LENGTH )?.AsDouble() ?? 0 ) > MinLengthConduit ) ;
          var rackss =  uiDocument.Document.CreateRacksAlignToConduits( horizontalConduits, 400d.MillimetersToRevitUnits() ).OfType<FamilyInstance>() ;
         
          //Insert Notation
          NewRackCommandBase.CreateNotationForRack( uiDocument.Document, rackss ) ;

          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    #region Methods

    private static void UpdateRouteNameAndRacksCaches( ICollection<RackMap> rackMaps, Element rack, Element routeElement, bool isAddToCableTray = true )
    {
      /*
       *  We need to caches rack instance by routeName because if the new rack instance is not direction with x or y,
       * then we can't map detail curve and rack side by side
       */
      var routeName = routeElement.GetRouteName()! ;

      if ( string.IsNullOrEmpty( routeName ) ) return ;

      var rackMap = rackMaps.FirstOrDefault( rm => rm.RouteName == routeName ) ;

      if ( rackMap is null ) {
        // Add new rack to cable tray collection
        var newRackMap = new RackMap( routeName ) ;
        newRackMap.RackIds.Add( rack.UniqueId ) ;
        if ( isAddToCableTray ) {
          newRackMap.CableTrays.Add( rack ) ;
        }
        // Add new rack to cable tray fitting collection
        else {
          newRackMap.CableTrayFittings.Add( rack ) ;
        }
        rackMaps.Add( newRackMap );
      }
      else {
        // Add new rack to cable tray collection
        if ( isAddToCableTray ) {
          rackMap.CableTrays.Add( rack ) ;
        }
        // Add new rack to cable tray fitting collection
        else {
          rackMap.CableTrayFittings.Add( rack ) ;
        }

        rackMap.RackIds.Add( rack.UniqueId ) ;
      }
    }

    private void CreateCableRackForConduit( UIDocument uiDocument, Conduit conduit, double cableRackWidth, List<FamilyInstance> racks, ICollection<RackMap> rackMaps )
    {
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
          
          UpdateRouteNameAndRacksCaches( rackMaps, instance,conduit ) ;

        racks.Add( instance ) ;

        if ( Math.Abs( 1.0 - Math.Abs(line.Direction.Z) ) > GeometryHelper.Tolerance ) {
          var elbows = conduit.GetConnectors().SelectMany( c => c.GetConnectedConnectors() ).OfEnd().Select( c => c.Owner ).OfType<FamilyInstance>() ;
          foreach ( var elbow in elbows ) {
            if ( _elbowsToCreate.ContainsKey( elbow.Id ) ) {
              _elbowsToCreate[ elbow.Id ].Add( NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(), ( elbow.Location as LocationPoint )!.Point )! ) ;
            }
            else {
              _elbowsToCreate.Add( elbow.Id, new List<Connector>() { NewRackCommandBase.GetConnectorClosestTo( instance.GetConnectors().ToList(), ( elbow.Location as LocationPoint )!.Point )! } ) ;
            }
          }
        }

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }
    
    private static void CreateElbow( UIDocument uiDocument, ElementId elementId, List<Connector> connectors, List<FamilyInstance> racks, ICollection<RackMap> rackMaps )
    {
      var document = uiDocument.Document ;
      using var transaction = new SubTransaction( document ) ;
      try {
        transaction.Start() ;
        var conduit = document.GetElementById<FamilyInstance>( elementId )! ;

        if ( conduit.FacingOrientation.Z is 1.0 or -1.0 || conduit.HandOrientation.Z is -1.0 or 1.0 ) {
          return ;
        }

        var location = ( conduit.Location as LocationPoint )! ;
        var instance = NewRackCommandBase.CreateRackForFittingConduit( uiDocument, conduit, location, CableTrayDefaultBendRadius ) ;

        // check cable tray exists
        if ( NewRackCommandBase.ExistsCableTray( document, instance ) ) {
          transaction.RollBack() ;
          return ;
        }

        var firstCableRack = connectors.First().Owner ;
        // get cable rack width
        var firstCableRackWidth = firstCableRack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ; // TODO may be must change when FamilyType change

        var secondCableRack = connectors.Last().Owner ;
        // get cable rack width
        var secondCableRackWidth = secondCableRack.ParametersMap.get_Item( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ) ).AsDouble() ; // TODO may be must change when FamilyType change

        // set cable rack length
        SetParameter( instance, "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ), firstCableRackWidth >= secondCableRackWidth ? firstCableRackWidth : secondCableRackWidth ) ; // TODO may be must change when FamilyType change

        foreach ( var connector in instance.GetConnectors() ) {
          var otherConnectors = connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) ;
          var connectTo = NewRackCommandBase.GetConnectorClosestTo( otherConnectors, connector.Origin, MaxDistanceTolerance ) ;
          if ( connectTo != null ) connector.ConnectTo( connectTo ) ;
        }

        UpdateRouteNameAndRacksCaches( rackMaps, instance, conduit, false ) ;

        racks.Add( instance ) ;

        transaction.Commit() ;
      }
      catch {
        transaction.RollBack() ;
      }
    }
    
    private double GetLengthOfRoute( string routeName, IEnumerable<MEPCurve> elements, Document document )
    {
      var routeNameArray = routeName.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      if ( _routeLengthCache.ContainsKey( routeName ) ) {
        return _routeLengthCache[ routeName ] ;
      }

      var routeLength = elements.Where( x =>
      {
        var rName = x.GetRouteName() ?? string.Empty ;
        if ( string.IsNullOrEmpty( rName ) ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return rName == routeName ;
      } ).Sum( x => ( x as Conduit )!.ParametersMap.get_Item( "Revit.Property.Builtin.Conduit.Length".GetDocumentStringByKeyOrDefault( document, "Length" ) ).AsDouble() ) ;

      _routeLengthCache.Add( routeName, routeLength ) ;

      return routeLength ;
    }
    
    #endregion
    
  }
}