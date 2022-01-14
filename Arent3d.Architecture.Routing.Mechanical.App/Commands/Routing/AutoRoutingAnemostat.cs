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
    private record SegmentSetting( MEPSystemClassificationInfo ClassificationInfo, FixedHeight? FixedHeight, MEPSystemType? SystemType, MEPCurveType? CurveType, string NameBase ) ;

    private readonly IList<(string routeName, RouteSegment)>? _listOfRightSegments ;
    private readonly IList<(string routeName, RouteSegment)>? _listOfLeftSegments ;
    private readonly IList<(string routeName, RouteSegment)>? _listOfRemainSegments ;
    private int _routeIndex ;

    public AutoRoutingAnemostat( Document doc, MechanicalSystem fasuMechanicalSystem, Connector fasuInConnector, IList<Connector> fasuNotInConnectors, IList<Connector> anemoConnectors )
    {
      // FASUのコネクタがRight, Left を区別すてソートする。
      var (rightFasuConnectors, leftFasuConnectors) = GetSortedConnectors( fasuInConnector, fasuNotInConnectors ) ;

      // システムアネモのコネクタがRight, Left を区別する
      var (rightAnemoConnectors, leftAnemoConnectors) = GetSortedConnectors( fasuInConnector, anemoConnectors ) ;

      var segmentSetting = GetSegmentSetting( doc, fasuInConnector ) ;
      _listOfRightSegments = CreateRouteSegments( rightFasuConnectors, rightAnemoConnectors, segmentSetting ) ;
      _listOfLeftSegments = CreateRouteSegments( leftFasuConnectors, leftAnemoConnectors, segmentSetting ) ;
      _listOfRemainSegments = CreateRemainRouteSegments( rightFasuConnectors, rightAnemoConnectors, leftFasuConnectors, leftAnemoConnectors, segmentSetting ) ;

      // Auto set Duct System for Anemostats
      CreateDuctSystem( anemoConnectors, fasuMechanicalSystem ) ;
    }

    private static void CreateDuctSystem( IEnumerable<Connector> anemoConnectors, MechanicalSystem fasuMechanicalSystem )
    {
      var connectorSets = new ConnectorSet() ;
      foreach ( var anemoConnector in anemoConnectors ) {
        if ( anemoConnector == null || anemoConnector.MEPSystem is MechanicalSystem ) continue ;
        connectorSets.Insert( anemoConnector ) ;
      }

      // Todo FASUのIn以外のコネクタを新しいダクトシステムに追加。今は例外が発生します。原因としてFASUはMechanical EquipmentやAir Terminalsじゃないものからです。
      fasuMechanicalSystem.Add( connectorSets ) ;
    }

    private static (List<Connector>, List<Connector>) GetSortedConnectors( IConnector inConnector, IEnumerable<Connector> notInConnectors )
    {
      var rightConnectors = new List<Connector>() ;
      var leftConnectors = new List<Connector>() ;
      var inConnectorOrigin = inConnector.Origin.To3dPoint().To2d() ;
      var inConnectorNormal = inConnector.CoordinateSystem.BasisZ.To3dDirection().To2d().normalized ;
      var orthogonalWithInConnectorNormal = new Vector2d( -inConnectorNormal.y, inConnectorNormal.x ) ;
      foreach ( var notInConnector in notInConnectors ) {
        // Vector from in connector to out connector or from in connector to anemo connector
        var inOutVector = notInConnector.Origin.To3dPoint().To2d() - inConnectorOrigin ;

        // 二つ側にINコネクタの以外を分別
        if ( Vector2d.Dot( orthogonalWithInConnectorNormal, inOutVector ) < 0 ) {
          rightConnectors.Add( notInConnector ) ;
        }
        else {
          leftConnectors.Add( notInConnector ) ;
        }
      }

      rightConnectors.Sort( ( a, b ) => CompareAngle( inConnectorOrigin, inConnectorNormal, a, b ) ) ;
      leftConnectors.Sort( ( a, b ) => CompareAngle( inConnectorOrigin, inConnectorNormal, a, b ) ) ;
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

    private static MEPCurveType? GetRoundDuctTypeWhosePreferred( Document document )
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( type => type is DuctType && type.Shape == ConnectorProfileType.Round ) ;
    }

    private SegmentSetting? GetSegmentSetting( Document doc, Connector inConnector )
    {
      var classificationInfo = MEPSystemClassificationInfo.From( inConnector ) ;
      if ( classificationInfo == null ) return null ;
      var systemType = doc.GetAllElements<MEPSystemType>().Where( classificationInfo.IsCompatibleTo ).FirstOrDefault() ;
      var curveType = GetRoundDuctTypeWhosePreferred( doc ) ;
      if ( curveType == null ) return null ;
      var nameBase = TTEUtil.GetNameBase( systemType, curveType ) ;
      var nextIndex = TTEUtil.GetRouteNameIndex( RouteCache.Get( doc ), nameBase ) ;
      _routeIndex = nextIndex ;
      var fasuLevel = doc.GuessLevel( inConnector.Origin ) ;
      var fixedHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, inConnector.Origin.Z - fasuLevel.Elevation ) ;
      return new SegmentSetting( classificationInfo, fixedHeight, systemType, curveType, nameBase ) ;
    }

    private IList<(string routeName, RouteSegment)> CreateRouteSegments( IReadOnlyList<Connector> fasuConnectors, IReadOnlyList<Connector> anemoConnectors, SegmentSetting? segmentSetting )
    {
      List<(string routeName, RouteSegment)> segmentList = new() ;
      if ( segmentSetting == null ) return segmentList ;
      var segmentCount = Math.Min( fasuConnectors.Count, anemoConnectors.Count ) ;
      for ( var index = 0 ; index < segmentCount ; index++ ) {
        var fromPoint = new ConnectorEndPoint( fasuConnectors[ index ], null ) ;
        var toPoint = new ConnectorEndPoint( anemoConnectors[ index ], null ) ;
        var routeName = segmentSetting.NameBase + "_" + _routeIndex++ ;
        segmentList.Add( ( routeName, new RouteSegment( segmentSetting.ClassificationInfo, segmentSetting.SystemType, segmentSetting.CurveType, fromPoint, toPoint, null, false, segmentSetting.FixedHeight, segmentSetting.FixedHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ) ) ;
      }

      return segmentList ;
    }

    private IList<(string routeName, RouteSegment)> CreateRemainRouteSegments( IReadOnlyList<Connector> rightFasuConnectors, IReadOnlyList<Connector> rightAnemoConnectors, IReadOnlyList<Connector> leftFasuConnectors, IReadOnlyList<Connector> leftAnemoConnectors, SegmentSetting? segmentSetting )
    {
      List<(string routeName, RouteSegment)> segmentList = new() ;
      if ( segmentSetting == null ) return segmentList ;

      List<Connector> remainAnemoConnectors = new() ;
      List<Connector> remainFasuConnectors = new() ;
      if ( rightAnemoConnectors.Count > rightFasuConnectors.Count ) {
        remainAnemoConnectors = rightAnemoConnectors.ToList().Skip( rightFasuConnectors.Count ).ToList() ;
        remainFasuConnectors = leftFasuConnectors.ToList() ;
      }

      if ( leftAnemoConnectors.Count > leftFasuConnectors.Count ) {
        remainAnemoConnectors = leftAnemoConnectors.ToList().Skip( leftFasuConnectors.Count ).ToList() ;
        remainFasuConnectors = rightFasuConnectors.ToList() ;
      }

      for ( var index = 0 ; index < remainAnemoConnectors.Count ; index++ ) {
        var fromPoint = new ConnectorEndPoint( remainFasuConnectors[ remainFasuConnectors.Count - index - 1 ], null ) ;
        var toPoint = new ConnectorEndPoint( remainAnemoConnectors[ index ], null ) ;
        var routeName = segmentSetting.NameBase + "_" + _routeIndex++ ;
        segmentList.Add( ( routeName, new RouteSegment( segmentSetting.ClassificationInfo, segmentSetting.SystemType, segmentSetting.CurveType, fromPoint, toPoint, null, false, segmentSetting.FixedHeight, segmentSetting.FixedHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ) ) ;
      }

      return segmentList ;
    }

    public IEnumerable<(string routeName, RouteSegment)> Execute()
    {
      if ( _listOfRightSegments != null ) {
        foreach ( var routeSegment in _listOfRightSegments ) {
          yield return routeSegment ;
        }
      }

      if ( _listOfLeftSegments != null ) {
        foreach ( var routeSegment in _listOfLeftSegments ) {
          yield return routeSegment ;
        }
      }

      if ( _listOfRemainSegments == null ) yield break ;
      foreach ( var routeSegment in _listOfRemainSegments ) {
        yield return routeSegment ;
      }
    }
  }
}