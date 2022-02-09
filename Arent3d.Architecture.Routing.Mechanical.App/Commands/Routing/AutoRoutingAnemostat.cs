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
    private record SegmentSetting( MEPSystemClassificationInfo ClassificationInfo, FixedHeight? FixedHeight, MEPSystemType? SystemType, MEPCurveType? CurveType, UniqueNameCreatorForRoute uniqueNameCreatorForRoute ) ;

    private readonly Document _document ;
    private readonly MEPSystem _fasuMechanicalSystem ;
    private readonly Connector _fasuInConnector ;
    private readonly IList<Connector> _fasuNotInConnectors ;
    private readonly IList<Connector> _anemoConnectors ;

    public AutoRoutingAnemostat( Document document, MEPSystem fasuMechanicalSystem, Connector fasuInConnector, IList<Connector> fasuNotInConnectors, IList<Connector> anemoConnectors )
    {
      _document = document ;
      _fasuMechanicalSystem = fasuMechanicalSystem ;
      _fasuInConnector = fasuInConnector ;
      _fasuNotInConnectors = fasuNotInConnectors ;
      _anemoConnectors = anemoConnectors ;
    }

    public IEnumerable<(string routeName, RouteSegment)> Execute()
    {
      // FASUのコネクタがRight, Left を区別してソートする。
      var (rightFasuConnectors, leftFasuConnectors) = GetSortedConnectors( _fasuInConnector, _fasuNotInConnectors ) ;

      // システムアネモのコネクタがRight, Left を区別すてソートする。
      var (rightAnemoConnectors, leftAnemoConnectors) = GetSortedConnectors( _fasuInConnector, _anemoConnectors ) ;

      var segmentSetting = CreateSegmentSetting( _document, _fasuInConnector ) ;
      if ( segmentSetting == null ) yield break ;

      // Auto set Duct System for Anemostats
      AddConnectorsToMechanicalSystem( _anemoConnectors, _fasuMechanicalSystem ) ;

      var listOfRightSegments = CreateRouteSegments( rightFasuConnectors, rightAnemoConnectors, segmentSetting ) ;
      foreach ( var routeSegment in listOfRightSegments ) {
        yield return routeSegment ;
      }

      var listOfLeftSegments = CreateRouteSegments( leftFasuConnectors, leftAnemoConnectors, segmentSetting ) ;
      foreach ( var routeSegment in listOfLeftSegments ) {
        yield return routeSegment ;
      }

      var listOfRemainSegments = CreateRemainRouteSegments( rightFasuConnectors, rightAnemoConnectors, leftFasuConnectors, leftAnemoConnectors, segmentSetting ) ;
      foreach ( var routeSegment in listOfRemainSegments ) {
        yield return routeSegment ;
      }
    }

    private (List<Connector>, List<Connector>) GetSortedConnectors( IConnector inConnector, IEnumerable<Connector> notInConnectors )
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

      rightConnectors.Sort( ( a, b ) => TTEUtil.CompareAngle( inConnectorOrigin, inConnectorNormal, a, b ) ) ;
      leftConnectors.Sort( ( a, b ) => TTEUtil.CompareAngle( inConnectorOrigin, inConnectorNormal, a, b ) ) ;
      return ( rightConnectors, leftConnectors ) ;
    }

    private SegmentSetting? CreateSegmentSetting( Document doc, Connector inConnector )
    {
      var classificationInfo = MEPSystemClassificationInfo.From( inConnector ) ;
      if ( classificationInfo == null ) return null ;
      var systemType = doc.GetAllElements<MEPSystemType>().Where( classificationInfo.IsCompatibleTo ).FirstOrDefault() ;
      var curveType = TTEUtil.GetRoundDuctTypeWhosePreferred( doc ) ;
      if ( curveType == null ) return null ;
      var nameBase = TTEUtil.GetNameBase( systemType, curveType ) ;
      var currentMaxRouteIndex = TTEUtil.GetRouteNameIndex( RouteCache.Get( DocumentKey.Get( doc ) ), nameBase ) ;
      var uniqueNameCreatorForRoute = new UniqueNameCreatorForRoute( nameBase, currentMaxRouteIndex ) ;
      var fasuLevel = doc.GuessLevel( inConnector.Origin ) ;
      var fixedHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, inConnector.Origin.Z - fasuLevel.Elevation ) ;
      return new SegmentSetting( classificationInfo, fixedHeight, systemType, curveType, uniqueNameCreatorForRoute ) ;
    }

    private IEnumerable<(string routeName, RouteSegment)> CreateRouteSegments( IReadOnlyList<Connector> fasuConnectors, IReadOnlyList<Connector> anemoConnectors, SegmentSetting segmentSetting, bool isRemain = false )
    {
      List<(string routeName, RouteSegment)> segmentList = new() ;
      var segmentCount = isRemain ? anemoConnectors.Count : Math.Min( fasuConnectors.Count, anemoConnectors.Count ) ;
      for ( var index = 0 ; index < segmentCount ; index++ ) {
        var fromConnectorIndex = isRemain ? fasuConnectors.Count - index - 1 : index ;
        var fromPoint = new ConnectorEndPoint( fasuConnectors[ fromConnectorIndex ], null ) ;
        var toPoint = new ConnectorEndPoint( anemoConnectors[ index ], null ) ;
        var routeName = segmentSetting.uniqueNameCreatorForRoute.CreateName() ;
        var routeSegment = new RouteSegment( segmentSetting.ClassificationInfo, segmentSetting.SystemType, segmentSetting.CurveType, fromPoint, toPoint, null, false, segmentSetting.FixedHeight, segmentSetting.FixedHeight, AvoidType.Whichever, null ) ;
        segmentList.Add( ( routeName, routeSegment ) ) ;
      }

      return segmentList ;
    }

    // FASU-システムアネモルーティング改良 片側のコネクタ数よりもシステムアネモが多いケース
    private IEnumerable<(string routeName, RouteSegment)> CreateRemainRouteSegments( IReadOnlyCollection<Connector> rightFasuConnectors, IReadOnlyCollection<Connector> rightAnemoConnectors, IReadOnlyCollection<Connector> leftFasuConnectors, IReadOnlyCollection<Connector> leftAnemoConnectors, SegmentSetting segmentSetting )
    {
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

      return CreateRouteSegments( remainFasuConnectors, remainAnemoConnectors, segmentSetting, true ) ;
    }

    private void AddConnectorsToMechanicalSystem( IEnumerable<Connector> anemoConnectors, MEPSystem fasuMechanicalSystem )
    {
      var connectorSets = new ConnectorSet() ;
      foreach ( var anemoConnector in anemoConnectors ) {
        if ( anemoConnector == null || anemoConnector.MEPSystem is MechanicalSystem ) continue ;
        connectorSets.Insert( anemoConnector ) ;
      }

      // Todo FASUのIn以外のコネクタを新しいダクトシステムに追加。今は例外が発生します。原因としてFASUはMechanical EquipmentやAir Terminalsじゃないものからです。
      fasuMechanicalSystem.Add( connectorSets ) ;
    }

    private class UniqueNameCreatorForRoute
    {
      private readonly string _baseName ;
      private int _routeIndex ;

      public UniqueNameCreatorForRoute( string baseName, int startRouteIndex )
      {
        _routeIndex = startRouteIndex ;
        _baseName = baseName ;
      }

      public string CreateName()
      {
        return _baseName + "_" + _routeIndex++ ;
      }
    }
  }
}