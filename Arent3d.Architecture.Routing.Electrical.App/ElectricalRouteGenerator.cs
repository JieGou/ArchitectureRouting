using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.FittingSizeCalculators ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  public class ElectricalRouteGenerator : RouteGenerator
  {
    private readonly Dictionary<(IRouteVertex BaseVertex, (EndPointKey From, EndPointKey To) segment), IRouteVertex> _splitVertices = new() ;
    
    public ElectricalRouteGenerator( IReadOnlyCollection<Route> routes, Document document, IFittingSizeCalculator fittingSizeCalculator, ICollisionCheckTargetCollector collector ) : base( routes, document, fittingSizeCalculator, collector )
    {
    }

    protected override IEnumerable<Element> CreateEdges( MEPSystemCreator mepSystemCreator, AutoRoutingResult result )
    {
      var autoRoutingTarget = mepSystemCreator.AutoRoutingTarget ;
      var allFromToList = autoRoutingTarget.GetAllSubRoutes().SelectMany( GetFromToEndPointKeys ).EnumerateAll() ;
      foreach ( var routeEdge in result.RouteEdges ) {
        var passingEndPointInfo = result.GetPassingEndPointInfo( routeEdge ) ;
        foreach ( var (subRoute, splitRouteEdge, splitPassingEndPointInfo) in GetMatchingRouteEdgeBySegments( allFromToList, routeEdge, passingEndPointInfo ) ) {
          yield return mepSystemCreator.CreateEdgeElement( splitRouteEdge, subRoute, splitPassingEndPointInfo ) ;
        }
      }
    }

    private static IEnumerable<(SubRoute, EndPointKey, EndPointKey)> GetFromToEndPointKeys( SubRoute subRoute )
    {
      return subRoute.Segments.Select( segment => ( subRoute, GetConnectingEndPointKey( segment.FromEndPoint ), GetConnectingEndPointKey( segment.ToEndPoint ) ) ) ;
    }

    private static EndPointKey GetConnectingEndPointKey( IEndPoint endPoint )
    {
      return ( endPoint as RouteEndPoint )?.EndPointKeyOverSubRoute ?? endPoint.Key ;
    }

    private IEnumerable<(SubRoute, IRouteEdge, PassingEndPointInfo)> GetMatchingRouteEdgeBySegments( IReadOnlyCollection<(SubRoute, EndPointKey, EndPointKey)> allFromToList, IRouteEdge routeEdge, PassingEndPointInfo passingEndPointInfo )
    {
      foreach ( var (subRoute, fromEndPointKey, toEndPointKey) in allFromToList ) {
        if ( false == passingEndPointInfo.TryGetFromEndPoint( fromEndPointKey, out var fromEndPoint ) ) continue ;
        if ( false == passingEndPointInfo.TryGetToEndPoint( toEndPointKey, out var toEndPoint ) ) continue ;

        var splitRouteEdge = CreateSplitRouteEdge( routeEdge, ( fromEndPointKey, toEndPointKey ) ) ;
        var splitPassingEndPointInfo = passingEndPointInfo.CreateSubPassingEndPointInfo( fromEndPoint, toEndPoint ) ;
        yield return ( subRoute, splitRouteEdge, splitPassingEndPointInfo ) ;
      }
    }

    private IRouteEdge CreateSplitRouteEdge( IRouteEdge routeEdge, (EndPointKey From, EndPointKey To) segment )
    {
      var startVertex = GetSplitVertex( routeEdge.Start, segment ) ;
      var endVertex = GetSplitVertex( routeEdge.End, segment ) ;
      return new SplitRouteEdge( routeEdge, startVertex, endVertex ) ;
    }

    private IRouteVertex GetSplitVertex( IRouteVertex routeVertex, (EndPointKey From, EndPointKey To) segment )
    {
      if ( false == _splitVertices.TryGetValue( ( routeVertex, segment ), out var splitVertex ) ) {
        splitVertex = new SplitRouteVertex( routeVertex ) ;
        _splitVertices.Add( ( routeVertex, segment ), splitVertex ) ;
      }

      return splitVertex ;
    }

    private class SplitRouteVertex : IRouteVertex
    {
      private readonly IRouteVertex _baseRouteVertex ;

      public SplitRouteVertex( IRouteVertex routeVertex )
      {
        _baseRouteVertex = routeVertex ;
      }

      public Vector3d Position => _baseRouteVertex.Position ;

      public IPipeDiameter PipeDiameter => _baseRouteVertex.PipeDiameter ;

      public IAutoRoutingEndPoint LineInfo => _baseRouteVertex.LineInfo ;

      public bool IsOverflow => _baseRouteVertex.IsOverflow ;
    }

    private class SplitRouteEdge : IRouteEdge
    {
      private readonly IRouteEdge _baseRouteEdge ;

      public SplitRouteEdge( IRouteEdge routeEdge, IRouteVertex startVertex, IRouteVertex endVertex )
      {
        _baseRouteEdge = routeEdge ;
        Start = startVertex ;
        End = endVertex ;
      }

      public IRouteVertex Start { get ; }

      public IRouteVertex End { get ; }

      public IAutoRoutingEndPoint LineInfo => _baseRouteEdge.LineInfo ;

      public ILayerProperty RelatedLayer => _baseRouteEdge.RelatedLayer ;

      public bool IsOverflowed => _baseRouteEdge.IsOverflowed ;
    }
  }
}