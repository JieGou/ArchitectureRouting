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
    private Document _document = null! ;
    private Segments _listOfRightSegments = null! ;
    private Segments _listOfLeftSegments = null! ;

    private static MEPSystemClassificationInfo _classificationInfo = null! ;
    private static FixedHeight? _fromFixedHeight ;
    private static MEPSystemType? _systemType ;
    private static MEPCurveType? _curveType ;
    private static string _routeName = string.Empty ;

    public void Setup( Document document, Element elmFasu )
    {
      _document = document ;
      var fasu = new Fasu( document, elmFasu ) ;
      if ( fasu.InConnector == null ) return ;

      // Segment setting
      if ( MEPSystemClassificationInfo.From( fasu.InConnector ) is { } classificationInfo ) _classificationInfo = classificationInfo ;
      var fasuLevel = document.GuessLevel( fasu.InConnector.Origin ) ;
      _fromFixedHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, fasu.InConnector.Origin.Z - fasuLevel.Elevation ) ;
      _systemType = document.GetAllElements<MEPSystemType>().Where( _classificationInfo.IsCompatibleTo ).FirstOrDefault() ;
      _curveType = GetRoundDuctTypeWhosePreferred( document ) ;
      if ( _curveType == null ) return ;
      var nameBase = AutoRoutingVavCommand.GetNameBase( _systemType, _curveType ) ;
      var nextIndex = AutoRoutingVavCommand.GetRouteNameIndex( RouteCache.Get( document ), nameBase ) ;
      _routeName = nameBase + "_" + nextIndex ;

      // INコネクタのBasisZの右側
      var rightSide = new Side( document, fasu, true ) ;

      // INコネクタのBasisZの左側
      var leftSide = new Side( document, fasu, false ) ;

      _listOfRightSegments = CreateSegments( rightSide ) ;
      _listOfLeftSegments = CreateSegments( leftSide ) ;
    }

    public IEnumerable<(string routeName, RouteSegment)> Execute()
    {
      foreach ( var routeSegment in _listOfRightSegments.CreateRouteSegments( _document ) ) {
        yield return routeSegment ;
      }

      foreach ( var routeSegment in _listOfLeftSegments.CreateRouteSegments( _document ) ) {
        yield return routeSegment ;
      }
    }

    private static Segments CreateSegments( Side side )
    {
      return new Segments( side ) ;
    }

    private static MEPCurveType? GetRoundDuctTypeWhosePreferred( Document document )
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( type => type is DuctType && type.Shape == ConnectorProfileType.Round ) ;
    }

    private class Fasu
    {
      public Vector2d Origin { get ; }
      public Vector2d BasisZ { get ; }
      public Vector2d Normal { get ; }
      public Connector? InConnector { get ; }
      public List<Connector> RightFasuConnectors { get ; }
      public List<Connector> LeftFasuConnectors { get ; }
      public Element? SpaceContainFasu { get ; }

      public Fasu( Document doc, Element fasu )
      {
        RightFasuConnectors = new List<Connector>() ;
        LeftFasuConnectors = new List<Connector>() ;
        var notInConnectors = fasu.GetConnectors().Where( connector => connector.Direction != FlowDirectionType.In ).ToList() ;
        InConnector = fasu.GetConnectors().FirstOrDefault( connector => connector.Direction == FlowDirectionType.In ) ;
        if ( InConnector != null && notInConnectors.Any() ) {
          Origin = InConnector.Origin.To3dPoint().To2d() ;
          BasisZ = InConnector.CoordinateSystem.BasisZ.To3dPoint().To2d() ;
          Normal = new Vector2d( -BasisZ.y, BasisZ.x ) ;
          foreach ( var notInConnector in notInConnectors ) {
            // Vector from in connector to out connector
            var inOutVector = notInConnector.Origin.To3dPoint().To2d() - Origin ;

            // 二つ側にINコネクタの以外を分別
            if ( Vector2d.Dot( Normal, inOutVector ) < 0 ) {
              RightFasuConnectors.Add( notInConnector ) ;
            }
            else {
              LeftFasuConnectors.Add( notInConnector ) ;
            }
          }
        }

        // Get all space
        var spaces = AutoRoutingVavCommand.GetAllSpaces( doc ) ;
        SpaceContainFasu = GetSpace( doc, spaces ) ;
      }

      private Element? GetSpace( Document doc, IList<Element> spaces )
      {
        if ( InConnector == null ) return null ;
        foreach ( var space in spaces ) {
          var spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
          if ( AutoRoutingVavCommand.IsInSpace( spaceBox, InConnector.Origin ) ) return space ;
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
        // Todo sort by angle
        SortedFasuConnectors = isRight ? fasu.RightFasuConnectors : fasu.LeftFasuConnectors ;
        SortedFasuConnectors.Sort( ( a, b ) => CompareAngle( fasu, a, b ) ) ;

        // Get anemostats
        var anemostats = GetAnemostat( doc, fasu.SpaceContainFasu ) ;
        foreach ( var anemostat in anemostats ) {
          var anemoConnector = anemostat.GetConnectors().FirstOrDefault() ;
          var anemoConnectorVector = anemoConnector.Origin.To3dPoint().To2d() - fasu.Origin ;
          if ( Vector2d.Dot( fasu.Normal, anemoConnectorVector ) < 0 && isRight ) {
            SortedAnemostatConnectors.Add( anemoConnector ) ;
          }

          if ( Vector2d.Dot( fasu.Normal, anemoConnectorVector ) >= 0 && ! isRight ) {
            SortedAnemostatConnectors.Add( anemoConnector ) ;
          }
        }

        SortedAnemostatConnectors.Sort( ( a, b ) => CompareAngle( fasu, a, b ) ) ;
      }

      private static int CompareAngle( Fasu fasu, IConnector a, IConnector b )
      {
        var aVector = a.Origin.To3dPoint().To2d() - fasu.Origin ;
        var bVector = b.Origin.To3dPoint().To2d() - fasu.Origin ;
        return Vector2d.Dot( fasu.BasisZ, aVector ).CompareTo( Vector2d.Dot( fasu.BasisZ, bVector ) ) ;
      }

      private IEnumerable<FamilyInstance> GetAnemostat( Document doc, Element? spaceContainFasu )
      {
        if ( spaceContainFasu == null ) yield break ;

        // Todo get anemostat don't use familyName
        var anemostats = doc.GetAllElements<FamilyInstance>().Where( anemostat => anemostat.Symbol.FamilyName == "システムアネモ" ) ;
        var spaceBox = spaceContainFasu.get_BoundingBox( doc.ActiveView ) ;
        foreach ( var anemostat in anemostats ) {
          if ( AutoRoutingVavCommand.IsInSpace( spaceBox, anemostat.GetConnectors().First().Origin ) ) yield return anemostat ;
        }
      }
    }

    private class Segment
    {
      private readonly ConnectorEndPoint _fromPoint ;
      private readonly ConnectorEndPoint _toPoint ;

      public Segment( ConnectorEndPoint fromPoint, ConnectorEndPoint toPoint )
      {
        _fromPoint = fromPoint ;
        _toPoint = toPoint ;
      }

      public IEnumerable<(string routeName, RouteSegment)> CreateRouteSegments( Document document )
      {
        yield return ( _routeName, new RouteSegment( _classificationInfo, _systemType, _curveType, _fromPoint, _toPoint, null, false, _fromFixedHeight, _fromFixedHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ) ;
      }
    }

    private class Segments
    {
      private readonly List<Segment> _segmentList = new() ;

      public Segments( Side side )
      {
        var segmentCount = Math.Min( side.SortedFasuConnectors.Count, side.SortedAnemostatConnectors.Count ) ;
        for ( var index = 0 ; index < segmentCount ; index++ ) {
          var fromPoint = new ConnectorEndPoint( side.SortedFasuConnectors[ index ], null ) ;
          var toPoint = new ConnectorEndPoint( side.SortedAnemostatConnectors[ index ], null ) ;
          _segmentList.Add( new Segment( fromPoint, toPoint ) ) ;
        }
      }

      public IEnumerable<(string routeName, RouteSegment)> CreateRouteSegments( Document document )
      {
        foreach ( var segment in _segmentList ) {
          foreach ( var (routeName, routeSegment) in segment.CreateRouteSegments( document ) ) {
            yield return ( routeName, routeSegment ) ;
          }
        }
      }
    }
  }
}