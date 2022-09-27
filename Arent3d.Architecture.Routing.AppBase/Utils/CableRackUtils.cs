using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using MoreLinq ;


namespace Arent3d.Architecture.Routing.AppBase.Utils
{
  public static class CableRackUtils
  {
    public const char SignJoinRouteName = '_' ;
    
    private static double ElbowMinimumRadius = 50d.MillimetersToRevitUnits() ;
    private static double ElbowPadding = 25d.MillimetersToRevitUnits() ;
    
    private record RackCreationParam( XYZ StartPoint, XYZ EndPoint, double Width, double ScaleFactor, Level? Level = null, FamilySymbol? RackType = null, Element? ReferenceElement = null, string RackClassification = "Normal Rack" ) ;

    private record ElbowCreationParam( XYZ InsertPoint, double Angle, double Width, double Radius, double AdditionalLength, double ScaleFactor, Level? Level = null, FamilySymbol? ElbowType = null, Element? ReferenceElement = null, string RackClassification = "Normal Rack" ) ;
    
    private static bool IsBetween( this XYZ p, XYZ p1, XYZ p2 )
    {
      return ( p.DistanceTo( p1 ) + p.DistanceTo( p2 ) ).Equals( p1.DistanceTo( p2 ) ) ;
    }

    public static bool HasPoint( this MEPCurve mepCurve, XYZ point )
    {
      var (firstConnector, secondConnector) = Get2ConnectorsOfConduit( mepCurve ) ;
      if ( firstConnector?.Origin is not { } p1 || secondConnector?.Origin is not { } p2 )
        return false ;
      return point.IsBetween( p1, p2 ) ;
    }

    public static XYZ? ProjectOn( this XYZ point, MEPCurve curve )
    {
      if ( curve.Location is not LocationCurve locationCurve )
        return null ;
      var (firstEndPoint, secondEndpoint) = ( locationCurve.Curve.GetEndPoint( 0 ), locationCurve.Curve.GetEndPoint( 1 ) ) ;
      var vCurve = ( secondEndpoint - firstEndPoint ).Normalize() ;
      return firstEndPoint + vCurve * ( point - firstEndPoint ).DotProduct( vCurve ) ;
    }

    public static (Connector? Connector1, Connector? Connector2) Get2ConnectorsOfConduit( this Element curve )
    {
      var connectorsOfConduit = curve.GetConnectors().ToArray() ;
      return connectorsOfConduit.Length != 2 ? ( null, null ) : ( connectorsOfConduit.ElementAt( 0 ), connectorsOfConduit.ElementAt( 1 ) ) ;
    }

    private static Connector? GetOppositeConnector( Connector connector )
    {
      var (firstConnector, secondConnector) = Get2ConnectorsOfConduit( connector.Owner ) ;
      if ( firstConnector?.Id == connector.Id )
        return secondConnector ;
      return secondConnector?.Id == connector.Id ? firstConnector : null ;
    }

    private static bool TryFindMEPCurvesFromTo( Connector startConnector, Element? stopElement, List<Element> accumulatedResultList )
    {
      if ( ! startConnector.IsConnected )
        return false ;
      var ids = accumulatedResultList.Select( el => el.Id ) ;
      var connectedConnectors = startConnector.GetConnectedConnectors() ;
      var connectedConnector = connectedConnectors.SingleOrDefault( con => ! ids.Contains( con.Owner.Id ) ) ;
      if ( connectedConnector is null )
        return false ;
      var element = connectedConnector.Owner ;
      accumulatedResultList.Add( element ) ;
      if ( element.Id == stopElement?.Id )
        return true ;
      var oppositeConnector = GetOppositeConnector( connectedConnector ) ;
      return oppositeConnector is { } && TryFindMEPCurvesFromTo( oppositeConnector, stopElement, accumulatedResultList ) ;
    }

    public static List<Element> GetLinkedMEPCurves( this MEPCurve startCurve, MEPCurve endCurve )
    {
      if ( startCurve.Id == endCurve.Id )
        return new List<Element>() { startCurve } ;
      var (startConnector1, startConnector2) = Get2ConnectorsOfConduit( startCurve ) ;
      // try to find endCurve from start1
      var linkedMEPCurves = new List<Element>() { startCurve } ;
      if ( startConnector1 is { } && TryFindMEPCurvesFromTo( startConnector1, endCurve, linkedMEPCurves ) )
        return linkedMEPCurves ;
      // try to find endCurve from start2
      linkedMEPCurves = new List<Element>() { startCurve } ;
      if ( startConnector2 is { } && TryFindMEPCurvesFromTo( startConnector2, endCurve, linkedMEPCurves ) )
        return linkedMEPCurves ;
      // if failed to find a connected road between startCurve and endCurve, return an empty list
      linkedMEPCurves.Clear() ;
      return linkedMEPCurves ;
    }

    public static (double StartParam, double EndParam) CalculatePositionOfRackOnConduit( this MEPCurve thisCurve, XYZ startPointOfRack, Element nextCurve )
    {
      var (thisConnector1, thisConnector2) = Get2ConnectorsOfConduit( thisCurve ) ;
      var (nextConnector1, nextConnector2) = Get2ConnectorsOfConduit( nextCurve ) ;
      if ( thisConnector1 is null || thisConnector2 is null || nextConnector1 is null || nextConnector2 is null )
        return ( 0.0, 1.0 ) ;

      if ( thisCurve.Location is not LocationCurve { Curve: Line line } )
        return ( 0.0, 1.0 ) ;

      // arrange connect so that vector connector 1 to connect 2 is same way as curve's direction
      if ( line.Direction.IsAlmostEqualTo( ( thisConnector2.Origin - thisConnector1.Origin ).Normalize() ) == false )
        ( thisConnector1, thisConnector2 ) = ( thisConnector2, thisConnector1 ) ;

      var d11 = thisConnector1.Origin.DistanceTo( nextConnector1.Origin ) ;
      var d12 = thisConnector1.Origin.DistanceTo( nextConnector2.Origin ) ;
      var d21 = thisConnector2.Origin.DistanceTo( nextConnector1.Origin ) ;
      var d22 = thisConnector2.Origin.DistanceTo( nextConnector2.Origin ) ;

      var routeToCon2 = ( d21 < d11 && d21 < d12 ) || ( d22 < d11 && d22 < d12 ) ;

      var startParam = 0.0 ;
      var endParam = ( startPointOfRack - thisConnector1.Origin ).DotProduct( line.Direction ) / line.Length ;
      return routeToCon2 ? ( endParam, 1.0 ) : ( startParam, endParam ) ;
    }

    public static (double StartParam, double EndParam) CalculatePositionOfRackOnConduit( this MEPCurve mepCurve, XYZ startPointOfRack, XYZ endPointOfRack )
    {
      var (startConnector, endConnector) = Get2ConnectorsOfConduit( mepCurve ) ;
      if ( startConnector is null || endConnector is null )
        return ( 0.0, 1.0 ) ;

      if ( mepCurve.Location is not LocationCurve { Curve: Line line } )
        return ( 0.0, 1.0 ) ;

      // arrange connect so that vector connector 1 to connect 2 is same way as curve's direction
      if ( line.Direction.IsAlmostEqualTo( ( endConnector.Origin - startConnector.Origin ).Normalize() ) == false )
        startConnector = endConnector ;

      var startParam = ( startPointOfRack - startConnector.Origin ).DotProduct( line.Direction ) / line.Length ;
      var endParam = ( endPointOfRack - startConnector.Origin ).DotProduct( line.Direction ) / line.Length ;
      if ( startParam > endParam )
        ( startParam, endParam ) = ( endParam, startParam ) ;
      return ( startParam, endParam ) ;
    }

    public static bool IsOnSameRack( this Conduit firstConduit, Conduit secondConduit )
    {
      if ( secondConduit.Location is not LocationCurve locationCure || locationCure.Curve.Length < 50d.MillimetersToRevitUnits() )
        return false ;
      if ( IsOverlappedEndPoint( firstConduit, secondConduit ) is (not null, not null) )
        return true ;
      if ( IsOverlappedEndPoint( secondConduit, firstConduit ) is (not null, not null) )
        return true ;
      if ( IsOverlappedEachOther( firstConduit, secondConduit ) is (not null, not null) )
        return true ;
      if ( IsInside( firstConduit, secondConduit ) != 0 )
        return true ;
      if ( IsSamePosition( firstConduit, secondConduit ) )
        return true ;
      return false ;
    }

    /// <summary>
    /// result is in millimeters
    /// </summary>
    /// <param name="conduitsOnRack"></param>
    /// <returns></returns>
    public static double CalculateRackWidth( Document document, Element element )
    {
      if ( element is not Conduit conduit )
        return 0 ;
      var overlappedConduits = document.GetAllElements<Conduit>().Where( cd => cd.Id != conduit.Id && IsSamePosition( conduit, cd ) ).ToList() ;
      overlappedConduits.Add( conduit ) ;

      var cableList = new List<NewRackCommandBase.ClassificationData>() ;
      var foundedCables = new Dictionary<string, List<NewRackCommandBase.ClassificationData>>() ;

      var routeNames = overlappedConduits.Select( cd => cd.GetRouteName() ?? "" ) ;

      foreach ( var routeName in routeNames ) {
        if ( string.IsNullOrEmpty( routeName ) )
          continue ;

        var cables = NewRackCommandBase.GetClassificationDatas( document, routeName!, foundedCables ) ;
        if ( ! cables.Any() )
          continue ;

        cableList.AddRange( cables ) ;
      }

      var powerCables = new List<double>() ;
      var instrumentationCables = new List<double>() ;

      foreach ( var cable in cableList ) {
        if ( cable.Classification == $"{CreateDetailTableCommandBase.SignalType.低電圧}" || cable.Classification == $"{CreateDetailTableCommandBase.SignalType.動力}" ) {
          powerCables.Add( cable.Diameter ) ;
        }
        else {
          instrumentationCables.Add( cable.Diameter ) ;
        }
      }

      var widthCable = ( powerCables.Count > 0 ? ( 60 + powerCables.Sum( x => x + 10 ) ) * 1.2 : 0 ) + ( instrumentationCables.Count > 0 ? ( 120 + instrumentationCables.Sum( x => x + 10 ) ) * 0.6 : 0 ) ;

      foreach ( var width in NewRackCommandBase.CableTrayWidthMapping ) {
        if ( widthCable > width )
          continue ;

        widthCable = width ;
        break ;
      }

      return widthCable.MillimetersToRevitUnits() ;
    }

    public static bool IsRack( this Element element )
    {
      return element.Document.GetElementById<ElementType>( element.GetTypeId() ) is { } elementType && elementType.FamilyName.Equals( ElectricalRoutingFamilyType.CableTray.GetFamilyName() ) ;
    }

    private static double RackWidth( this Element element )
    {
      if ( element.GetParameter( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( element.Document, "トレイ幅" ) ) is not { } param )
        return 0 ;
      return param.AsDouble() ;
    }

    #region Classify relative position of 2 racks

    private static bool IsSamePosition( Element firstCurve, Element secondCurve )
    {
      var (connectorFirst1, connectorFirst2) = Get2ConnectorsOfConduit( firstCurve ) ;
      var (connectorSecond1, connectorSecond2) = Get2ConnectorsOfConduit( secondCurve ) ;
      if ( connectorFirst1?.Origin is not { } pointFirst1 || connectorFirst2?.Origin is not { } pointFirst2 || connectorSecond1?.Origin is not { } pointSecond1 || connectorSecond2?.Origin is not { } pointSecond2 )
        return false ;

      return ( pointFirst1.IsAlmostEqualTo( pointSecond1 ) && pointFirst2.IsAlmostEqualTo( pointSecond2 ) ) || ( pointFirst1.IsAlmostEqualTo( pointSecond2 ) && pointFirst2.IsAlmostEqualTo( pointSecond1 ) ) ;
    }

    private static (Connector? ConnectorShort, Connector? ConnectorLong) IsOverlappedEndPoint( Element shortCurve, Element longCurve )
    {
      var (connectorShort1, connectorShort2) = Get2ConnectorsOfConduit( shortCurve ) ;
      var (connectorLong1, connectorLong2) = Get2ConnectorsOfConduit( longCurve ) ;
      if ( connectorShort1?.Origin is not { } pointShort1 || connectorShort2?.Origin is not { } pointShort2 || connectorLong1?.Origin is not { } pointLong1 || connectorLong2?.Origin is not { } pointLong2 )
        return ( null, null ) ;

      if ( pointShort1.IsAlmostEqualTo( pointLong1 ) && pointShort2.IsBetween( pointLong1, pointLong2 ) )
        return ( connectorShort1, connectorLong1 ) ;
      if ( pointShort1.IsAlmostEqualTo( pointLong2 ) && pointShort2.IsBetween( pointLong1, pointLong2 ) )
        return ( connectorShort1, connectorLong2 ) ;
      if ( pointShort2.IsAlmostEqualTo( pointLong1 ) && pointShort1.IsBetween( pointLong1, pointLong2 ) )
        return ( connectorShort2, connectorLong1 ) ;
      if ( pointShort2.IsAlmostEqualTo( pointLong2 ) && pointShort1.IsBetween( pointLong1, pointLong2 ) )
        return ( connectorShort2, connectorLong2 ) ;

      return ( null, null ) ;
    }

    private static (Connector? ConnectorFirst, Connector? ConnectorSecond) IsOverlappedEachOther( Element firstCurve, Element secondCurve )
    {
      var (connectorFirst1, connectorFirst2) = Get2ConnectorsOfConduit( firstCurve ) ;
      var (connectorSecond1, connectorSecond2) = Get2ConnectorsOfConduit( secondCurve ) ;
      if ( connectorFirst1?.Origin is not { } pointFirst1 || connectorFirst2?.Origin is not { } pointFirst2 || connectorSecond1?.Origin is not { } pointSecond1 || connectorSecond2?.Origin is not { } pointSecond2 )
        return ( null, null ) ;
      if ( pointFirst1.IsBetween( pointSecond1, pointSecond2 ) && pointSecond1.IsBetween( pointFirst1, pointFirst2 ) )
        return ( connectorFirst2, connectorSecond2 ) ;
      if ( pointFirst1.IsBetween( pointSecond1, pointSecond2 ) && pointSecond2.IsBetween( pointFirst1, pointFirst2 ) )
        return ( connectorFirst2, connectorSecond1 ) ;
      if ( pointFirst2.IsBetween( pointSecond1, pointSecond2 ) && pointSecond1.IsBetween( pointFirst1, pointFirst2 ) )
        return ( connectorFirst1, connectorSecond2 ) ;
      if ( pointFirst2.IsBetween( pointSecond1, pointSecond2 ) && pointSecond2.IsBetween( pointFirst1, pointFirst2 ) )
        return ( connectorFirst1, connectorSecond1 ) ;

      return ( null, null ) ;
    }

    /// <summary>
    /// Check if a curve is completely inside another curve
    /// </summary>
    /// <param name="firstCurve">first MEP curve</param>
    /// <param name="secondCurve">second MEP curve</param>
    /// <returns>1: first is inside second, -1: second is inside first, 0: neither</returns>
    private static int IsInside( Element firstCurve, Element secondCurve )
    {
      var (connectorFirst1, connectorFirst2) = Get2ConnectorsOfConduit( firstCurve ) ;
      var (connectorSecond1, connectorSecond2) = Get2ConnectorsOfConduit( secondCurve ) ;
      if ( connectorFirst1?.Origin is not { } pointFirst1 || connectorFirst2?.Origin is not { } pointFirst2 || connectorSecond1?.Origin is not { } pointSecond1 || connectorSecond2?.Origin is not { } pointSecond2 )
        return 0 ;
      if ( pointFirst1.IsBetween( pointSecond1, pointSecond2 ) && pointFirst2.IsBetween( pointSecond1, pointSecond2 ) )
        return 1 ;
      if ( pointSecond1.IsBetween( pointFirst1, pointFirst2 ) && pointSecond2.IsBetween( pointFirst1, pointFirst2 ) )
        return -1 ;
      return 0 ;
    }

    #endregion

    private static void Reconnect( Connector disconnectedConnector, Connector reconnectedConnector )
    {
      var connectedConnectors = disconnectedConnector.GetConnectedConnectors().ToArray() ;
      MoreEnumerable.ForEach( connectedConnectors, disconnectedConnector.DisconnectFrom ) ;
      MoreEnumerable.ForEach( connectedConnectors, c =>
      {
        if ( ! reconnectedConnector.IsConnectedTo( c ) )
          reconnectedConnector.ConnectTo( c ) ;
      } ) ;
    }

    private static bool TryExtendRack( Element firstCurve, Connector startConnector, Connector endConnector )
    {
      var doc = firstCurve.Document ;
      if ( firstCurve is not FamilyInstance firstFi )
        return false ;

      // remember linking connectors
      var connectedConnectors1 = startConnector.GetConnectedConnectors().ToArray() ;
      var connectedConnectors2 = endConnector.GetConnectedConnectors().ToArray() ;
      MoreEnumerable.ForEach( connectedConnectors1, startConnector.DisconnectFrom ) ;
      MoreEnumerable.ForEach( connectedConnectors2, endConnector.DisconnectFrom ) ;
      var startPoint = startConnector.Origin ;
      var endPoint = endConnector.Origin ;

      // change length and rotate first curve
      var tf1 = firstFi.GetTransform() ;

      if ( firstCurve.Location is LocationPoint lcPoint )
        lcPoint.Point = startPoint ;

      ElementTransformUtils.RotateElement( doc, firstCurve.Id, Line.CreateBound( startPoint, startPoint + tf1.BasisZ ), tf1.BasisX.AngleTo( endPoint - startPoint ) ) ;
      firstCurve.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ) ).Set( ( endPoint - startPoint ).GetLength() ) ;

      // reconnect:
      if ( Get2ConnectorsOfConduit( firstCurve ) is not { Connector1: { } connector1, Connector2: { } connector2 } )
        return false ;
      if ( connector2.Origin.IsAlmostEqualTo( startPoint ) )
        ( connector1, connector2 ) = ( connector2, connector1 ) ;
      MoreEnumerable.ForEach( connectedConnectors1, connector1.ConnectTo ) ;
      MoreEnumerable.ForEach( connectedConnectors2, connector2.ConnectTo ) ;
      doc.Regenerate() ;
      return true ;
    }

    public static IEnumerable<Element> ResolveOverlapCases( this Document document, IEnumerable<Element> racks )
    {
      var rackList = racks.ToList() ;
      var rackWidth = ! rackList.Any() ? 0 : rackList.First().RackWidth() ;
      if ( rackWidth == 0 )
        return Array.Empty<Element>() ;
      var ids = rackList.Select( rack => rack.Id ) ;
      var otherRacks = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_CableTrayFitting ).Where( r => ! ids.Contains( r.Id ) && r.RackWidth().Equals( rackWidth ) ).ToList() ;

      if ( ! otherRacks.Any() )
        return rackList ;
      var deletedRacks = new List<Element>() ;
      var idsToRemove = new List<ElementId>() ;
      foreach ( var thisRack in rackList ) {
        foreach ( var otherRack in otherRacks ) {
          // case 1: overlap at an end point
          if ( IsOverlappedEndPoint( thisRack, otherRack ) is { ConnectorShort: { } cDisConnectNew, ConnectorLong: { } cReConnectOld } ) {
            idsToRemove.Add( thisRack.Id ) ;
            deletedRacks.Add( thisRack ) ;
            Reconnect( cDisConnectNew, cReConnectOld ) ;
            continue ;
          }

          if ( IsOverlappedEndPoint( otherRack, thisRack ) is { ConnectorShort: { } cDisConnectOld, ConnectorLong: { } cReConnectNew } ) {
            idsToRemove.Add( otherRack.Id ) ;
            Reconnect( cDisConnectOld, cReConnectNew ) ;
            continue ;
          }

          // case 2: completely inside
          var isInSide = IsInside( thisRack, otherRack ) ;
          if ( isInSide == 1 ) {
            idsToRemove.Add( thisRack.Id ) ;
            deletedRacks.Add( thisRack ) ;
          }
          else if ( isInSide == -1 ) {
            idsToRemove.Add( otherRack.Id ) ;
          }

          // case 3: overlap each other
          if ( IsOverlappedEachOther( thisRack, otherRack ) is { ConnectorFirst: { } connector1, ConnectorSecond: { } connector2 } && TryExtendRack( thisRack, connector1, connector2 ) ) {
            idsToRemove.Add( otherRack.Id ) ;
          }
        }
      }

      deletedRacks.ForEach( r => rackList.Remove( r ) ) ;
      document.Delete( idsToRemove ) ;
      return rackList ;
    }

    public static void HideConnectedEdgesOfRack( this Element rack )
    {
      if ( rack is not FamilyInstance instance || ! rack.IsRack() || Get2ConnectorsOfConduit( instance ) is not { Connector1: { } startConnector, Connector2: { } endConnector } )
        return ;

      var tf = instance.GetTransform() ;
      if ( startConnector.Origin.DistanceTo( tf.Origin ) > endConnector.Origin.DistanceTo( tf.Origin ) )
        ( startConnector, endConnector ) = ( endConnector, startConnector ) ;

      instance.SetProperty( "起点の表示", ! startConnector.IsConnected ) ;
      instance.SetProperty( "終点の表示", ! endConnector.IsConnected ) ;
    }


    private static (XYZ, XYZ) GetEndPoints( MEPCurve conduit, double startPosition = 0.0, double endPosition = 1.0 )
    {
      if ( conduit.Location is not LocationCurve locationCurve )
        return ( XYZ.Zero, XYZ.Zero ) ;
      if ( locationCurve.Curve.GetEndPoint( 0 ) is not { } startPoint || locationCurve.Curve.GetEndPoint( 1 ) is not { } endPoint )
        return ( XYZ.Zero, XYZ.Zero ) ;
      var direction = endPoint - startPoint ;
      return ( startPoint + direction * startPosition, startPoint + endPosition * direction ) ;
    }

    private static FamilySymbol? GetGenericRackSymbol( Document doc, string typeName = "汎用" )
    {
      var rackSymbol = doc.GetFamilySymbols( ElectricalRoutingFamilyType.CableTray ).FirstOrDefault( symbol => symbol.Name == typeName ) ;
      if ( rackSymbol is null )
        return null ;
      if ( ! rackSymbol.IsActive )
        rackSymbol.Activate() ;
      return rackSymbol ;
    }

    private static FamilySymbol? GetElbowSymbol( Document doc )
    {
      var rackSymbol = doc.GetFamilySymbols( ElectricalRoutingFamilyType.CableTrayFitting ).FirstOrDefault() ;
      if ( rackSymbol is null )
        return null ;
      if ( ! rackSymbol.IsActive )
        rackSymbol.Activate() ;
      return rackSymbol ;
    }

    private static void CopyProperties( this FamilyInstance rack, Element referenceElement )
    {
      var routeName = referenceElement.GetRouteName() ;
      if ( string.IsNullOrEmpty( routeName ) ) return ;

      var routeNameArray = routeName!.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      rack.SetProperty( RoutingParameter.RouteName, routeName ) ;

      if ( ! referenceElement.IsRack() )
        return ;
      
      if(referenceElement.TryGetProperty(ElectricalRoutingElementParameter.Separator, out bool separator))
        rack.TrySetProperty( ElectricalRoutingElementParameter.Separator, separator ) ;
      if(referenceElement.TryGetProperty(ElectricalRoutingElementParameter.Material, out string? material) && material is {})
        rack.TrySetProperty( ElectricalRoutingElementParameter.Material, material ) ;
      if(referenceElement.TryGetProperty(ElectricalRoutingElementParameter.Cover, out string? cover) && cover is {})
        rack.TrySetProperty( ElectricalRoutingElementParameter.Material, cover ) ;
    }

    public static ( string, string ) GetFromAndToConnectorUniqueIdOfRack( Element rack )
    {
      if ( rack.TryGetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, out string? toConnectorId ) && rack.TryGetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, out string? fromConnectorId ) )
        return ( fromConnectorId!, toConnectorId! ) ;
      throw new NullReferenceException() ;
    }

    private static FamilyInstance? CreateRack( Document doc, RackCreationParam creationParam )
    {
      if ( ( creationParam.RackType ?? GetGenericRackSymbol( doc ) ) is not { } rackSymbol )
        return null ;
      var instance = doc.Create.NewFamilyInstance( creationParam.StartPoint, rackSymbol, creationParam.Level, StructuralType.NonStructural ) ;
      // set cable rack length
      instance.SetProperty( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ), creationParam.StartPoint.DistanceTo( creationParam.EndPoint ) ) ;

      // set cable rack width
      instance.SetProperty( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( doc, "トレイ幅" ), creationParam.Width ) ;

      instance.SetProperty( "ラックの倍率", creationParam.ScaleFactor ) ;

      // rack classification
      var rackClassification = "" ;
      if ( ! string.IsNullOrEmpty( creationParam.RackClassification ) )
        rackClassification = creationParam.RackClassification ;
      else if ( creationParam.ReferenceElement is FamilyInstance fi && fi.TryGetProperty( ElectricalRoutingElementParameter.RackType, out string? oldRackType ) && oldRackType is { } )
        rackClassification = oldRackType ;
      if ( ! string.IsNullOrEmpty( rackClassification ) )
        instance.SetProperty( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ), rackClassification ) ;

      if ( creationParam.ReferenceElement is { } refElement ) {
        // set To-Side Connector Id
        var (fromConnectorId, toConnectorId) = refElement is Conduit ? RackCommandBase.GetFromAndToConnectorUniqueId( refElement ) : GetFromAndToConnectorUniqueIdOfRack( refElement ) ;
        if ( ! string.IsNullOrEmpty( toConnectorId ) )
          instance.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
        if ( ! string.IsNullOrEmpty( fromConnectorId ) )
          instance.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;

        // set route name + rack specified parameters
        instance.CopyProperties( refElement ) ;
      }

      if ( instance.GetTransform() is not { } tf )
        return null ;

      var dAngle = ( creationParam.EndPoint - creationParam.StartPoint ).AngleOnPlaneTo( tf.BasisX ?? XYZ.BasisX, XYZ.BasisZ ) ;
      ElementTransformUtils.RotateElement( doc, instance.Id, Line.CreateBound( creationParam.StartPoint, creationParam.StartPoint + XYZ.BasisZ ), -dAngle ) ;
      return instance ;
    }

    private static FamilyInstance? CreateElbow( Document doc, ElbowCreationParam creationParam )
    {
      if ( ( creationParam.ElbowType ?? GetGenericRackSymbol( doc ) ) is not { } elbowSymbol )
        return null ;
      var instance = doc.Create.NewFamilyInstance( XYZ.Zero, elbowSymbol, creationParam.Level, StructuralType.NonStructural ) ;

      // elbow width
      instance.SetProperty( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( doc, "トレイ幅" ), creationParam.Width ) ;

      // elbow Length
      instance.SetProperty( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ), creationParam.AdditionalLength ) ;

      // elbow radius
      instance.SetProperty( "Revit.Property.Builtin.BendRadius".GetDocumentStringByKeyOrDefault( doc, "Bend Radius" ), creationParam.Radius ) ;

      instance.SetProperty( "ラックの倍率", creationParam.ScaleFactor ) ;

      // rack classification
      var rackClassification = "" ;
      if ( ! string.IsNullOrEmpty( creationParam.RackClassification ) )
        rackClassification = creationParam.RackClassification ;
      else if ( creationParam.ReferenceElement is FamilyInstance fi && fi.TryGetProperty( ElectricalRoutingElementParameter.RackType, out string? oldRackType ) && oldRackType is { } )
        rackClassification = oldRackType ;
      instance.SetProperty( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ), rackClassification ) ;

      if ( creationParam.ReferenceElement is { } refElement ) {
        // set To-Side Connector Id
        var (fromConnectorId, toConnectorId) = refElement is Conduit ? RackCommandBase.GetFromAndToConnectorUniqueId( refElement ) : GetFromAndToConnectorUniqueIdOfRack( refElement ) ;
        if ( ! string.IsNullOrEmpty( toConnectorId ) )
          instance.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
        if ( ! string.IsNullOrEmpty( fromConnectorId ) )
          instance.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;

        // set route name + rack specified parameters
        instance.CopyProperties( refElement ) ;
      }

      doc.Regenerate() ;

      if ( instance.Get2ConnectorsOfConduit() is not { Connector1: { } connector1, Connector2: { } connector2 } )
        return instance ;

      var origin = connector1.Origin.X < connector2.Origin.X ? connector1.Origin : connector2.Origin ;

      ElementTransformUtils.RotateElement( doc, instance.Id, Line.CreateBound( origin, origin + XYZ.BasisZ ), creationParam.Angle ) ;
      doc.Regenerate() ;
      ElementTransformUtils.MoveElement( doc, instance.Id, creationParam.InsertPoint - origin ) ;
      return instance ;
    }

    private static (XYZ P11, XYZ P12, XYZ P21, XYZ P22) ReArrange( (XYZ, XYZ) pair1, (XYZ, XYZ) pair2 )
    {
      var d11 = pair1.Item1.DistanceTo( pair2.Item1 ) ;
      var d12 = pair1.Item1.DistanceTo( pair2.Item2 ) ;
      var d21 = pair1.Item2.DistanceTo( pair2.Item1 ) ;
      var d22 = pair1.Item2.DistanceTo( pair2.Item2 ) ;
      var distances = new List<double>() { d11, d12, d21, d22 } ;
      var minDistance = distances.Min() ;
      ( XYZ P11, XYZ P12, XYZ P21, XYZ P22 ) markPoints ;
      if ( Math.Abs( minDistance - d11 ) < GeometryUtil.Tolerance )
        markPoints = ( pair1.Item2, pair1.Item1, pair2.Item1, pair2.Item2 ) ;
      else if ( Math.Abs( minDistance - d12 ) < GeometryUtil.Tolerance )
        markPoints = ( pair1.Item2, pair1.Item1, pair2.Item2, pair2.Item1 ) ;
      else if ( Math.Abs( minDistance - d21 ) < GeometryUtil.Tolerance )
        markPoints = ( pair1.Item1, pair1.Item2, pair2.Item1, pair2.Item2 ) ;
      else
        markPoints = ( pair1.Item1, pair1.Item2, pair2.Item2, pair2.Item1 ) ;
      return markPoints ;
    }

    private static double RealElbowRadius( double elbowWidth, double elbowMinRadius, double scaleFactor )
    {
      return scaleFactor > 1 ? ( scaleFactor - 1 ) * elbowWidth / 2 + elbowMinRadius : elbowMinRadius ;
    }

    private static (XYZ, XYZ, XYZ, XYZ) ReArrangeToConnect( (XYZ, XYZ, double) marker1, (XYZ, XYZ, double) marker2, int scale, double elbowMinRadius, double elbowPaddingLength )
    {
      var markPoints = ReArrange( ( marker1.Item1, marker1.Item2 ), ( marker2.Item1, marker2.Item2 ) ) ;
      if ( markPoints.P11.IsAlmostEqualTo( markPoints.P12 ) )
        return markPoints ;

      var isSameWidth = Math.Abs( marker1.Item3.RevitUnitsToMillimeters() - marker2.Item3.RevitUnitsToMillimeters() ) < 1.0 ;
      var isSameDirection = ( markPoints.P11 - markPoints.P12 ).Normalize().IsAlmostEqualTo( ( markPoints.P21 - markPoints.P22 ).Normalize() ) ;
      if ( isSameWidth && markPoints.P12.IsAlmostEqualTo( markPoints.P21 ) && isSameDirection ) {
        // join 2 short markers into a long marker
        markPoints.P21 = markPoints.P11 ;
        markPoints.P11 = XYZ.Zero ;
        markPoints.P12 = XYZ.Zero ;
      }
      else {
        // perpendicular: modify to have enough space for elbow
        var elbowWidth = Math.Max( marker1.Item3, marker2.Item3 ) ;
        var scaleFactor = RackWidthOnPlanView( scale ) / elbowWidth ;

        var line1 = Line.CreateUnbound( markPoints.P11, markPoints.P12 - markPoints.P11 ) ;
        var line2 = Line.CreateUnbound( markPoints.P22, markPoints.P21 - markPoints.P22 ) ;
        line1.Intersect( line2, out var resultArray ) ;
        if ( resultArray is null || resultArray.IsEmpty )
          return markPoints ;

        var intersectedPoint = resultArray.get_Item( 0 ).XYZPoint ;
        var realElbowRadius = RealElbowRadius( elbowWidth, elbowMinRadius, scaleFactor ) ;
        var distanceFromIntersect = realElbowRadius + elbowWidth / 2 + elbowPaddingLength ;
        
        // marker is too short so it's completely inside elbow
        var isMarker1TooShort = distanceFromIntersect > markPoints.P11.DistanceTo( intersectedPoint ) ;
        var isMarker2TooShort = distanceFromIntersect > markPoints.P22.DistanceTo( intersectedPoint ) ;
        
        if ( isMarker2TooShort ) {
          // marker2 become zero-length
          markPoints.P21 = markPoints.P22 ;
        }
        if ( isMarker1TooShort ) {
          // marker1 become zero-length
          markPoints.P12 = markPoints.P11 ;
        }
        
        if ( ! isMarker1TooShort && ! isMarker2TooShort ) {
          // both marker have enough space
          markPoints.P12 = intersectedPoint + ( markPoints.P11 - markPoints.P12 ).Normalize() * distanceFromIntersect ;
          markPoints.P21 = intersectedPoint + ( markPoints.P22 - markPoints.P21 ).Normalize() * distanceFromIntersect ;
        }
      }

      return markPoints ;
    }

    private static List<((XYZ P11, XYZ P12, double Width) RackMarker, int OriginalIndex)> GetSimplifiedMarkers( IEnumerable<(XYZ StartPoint, XYZ EndPoint, double Width)> markers )
    {
      var simplifiedMarkers = new List<((XYZ P11, XYZ P12, double Width), int)>() ;
      var markerList = markers.ToList() ;
      for ( var i = 0 ; i < markerList.Count ; i++ ) {
        // final marker
        if ( i == markerList.Count - 1 ) {
          simplifiedMarkers.Add( ( markerList[ i ], i ) ) ;
          break ;
        }

        var fourPoints = ReArrange( ( markerList[ i ].StartPoint, markerList[ i ].EndPoint ), ( markerList[ i + 1 ].StartPoint, markerList[ i + 1 ].EndPoint ) ) ;
        var isSameDirection = ( fourPoints.P11 - fourPoints.P12 ).Normalize().IsAlmostEqualTo( ( fourPoints.P21 - fourPoints.P22 ).Normalize() ) ;
        if ( isSameDirection && fourPoints.P12.IsAlmostEqualTo( fourPoints.P21 ) && isSameDirection ) {
          // extend next marker and ignore this marker
          markerList[ i + 1 ] = ( fourPoints.P11, fourPoints.P22, Math.Max(markerList[ i ].Width, markerList[ i + 1 ].Width) ) ;
        }
        else {
          // add this marker to list
          simplifiedMarkers.Add( ( ( fourPoints.P11, fourPoints.P12, markerList[ i ].Width ), i ) ) ;
        }
      }

      return simplifiedMarkers ;
    }

    private static bool TryConnectRackItems( FamilyInstance? firstInstance, FamilyInstance? secondInstance )
    {
      if ( firstInstance is null || secondInstance is null )
        return false ;
      var connectorList1 = firstInstance.GetConnectors().Where( c => ! c.IsConnected ).ToList() ;
      var connectorList2 = secondInstance.GetConnectors().Where( c => ! c.IsConnected ).ToList() ;
      foreach ( var connector1 in connectorList1 ) {
        if ( connectorList2.FirstOrDefault( con => con.Origin.IsAlmostEqualTo( connector1.Origin ) ) is not { } connector2 )
          continue ;
        connector1.ConnectTo( connector2 ) ;
        return true ;
      }

      return false ;
    }

    private static FamilyInstance? CreateElbowBetweenRackMarkers( Document document, (XYZ, XYZ, double) thisMarker, (XYZ, XYZ, double) nextMarker, double elbowMinRadius, double paddingLength, FamilySymbol? elbowType, Element? referenceElement, string rackClassification, int scale )
    {
      if ( thisMarker.Item1.IsAlmostEqualTo( thisMarker.Item2 ) || nextMarker.Item1.IsAlmostEqualTo( nextMarker.Item2 ) )
        return null ;
      if ( ! ( thisMarker.Item1 - thisMarker.Item2 ).IsPerpendicularTo( nextMarker.Item1 - nextMarker.Item2, 1d.Deg2Rad() ) )
        return null ;
      var rotateClockWise = ( thisMarker.Item2 - thisMarker.Item1 ).CrossProduct( nextMarker.Item1 - thisMarker.Item2 ).Z > 0 ;
      var elbowInsertPoint = rotateClockWise ? thisMarker.Item2 : nextMarker.Item1 ;
      var elbowDirection = rotateClockWise ? thisMarker.Item2 - thisMarker.Item1 : nextMarker.Item1 - nextMarker.Item2 ;
      var elbowRotateAngle = XYZ.BasisX.AngleOnPlaneTo( elbowDirection, XYZ.BasisZ ) ;

      var rackWidth = Math.Max( thisMarker.Item3, nextMarker.Item3 ) ;
      var scaleFactor = RackWidthOnPlanView( scale ) / rackWidth ;
      var elbowRadius = RealElbowRadius( rackWidth, elbowMinRadius, scaleFactor ) ;
      var elbowCreationParam = new ElbowCreationParam( elbowInsertPoint, elbowRotateAngle, rackWidth, elbowRadius, paddingLength, scaleFactor, null, elbowType, referenceElement, rackClassification ) ;
      return CreateElbow( document, elbowCreationParam ) ;
    }

    /// <summary>
    /// calculate 2d width in Revit unit
    /// </summary>
    public static double RackWidthOnPlanView( int nScale )
    {
      var symbolRatio = Model.ImportDwgMappingModel.GetDefaultSymbolRatio( nScale ) ;
      return ( 4 * nScale * symbolRatio ).MillimetersToRevitUnits() ;
    }

    public static IEnumerable<Element> CreateRacksAndElbowsAlongConduits( this Document doc, IEnumerable<(Element, double)> conduitWidthMap, string rackClassification = "Normal Rack", 
      bool IsAutoSizing = false, IEnumerable<(Element Conduit, double StartParam, double EndParam)>? specialLengthList = null )
    {
      // auto sizing
      var listConduitWidth = ! IsAutoSizing ? conduitWidthMap : conduitWidthMap.Select( pair => ( pair.Item1, CalculateRackWidth( doc, pair.Item1 ) ) ) ;

      // read conduit markers
      var racksAndElbows = new List<Element>() ;
      var conduits = new List<Element>() ;
      var conduitMarkers = new List<(XYZ StartPoint, XYZ EndPoint, double Width)>() ;
      var creationInfors = new List<(XYZ , XYZ , double , Element)>() ;
      foreach ( var item in listConduitWidth ) {
        if ( item.Item1 is not Conduit conduit )
          continue ;
        var (startParam, endParam) = ( 0.0, 1.0 ) ;
        if ( specialLengthList?.FirstOrDefault( x => x.Conduit.Id.Equals( conduit.Id ) ) is { Conduit: Conduit } specialLengthItem )
          ( startParam, endParam ) = ( specialLengthItem.StartParam, specialLengthItem.EndParam ) ;

        var (startPoint, endPoint) = GetEndPoints( conduit, startParam, endParam ) ;
        conduitMarkers.Add( ( startPoint, endPoint, item.Item2 ) ) ;
        conduits.Add( conduit ) ;
        creationInfors.Add( ( startPoint, endPoint, item.Item2, conduit ) ) ;
      }

      return doc.CreateRacksAndElbowsFromRawMarkers( creationInfors, doc.ActiveView.Scale, rackClassification ) ;
    }
    
    private static IEnumerable<Element> CreateRacksAndElbowsFromRawMarkers( this Document doc, IEnumerable<(XYZ StartPoint, XYZ EndPoint, double Width, Element ReferenceElement)> creationInforList, int viewScale, string rackClassification )
    {
      var racksAndElbows = new List<Element>() ;
      var referencedElements = creationInforList.Select( x => x.ReferenceElement ).ToList() ;
      var conduitMarkers = creationInforList.Select( x => ( x.StartPoint, x.EndPoint, x.Width ) ) ;
      // simplify : join co-direction short markers into a long marker
      var simplifiedMarkerMap = GetSimplifiedMarkers( conduitMarkers ) ;

      var rackType = GetGenericRackSymbol( doc ) ;
      var elbowType = GetElbowSymbol( doc ) ;
      
      for ( var i = 0 ; i < simplifiedMarkerMap.Count ; i++ ) {
        if ( i == simplifiedMarkerMap.Count - 1 ) {
          break ;
        }

        var thisMarker = simplifiedMarkerMap[ i ].RackMarker ;
        var nextMarker = simplifiedMarkerMap[ i + 1 ].RackMarker ;
        var fourPoints = ReArrangeToConnect( thisMarker, nextMarker, viewScale, ElbowMinimumRadius, ElbowPadding ) ;

        // modify marker to have enough space for elbow
        simplifiedMarkerMap[ i ] = ( ( fourPoints.Item1, fourPoints.Item2, thisMarker.Item3 ), i ) ;
        simplifiedMarkerMap[ i + 1 ] = ( ( fourPoints.Item3, fourPoints.Item4, nextMarker.Item3 ), i + 1 ) ;
      }

      // create racks and elbows by markers
      FamilyInstance? newestInstance = null ;
      for ( var i = 0 ; i < simplifiedMarkerMap.Count ; i++ ) {
        var rackMarker = simplifiedMarkerMap[ i ].RackMarker ;
        if ( rackMarker.Item1.IsAlmostEqualTo( rackMarker.Item2 ) )
          continue ;
        var referencedElement = referencedElements[ simplifiedMarkerMap[ i ].OriginalIndex ] ;

        // create rack
        var rackWidth = rackMarker.Item3 ;
        var scaleFactor = RackWidthOnPlanView( viewScale ) / rackWidth ;
        var creationParam = new RackCreationParam( rackMarker.Item1, rackMarker.Item2, rackWidth, scaleFactor, null, rackType, referencedElement, rackClassification ) ;

        if ( CreateRack( doc, creationParam ) is not { } rack )
          continue ;
        rack.SetProperty( "起点の表示", true ) ;
        rack.SetProperty( "終点の表示", true ) ;
        racksAndElbows.Add( rack ) ;

        // Connect to current elbow:
        if ( newestInstance is not null && TryConnectRackItems( rack, newestInstance ) ) {
          rack.SetProperty( "起点の表示", false ) ;
          if (newestInstance.IsRack())
            newestInstance.SetProperty( "終点の表示", false ) ;
        }

        // Create rack elbow
        if ( i == simplifiedMarkerMap.Count - 1 )
          continue ;

        var nextMarker = simplifiedMarkerMap[ i + 1 ].RackMarker ;

        var elbow = CreateElbowBetweenRackMarkers( doc, rackMarker, nextMarker, ElbowMinimumRadius, ElbowPadding, elbowType, referencedElement, rackClassification, viewScale ) ;
        if ( elbow is null ) {
          newestInstance = rack ;
          continue ;
        }
        
        // Connect rack to new elbow
        var isEndPointConnected = TryConnectRackItems( rack, elbow ) ;
        rack.SetProperty( "終点の表示", ! isEndPointConnected ) ;
        racksAndElbows.Add( elbow ) ;
        newestInstance = elbow ;
      }

      return racksAndElbows ;
    }

    public static bool TryGetRackWidth( this FamilyInstance rack, out double width ) => rack.TryGetProperty( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( rack.Document, "トレイ幅" ), out width ) ;

    public static bool TryGetRackLength( this FamilyInstance rack, out double length ) => rack.TryGetProperty( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( rack.Document, "トレイ長さ" ), out length ) ;

    private static (XYZ? StartPoint, XYZ? EndPoint) GetRackStartAndEndPoints( FamilyInstance rackInstance )
    {
      if ( ! rackInstance.IsRack() )
        return ( null, null ) ;
      
      if ( ! rackInstance.TryGetRackLength( out var length ) )
        return ( null, null ) ;
      
      var transform = rackInstance.GetTransform() ;
      return ( transform.Origin, transform.Origin + transform.BasisX * length ) ;
    }

    private static IEnumerable<(XYZ StartPoint, XYZ EndPoint, double Width, Element ReferenceElement)> ConvertRacksToFullMarkers( IEnumerable<Element> existingRacks )
    {
      foreach ( var rack in existingRacks ) {
        if ( rack is not FamilyInstance rackInstance || GetRackStartAndEndPoints( rackInstance ) is not { StartPoint: { } startPoint, EndPoint: { } endPoint } )
          continue ;
        if ( ! rackInstance.TryGetRackWidth( out var width ) )
          continue ;
        yield return ( startPoint, endPoint, width, rack ) ;
      }
    }

    private static List<Element> ReDrawArrayOfRacksAndElbows( this Document document, IEnumerable<Element> oldElements, View view )
    {
      var newElements = new List<Element>() ;
      // modify scale factor of vertical rack 
      var verticalRacks = oldElements.Where( x => x.IsValidObject ).OfType<FamilyInstance>().Where( element => element.IsVerticalRack() ).ToList() ;
      var planViewRackWidth = RackWidthOnPlanView( view.Scale ) ;
      foreach ( var verticalRack in verticalRacks ) {
        if ( verticalRack.TryGetRackWidth( out var width ) )
          verticalRack.SetProperty( "ラックの倍率", planViewRackWidth / width ) ;
        newElements.Add( verticalRack ) ;
      }

      // read positions of existing racks and fittings
      var horizontalRacks = oldElements.Where( element => ! element.IsVerticalRack() ).ToList() ;
      var fullMakers = ConvertRacksToFullMarkers( horizontalRacks ) ;

      // create racks and fittings with new scale
      var newHorizontalRacksAndFittings = document.CreateRacksAndElbowsFromRawMarkers( fullMakers, view.Scale, "" ).ToList() ;
      newElements.AddRange( newHorizontalRacksAndFittings ) ;

      // delete existing notations and rack items
      var oldUniqueIds = horizontalRacks.Select( e => e.UniqueId ).ToArray() ;
      EraseRackCommandBase.RemoveRackNotation( document, oldUniqueIds ) ;
      document.Delete( oldUniqueIds ) ;

      // create notation for new rack
      RackCommandBase.CreateNotationForRack( document, newHorizontalRacksAndFittings.OfType<FamilyInstance>().Where( fi => fi.IsRack() ), view ) ;

      return newElements ;
    }

    public static void ReDrawAllRacksAndElbows( this Document document, View view, int? scale = null )
    {
      var newScale = scale ?? view.Scale ;

      // redraw manual racks
      var storageRackFromTo = new StorageService<Level, RackFromToModel>( view.GenLevel ) ;
      {
        var rackFromToList = storageRackFromTo.Data.RackFromToItems.Where( x => x.UniqueIds.All( uniqueId => document.GetElement( uniqueId ) is { } ) ).ToList() ;
        storageRackFromTo.Data.RackFromToItems.Clear() ;
        foreach ( var rackFromTo in rackFromToList ) {
          // redraw racks and fittings with new scale
          var newRacksAndFittings = document.ReDrawArrayOfRacksAndElbows( rackFromTo.UniqueIds.ConvertAll( document.GetElement ), view ) ;

          // add to storage
          storageRackFromTo.Data.RackFromToItems.Add( new RackFromToItem() { UniqueIds = newRacksAndFittings.Select( element => element.UniqueId ).ToList() } ) ;
        }

        storageRackFromTo.SaveChange() ;
      }

      // redraw auto racks
      var storageRackForRoute = new StorageService<Level, RackForRouteModel>( view.GenLevel ) ;
      {
        var rackForRouteItems = storageRackForRoute.Data.RackForRoutes.Where( item => item.RackIds.All( id => id.IsValid() ) ).ToList() ;
        storageRackForRoute.Data.RackForRoutes.Clear() ;
        foreach ( var rackForRouteItem in rackForRouteItems ) {
          // redraw racks and fittings with new scale
          var newRacksAndFittings = document.ReDrawArrayOfRacksAndElbows( rackForRouteItem.RackIds.ConvertAll( document.GetElement ), view ) ;

          // add to storage
          storageRackForRoute.Data.RackForRoutes.Add( new RackForRouteItem() { RouteName = rackForRouteItem.RouteName, RackIds = newRacksAndFittings.Select( element => element.Id ).ToList() } ) ;
        }

        storageRackForRoute.SaveChange() ;
      }
    }

    public static bool IsVerticalRack( this Element element )
    {
      if ( element is not FamilyInstance rack || ! rack.IsRack() )
        return false ;
      var tf = rack.GetTransform() ;
      var rackDirection = tf.BasisX ;
      return rackDirection.IsAlmostEqualTo( XYZ.BasisZ ) || rackDirection.IsAlmostEqualTo( -XYZ.BasisZ ) ;
    }
    
    public static bool IsVertical( this Conduit conduit )
    {
      if ( conduit.Location is not LocationCurve { Curve: Line line } )
        throw new Exception( "The required location is line!" ) ;
        
      return Math.Abs( Math.Abs( line.Direction.DotProduct( XYZ.BasisZ ) ) - 1 ) < GeometryUtil.Tolerance ;
    }
    
    public static string GetMainRouteName( string? routeName )
    {
      if ( string.IsNullOrEmpty( routeName ) )
        throw new ArgumentNullException( nameof( routeName ) ) ;

      var array = routeName!.Split( SignJoinRouteName ) ;
      if ( array.Length < 2 )
        throw new FormatException( nameof( routeName ) ) ;

      return string.Join( $"{SignJoinRouteName}", array[ 0 ], array[ 1 ] ) ;
    }

    public static IEnumerable<FamilyInstance> CreateVerticalCableTray( this Document document, IList<(Conduit Conduit, double Width)> conduitMaps, int scale , string rackClassification = "Normal Rack")
    {
      var cableTrays = new List<FamilyInstance>() ;
      if ( ! conduitMaps.Any() )
        return cableTrays ;

      var cableTrayType = GetGenericRackSymbol( document, "シャフト用" ) ?? throw new InvalidOperationException() ;
      foreach ( var conduitMap in conduitMaps ) {
        var line = (Line) ( (LocationCurve) conduitMap.Conduit.Location ).Curve ;
        var firstPoint = line.GetEndPoint( 0 ).Z > line.GetEndPoint( 1 ).Z ? line.GetEndPoint( 1 ) : line.GetEndPoint( 0 ) ;
        
        var cableTrayInstance = document.Create.NewFamilyInstance( firstPoint, cableTrayType, StructuralType.NonStructural ) ;
        
        var verticalFitting = GetFittingsFromConduit( conduitMap.Conduit ).FirstOrDefault( x => Math.Abs(1 - Math.Abs(x.GetTransform().OfVector(XYZ.BasisZ).Z)) > GeometryHelper.Tolerance ) ;
        if(null == verticalFitting)
          continue;

        var directionOfCableTray = GetHorizontalDirectionOfFitting( verticalFitting ) ;
        if(null == directionOfCableTray)
          continue;
        
        ElementTransformUtils.RotateElement(document, cableTrayInstance.Id, CreateAxis(firstPoint, XYZ.BasisY), - 0.5 * Math.PI);
        var zDirectionOfFamily = cableTrayInstance.GetTransform().OfVector( XYZ.BasisZ ) ;
        var angle = zDirectionOfFamily.AngleTo( directionOfCableTray ) ;
        ElementTransformUtils.RotateElement(document, cableTrayInstance.Id, CreateAxis(firstPoint, XYZ.BasisZ), angle);
        
        cableTrayInstance.SetProperty( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( document, "トレイ長さ" ), line.Length ) ;
        cableTrayInstance.SetProperty( "Revit.Property.Builtin.TrayWidth".GetDocumentStringByKeyOrDefault( document, "トレイ幅" ), conduitMap.Width ) ;
        cableTrayInstance.SetProperty( "ラックの倍率", RackWidthOnPlanView( scale ) / conduitMap.Width ) ;
        cableTrayInstance.SetProperty( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), rackClassification ) ;
        cableTrayInstance.CopyProperties( conduitMap.Conduit ) ;

        var (fromConnectorId, toConnectorId) = RackCommandBase.GetFromAndToConnectorUniqueId( conduitMap.Conduit ) ;
        if ( ! string.IsNullOrEmpty( toConnectorId ) )
          cableTrayInstance.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
        if ( ! string.IsNullOrEmpty( fromConnectorId ) )
          cableTrayInstance.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;

        cableTrays.Add(cableTrayInstance);
      }

      return cableTrays ;
    }

    public static IEnumerable<FamilyInstance> GetFittingsFromConduit( Conduit conduit )
    {
      return conduit.ConnectorManager
        .Connectors
        .OfType<Connector>()
        .Where( x => x.IsConnected )
        .SelectMany(x => x.AllRefs.OfType<Connector>().Select(y => y.Owner).OfType<FamilyInstance>());
    }

    private static XYZ? GetHorizontalDirectionOfFitting( FamilyInstance verticalFitting )
    {
      foreach ( Connector connector in verticalFitting.MEPModel.ConnectorManager.Connectors ) {
        if ( Math.Abs( Math.Abs( connector.CoordinateSystem.BasisZ.Z ) - 1 ) > GeometryHelper.Tolerance )
          return connector.CoordinateSystem.BasisZ ;
      }
      return null ;
    }

    private static Line CreateAxis( XYZ point, XYZ direction )
    {
      var movedPoint = Transform.CreateTranslation( direction.Normalize() ).OfPoint( point ) ;
      return Line.CreateBound( point, movedPoint ) ;
    }
  }
}