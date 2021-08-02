using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.CollisionTree ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  public class ElectricalRouteGenerator : RouteGenerator
  {
    private readonly Dictionary<(IRouteVertex BaseVertex, (EndPointKey From, EndPointKey To) segment), IRouteVertex> _splitVertices = new() ;
    
    public ElectricalRouteGenerator( IReadOnlyCollection<Route> routes, Document document, ICollisionCheckTargetCollector collector ) : base( routes, document, ElectricalFittingSizeCalculator.Instance, collector )
    {
    }

    protected override IEnumerable<Element> CreateEdges( MEPSystemCreator mepSystemCreator, AutoRoutingResult result )
    {
      var autoRoutingTarget = mepSystemCreator.AutoRoutingTarget ;
      foreach ( var routeEdge in result.RouteEdges ) {
        var subRoute = autoRoutingTarget.GetSubRoute( routeEdge ) ;
        var passingEndPointInfo = result.GetPassingEndPointInfo( routeEdge ) ;

        foreach ( var (splitRouteEdge, splitPassingEndPointInfo) in SplitRouteEdgeBySegment( routeEdge, passingEndPointInfo, subRoute ) ) {
          yield return mepSystemCreator.CreateEdgeElement( splitRouteEdge, splitPassingEndPointInfo ) ;
        }
      }
    }

    private IEnumerable<(IRouteEdge, PassingEndPointInfo)> SplitRouteEdgeBySegment( IRouteEdge routeEdge, PassingEndPointInfo passingEndPointInfo, SubRoute subRoute )
    {
      var fromPoints = passingEndPointInfo.FromEndPoints.ToList() ;
      var toPoints = passingEndPointInfo.ToEndPoints.ToList() ;

      while ( 0 < fromPoints.Count && 0 < toPoints.Count ) {
        if ( 1 == fromPoints.Count ) {
          var fromPoint = fromPoints[ 0 ] ;
          var fromPointKey = fromPoint.Key ;
          foreach ( var toPoint in toPoints ) {
            var toPointKey = toPoint.Key ;
            var splitRouteEdge = CreateSplitRouteEdge( routeEdge, ( fromPointKey, toPointKey ) ) ;
            var splitPassingEndPointInfo = passingEndPointInfo.CreateSubPassingEndPointInfo( fromPoint, toPoint ) ;
            yield return ( splitRouteEdge, splitPassingEndPointInfo ) ;
          }
          yield break ;
        }

        if ( 1 == toPoints.Count ) {
          var toPoint = toPoints[ 0 ] ;
          var toPointKey = toPoint.Key ;
          foreach ( var fromPoint in fromPoints ) {
            var fromPointKey = fromPoint.Key ;
            var splitRouteEdge = CreateSplitRouteEdge( routeEdge, ( fromPointKey, toPointKey ) ) ;
            var splitPassingEndPointInfo = passingEndPointInfo.CreateSubPassingEndPointInfo( fromPoint, toPoint ) ;
            yield return ( splitRouteEdge, splitPassingEndPointInfo ) ;
          }
          yield break ;
        }

        {
          // provisional implementation
          // TODO: pair true from & to end points (for H type or 工 type junctions)
          var fromPoint = fromPoints[ 0 ] ;
          var fromPointKey = fromPoint.Key ;
          var toPoint = toPoints[ 0 ] ;
          var toPointKey = toPoint.Key ;

          var splitRouteEdge = CreateSplitRouteEdge( routeEdge, ( fromPointKey, toPointKey ) ) ;
          var splitPassingEndPointInfo = passingEndPointInfo.CreateSubPassingEndPointInfo( fromPoint, toPoint ) ;
          yield return ( splitRouteEdge, splitPassingEndPointInfo ) ;

          fromPoints.RemoveAt( 0 ) ;
          toPoints.RemoveAt( 0 ) ;
        }
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