using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  public class AutoRoutingAnemostat
  {
    private record SegmentSetting( MEPSystemClassificationInfo ClassificationInfo, FixedHeight? FixedHeight, MEPSystemType? SystemType, MEPCurveType? CurveType, string RouteName ) ;

    private readonly IList<(string routeName, RouteSegment)>? _listOfRightSegments ;
    private readonly IList<(string routeName, RouteSegment)>? _listOfLeftSegments ;

    public AutoRoutingAnemostat( Document doc, Element fasu )
    {
      var notInConnectors = fasu.GetConnectors().Where( connector => connector.Direction != FlowDirectionType.In && ! connector.IsConnected ).ToList() ;
      var inConnector = fasu.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
      if ( inConnector == null || ! notInConnectors.Any() ) return ;

      // FASUのコネクタがRight, Left を区別すてソートする。
      var (rightFasuConnectors, leftFasuConnectors) = GetSortedConnectors( inConnector, notInConnectors ) ;

      var spaces = TTEUtil.GetAllSpaces( doc ) ;
      var spaceContainFasu = GetSpace( doc, spaces, inConnector ) ;
      var anemostats = GetAnemostat( doc, spaceContainFasu ) ;

      // Get all anemostat connectors in the space
      var anemoConnectors = anemostats.Select( anemostat => anemostat.GetConnectors().FirstOrDefault( connector => ! connector.IsConnected ) ).Where( anemoConnector => anemoConnector != null ).ToList() ;

      // システムアネモのコネクタがRight, Left を区別する
      var (rightAnemoConnectors, leftAnemoConnectors) = GetSortedConnectors( inConnector, anemoConnectors ) ;

      var segmentSetting = GetSegmentSetting( doc, inConnector ) ;
      _listOfRightSegments = CreateRouteSegments( doc, rightFasuConnectors, rightAnemoConnectors, segmentSetting ) ;
      _listOfLeftSegments = CreateRouteSegments( doc, leftFasuConnectors, leftAnemoConnectors, segmentSetting ) ;
    }

    private static (List<Connector>, List<Connector>) GetSortedConnectors( IConnector inConnector, List<Connector> notInConnectors )
    {
      var rightConnectors = new List<Connector>() ;
      var leftConnectors = new List<Connector>() ;
      var inConnectorOrigin = inConnector.Origin.To3dPoint().To2d() ;
      var inConnectorDirection = inConnector.CoordinateSystem.BasisZ.To3dPoint().To2d() ;
      var inConnectorNormal = new Vector2d( -inConnectorDirection.y, inConnectorDirection.x ) ;
      foreach ( var notInConnector in notInConnectors ) {
        // Vector from in connector to out connector or from in connector to anemo connector
        var inOutVector = notInConnector.Origin.To3dPoint().To2d() - inConnectorOrigin ;

        // 二つ側にINコネクタの以外を分別
        if ( Vector2d.Dot( inConnectorNormal, inOutVector ) < 0 ) {
          rightConnectors.Add( notInConnector ) ;
        }
        else {
          leftConnectors.Add( notInConnector ) ;
        }
      }

      rightConnectors.Sort( ( a, b ) => CompareAngle( inConnectorOrigin, inConnectorDirection, a, b ) ) ;
      leftConnectors.Sort( ( a, b ) => CompareAngle( inConnectorOrigin, inConnectorDirection, a, b ) ) ;
      return ( rightConnectors, leftConnectors ) ;
    }

    private static int CompareAngle( Vector2d inConnectorOrigin, Vector2d inConnectorDirection, IConnector a, IConnector b )
    {
      var aVector = a.Origin.To3dPoint().To2d() - inConnectorOrigin ;
      var bVector = b.Origin.To3dPoint().To2d() - inConnectorOrigin ;
      var aAngle = TTEUtil.GetAngleBetweenVector( inConnectorDirection, aVector ) ;
      var bAngle = TTEUtil.GetAngleBetweenVector( inConnectorDirection, bVector ) ;
      return aAngle.CompareTo( bAngle ) ;
    }

    private static IEnumerable<FamilyInstance> GetAnemostat( Document doc, Element? spaceContainFasu )
    {
      if ( spaceContainFasu == null ) yield break ;

      // Todo get anemostat don't use familyName
      var anemostats = doc.GetAllElements<FamilyInstance>().Where( anemostat => anemostat.Symbol.FamilyName == "システムアネモ" ) ;
      var spaceBox = spaceContainFasu.get_BoundingBox( doc.ActiveView ) ;
      foreach ( var anemostat in anemostats ) {
        if ( TTEUtil.IsInSpace( spaceBox, anemostat.GetConnectors().First().Origin ) ) yield return anemostat ;
      }
    }

    private static MEPCurveType? GetRoundDuctTypeWhosePreferred( Document document )
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( type => type is DuctType && type.Shape == ConnectorProfileType.Round ) ;
    }

    private static Element? GetSpace( Document doc, IEnumerable<Element> spaces, IConnector inConnector )
    {
      foreach ( var space in spaces ) {
        var spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
        if ( TTEUtil.IsInSpace( spaceBox, inConnector.Origin ) ) return space ;
      }

      return null ;
    }

    private static SegmentSetting? GetSegmentSetting( Document doc, Connector inConnector )
    {
      var classificationInfo = MEPSystemClassificationInfo.From( inConnector ) ;
      if ( classificationInfo == null ) return null ;
      var systemType = doc.GetAllElements<MEPSystemType>().Where( classificationInfo.IsCompatibleTo ).FirstOrDefault() ;
      var curveType = GetRoundDuctTypeWhosePreferred( doc ) ;
      if ( curveType == null ) return null ;
      var nameBase = TTEUtil.GetNameBase( systemType, curveType ) ;
      var nextIndex = TTEUtil.GetRouteNameIndex( RouteCache.Get( doc ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;
      var fasuLevel = doc.GuessLevel( inConnector.Origin ) ;
      var fixedHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, inConnector.Origin.Z - fasuLevel.Elevation ) ;
      return new SegmentSetting( classificationInfo, fixedHeight, systemType, curveType, routeName ) ;
    }

    private static IList<(string routeName, RouteSegment)> CreateRouteSegments( Document doc, IReadOnlyList<Connector> fasuConnectors, IReadOnlyList<Connector> anemoConnectors, SegmentSetting? segmentSetting )
    {
      List<(string routeName, RouteSegment)> segmentList = new() ;
      if ( segmentSetting == null ) return segmentList ;
      var segmentCount = Math.Min( fasuConnectors.Count, anemoConnectors.Count ) ;
      for ( var index = 0 ; index < segmentCount ; index++ ) {
        var fromPoint = new ConnectorEndPoint( fasuConnectors[ index ], null ) ;
        var toPoint = new ConnectorEndPoint( anemoConnectors[ index ], null ) ;
        segmentList.Add( ( segmentSetting.RouteName, new RouteSegment( segmentSetting.ClassificationInfo, segmentSetting.SystemType, segmentSetting.CurveType, fromPoint, toPoint, null, false, segmentSetting.FixedHeight, segmentSetting.FixedHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ) ) ;
        // Auto create Duct System  
        var connectors = new ConnectorSet() ;
        connectors.Insert( anemoConnectors[ index ] ) ;
        var system = doc.Create.NewMechanicalSystem( null, connectors, DuctSystemType.SupplyAir ) ;
      }

      return segmentList ;
    }

    public IEnumerable<(string routeName, RouteSegment)> Execute()
    {
      if ( _listOfRightSegments != null && _listOfRightSegments.Any() ) {
        foreach ( var routeSegment in _listOfRightSegments ) {
          yield return routeSegment ;
        }
      }

      if ( _listOfLeftSegments == null || ! _listOfLeftSegments.Any() ) yield break ;
      foreach ( var routeSegment in _listOfLeftSegments ) {
        yield return routeSegment ;
      }
    }
  }
}