using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class PickCommandUtil
  {
    public static IDisposable SetTempColor( this UIDocument uiDocument, ConnectorPicker.IPickResult pickResult )
    {
      return new TempColorWrapper( uiDocument, pickResult.GetAllRelatedElements() ) ;
    }

    private class TempColorWrapper : IDisposable
    {
      private readonly Document _document ;
      private readonly TempColor _tempColor ;
      public TempColorWrapper( UIDocument uiDocument, IEnumerable<ElementId> elements )
      {
        _document = uiDocument.Document ;
        _tempColor = new TempColor( uiDocument.ActiveView, new Color( 0, 0, 255 ) ) ;
        _document.Transaction( "TransactionName.Commands.Routing.Common.ChangeColor".GetAppStringByKeyOrDefault( null ), t =>
        {
          _tempColor.AddRange( elements ) ;
          return Result.Succeeded ;
        } ) ;
      }

      public void Dispose()
      {
        GC.SuppressFinalize( this ) ;

        _document.Transaction( "TransactionName.Commands.Routing.Common.RevertColor".GetAppStringByKeyOrDefault( null ), t =>
        {
          _tempColor.Dispose() ;
          return Result.Succeeded ;
        } ) ;
      }

      ~TempColorWrapper()
      {
        throw new InvalidOperationException( $"`{nameof( TempColorWrapper )}` is not disposed. Use `using` statement." ) ;
      }
    }

    public static IEndPoint GetEndPoint( ConnectorPicker.IPickResult pickResult, ConnectorPicker.IPickResult anotherResult )
    {
      if ( pickResult.PickedConnector is { } connector ) return new ConnectorEndPoint( connector ) ;

      var element = pickResult.PickedElement ;
      var pos = pickResult.GetOrigin() ;
      var anotherPos = anotherResult.GetOrigin() ;
      var dir = GetPreferredDirection( pos, anotherPos ) ;
      var preferredRadius = ( pickResult.PickedConnector ?? anotherResult.PickedConnector )?.Radius ;

      return new TerminatePointEndPoint( element.Document, ElementId.InvalidElementId, pos, dir, preferredRadius, element.Id ) ;
    }

    private static XYZ GetPreferredDirection( XYZ pos, XYZ anotherPos )
    {
      var dir = anotherPos - pos ;

      double x = Math.Abs( dir.X ), y = Math.Abs( dir.Y ) ;
      if ( x < y ) {
        return ( 0 <= dir.Y ) ? XYZ.BasisY : -XYZ.BasisY ;
      }
      else {
        return ( 0 <= dir.X ) ? XYZ.BasisX : -XYZ.BasisX ;
      }
    }

    public static (ConnectorPicker.IPickResult PickResult, bool AnotherIsFrom) PickResultFromAnother( Route route, IEndPoint endPoint )
    {
      var ((subRoute, anotherEndPoint), isFrom) = GetOtherEndPoint( route, endPoint ) ;
      return (new PseudoPickResult( subRoute, anotherEndPoint, isFrom ), isFrom) ;
    }

    public static IEndPoint CreateRouteEndPoint( ConnectorPicker.IPickResult routePickResult )
    {
      return new RouteEndPoint( routePickResult.SubRoute! ) ;
    }

    public static (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateBranchingRouteEndPoint( ConnectorPicker.IPickResult routePickResult, ConnectorPicker.IPickResult anotherPickResult, bool isFrom )
    {
      var element = routePickResult.GetOriginElement() ;
      var document = element.Document ;
      var pos = routePickResult.GetOrigin() ;

      // Create Pass Point
      var subRoute = GetRepresentativeSubRoute( element ) ?? routePickResult.SubRoute! ;
      var routeName = subRoute.Route.Name ;
      if ( InsertBranchingPassPointElement( document, subRoute, element, pos ) is not { } passPointElement ) throw new InvalidOperationException() ;
      var otherSegments = GetNewSegmentList( subRoute, element, passPointElement ).Select( segment => ( routeName, segment ) ).EnumerateAll() ;

      // Create PassPointBranchEndPoint
      var preferredRadius = ( routePickResult.PickedConnector ?? anotherPickResult.PickedConnector )?.Radius ;
      var endPoint = new PassPointBranchEndPoint( document, passPointElement.Id, preferredRadius, routePickResult.EndPointOverSubRoute! ) ;

      return ( endPoint, otherSegments ) ;
    }

    private static SubRoute? GetRepresentativeSubRoute( Element element )
    {
      if ( ( element.GetRepresentativeSubRoute() ?? element.GetSubRouteInfo() ) is not { } subRouteInfo ) return null ;

      return RouteCache.Get( element.Document ).GetSubRoute( subRouteInfo ) ;
    }

    private static Instance? InsertBranchingPassPointElement( Document document, SubRoute subRoute, Element routingElement, XYZ pos )
    {
      if ( routingElement.GetRoutingConnectors( true ).FirstOrDefault() is not { } fromConnector ) return null ;
      if ( routingElement.GetRoutingConnectors( false ).FirstOrDefault() is not { } toConnector ) return null ;

      var dir = ( toConnector.Origin - fromConnector.Origin ).Normalize() ;
      return document.AddPassPoint( subRoute.Route.RouteName, pos, dir, subRoute.GetDiameter() * 0.5 ) ;
    }

    private const double HalfPI = Math.PI / 2 ;
    private const double OneAndAHalfPI = Math.PI + HalfPI ;
    private static double GetPreferredAngle( Transform transform, XYZ pos, XYZ anotherPos )
    {
      var vec = pos - anotherPos ;
      var x = transform.BasisY.DotProduct( vec ) ;
      var y = transform.BasisZ.DotProduct( vec ) ;
      if ( Math.Abs( y ) < Math.Abs( x ) ) {
        return ( 0 < x ? 0 : Math.PI ) ;
      }
      else {
        return ( 0 < y ? HalfPI : OneAndAHalfPI ) ;
      }
    }

    public static IEnumerable<RouteSegment> GetNewSegmentList( SubRoute subRoute, Element insertingElement, Instance passPointElement )
    {
      var detector = new RouteSegmentDetector( subRoute, insertingElement ) ;
      var passPoint = new PassPointEndPoint( passPointElement ) ;
      foreach ( var segment in subRoute.Route.RouteSegments.EnumerateAll() ) {
        if ( detector.IsPassingThrough( segment ) ) {
          // split segment
          var diameter = segment.GetRealNominalDiameter() ?? segment.PreferredNominalDiameter ;
          var isRoutingOnPipeSpace = segment.IsRoutingOnPipeSpace ;
          var fixeBopHeight = segment.FixedBopHeight ;
          var avoidType = segment.AvoidType ;
          var shaft1 = ( segment.FromEndPoint.GetLevelId( subRoute.Route.Document ) != passPoint.GetLevelId( subRoute.Route.Document ) ) ? segment.ShaftElementId : ElementId.InvalidElementId ;
          var shaft2 = ( passPoint.GetLevelId( subRoute.Route.Document ) != segment.ToEndPoint.GetLevelId( subRoute.Route.Document ) ) ? segment.ShaftElementId : ElementId.InvalidElementId ;
          yield return new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, segment.FromEndPoint, passPoint, diameter, isRoutingOnPipeSpace, fixeBopHeight, avoidType, shaft1 ) ;
          yield return new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, passPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fixeBopHeight, avoidType, shaft2 ) ;
        }
        else {
          yield return segment ;
        }
      }
    }

    private static ((SubRoute SubRoute, IEndPoint EndPoint), bool IsFrom) GetOtherEndPoint( Route route, IEndPoint endPoint )
    {
      var endPointSubRouteMap = new Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)>() ;
      foreach ( var subRoute in route.SubRoutes ) {
        // from-side
        foreach ( var fromEndPoint in subRoute.FromEndPoints ) {
          if ( endPointSubRouteMap.TryGetValue( fromEndPoint, out var tuple ) ) {
            endPointSubRouteMap[ fromEndPoint ] = ( subRoute, tuple.OfTo ) ;
          }
          else {
            endPointSubRouteMap.Add( fromEndPoint, ( subRoute, null ) ) ;
          }
        }

        // to-side
        foreach ( var toEndPoint in subRoute.ToEndPoints ) {
          if ( endPointSubRouteMap.TryGetValue( toEndPoint, out var tuple ) ) {
            endPointSubRouteMap[ toEndPoint ] = ( tuple.OfFrom, subRoute ) ;
          }
          else {
            endPointSubRouteMap.Add( toEndPoint, ( null, subRoute ) ) ;
          }
        }
      }

      // seek other end point
      if ( false == endPointSubRouteMap.TryGetValue( endPoint, out var ofFromTo ) ) throw new InvalidOperationException() ;

      if ( null != ofFromTo.OfFrom ) {
        return (TrailFrom( endPointSubRouteMap, ofFromTo.OfFrom ) ?? throw new InvalidOperationException(), true) ;
      }
      else {
        return (TrailTo( endPointSubRouteMap, ofFromTo.OfTo! ) ?? throw new InvalidOperationException(), false) ;
      }
    }

    private static (SubRoute, IEndPoint)? TrailFrom( Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)> endPointSubRouteMap, SubRoute subRoute )
    {
      foreach ( var toEndPoint in subRoute.ToEndPoints ) {
        if ( false == endPointSubRouteMap.TryGetValue( toEndPoint, out var tuple ) ) continue ;

        if ( null == tuple.OfFrom ) return ( subRoute, toEndPoint ) ;
        if ( TrailFrom( endPointSubRouteMap, tuple.OfTo! ) is { } result ) return result ;
      }

      return null ;
    }

    private static (SubRoute, IEndPoint)? TrailTo( Dictionary<IEndPoint, (SubRoute? OfFrom, SubRoute? OfTo)> endPointSubRouteMap, SubRoute subRoute )
    {
      foreach ( var fromEndPoint in subRoute.FromEndPoints ) {
        if ( false == endPointSubRouteMap.TryGetValue( fromEndPoint, out var tuple ) ) continue ;

        if ( null == tuple.OfTo ) return ( subRoute, fromEndPoint ) ;
        if ( TrailTo( endPointSubRouteMap, tuple.OfFrom! ) is { } result ) return result ;
      }

      return null ;
    }

    private class PseudoPickResult : ConnectorPicker.IPickResult
    {
      private readonly SubRoute _subRoute ;
      private readonly IEndPoint _endPoint ;

      public PseudoPickResult( SubRoute subRoute, IEndPoint endPoint, bool isFrom )
      {
        _subRoute = subRoute ;
        _endPoint = endPoint ;

        if ( endPoint is RouteEndPoint routeEndPoint ) {
          SubRoute = routeEndPoint.GetSubRoute() ;
          EndPointOverSubRoute = null ;
        }
        else if ( endPoint is PassPointBranchEndPoint passPointBranchEndPoint ) {
          SubRoute = passPointBranchEndPoint.GetSubRoute( isFrom ) ?? subRoute ;
          EndPointOverSubRoute = passPointBranchEndPoint.EndPointKeyOverSubRoute ;
        }
        else {
          SubRoute = null ;
          EndPointOverSubRoute = null ;
        }
      }

      public IEnumerable<ElementId> GetAllRelatedElements() => Enumerable.Empty<ElementId>() ;

      public SubRoute? SubRoute { get ; }
      public EndPointKey? EndPointOverSubRoute { get ; }
      public Element PickedElement => throw new InvalidOperationException() ;
      public Connector? PickedConnector => ( _endPoint as ConnectorEndPoint )?.GetConnector() ?? _subRoute.GetReferenceConnector() ;
      public XYZ GetOrigin() => _endPoint.RoutingStartPosition ;
      public Element GetOriginElement() => throw new InvalidOperationException() ;

      public bool IsCompatibleTo( Connector connector )
      {
        return ( PickedConnector ?? SubRoute?.GetReferenceConnector() ?? _subRoute.GetReferenceConnector() ).IsCompatibleTo( connector ) ;
      }

      public bool IsCompatibleTo( Element element )
      {
        return ( _subRoute.Route.RouteName != element.GetRouteName() ) ;
      }
    }
  }
}