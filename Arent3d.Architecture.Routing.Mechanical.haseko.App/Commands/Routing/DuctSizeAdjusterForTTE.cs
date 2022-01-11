using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.FittingSizeCalculators.MEPCurveGenerators ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Routing
{
  /// <summary>
  /// 風量->径の変換に高砂用のテーブルを使っているため、他で使わないこと
  /// 制約 : rootがFrom側. PassPointが入っていないこと.
  /// </summary>
  public class DuctSizeAdjusterForTTE
  {
    private Document _document = null! ;
    private Segments _topSegments = null! ;

    public void Setup( Document document, Route route, double passPointOffsetMilliMeters )
    {
      _document = document ;
      var sizeCalculator = new FittingSizeCalculator( document, route ) ;

      _topSegments = CreateSegments( document, route ) ;

      var mergeFinished = false ;
      while ( ! mergeFinished ) {
        _topSegments.CalcAirFlowAndSetDiameter( document ) ;
        _topSegments.UpdatePassPointPosition( sizeCalculator, passPointOffsetMilliMeters ) ;
        mergeFinished = ! _topSegments.MergeSegmentsIfSmall( sizeCalculator ) ;
      }
      
      _topSegments.SetFromConnectorDiameterAsFirstSegmentDiameterForcibly();
    }

    public IEnumerable<(string routeName, RouteSegment)> Execute()
    {
      if ( _topSegments != null ) return _topSegments.CreateRouteSegments( _document, 0 ) ;
      return Enumerable.Empty<(string routeName, RouteSegment)>() ;
    }

    private class FittingSizeCalculator
    {
      private Document _document ;
      private IMEPCurveGenerator _curveGenerator ;
      private FittingSizeCalculators.IFittingSizeCalculator _calculator ;

      public FittingSizeCalculator( Document document, Route route )
      {
        _document = document ;
        _curveGenerator = new DuctCurveGenerator( document, route.GetMEPSystemType(), route.GetDefaultCurveType() ) ;
        _calculator = FittingSizeCalculators.DefaultFittingSizeCalculator.Instance ;
      }

      public double GetTeeHeaderLength( double headerDiameter, double branchDiameter )
      {
        return _calculator.CalculateTeeLengths( _document, _curveGenerator, headerDiameter, branchDiameter ).Header ;
      }

      public double GetTeeBranchLength( double headerDiameter, double branchDiameter )
      {
        return _calculator.CalculateTeeLengths( _document, _curveGenerator, headerDiameter, branchDiameter ).Branch ;
      }

      public double GetReducerLength( double diameter1, double diameter2 )
      {
        return _calculator.CalculateReducerLength( _document, _curveGenerator, diameter1, diameter2 ) ;
      }
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
      
      public double? GetDiameter()
      {
        return _endPoint.GetDiameter() ;
      }
    }

    private class PassPointTerm : ITermPoint
    {
      private PassPointEndPoint? _endPoint = null ;
      private readonly string _routeName ;
      private readonly XYZ _teePosition ;
      private readonly XYZ _direction ;
      private XYZ _position ;
      private readonly ElementId _levelId ;
      private double? _radius ;

      public PassPointTerm( string routeName, XYZ teePosition, XYZ direction, ElementId levelId )
      {
        _routeName = routeName ;
        _teePosition = teePosition ;
        _position = teePosition ;
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

      public double? GetRadius()
      {
        return _radius ;
      }

      public void UpdateRadius( double radius )
      {
        _radius = radius ;
      }

      public void UpdateDistanceFromTee( double distanceMillimeters )
      {
        var d = distanceMillimeters.MillimetersToRevitUnits() ;
        _position = _teePosition.Add( d * _direction ) ;
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
        string routeName, ITermPoint fromPoint, ITermPoint toPoint, RouteSegment orgSegment, Route childRoute, bool branchSideOfParentTee )
      {
        _routeName = routeName ;
        _fromPoint = fromPoint ;
        _toPoint = toPoint ;

        _orgSegment = orgSegment ;
        _childSegments = new List<Segments>() { new Segments( branchNameToBranchPointInfo, childRoute, branchSideOfParentTee ) } ;

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

      static double GetTeeLengthRequiredForParentSegment( FittingSizeCalculator sizeCalculator, double parentSegmentDiameter, Segments childSegments )
      {
        var childRouteDiameter = childSegments.GetRootDiameter() ;
        if ( childRouteDiameter == null ) {
          throw new InvalidOperationException() ;
        }

        var isPassPointBranchSide = ! childSegments.IsBranchSideOfParentTee() ;
        return isPassPointBranchSide
          ? sizeCalculator.GetTeeBranchLength( parentSegmentDiameter, childRouteDiameter.Value )
          : sizeCalculator.GetTeeHeaderLength( parentSegmentDiameter, childRouteDiameter.Value ) ;
      }

      // TODO From側のElementの径が変更されるとルーティングの情報がおかしくなる不具合の暫定対応
      public void SetFromConnectorDiameterAsSegmentDiameterForcibly()
      {
        if ( _fromPoint is FixedTerm ft && ft.GetDiameter() is { } diameter ) {
          _diameter = diameter ;
        } 
      }
      
      public bool IsSmallSegment( FittingSizeCalculator sizeCalculator )
      {
        // TODO 本来はレデューサとTEEサイズから計算. 現状ではおそらくルーティング側の問題でTEEがうまく入らないケースがあるので広めに確保しておく.

        // if ( ! _diameter.HasValue ) return false ; // こないはず
        //
        // var requiredLength = 0.0 ;
        //
        // // PassPointのあとにはいるReducer分
        // if ( _fromPoint is PassPointTerm ppt ) {
        //   var radius = ppt.GetRadius() ;
        //   if ( radius.HasValue ) {
        //     requiredLength += sizeCalculator.GetReducerLength( 2 * radius.Value, _diameter.Value ) ;
        //   }
        // }
        //
        // foreach ( var segments in _childSegments ) {
        //   requiredLength += GetTeeLengthRequiredForParentSegment( sizeCalculator, _diameter.Value, segments ) ;
        // }
        //
        // requiredLength += 10.0.MillimetersToRevitUnits() ;

        // TODO 仮の値
        var requiredLength = ( 0.5 ).MetersToRevitUnits() * ( _childSegments.Count + 1 ) ;
        return Vector3d.Distance( _fromPoint.GetPosition(), _toPoint.GetPosition() ) < requiredLength.RevitUnitsToMeters() ;
      }

      public double? GetDiameter()
      {
        return _diameter ;
      }

      public void UpdatePassPointPosition( FittingSizeCalculator sizeCalculator, double passPointOffsetMilliMeters )
      {
        if ( ! _childSegments.Any() ) return ;
        if ( ! ( _toPoint is PassPointTerm ppt ) ) return ;

        var requiredLength = GetTeeLengthRequiredForParentSegment( sizeCalculator, _diameter!.Value, _childSegments.Last() ) ;
        ppt.UpdateDistanceFromTee( requiredLength.RevitUnitsToMillimeters() + passPointOffsetMilliMeters ) ;

        foreach ( var childSegments in _childSegments ) {
          childSegments.UpdatePassPointPosition( sizeCalculator, passPointOffsetMilliMeters ) ;
        }
      }

      public void Merge( Segment toSideSegment )
      {
        _toPoint = toSideSegment._toPoint ;
        _childSegments.AddRange( toSideSegment._childSegments ) ;
      }

      public bool MergeChildSegmentsIfSmall( FittingSizeCalculator sizeCalculator )
      {
        var merged = false ;
        foreach ( var childSegment in _childSegments ) {
          merged |= childSegment.MergeSegmentsIfSmall( sizeCalculator ) ;
        }

        return merged ;
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
      private readonly bool _branchSideOfParentTee ;

      // TODO From側のElementの径が変更されるとルーティングの情報がおかしくなる不具合の暫定対応B
      public void SetFromConnectorDiameterAsFirstSegmentDiameterForcibly()
      {
        _segmentList.FirstOrDefault()?.SetFromConnectorDiameterAsSegmentDiameterForcibly() ;
      }
      
      public Segments( IReadOnlyDictionary<string, BranchPointInfo> branchNameToBranchPointInfo, Route route, bool branchSideOfParentTee )
      {
        _branchSideOfParentTee = branchSideOfParentTee ;

        var routeSegment = route.RouteSegments.First() ;

        var childBranches = route.GetChildBranches().ToList() ;

        var fromEndPoint = route.RouteSegments.First().FromEndPoint ;
        var startPosition = fromEndPoint is RouteEndPoint
          ? branchNameToBranchPointInfo[ route.RouteName ].BranchPosition
          : fromEndPoint.RoutingStartPosition.To3dPoint() ;
        var sortedRoutes = SortRoutesOrderByDistanceFromStartPosition( childBranches, branchNameToBranchPointInfo, startPosition ) ;

        var passPointTerms = sortedRoutes.Select( r => CreatePassPointTerm( branchNameToBranchPointInfo[ r.RouteName ] ) ).ToArray() ;
        var terms = new List<ITermPoint>() ;
        terms.Add( CreateTermPointFromEndPoint( routeSegment.FromEndPoint ) ) ;
        terms.AddRange( passPointTerms ) ;
        terms.Add( CreateTermPointFromEndPoint( routeSegment.ToEndPoint ) ) ;

        _segmentList = new List<Segment>() ;
        for ( var i = 0 ; i < sortedRoutes.Count ; ++i ) {
          var connectedToBranchSide = IsChildRouteConnectedToBranchSideConnector( branchNameToBranchPointInfo[ sortedRoutes[ i ].RouteName ].Tee ) ;
          _segmentList.Add( new Segment( branchNameToBranchPointInfo, route.RouteName, terms[ i ], terms[ i + 1 ], routeSegment, sortedRoutes[ i ], connectedToBranchSide ) ) ;
        }

        _segmentList.Add( new Segment( route.RouteName, terms[ sortedRoutes.Count ], terms[ sortedRoutes.Count + 1 ], routeSegment ) ) ;
      }

      public double? GetRootDiameter()
      {
        return _segmentList.First().GetDiameter() ;
      }

      public bool IsBranchSideOfParentTee()
      {
        return _branchSideOfParentTee ;
      }

      public void UpdatePassPointPosition( FittingSizeCalculator sizeCalculator, double passPointOffset )
      {
        foreach ( var segment in _segmentList ) {
          segment.UpdatePassPointPosition( sizeCalculator, passPointOffset ) ;
        }
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

      public bool MergeSegmentsIfSmall( FittingSizeCalculator sizeCalculator )
      {
        if ( ! _segmentList.Any() ) return false ;

        var merged = false ;
        var segmentList = new List<Segment> { _segmentList.First() } ;

        foreach ( var segment in _segmentList.Skip( 1 ) ) {
          if ( segment.IsSmallSegment( sizeCalculator ) ) {
            segmentList.Last().Merge( segment ) ;
            merged = true ;
            continue ;
          }

          segmentList.Add( segment ) ;
        }

        _segmentList = segmentList ;
        foreach ( var segment in _segmentList ) {
          merged |= segment.MergeChildSegmentsIfSmall( sizeCalculator ) ;
        }

        return merged ;
      }

      public double CalcAirFlowAndSetDiameter( Document document )
      {
        var airFlow = 0.0 ;

        var reversedSegmentList = _segmentList.ToList() ;
        reversedSegmentList.Reverse() ;
        foreach ( var segment in reversedSegmentList ) {
          airFlow = segment.CalcAirFlowAndSetDiameter( document, airFlow ) ;
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

      private static PassPointTerm CreatePassPointTerm( BranchPointInfo info )
      {
        var behindTeeConnector = GetHeaderRouteOutDirectionConnectors( info.Tee ) ;
        var passPointDir = behindTeeConnector.CoordinateSystem.BasisZ ;
        var routeName = info.ChildRouteName ;
        return new PassPointTerm( routeName, behindTeeConnector.Origin, passPointDir, info.Tee.GetLevelId() ) ;
      }

      private static bool IsChildRouteConnectedToBranchSideConnector( Element tee )
      {
        return GetHeaderRouteOutDirectionConnectors( tee ).Id == 3 ;
      }

      private static Connector GetHeaderRouteOutDirectionConnectors( Element tee )
      {
        var routeNameOfTee = tee.GetRouteName() ;
        var toSideConnectors = tee.GetConnectors().Where( c => c.IsRoutingConnector( false ) ).ToArray() ;

        foreach ( var connector in toSideConnectors ) {
          if ( connector.GetConnectedConnectors().Any( c => c.Owner.GetRouteName() == routeNameOfTee ) ) return connector ;
        }

        // この時点で失敗しているが、完全に失敗させるよりはそのままつづけたほうがましと判断
        return toSideConnectors.FirstOrDefault() ?? tee.GetConnectors().FirstOrDefault()! ;
      }

      private static ITermPoint CreateTermPointFromEndPoint( IEndPoint endPoint )
      {
        if ( endPoint is RouteEndPoint rep ) return new BranchTerm( rep ) ;
        return new FixedTerm( endPoint ) ;
      }
    }

    private static Segments CreateSegments( Document document, Route route )
    {
      var routeNameToBranchPointInfo = new Dictionary<string, BranchPointInfo>() ;
      foreach ( var info in CollectBranchPointInfos( document, route ) ) {
        routeNameToBranchPointInfo.Add( info.ChildRouteName, info ) ;
      }

      const bool dummyValue = true ;
      return new Segments( routeNameToBranchPointInfo, route, dummyValue ) ;
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

    private static IEnumerable<BranchPointInfo> CollectBranchPointInfos( Document document, Route route )
    {
      var childBranches = route.GetChildBranches().ToList() ;

      // Tee, Tapで探して、Routeに組み込まれているものを対象にする。現状内部処理的にコネクタが3つあることを前提としていますのでコネクタが3つあるものを取得
      var tees = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategory.OST_DuctFitting ).Where( tee => tee.GetConnectors().Count() == 3 ) ;
      foreach ( var tee in tees ) {
        // Ignore tee out of selected routes
        if ( childBranches.All( childRoute => childRoute.RouteName != tee.GetRouteName() ) && route.RouteName != tee.GetRouteName() ) continue ;
        var branchRouteName = tee.GetBranchRouteNames().First() ;
        var branchLocation = tee.Location as LocationPoint ;

        yield return new BranchPointInfo( branchRouteName, branchLocation!.Point.To3dPoint(), tee ) ;
      }
    }
  }
}