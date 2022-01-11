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
    private IList<(string routeName, RouteSegment)>? _listOfRightSegments ;
    private IList<(string routeName, RouteSegment)>? _listOfLeftSegments ;

    private record SegmentSetting( MEPSystemClassificationInfo ClassificationInfo, FixedHeight? FixedHeight, MEPSystemType? SystemType, MEPCurveType? CurveType, string RouteName ) ;

    public void Setup( Document document, Element elmFasu )
    {
      var fasu = new Fasu( document, elmFasu ) ;
      if ( fasu.InConnector == null ) return ;

      // Segment setting
      var classificationInfo = MEPSystemClassificationInfo.From( fasu.InConnector ) ;
      if ( classificationInfo == null ) return ;
      var systemType = document.GetAllElements<MEPSystemType>().Where( classificationInfo.IsCompatibleTo ).FirstOrDefault() ;
      var curveType = GetRoundDuctTypeWhosePreferred( document ) ;
      if ( curveType == null ) return ;
      var nameBase = TTEUtil.GetNameBase( systemType, curveType ) ;
      var nextIndex = TTEUtil.GetRouteNameIndex( RouteCache.Get( document ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;
      var fasuLevel = document.GuessLevel( fasu.InConnector.Origin ) ;
      var fixedHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, fasu.InConnector.Origin.Z - fasuLevel.Elevation ) ;
      var segmentSetting = new SegmentSetting( classificationInfo, fixedHeight, systemType, curveType, routeName ) ;

      // INコネクタのBasisZの右側
      var rightSide = new Side( document, fasu, true ) ;

      // INコネクタのBasisZの左側
      var leftSide = new Side( document, fasu, false ) ;

      _listOfRightSegments = CreateRouteSegments( rightSide, segmentSetting ) ;
      _listOfLeftSegments = CreateRouteSegments( leftSide, segmentSetting ) ;
    }

    public IEnumerable<(string routeName, RouteSegment)> Execute()
    {
      if ( _listOfRightSegments != null ) {
        foreach ( var routeSegment in _listOfRightSegments ) {
          yield return routeSegment ;
        }
      }

      if ( _listOfLeftSegments == null ) yield break ;
      foreach ( var routeSegment in _listOfLeftSegments ) {
        yield return routeSegment ;
      }
    }

    private static IList<(string routeName, RouteSegment)> CreateRouteSegments( Side side, SegmentSetting segmentSetting )
    {
      List<(string routeName, RouteSegment)> segmentList = new() ;
      var segmentCount = Math.Min( side.SortedFasuConnectors.Count, side.SortedAnemostatConnectors.Count ) ;
      for ( var index = 0 ; index < segmentCount ; index++ ) {
        var fromPoint = new ConnectorEndPoint( side.SortedFasuConnectors[ index ], null ) ;
        var toPoint = new ConnectorEndPoint( side.SortedAnemostatConnectors[ index ], null ) ;
        segmentList.Add( ( segmentSetting.RouteName, new RouteSegment( segmentSetting.ClassificationInfo, segmentSetting.SystemType, segmentSetting.CurveType, fromPoint, toPoint, null, false, segmentSetting.FixedHeight, segmentSetting.FixedHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ) ) ;
      }

      return segmentList ;
    }

    private static MEPCurveType? GetRoundDuctTypeWhosePreferred( Document document )
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( type => type is DuctType && type.Shape == ConnectorProfileType.Round ) ;
    }

    private class Fasu
    {
      public Vector2d InConnectorOrigin { get ; }
      public Vector2d InConnectorDirection { get ; }
      public Vector2d InConnectorNormal { get ; }
      public Connector? InConnector { get ; }
      public List<Connector> RightFasuConnectors { get ; }
      public List<Connector> LeftFasuConnectors { get ; }
      public Element? SpaceContainFasu { get ; }

      public Fasu( Document doc, Element fasu )
      {
        RightFasuConnectors = new List<Connector>() ;
        LeftFasuConnectors = new List<Connector>() ;
        var notInConnectors = fasu.GetConnectors().Where( connector => connector.Direction != FlowDirectionType.In && ! connector.IsConnected ).ToList() ;
        InConnector = fasu.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
        if ( InConnector != null && notInConnectors.Any() ) {
          InConnectorOrigin = InConnector.Origin.To3dPoint().To2d() ;
          InConnectorDirection = InConnector.CoordinateSystem.BasisZ.To3dPoint().To2d() ;
          InConnectorNormal = new Vector2d( -InConnectorDirection.y, InConnectorDirection.x ) ;
          foreach ( var notInConnector in notInConnectors ) {
            // Vector from in connector to out connector
            var inOutVector = notInConnector.Origin.To3dPoint().To2d() - InConnectorOrigin ;

            // 二つ側にINコネクタの以外を分別
            if ( Vector2d.Dot( InConnectorNormal, inOutVector ) < 0 ) {
              RightFasuConnectors.Add( notInConnector ) ;
            }
            else {
              LeftFasuConnectors.Add( notInConnector ) ;
            }
          }
        }

        // Get all space
        var spaces = TTEUtil.GetAllSpaces( doc ) ;
        SpaceContainFasu = GetSpace( doc, spaces ) ;
      }

      private Element? GetSpace( Document doc, IList<Element> spaces )
      {
        if ( InConnector == null ) return null ;
        foreach ( var space in spaces ) {
          var spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
          if ( TTEUtil.IsInSpace( spaceBox, InConnector.Origin ) ) return space ;
        }

        return null ;
      }
    }

    private class Side
    {
      public readonly List<Connector> SortedFasuConnectors ;
      public readonly List<Connector> SortedAnemostatConnectors = new() ;

      public Side( Document doc, Fasu fasu, bool isRight )
      {
        SortedFasuConnectors = isRight ? fasu.RightFasuConnectors : fasu.LeftFasuConnectors ;
        SortedFasuConnectors.Sort( ( a, b ) => CompareAngle( fasu, a, b ) ) ;

        // Get anemostats
        var anemostats = GetAnemostat( doc, fasu.SpaceContainFasu ) ;
        foreach ( var anemostat in anemostats ) {
          var anemoConnector = anemostat.GetConnectors().FirstOrDefault( connector => ! connector.IsConnected ) ;
          if ( anemoConnector == null ) continue ;
          var anemoConnectorVector = anemoConnector.Origin.To3dPoint().To2d() - fasu.InConnectorOrigin ;
          if ( Vector2d.Dot( fasu.InConnectorNormal, anemoConnectorVector ) < 0 && isRight ) {
            SortedAnemostatConnectors.Add( anemoConnector ) ;
          }

          if ( Vector2d.Dot( fasu.InConnectorNormal, anemoConnectorVector ) >= 0 && ! isRight ) {
            SortedAnemostatConnectors.Add( anemoConnector ) ;
          }
        }

        SortedAnemostatConnectors.Sort( ( a, b ) => CompareAngle( fasu, a, b ) ) ;
      }

      private static int CompareAngle( Fasu fasu, IConnector a, IConnector b )
      {
        var aVector = a.Origin.To3dPoint().To2d() - fasu.InConnectorOrigin ;
        var bVector = b.Origin.To3dPoint().To2d() - fasu.InConnectorOrigin ;
        var aAngle = TTEUtil.GetAngleBetweenVector( fasu.InConnectorDirection, aVector ) ;
        var bAngle = TTEUtil.GetAngleBetweenVector( fasu.InConnectorDirection, bVector ) ;
        return aAngle.CompareTo( bAngle ) ;
      }

      private IEnumerable<FamilyInstance> GetAnemostat( Document doc, Element? spaceContainFasu )
      {
        if ( spaceContainFasu == null ) yield break ;

        // Todo get anemostat don't use familyName
        var anemostats = doc.GetAllElements<FamilyInstance>().Where( anemostat => anemostat.Symbol.FamilyName == "システムアネモ" ) ;
        var spaceBox = spaceContainFasu.get_BoundingBox( doc.ActiveView ) ;
        foreach ( var anemostat in anemostats ) {
          if ( TTEUtil.IsInSpace( spaceBox, anemostat.GetConnectors().First().Origin ) ) yield return anemostat ;
        }
      }
    }
  }
}