using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MoreLinq ;
using ImageType = Arent3d.Revit.UI.ImageType ;
using Line = Autodesk.Revit.DB.Line ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Rack
{
  public static class CreateRackBySelectedConduitsCommandUtils
  {
    public static bool IsBetween( this XYZ p, XYZ p1, XYZ p2 )
    {
      return Math.Abs( ( p2 - p ).AngleTo( p1 - p ) - Math.PI ) < 0.01 ;
    }

    public static bool HasPoint( this MEPCurve mepCurve, XYZ point )
    {
      var (con1, con2) = Get2Connector( mepCurve ) ;
      if ( con1?.Origin is not { } p1 || con2?.Origin is not {} p2 )
        return false ;
      
      return point.IsBetween( p1, p2 ) ;
    }

    public static XYZ? ProjectOn( this XYZ point, MEPCurve curve )
    {
      if ( curve.Location is not LocationCurve locationCurve )
        return null ;
      var (p1, p2) = ( locationCurve.Curve.GetEndPoint( 0 ), locationCurve.Curve.GetEndPoint( 1 ) ) ;
      var vCurve = (p2 - p1).Normalize() ;
      return p1 + vCurve * ( point - p1 ).DotProduct( vCurve ) ;
    }

    private static (Connector? Connector1, Connector? Connector2) Get2Connector( Element curve )
    {
      var cons = curve.GetConnectors() ;
      if(cons.Count() != 2)
        return ( null, null ) ;
      return ( cons.ElementAt( 0 ), cons.ElementAt( 1 ) ) ;
    }
    
    private static (Connector?, Connector?) Get2Connector( Connector connector )
    {
      var (con1, con2) = Get2Connector( connector.Owner ) ;
      if ( con1?.Id == connector.Id )
        return ( con1, con2 ) ;
      if ( con2?.Id == connector.Id )
        return ( con2, con1 ) ;
      return ( null, null ) ;
    }

    private static bool GetLinkedMepCurve(List<Element> accumulateList, Connector startConnector, Element? stopElement = null)
    {
      if ( ! startConnector.IsConnected )
        return false;
      var ids = accumulateList.Select( el => el.Id ) ;
      var connectedConnectors = startConnector.GetConnectedConnectors() ;
      var connectedConnector = connectedConnectors.FirstOrDefault( con => ! ids.Contains( con.Owner.Id ) ) ;
      Element element = connectedConnector.Owner ;
      accumulateList.Add(element);
      if ( element.Id == stopElement?.Id )
        return true;
      var (con1, con2) = Get2Connector( connectedConnector ) ;
      if ( con2 is { } )
        return GetLinkedMepCurve( accumulateList, con2, stopElement ) ;
      return false ;
    }
    
    public static List<Element> FindLinkedRacks( this MEPCurve startCurve, MEPCurve endCurve )
    {
      if ( startCurve.Id == endCurve.Id )
        return new List<Element>() { startCurve } ;
      var (start1, start2) = Get2Connector( startCurve ) ;
      var accumulateList = new List<Element>() {startCurve} ;
      if ( start1 is { } && GetLinkedMepCurve( accumulateList, start1, endCurve ) )
        return accumulateList ;
      accumulateList = new List<Element>() {startCurve} ;
      if ( start2 is { } && GetLinkedMepCurve( accumulateList, start2, endCurve ) )
        return accumulateList ;
      accumulateList.Clear() ;
      
      
      return accumulateList ;
    }

    public static (double StartParam, double EndParam) CalculateParam(this MEPCurve thisCurve, XYZ point, Element nextCurve )
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
      
      var routeToCon2 = (d21 < d11 && d21 < d12) || (d22 < d11 && d22 < d12) ;

      var startParam = 0.0 ;
      var endParam = ( point - thisCon1.Origin ).DotProduct( line.Direction ) / line.Length ;
      return routeToCon2 ?( endParam, 1.0 ): ( startParam, endParam ) ;
    }
    
    public static (double StartParam, double EndParam) CalculateParam(this MEPCurve thisCurve, XYZ point1, XYZ point2 )
    {
      var resParams = ( 0.0, 1.0 ) ;
      var (thisCon1, thisCon2) = Get2Connector( thisCurve ) ;
      if ( thisCon1 is null || thisCon2 is null )
        return resParams ;
      
      if ( thisCurve.Location is not LocationCurve { Curve: Line line } )
        return resParams ;
      
      // arrange connect so that vector connector 1 to connect 2 is same way as curve's direction
      if ( line.Direction.IsAlmostEqualTo( ( thisCon2.Origin - thisCon1.Origin ).Normalize() ) == false )
        ( thisCon1, thisCon2 ) = ( thisCon2, thisCon1 ) ;

      var startParam = ( point1 - thisCon1.Origin ).DotProduct( line.Direction ) / line.Length ;
      var endParam = ( point2 - thisCon1.Origin ).DotProduct( line.Direction ) / line.Length ;
      if ( startParam > endParam )
        ( startParam, endParam ) = ( endParam, startParam ) ;
      return ( startParam, endParam ) ;
    }

    #region Classify relative position of 2 racks
    private static (Connector? ConnectorShort, Connector? ConnectorLong) IsOverlappedEndPoint( Element shortCurve, Element longCurve )
    {
      var (c11, c12) = Get2Connector( shortCurve ) ;
      var (c21, c22) = Get2Connector( longCurve ) ;
      if ( c11?.Origin is not { } p11 || c12?.Origin is not { } p12 || c21?.Origin is not { } p21 ||
           c22?.Origin is not { } p22 )
        return ( null, null ) ;
      if ( p11.IsAlmostEqualTo( p21 ) && p12.IsBetween( p21, p22 ))
        return ( c11, c21 ) ;
      if ( p11.IsAlmostEqualTo( p22 ) && p12.IsBetween( p21, p22 ))
        return ( c11, c22 ) ;
      if ( p12.IsAlmostEqualTo( p21 ) && p11.IsBetween( p21, p22 ))
        return ( c12, c21 ) ;
      if ( p12.IsAlmostEqualTo( p22 ) && p11.IsBetween( p21, p22 ))
        return ( c12, c22 ) ;
      
      return ( null, null ) ;
    }
    
    private static (Connector? ConnectorFirst, Connector? ConnectorSecond) IsOverlappedEachOther( Element firstCurve, Element secondCurve )
    {
      var (c11, c12) = Get2Connector( firstCurve ) ;
      var (c21, c22) = Get2Connector( secondCurve ) ;
      if ( c11?.Origin is not { } p11 || c12?.Origin is not { } p12 || c21?.Origin is not { } p21 ||
           c22?.Origin is not { } p22 )
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
    /// <param name="shortCurve">first MEP curve</param>
    /// <param name="longCurve">second MEP curve</param>
    /// <returns>1: first is inside second, -1: second is inside first, 0: neither</returns>
    private static int IsInside( Element firstCurve, Element secondCurve )
    {
      var (c11, c12) = Get2Connector( firstCurve ) ;
      var (c21, c22) = Get2Connector( secondCurve ) ;
      if ( c11?.Origin is not { } p11 || c12?.Origin is not { } p12 || c21?.Origin is not { } p21 ||
           c22?.Origin is not { } p22 )
        return 0 ;
      if ( p11.IsBetween( p21, p22 ) && p12.IsBetween( p21, p22 ) )
        return 1 ;
      if ( p21.IsBetween( p11, p12 ) && p22.IsBetween( p11, p12 ) )
        return -1 ;
      return 0 ;
    }
    

    #endregion Relative position classify

    private static void Reconnect( Connector disconnectedConnector, Connector reconnectedConnector )
    {
      var connectedConnectors = disconnectedConnector.GetConnectedConnectors().ToArray() ;
      connectedConnectors.ForEach(disconnectedConnector.DisconnectFrom);
      connectedConnectors.ForEach(c => {
        if(!reconnectedConnector.IsConnectedTo(c))
          reconnectedConnector.ConnectTo(c);
      });
    }

    private static bool TryExtendRack( Element firstCurve, Connector c1, Connector c2 )
    {
      var doc = firstCurve.Document ;
      if ( firstCurve is not FamilyInstance firstFi )
        return false ;
      
      // remember linking connectors
      var connectedConnectors1 = c1.GetConnectedConnectors().ToArray() ;
      var connectedConnectors2 = c2.GetConnectedConnectors().ToArray() ;
      connectedConnectors1.ForEach(c1.DisconnectFrom);
      connectedConnectors2.ForEach(c2.DisconnectFrom);
      var startPoint = c1.Origin ;
      var endPoint = c2.Origin ;
      
      // change length and rotate first curve
      var tf1 = firstFi.GetTransform() ;
      var needToReverse = ! tf1.BasisX.IsAlmostEqualTo( ( endPoint - startPoint ).Normalize() ) ;

      if ( firstCurve.Location is LocationPoint lcPoint )
        lcPoint.Point = startPoint ;
      
      ElementTransformUtils.RotateElement(doc, firstCurve.Id, Line.CreateBound(startPoint, startPoint + tf1.BasisZ), tf1.BasisX.AngleTo(endPoint - startPoint) );
      
      firstCurve.ParametersMap
        .get_Item( "Revit.Property.Builtin.TrayLength".GetDocumentStringByKeyOrDefault( doc, "トレイ長さ" ) ).Set( ( endPoint - startPoint ).GetLength() ) ;
      
      // reconnect:
      if ( Get2Connector( firstCurve ) is not { Connector1: { } con1, Connector2: { } con2 } )
        return false ;
      if ( con2.Origin.IsAlmostEqualTo( startPoint ) )
        ( con1, con2 ) = ( con2, con1 ) ;
      connectedConnectors1.ForEach(con1.ConnectTo);
      connectedConnectors2.ForEach(con2.ConnectTo);
      doc.Regenerate();
      return true ;

    }
    
    public static void RemoveShorterInDuplicatedRacks( this Document document, List<FamilyInstance> racks )
    {
      var ids = racks.Select( rack => rack.Id ) ;
      var otherRacks = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_CableTrayFitting )
        .Where( r => ! ids.Contains( r.Id ) ).ToList() ;

      if ( !otherRacks.Any() )
        return ;
      var deletedRacks = new List<FamilyInstance>() ;
      var idsToRemove = new List<ElementId>() ;
      foreach ( var thisRack in racks ) {
        foreach ( var otherRack in otherRacks ) {
          
          // case 1: overlap at an end point
          if ( IsOverlappedEndPoint( thisRack, otherRack ) is { ConnectorShort: {} cDisConnectNew, ConnectorLong: {} cReConnectOld } ) {
            idsToRemove.Add( thisRack.Id ) ;
            deletedRacks.Add( thisRack ) ;
            Reconnect( cDisConnectNew, cReConnectOld ) ;
            continue;
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
          if ( IsOverlappedEachOther( thisRack, otherRack ) is { ConnectorFirst: { } c1, ConnectorSecond: { } c2 } 
               && TryExtendRack( thisRack, c1, c2 ) ) {
            idsToRemove.Add( otherRack.Id ) ;
          }
        }
      }

      deletedRacks.ForEach(r => racks.Remove(r));
      document.Delete( idsToRemove ) ;
    }
  }
  public class ConduitFilter : ISelectionFilter
  {
    private Document Doc { get ;  }
    private IEnumerable<string>? RouteNames { get ; }

    public ConduitFilter( Document doc , IEnumerable<string>? routeNames = null )
    {
      Doc = doc ;
      RouteNames = routeNames ;
    }

    public bool AllowElement( Element element )
    {
      if ( BuiltInCategory.OST_Conduit != element.GetBuiltInCategory() )
        return false ;
      if ( RouteNames is null )
        return true ;
      return element.GetRouteName() is { } name && RouteNames.Contains( name ) ;
    }

    public bool AllowReference( Reference reference, XYZ position )
    {
      var element = Doc.GetElement( reference ) ;
      return AllowElement( element ) ;
    }
  }

  
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Rack.CreateRackBySelectedConduitsCommand", DefaultString = "Manually\nCreate Rack" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class CreateRackBySelectedConduitsCommand : IExternalCommand
  {
    private record SelectState( MEPCurve? ConduitStart, MEPCurve? ConduitEnd, XYZ StartPoint, XYZ EndPoint, string RouteName ) ;
    private OperationResult<SelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var doc = uiDocument.Document ;
      var filterConduit = new ConduitFilter( doc ) ;
      var allConduits = doc.GetAllElements<Conduit>() ;

      try {
        
        // select point 1 and conduit 1
        var rf = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filterConduit, "始点を選択して下さい。" ) ;
        XYZ p1 = rf.GlobalPoint ;
        if(doc.GetElement( rf ) is not MEPCurve cd1)
          return OperationResult<SelectState>.Cancelled;
        var p1OnConduit = p1.ProjectOn( cd1 ) ;
        if(p1OnConduit is null)
          return OperationResult<SelectState>.Cancelled;
        
        // get all conduits that overlapped with conduit 1
        var overlappedConduits = allConduits.Where( cd => cd.HasPoint(p1OnConduit) ).OfType<MEPCurve>().ToList() ;
        overlappedConduits.Add( cd1 ) ;
        
        // get route names of conduit group
        var routeNames = overlappedConduits.Select( cd => cd.GetRouteName()?? "" ).Where(name => name != "").Distinct().ToList() ;

        // select point 2 on conduits that has route name exists in route names selected before
        filterConduit = new ConduitFilter( doc , routeNames ) ;
        rf = uiDocument.Selection.PickObject( ObjectType.PointOnElement, filterConduit, "終点を選択して下さい。" ) ;
        XYZ p2 = rf.GlobalPoint ;
        var cd2 = doc.GetElement( rf ) as MEPCurve ;
        var routeName = cd2?.GetRouteName() ?? "" ;
        cd1 = overlappedConduits.FirstOrDefault( cd => cd.GetRouteName() == routeName ) ;

        return new OperationResult<SelectState>( new SelectState( cd1,cd2, p1, p2, routeName ) ) ;

      }
      catch ( OperationCanceledException ) {
        return OperationResult<SelectState>.Cancelled;
      }
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiApp = commandData.Application ;
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      // select conduit, start point, end point of rack
      var uiResult = OperateUI( commandData, elements ) ;
      if ( !uiResult.HasValue || uiResult.Value.ConduitStart is not { } conduit1|| uiResult.Value.ConduitEnd is not { } conduit2 ) {
        return Result.Cancelled ;
      }
      
      // make cable racks:
      using var ts = new Transaction( document, "create rack for conduits" ) ;
      var linkedConduits = conduit1.FindLinkedRacks( conduit2 ) ;
      var specialLengthList = new List<(Element Conduit, double StartParam, double EndParam)>() ;
      if ( conduit1.Id != conduit2.Id ) {
        var firstParams = conduit1.CalculateParam( uiResult.Value.StartPoint, linkedConduits.ElementAt( 1 )) ;
        var lastParams = conduit2.CalculateParam( uiResult.Value.EndPoint, linkedConduits.ElementAt( linkedConduits.Count - 2 )) ;
        specialLengthList.Add( (conduit1, firstParams.StartParam, firstParams.EndParam) );
        specialLengthList.Add( (conduit2, lastParams.StartParam, lastParams.EndParam) );
      }
      else {
        var firstParams = conduit1.CalculateParam( uiResult.Value.StartPoint, uiResult.Value.EndPoint) ;
        specialLengthList.Add( (conduit1, firstParams.StartParam, firstParams.EndParam) );
      }

      ts.Start() ;
      var racks = new List<FamilyInstance>() ;
      // create racks along with conduits
      NewRackCommandBase.CreateRackForConduit( uiDocument, uiApp.Application, linkedConduits, racks,
        specialLengthList ) ;
      
      // resolve overlapped cases
      document.RemoveShorterInDuplicatedRacks( racks ) ;
      
      // create annotations for racks
      NewRackCommandBase.CreateNotationForRack( document, uiApp.Application, racks ) ;
      ts.Commit() ;
      return Result.Succeeded ;
    }
  }
}