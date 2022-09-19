using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using MoreLinq ;


namespace Arent3d.Architecture.Routing.AppBase.Utils
{
  public static class CableRackUtils
  {
    
    private record RackCreationParam( XYZ StartPoint, XYZ EndPoint, double Width, double ScaleFactor, Level? Level = null, FamilySymbol? RackType = null ) ;

    private record ElbowCreationParam( XYZ InsertPoint, double Angle, double Width, double Radius, double AdditionalLength, double ScaleFactor, Level? Level = null, FamilySymbol? ElbowType = null ) ;
    
    private static bool IsBetween( this XYZ p, XYZ p1, XYZ p2 )
    {
      return ( p.DistanceTo( p1 ) + p.DistanceTo( p2 ) ).Equals( p1.DistanceTo( p2 ) ) ;
    }

    public static bool HasPoint( this MEPCurve mepCurve, XYZ point )
    {
      var (con1, con2) = Get2Connector( mepCurve ) ;
      if ( con1?.Origin is not { } p1 || con2?.Origin is not { } p2 )
        return false ;
      return point.IsBetween( p1, p2 ) ;
    }

    public static XYZ? ProjectOn( this XYZ point, MEPCurve curve )
    {
      if ( curve.Location is not LocationCurve locationCurve )
        return null ;
      var (p1, p2) = ( locationCurve.Curve.GetEndPoint( 0 ), locationCurve.Curve.GetEndPoint( 1 ) ) ;
      var vCurve = ( p2 - p1 ).Normalize() ;
      return p1 + vCurve * ( point - p1 ).DotProduct( vCurve ) ;
    }

    public static (Connector? Connector1, Connector? Connector2) Get2Connector( this Element curve )
    {
      var connectorsOfConduit = curve.GetConnectors().ToArray() ;
      return connectorsOfConduit.Length != 2 ? ( null, null ) : ( connectorsOfConduit.ElementAt( 0 ), connectorsOfConduit.ElementAt( 1 ) ) ;
    }

    private static Connector? GetOppositeConnector( Connector connector )
    {
      var (con1, con2) = Get2Connector( connector.Owner ) ;
      if ( con1?.Id == connector.Id )
        return con2 ;
      return con2?.Id == connector.Id ? con1 : null ;
    }

    private static bool GetConnectedMepCurveList( List<Element> accumulateList, Connector startConnector, Element? stopElement = null )
    {
      if ( ! startConnector.IsConnected )
        return false ;
      var ids = accumulateList.Select( el => el.Id ) ;
      var connectedConnectors = startConnector.GetConnectedConnectors() ;
      var connectedConnector = connectedConnectors.SingleOrDefault( con => ! ids.Contains( con.Owner.Id ) ) ;
      if ( connectedConnector is null )
        return false ;
      var element = connectedConnector.Owner ;
      accumulateList.Add( element ) ;
      if ( element.Id == stopElement?.Id )
        return true ;
      var oppositeConnector = GetOppositeConnector( connectedConnector ) ;
      return oppositeConnector is { } && GetConnectedMepCurveList( accumulateList, oppositeConnector, stopElement ) ;
    }

    public static List<Element> GetLinkedMEPCurves( this MEPCurve startCurve, MEPCurve endCurve )
    {
      if ( startCurve.Id == endCurve.Id )
        return new List<Element>() { startCurve } ;
      var (start1, start2) = Get2Connector( startCurve ) ;
      // try to find endCurve from start1
      var accumulateList = new List<Element>() { startCurve } ;
      if ( start1 is { } && GetConnectedMepCurveList( accumulateList, start1, endCurve ) )
        return accumulateList ;
      // try to find endCurve from start2
      accumulateList = new List<Element>() { startCurve } ;
      if ( start2 is { } && GetConnectedMepCurveList( accumulateList, start2, endCurve ) )
        return accumulateList ;
      // if failed to find a connected road between startCurve and endCurve, return an empty list
      accumulateList.Clear() ;
      return accumulateList ;
    }

    public static (double StartParam, double EndParam) CalculateParam( this MEPCurve thisCurve, XYZ point, Element nextCurve )
    {
      var resParams = ( 0.0, 1.0 ) ;
      var (thisCon1, thisCon2) = Get2Connector( thisCurve ) ;
      var (nextCon1, nextCon2) = Get2Connector( nextCurve ) ;
      if ( thisCon1 is null || thisCon2 is null || nextCon1 is null || nextCon2 is null )
        return resParams ;

      if ( thisCurve.Location is not LocationCurve { Curve: Line line } )
        return resParams ;

      // arrange connect so that vector connector 1 to connect 2 is same way as curve's direction
      if ( line.Direction.IsAlmostEqualTo( ( thisCon2.Origin - thisCon1.Origin ).Normalize() ) == false )
        ( thisCon1, thisCon2 ) = ( thisCon2, thisCon1 ) ;

      var d11 = thisCon1.Origin.DistanceTo( nextCon1.Origin ) ;
      var d12 = thisCon1.Origin.DistanceTo( nextCon2.Origin ) ;
      var d21 = thisCon2.Origin.DistanceTo( nextCon1.Origin ) ;
      var d22 = thisCon2.Origin.DistanceTo( nextCon2.Origin ) ;

      var routeToCon2 = ( d21 < d11 && d21 < d12 ) || ( d22 < d11 && d22 < d12 ) ;

      var startParam = 0.0 ;
      var endParam = ( point - thisCon1.Origin ).DotProduct( line.Direction ) / line.Length ;
      return routeToCon2 ? ( endParam, 1.0 ) : ( startParam, endParam ) ;
    }

    public static (double StartParam, double EndParam) CalculateParam( this MEPCurve mepCurve, XYZ point1, XYZ point2 )
    {
      var resParams = ( 0.0, 1.0 ) ;
      var (startConnector, endConnector) = Get2Connector( mepCurve ) ;
      if ( startConnector is null || endConnector is null )
        return resParams ;

      if ( mepCurve.Location is not LocationCurve { Curve: Line line } )
        return resParams ;

      // arrange connect so that vector connector 1 to connect 2 is same way as curve's direction
      if ( line.Direction.IsAlmostEqualTo( ( endConnector.Origin - startConnector.Origin ).Normalize() ) == false )
        startConnector = endConnector ;

      var startParam = ( point1 - startConnector.Origin ).DotProduct( line.Direction ) / line.Length ;
      var endParam = ( point2 - startConnector.Origin ).DotProduct( line.Direction ) / line.Length ;
      if ( startParam > endParam )
        ( startParam, endParam ) = ( endParam, startParam ) ;
      return ( startParam, endParam ) ;
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

    private static (Connector? ConnectorShort, Connector? ConnectorLong) IsOverlappedEndPoint( Element shortCurve, Element longCurve )
    {
      var (c11, c12) = Get2Connector( shortCurve ) ;
      var (c21, c22) = Get2Connector( longCurve ) ;
      if ( c11?.Origin is not { } p11 || c12?.Origin is not { } p12 || c21?.Origin is not { } p21 || c22?.Origin is not { } p22 )
        return ( null, null ) ;
      if ( p11.IsAlmostEqualTo( p21 ) && p12.IsBetween( p21, p22 ) )
        return ( c11, c21 ) ;
      if ( p11.IsAlmostEqualTo( p22 ) && p12.IsBetween( p21, p22 ) )
        return ( c11, c22 ) ;
      if ( p12.IsAlmostEqualTo( p21 ) && p11.IsBetween( p21, p22 ) )
        return ( c12, c21 ) ;
      if ( p12.IsAlmostEqualTo( p22 ) && p11.IsBetween( p21, p22 ) )
        return ( c12, c22 ) ;

      return ( null, null ) ;
    }

    private static (Connector? ConnectorFirst, Connector? ConnectorSecond) IsOverlappedEachOther( Element firstCurve, Element secondCurve )
    {
      var (c11, c12) = Get2Connector( firstCurve ) ;
      var (c21, c22) = Get2Connector( secondCurve ) ;
      if ( c11?.Origin is not { } p11 || c12?.Origin is not { } p12 || c21?.Origin is not { } p21 || c22?.Origin is not { } p22 )
        return ( null, null ) ;
      if ( p11.IsBetween( p21, p22 ) && p21.IsBetween( p11, p12 ) )
        return ( c12, c22 ) ;
      if ( p11.IsBetween( p21, p22 ) && p22.IsBetween( p11, p12 ) )
        return ( c12, c21 ) ;
      if ( p12.IsBetween( p21, p22 ) && p21.IsBetween( p11, p12 ) )
        return ( c11, c22 ) ;
      if ( p12.IsBetween( p21, p22 ) && p22.IsBetween( p11, p12 ) )
        return ( c11, c21 ) ;

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
      var (c11, c12) = Get2Connector( firstCurve ) ;
      var (c21, c22) = Get2Connector( secondCurve ) ;
      if ( c11?.Origin is not { } p11 || c12?.Origin is not { } p12 || c21?.Origin is not { } p21 || c22?.Origin is not { } p22 )
        return 0 ;
      if ( p11.IsBetween( p21, p22 ) && p12.IsBetween( p21, p22 ) )
        return 1 ;
      if ( p21.IsBetween( p11, p12 ) && p22.IsBetween( p11, p12 ) )
        return -1 ;
      return 0 ;
    }

    #endregion

    private static void Reconnect( Connector disconnectedConnector, Connector reconnectedConnector )
    {
      var connectedConnectors = disconnectedConnector.GetConnectedConnectors().ToArray() ;
      connectedConnectors.ForEach( disconnectedConnector.DisconnectFrom ) ;
      connectedConnectors.ForEach( c =>
      {
        if ( ! reconnectedConnector.IsConnectedTo( c ) )
          reconnectedConnector.ConnectTo( c ) ;
      } ) ;
    }

    private static bool TryExtendRack( Element firstCurve, Connector c1, Connector c2 )
    {
      var doc = firstCurve.Document ;
      if ( firstCurve is not FamilyInstance firstFi )
        return false ;

      // remember linking connectors
      var connectedConnectors1 = c1.GetConnectedConnectors().ToArray() ;
      var connectedConnectors2 = c2.GetConnectedConnectors().ToArray() ;
      connectedConnectors1.ForEach( c1.DisconnectFrom ) ;
      connectedConnectors2.ForEach( c2.DisconnectFrom ) ;
      var startPoint = c1.Origin ;
      var endPoint = c2.Origin ;

      // change length and rotate first curve
      var tf1 = firstFi.GetTransform() ;

      if ( firstCurve.Location is LocationPoint lcPoint )
        lcPoint.Point = startPoint ;

      ElementTransformUtils.RotateElement( doc, firstCurve.Id, Line.CreateBound( startPoint, startPoint + tf1.BasisZ ), tf1.BasisX.AngleTo( endPoint - startPoint ) ) ;
      firstCurve.ParametersMap.get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ) ).Set( ( endPoint - startPoint ).GetLength() ) ;

      // reconnect:
      if ( Get2Connector( firstCurve ) is not { Connector1: { } con1, Connector2: { } con2 } )
        return false ;
      if ( con2.Origin.IsAlmostEqualTo( startPoint ) )
        ( con1, con2 ) = ( con2, con1 ) ;
      connectedConnectors1.ForEach( con1.ConnectTo ) ;
      connectedConnectors2.ForEach( con2.ConnectTo ) ;
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
        return Array.Empty<Element>() ;
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
          if ( IsOverlappedEachOther( thisRack, otherRack ) is { ConnectorFirst: { } c1, ConnectorSecond: { } c2 } && TryExtendRack( thisRack, c1, c2 ) ) {
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
      if ( rack is not FamilyInstance instance || ! rack.IsRack() || Get2Connector( instance ) is not { Connector1: { } startConnector, Connector2: { } endConnector } )
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

    private static FamilySymbol? GetGenericRackSymbol( Document doc )
    {
      var rackSymbol = doc.GetFamilySymbols( ElectricalRoutingFamilyType.CableTray ).FirstOrDefault( symbol => symbol.Name == "汎用" ) ;
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

      // set cable rack comments
      // instance.SetProperty( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ), cableRackWidth == 0 ? RackTypes[ 0 ] : RackTypes[ 1 ] ) ;

      if ( instance.GetTransform() is not { } tf )
        return null ;

      var dAngle = ( creationParam.EndPoint - creationParam.StartPoint ).AngleOnPlaneTo( tf.BasisX ?? XYZ.BasisX, XYZ.BasisZ ) ;
      ElementTransformUtils.RotateElement( doc, instance.Id, Line.CreateBound( creationParam.StartPoint, creationParam.StartPoint + XYZ.BasisZ ), -dAngle ) ;
      //doc.Regenerate();
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
      instance.SetProperty( "トレイ長さ", creationParam.AdditionalLength ) ;

      // elbow radius
      instance.SetProperty( "Bend Radius", creationParam.Radius ) ;

      instance.SetProperty( "ラックの倍率", creationParam.ScaleFactor ) ;

      doc.Regenerate() ;

      if ( instance.Get2Connector() is not { Connector1: { } connector1, Connector2: { } connector2 } )
        return instance ;

      var origin = connector1.Origin.X < connector2.Origin.X ? connector1.Origin : connector2.Origin ;

      ElementTransformUtils.RotateElement( doc, instance.Id, Line.CreateBound( origin, origin + XYZ.BasisZ ), creationParam.Angle ) ;
      doc.Regenerate() ;
      ElementTransformUtils.MoveElement( doc, instance.Id, creationParam.InsertPoint - origin ) ;
      //doc.Regenerate();
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
      if ( minDistance == d11 )
        markPoints = ( pair1.Item2, pair1.Item1, pair2.Item1, pair2.Item2 ) ;
      else if ( minDistance == d12 )
        markPoints = ( pair1.Item2, pair1.Item1, pair2.Item2, pair2.Item1 ) ;
      else if ( minDistance == d21 )
        markPoints = ( pair1.Item1, pair1.Item2, pair2.Item1, pair2.Item2 ) ;
      else
        markPoints = ( pair1.Item1, pair1.Item2, pair2.Item2, pair2.Item1 ) ;
      return markPoints ;
    }

    private static (XYZ, XYZ, XYZ, XYZ) ReArrangeToConnect( (XYZ, XYZ) pair1, (XYZ, XYZ) pair2, double width, double scaleRate, double elbowAnnotationRadius, double elbowAdditionLength )
    {
      var markPoints = ReArrange( pair1, pair2 ) ;
      var minDistance = markPoints.P12.DistanceTo( markPoints.P21 ) ;

      var isSameDirection = ( markPoints.P11 - markPoints.P12 ).Normalize().IsAlmostEqualTo( ( markPoints.P21 - markPoints.P22 ).Normalize() ) ;
      if ( minDistance < 0.01 && isSameDirection ) {
        // align : unify
        markPoints.P21 = markPoints.P11 ;
        markPoints.P11 = XYZ.Zero ;
        markPoints.P12 = XYZ.Zero ;
      }
      else {
        // perpendicular: modify to have enough space for elbow
        var line1 = Line.CreateUnbound( markPoints.P11, markPoints.P12 - markPoints.P11 ) ;
        var line2 = Line.CreateUnbound( markPoints.P22, markPoints.P21 - markPoints.P22 ) ;
        IntersectionResultArray resultArray ;
        var result = line1.Intersect( line2, out resultArray ) ;
        if ( result == SetComparisonResult.Disjoint || resultArray is null || resultArray.IsEmpty )
          return markPoints ;
        var res1 = resultArray!.get_Item( 0 ) ;
        var intersectedPoint = res1.XYZPoint ;
        var realElbowRadius = ( scaleRate - 1 ) * width / 2 + elbowAnnotationRadius ;
        var distanceFromIntersect = realElbowRadius + width / 2 + elbowAdditionLength ;

        markPoints.P12 = intersectedPoint + ( markPoints.P11 - markPoints.P12 ).Normalize() * distanceFromIntersect ;
        markPoints.P21 = intersectedPoint + ( markPoints.P22 - markPoints.P21 ).Normalize() * distanceFromIntersect ;
      }

      return markPoints ;
    }

    private static List<(XYZ, XYZ)> Simplify( IEnumerable<(XYZ, XYZ)> markers )
    {
      var simplifiedMarkers = new List<(XYZ, XYZ)>() ;
      var markerList = markers.ToList() ;
      for ( var i = 0 ; i < markerList.Count ; i++ ) {
        // final marker
        if ( i == markerList.Count - 1 ) {
          simplifiedMarkers.Add( markerList[ i ] ) ;
          break ;
        }

        var fourPoints = ReArrange( markerList[ i ], markerList[ i + 1 ] ) ;
        var isSameDirection = ( fourPoints.P11 - fourPoints.P12 ).Normalize().IsAlmostEqualTo( ( fourPoints.P21 - fourPoints.P22 ).Normalize() ) ;
        if ( fourPoints.P12.IsAlmostEqualTo( fourPoints.P21 ) && isSameDirection ) {
          // extend next marker and ignore this marker
          markerList[ i + 1 ] = ( fourPoints.P11, fourPoints.P22 ) ;
        }
        else {
          // add this marker to list
          simplifiedMarkers.Add( ( fourPoints.P11, fourPoints.P12 ) ) ;
        }
      }

      return simplifiedMarkers ;
    }

    private static bool TryToConnect( FamilyInstance? fi1, FamilyInstance? fi2 )
    {
      if ( fi1 is null || fi2 is null )
        return false ;
      var connectorList1 = fi1.GetConnectors().Where( c => ! c.IsConnected ).ToList() ;
      var connectorList2 = fi2.GetConnectors().Where( c => ! c.IsConnected ).ToList() ;
      foreach ( var connector1 in connectorList1 ) {
        if ( connectorList2.FirstOrDefault( con => con.Origin.IsAlmostEqualTo( connector1.Origin ) ) is not { } connector2 )
          continue ;
        connector1.ConnectTo( connector2 ) ;
        return true ;
      }

      return false ;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rackWidth">must be in Revit API unit</param>
    /// <returns></returns>
    public static IEnumerable<Element> CreateRacksAlignToConduits( this Document doc, IEnumerable<Element> allElementsInRoute, double rackWidth, IEnumerable<(Element Conduit, double StartParam, double EndParam)>? specialLengthList = null )
    {
      // read conduit markers
      var racks = new List<Element>() ;

      var conduitMarkers = new List<(XYZ, XYZ)>() ;
      foreach ( var element in allElementsInRoute ) {
        if ( element is not Conduit conduit )
          continue ;
        var (startParam, endParam) = ( 0.0, 1.0 ) ;
        if ( specialLengthList?.FirstOrDefault( x => x.Conduit.Id.Equals( element.Id ) ) is { Conduit: Conduit } specialLengthItem )
          ( startParam, endParam ) = ( specialLengthItem.StartParam, specialLengthItem.EndParam ) ;

        conduitMarkers.Add( GetEndPoints( conduit, startParam, endParam ) ) ;
      }

      // new function here: input rawRackMarkers
      var rackMarkers = Simplify( conduitMarkers ) ;

      var rackType = GetGenericRackSymbol( doc ) ;
      var elbowType = GetElbowSymbol( doc ) ;
      var scaleFactor = doc.ActiveView.Scale * 1.0 / 100 ;
      var r0 = 20d.MillimetersToRevitUnits() ;
      var l0 = 25d.MillimetersToRevitUnits() ;
      var elbowRadius = ( scaleFactor - 1 ) * rackWidth / 2 + r0 ;

      for ( var i = 0 ; i < rackMarkers.Count ; i++ ) {
        if ( i == rackMarkers.Count - 1 ) {
          break ;
        }

        var fourPoints = ReArrangeToConnect( rackMarkers[ i ], rackMarkers[ i + 1 ], rackWidth, scaleFactor, r0, l0 ) ;
        rackMarkers[ i ] = ( fourPoints.Item1, fourPoints.Item2 ) ;
        rackMarkers[ i + 1 ] = ( fourPoints.Item3, fourPoints.Item4 ) ;
      }

      FamilyInstance? currentElbow = null ;

      // create rack by markers
      for ( var i = 0 ; i < rackMarkers.Count ; i++ ) {
        var rackMarker = rackMarkers[ i ] ;
        // create rack
        var creationParam = new RackCreationParam( rackMarker.Item1, rackMarker.Item2, rackWidth, scaleFactor, null, rackType ) ;

        FamilyInstance? newRack = null ;
        if ( CreateRack( doc, creationParam ) is { } rack ) {
          rack.SetProperty( "起点の表示", i == 0 ) ;
          rack.SetProperty( "終点の表示", i == rackMarkers.Count - 1 ) ;
          racks.Add( rack ) ;
          newRack = rack ;
        }

        // Connect to current elbow:
        TryToConnect( newRack, currentElbow ) ;

        // create rack elbow
        if ( i == rackMarkers.Count - 1 )
          continue ;
        var nextMarker = rackMarkers[ i + 1 ] ;
        var rotateClockWise = ( rackMarker.Item2 - rackMarker.Item1 ).CrossProduct( nextMarker.Item1 - rackMarker.Item2 ).Z > 0 ;
        var elbowInsertPoint = rotateClockWise ? rackMarker.Item2 : nextMarker.Item1 ;
        var elbowDirection = rotateClockWise ? rackMarker.Item2 - rackMarker.Item1 : nextMarker.Item1 - nextMarker.Item2 ;
        var elbowRotateAngle = XYZ.BasisX.AngleOnPlaneTo( elbowDirection, XYZ.BasisZ ) ;

        var elbowCreationParam = new ElbowCreationParam( elbowInsertPoint, elbowRotateAngle, rackWidth, elbowRadius, l0, scaleFactor, null, elbowType ) ;
        var elbow = CreateElbow( doc, elbowCreationParam ) ;

        // connect rack to new elbow
        TryToConnect( newRack, elbow ) ;
        currentElbow = elbow ;
      }

      return racks ;

      // foreach ( var element in allElementsInRoute ) {
      //   using var subTransaction = new SubTransaction( doc ) ;
      //   try {
      //     subTransaction.Start() ;
      //     if ( element is Conduit conduit ) // element is straight conduit
      //     {
      //       // create rack
      //       var level = (Level)doc.GetElement( conduit.LevelId ) ;
      //       var (startParam , endParam) = (0.0, 1.0) ;
      //       if ( specialLengthList?.FirstOrDefault( x => x.Conduit.Id.Equals( element.Id ) ) is { Conduit: Conduit } specialLengthItem )
      //         (startParam , endParam) = (specialLengthItem.StartParam, specialLengthItem.EndParam ) ;
      //
      //       var (startPoint, endPoint) = GetEndPoints( conduit, startParam, endParam ) ;
      //       
      //       var creationParam = new RackCreationParam( startPoint, endPoint, rackWidth, level, rackType ) ;
      //       var rack = CreateRack( doc, creationParam ) ;
      //       
      //       // check cable tray exists
      //       if ( rack is null || NewRackCommandBase.ExistsCableTray( doc, rack ) ) {
      //         subTransaction.RollBack() ;
      //         continue ;
      //       }
      //       
      //       
      //       
      //       
      //       
      //       
      //       // set To-Side Connector Id
      //       var (fromConnectorId, toConnectorId) = NewRackCommandBase.GetFromAndToConnectorUniqueId( conduit ) ;
      //       if ( ! string.IsNullOrEmpty( toConnectorId ) )
      //         rack.TrySetProperty( ElectricalRoutingElementParameter.ToSideConnectorId, toConnectorId ) ;
      //       if ( ! string.IsNullOrEmpty( fromConnectorId ) )
      //         rack.TrySetProperty( ElectricalRoutingElementParameter.FromSideConnectorId, fromConnectorId ) ;
      //       
      //
      //       // save connectors of cable rack
      //       connectors.AddRange( rack.GetConnectorManager()!.Connectors.Cast<Connector>() ) ;
      //       racksAndFitting.Add( rack ) ;
      //     }
      //     else // element is conduit fitting
      //     {
      //       continue ;
      //     }
      //
      //
      //     subTransaction.Commit() ;
      //   }
      //   catch {
      //     subTransaction.RollBack() ;
      //   }
      // }
      //
      // var maxTolerance = ( 20.0 ).MillimetersToRevitUnits() ;
      // // connect all connectors
      // foreach ( var connector in connectors ) {
      //   if ( connector.IsConnected || connectors.FindAll( x => ! x.IsConnected && x.Owner.Id != connector.Owner.Id ) is not { } otherConnectors )
      //     continue ;
      //   if ( NewRackCommandBase.GetConnectorClosestTo( otherConnectors, connector.Origin, maxTolerance ) is { } connectTo )
      //     connector.ConnectTo( connectTo ) ;
      //   doc.Regenerate() ;
      // }
    }
    
    public static bool IsVertical( this Conduit conduit )
    {
      if ( conduit?.Location is not LocationCurve { Curve: Line line } )
        throw new Exception( "The required location is line!" ) ;
        
      return Math.Abs( Math.Abs( line.Direction.DotProduct( XYZ.BasisZ ) ) - 1 ) < GeometryUtil.Tolerance ;
    }
  }
}