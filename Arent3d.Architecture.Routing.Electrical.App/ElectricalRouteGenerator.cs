using System ;
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
    private readonly Dictionary<(IRouteVertex BaseVertex, Route Route), IRouteVertex> _splitVertices = new() ;
    private IReadOnlyDictionary<SubRoute, PassingBranchInfo>? _branchMap = null ;

    public ElectricalRouteGenerator( IReadOnlyCollection<Route> routes, Document document, IFittingSizeCalculator fittingSizeCalculator, ICollisionCheckTargetCollector collector ) : base( routes, document, fittingSizeCalculator, collector )
    {
    }

    protected override void OnGenerationStarted()
    {
      _branchMap = PassingBranchInfo.CreateBranchMap( RoutingTargets.SelectMany( target => target.Routes ).Distinct() ) ;
    }

    protected override IEnumerable<Element> CreateEdges( MEPSystemCreator mepSystemCreator, AutoRoutingResult result )
    {
      if ( null == _branchMap ) throw new InvalidOperationException() ;

      var autoRoutingTarget = mepSystemCreator.AutoRoutingTarget ;
      foreach ( var routeEdge in result.RouteEdges ) {
        var passingEndPointInfo = result.GetPassingEndPointInfo( routeEdge ) ;
        var representativeSubRoute = autoRoutingTarget.GetSubRoute( routeEdge ) ;
        var representativeSubRouteInfo = new SubRouteInfo( representativeSubRoute ) ;

        var groupedSubRoutes = new List<SubRoute>() ;
        foreach ( var (subRoute, splitRouteEdge, splitPassingEndPointInfo) in GetMatchingRouteEdgeBySegments( representativeSubRoute, routeEdge, passingEndPointInfo ) ) {
          var mepCurve = mepSystemCreator.CreateEdgeElement( splitRouteEdge, subRoute, splitPassingEndPointInfo ) ;
          mepCurve.SetRepresentativeSubRoute( representativeSubRouteInfo ) ;
          groupedSubRoutes.Add( subRoute );
          yield return mepCurve ;
        }

        representativeSubRoute.SetSubRouteGroup( groupedSubRoutes.ConvertAll( subRoute => new SubRouteInfo( subRoute ) ) ) ;
      }
    }

    private IEnumerable<(SubRoute, IRouteEdge, PassingEndPointInfo)> GetMatchingRouteEdgeBySegments( SubRoute representativeSubRoute, IRouteEdge routeEdge, PassingEndPointInfo passingEndPointInfo )
    {
      if ( false == _branchMap!.TryGetValue( representativeSubRoute, out var passingBranchInfo ) ) throw new KeyNotFoundException() ;
      
      foreach ( var (subRoute, fromEndPointKey, toEndPointKey) in passingBranchInfo.GetPassingSubRoutes() ) {
        if ( false == passingEndPointInfo.TryGetFromEndPoint( fromEndPointKey, out var fromEndPoint ) ) continue ;
        if ( false == passingEndPointInfo.TryGetToEndPoint( toEndPointKey, out var toEndPoint ) ) continue ;

        var splitRouteEdge = CreateSplitRouteEdge( routeEdge, subRoute.Route ) ;
        var splitPassingEndPointInfo = PassingEndPointInfo.CreatePassingEndPointInfo( fromEndPoint, toEndPoint ) ;
        yield return ( subRoute, splitRouteEdge, splitPassingEndPointInfo ) ;
      }
    }

    private IRouteEdge CreateSplitRouteEdge( IRouteEdge routeEdge, Route route )
    {
      var startVertex = GetSplitVertex( routeEdge.Start, route ) ;
      var endVertex = GetSplitVertex( routeEdge.End, route ) ;
      return new SplitRouteEdge( routeEdge, startVertex, endVertex ) ;
    }

    private IRouteVertex GetSplitVertex( IRouteVertex routeVertex, Route route )
    {
      if ( false == _splitVertices.TryGetValue( ( routeVertex, route ), out var splitVertex ) ) {
        splitVertex = new SplitRouteVertex( routeVertex ) ;
        _splitVertices.Add( ( routeVertex, route ), splitVertex ) ;
      }

      return splitVertex ;
    }

    private class SplitRouteVertex : IPseudoTerminalPoint
    {
      public SplitRouteVertex( IRouteVertex routeVertex )
      {
        BaseRouteVertex = routeVertex ;
      }

      public IRouteVertex BaseRouteVertex { get ; }

      public Vector3d Position => BaseRouteVertex.Position ;

      public IPipeDiameter PipeDiameter => BaseRouteVertex.PipeDiameter ;

      public IAutoRoutingEndPoint LineInfo => BaseRouteVertex.LineInfo ;

      public bool IsOverflow => BaseRouteVertex.IsOverflow ;
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