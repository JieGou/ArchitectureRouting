using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  /// <summary>
  /// 風量->径の変換に高砂用のテーブルを使っているため、他で使わないこと
  /// 制約 : rootがFrom側. PassPointが入っていないこと.
  /// </summary>
  public static class DuctSizeAdjusterForTTE
  {
    public static IEnumerable<(string routeName, RouteSegment)> AdjustDuctSize( Document document, Route route, double passPointOffset )
    {
      // TODO tolerance調整
      const double tolerance = 1000 ;
      var segments = CreateSegments( document, route, passPointOffset.MillimetersToRevitUnits() ) ;
      segments.MergeSegmentsIfSmall( tolerance.MillimetersToRevitUnits() ) ;
      segments.CalcAirFlowAndSetDiameter( document ) ;
      return segments.CreateRouteSegments( document, 0 ) ;
    }

    private interface ITermPoint
    {
      IEndPoint GetOrCreateEndPoint( Document document ) ;
      Vector3d GetPosition() ;
    }

    private class BranchTerm : ITermPoint
    {
      private readonly IRouteBranchEndPoint _endPoint ;
      private RouteEndPoint? _newEndPoint ;

      private int _subRouteIndex = 0 ;

      public BranchTerm( IRouteBranchEndPoint endPoint )
      {
        _endPoint = endPoint ;
      }

      public IEndPoint GetOrCreateEndPoint( Document document )
      {
        return _newEndPoint ??= new RouteEndPoint( document, _endPoint.RouteName, _subRouteIndex ) ;
      }

      public Vector3d GetPosition()
      {
        return _endPoint.RoutingStartPosition.To3dPoint() ;
      }

      public void SetSubRouteIndex( int index )
      {
        _subRouteIndex = index ;
      }
    }

    private class FixedTerm : ITermPoint
    {
      private readonly IEndPoint _endPoint ;

      public FixedTerm( IEndPoint endPoint )
      {
        _endPoint = endPoint ;
      }

      public IEndPoint GetOrCreateEndPoint( Document document )
      {
        return _endPoint ;
      }

      public Vector3d GetPosition()
      {
        return _endPoint.RoutingStartPosition.To3dPoint() ;
      }
    }

    private class PassPointTerm : ITermPoint
    {
      private PassPointEndPoint? _endPoint = null ;
      private readonly string _routeName ;
      private readonly XYZ _position ;
      private readonly XYZ _direction ;
      private readonly ElementId _levelId ;
      private double? _radius ;

      public PassPointTerm( string routeName, XYZ position, XYZ direction, ElementId levelId )
      {
        _routeName = routeName ;
        _position = position ;
        _direction = direction ;
        _levelId = levelId ;
      }

      public IEndPoint GetOrCreateEndPoint( Document document )
      {
        if ( _endPoint == null ) {
          var passPointInstance = document.AddPassPoint( _routeName, _position, _direction, _radius, _levelId ) ;
          _endPoint = new PassPointEndPoint( passPointInstance ) ;
        }

        return _endPoint ;
      }

      public Vector3d GetPosition()
      {
        return _position.To3dPoint() ;
      }

      public void UpdateRadius( double radius )
      {
        _radius = radius ;
      }
    }

    // SubRoute+その子供に相当する区間
    private class Segment
    {
      private readonly string _routeName ;
      private readonly ITermPoint _fromPoint ;
      private ITermPoint _toPoint ;

      private readonly RouteSegment _orgSegment ;
      private readonly List<Segments> _childSegments ;

      private double? _diameter ;

      public Segment( IReadOnlyDictionary<string, BranchPointInfo> branchNameToBranchPointInfo,
        string routeName, ITermPoint fromPoint, ITermPoint toPoint, RouteSegment orgSegment, Route childRoute, double passPointOffset )
      {
        _routeName = routeName ;
        _fromPoint = fromPoint ;
        _toPoint = toPoint ;

        _orgSegment = orgSegment ;
        _childSegments = new List<Segments>() { new Segments( branchNameToBranchPointInfo, childRoute, passPointOffset ) } ;

        _diameter = orgSegment.PreferredNominalDiameter ;
      }

      public Segment( string routeName, ITermPoint fromPoint, ITermPoint toPoint, RouteSegment orgSegment )
      {
        _routeName = routeName ;
        _fromPoint = fromPoint ;
        _toPoint = toPoint ;
        _orgSegment = orgSegment ;
        _childSegments = new List<Segments>() ;
      }

      public bool IsSmallSegment( double distancePerBranch )
      {
        return Vector3d.Distance( _fromPoint.GetPosition(), _toPoint.GetPosition() ) < distancePerBranch * _childSegments.Count ;
      }

      public void Merge( Segment toSideSegment )
      {
        _toPoint = toSideSegment._toPoint ;
        _childSegments.AddRange( toSideSegment._childSegments ) ;
      }

      public void MergeChildSegmentsIfSmall( double distancePerBranch )
      {
        foreach ( var childSegment in _childSegments ) {
          childSegment.MergeSegmentsIfSmall( distancePerBranch ) ;
        }
      }

      public double CalcAirFlowAndSetDiameter( Document document, double nextSegmentAirFlow )
      {
        var totalAirFlow = nextSegmentAirFlow ;

        var childSegmentsTotalAirFlow = 0.0 ;
        foreach ( var childSegments in _childSegments ) {
          childSegmentsTotalAirFlow += childSegments.CalcAirFlowAndSetDiameter( document ) ;
        }

        totalAirFlow += childSegmentsTotalAirFlow ;

        if ( _toPoint is FixedTerm ft ) {
          var airflowOfSpace = TTEUtil.GetAirFlowOfSpace( document, ft.GetPosition() ) ;
          if ( airflowOfSpace.HasValue ) totalAirFlow += airflowOfSpace.Value ;
        }

        _diameter = TTEUtil.ConvertAirflowToDiameterForTTE( totalAirFlow ).MillimetersToRevitUnits() ;

        if ( _toPoint is PassPointTerm ppt ) {
          if ( _diameter.HasValue ) ppt.UpdateRadius( _diameter.Value * 0.5 ) ;
        }

        return totalAirFlow ;
      }

      public void SetParentSubRouteIndex( int parentSubRouteIndex )
      {
        if ( _fromPoint is BranchTerm branchTerm ) branchTerm.SetSubRouteIndex( parentSubRouteIndex ) ;
      }

      public IEnumerable<(string routeName, RouteSegment)> CreateRouteSegments( Document document, int subRouteIndex )
      {
        var fromEndPoint = _fromPoint.GetOrCreateEndPoint( document ) ;
        var toEndPoint = _toPoint.GetOrCreateEndPoint( document ) ;
        yield return ( _routeName, new RouteSegment(
          _orgSegment.SystemClassificationInfo,
          _orgSegment.SystemType,
          _orgSegment.CurveType,
          fromEndPoint, toEndPoint,
          _diameter,
          _orgSegment.IsRoutingOnPipeSpace,
          _orgSegment.FromFixedHeight,
          _orgSegment.ToFixedHeight,
          _orgSegment.AvoidType,
          _orgSegment.ShaftElementId ) ) ;

        foreach ( var childSegment in _childSegments ) {
          foreach ( var routeSegment in childSegment.CreateRouteSegments( document, subRouteIndex ) ) {
            yield return routeSegment ;
          }
        }
      }
    }

    private class Segments
    {
      private List<Segment> _segmentList ;

      public Segments( IReadOnlyDictionary<string, BranchPointInfo> branchNameToBranchPointInfo, Route route, double passPointOffset )
      {
        var routeSegment = route.RouteSegments.First() ;

        var childBranches = route.GetChildBranches().ToList() ;

        var fromEndPoint = route.RouteSegments.First().FromEndPoint ;
        var startPosition = fromEndPoint is RouteEndPoint
          ? branchNameToBranchPointInfo[ route.RouteName ].BranchPosition
          : fromEndPoint.RoutingStartPosition.To3dPoint() ;
        var sortedRoutes = SortRoutesOrderByDistanceFromStartPosition( childBranches, branchNameToBranchPointInfo, startPosition ) ;

        var passPointTerms = sortedRoutes.Select( r =>
          CreatePassPointTerm( startPosition, branchNameToBranchPointInfo[ r.RouteName ], passPointOffset ) ).ToArray() ;
        var terms = new List<ITermPoint>() ;
        terms.Add( CreateTermPointFromEndPoint( routeSegment.FromEndPoint ) ) ;
        terms.AddRange( passPointTerms ) ;
        terms.Add( CreateTermPointFromEndPoint( routeSegment.ToEndPoint ) ) ;

        _segmentList = new List<Segment>() ;
        for ( var i = 0 ; i < sortedRoutes.Count ; ++i ) {
          _segmentList.Add( new Segment( branchNameToBranchPointInfo, route.RouteName, terms[ i ], terms[ i + 1 ], routeSegment, sortedRoutes[ i ], passPointOffset ) ) ;
        }

        _segmentList.Add( new Segment( route.RouteName, terms[ sortedRoutes.Count ], terms[ sortedRoutes.Count + 1 ], routeSegment ) ) ;
      }

      public IEnumerable<(string routeName, RouteSegment)> CreateRouteSegments( Document document, int parentSubRouteIndex )
      {
        _segmentList.FirstOrDefault()?.SetParentSubRouteIndex( parentSubRouteIndex ) ;
        for ( var subRouteIndex = 0 ; subRouteIndex < _segmentList.Count ; ++subRouteIndex ) {
          foreach ( var routeSegment in _segmentList[ subRouteIndex ].CreateRouteSegments( document, subRouteIndex ) ) {
            yield return routeSegment ;
          }
        }
      }

      public void MergeSegmentsIfSmall( double distancePerBranch )
      {
        if ( ! _segmentList.Any() ) return ;

        var segmentList = new List<Segment> { _segmentList.First() } ;

        foreach ( var segment in _segmentList.Skip( 1 ) ) {
          if ( segment.IsSmallSegment( distancePerBranch ) ) {
            segmentList.Last().Merge( segment ) ;
            continue ;
          }

          segmentList.Add( segment ) ;
        }

        _segmentList = segmentList ;
        foreach ( var segment in _segmentList ) {
          segment.MergeChildSegmentsIfSmall( distancePerBranch ) ;
        }
      }

      public double CalcAirFlowAndSetDiameter( Document document )
      {
        var airFlow = 0.0 ;

        var reversedSegmentList = _segmentList.ToList() ;
        reversedSegmentList.Reverse() ;
        foreach ( var segment in reversedSegmentList ) {
          airFlow += segment.CalcAirFlowAndSetDiameter( document, airFlow ) ;
        }

        return airFlow ;
      }

      private static IReadOnlyList<Route> SortRoutesOrderByDistanceFromStartPosition( IReadOnlyCollection<Route> routes,
        IReadOnlyDictionary<string, BranchPointInfo> branchNameToBranchPointInfo, Vector3d startPosition )
      {
        int Sorter( Route left, Route right )
        {
          return ( branchNameToBranchPointInfo[ left.RouteName ].BranchPosition - startPosition ).sqrMagnitude.CompareTo(
            ( branchNameToBranchPointInfo[ right.RouteName ].BranchPosition - startPosition ).sqrMagnitude ) ;
        }

        var result = routes.ToList() ;
        result.Sort( Sorter ) ;
        return result ;
      }

      private static PassPointTerm CreatePassPointTerm( Vector3d startPosition, BranchPointInfo info, double offset )
      {
        // TODO コネクタのIn,Outから対象を絞る
        var behindTeeConnector = info.Tee.GetConnectors().Where( conn => conn.Id == 1 || conn.Id == 2 ).MaxBy( conn =>
          Vector2d.Distance( conn.Origin.To3dPoint().To2d(), startPosition.To2d() ) ) ;
        if ( behindTeeConnector == null ) return null! ;
        var passPointDir = behindTeeConnector.CoordinateSystem.BasisZ ;
        var routeName = info.ChildRouteName ;
        var passPointPosition = behindTeeConnector.Origin + passPointDir * offset.MillimetersToRevitUnits() ;
        return new PassPointTerm( routeName, passPointPosition, passPointDir, info.Tee.GetLevelId() ) ;
      }

      private static ITermPoint CreateTermPointFromEndPoint( IEndPoint endPoint )
      {
        if ( endPoint is RouteEndPoint rep ) return new BranchTerm( rep ) ;
        return new FixedTerm( endPoint ) ;
      }
    }

    private static Segments CreateSegments( Document document, Route route, double passPointOffset )
    {
      var routeNameToBranchPointInfo = new Dictionary<string, BranchPointInfo>() ;
      foreach ( var info in CollectBranchPointInfos( document, new Route[] { route } ) ) {
        routeNameToBranchPointInfo.Add( info.ChildRouteName, info ) ;
      }

      return new Segments( routeNameToBranchPointInfo, route, passPointOffset ) ;
    }

    private class BranchPointInfo
    {
      public BranchPointInfo( string childRouteName, Vector3d branchPosition, FamilyInstance tee )
      {
        ChildRouteName = childRouteName ;
        BranchPosition = branchPosition ;
        Tee = tee ;
      }

      public string ChildRouteName { get ; }
      public Vector3d BranchPosition { get ; }
      public FamilyInstance Tee { get ; }
    }

    private static IEnumerable<BranchPointInfo> CollectBranchPointInfos( Document document, IEnumerable<Route> routes )
    {
      // TODO FamilyNameを使わない. Tee, Tapで探して、Routeに組み込まれているものを対象にする
      var tees = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_DuctFitting ).Where( tee => tee.Symbol.FamilyName == "022_丸型 T 型" ) ;
      foreach ( var tee in tees ) {
        var branchRouteName = tee.GetBranchRouteNames().First() ;
        var branchLocation = tee.Location as LocationPoint ;

        yield return new BranchPointInfo( branchRouteName, branchLocation!.Point.To3dPoint(), tee ) ;
      }
    }
  }
}