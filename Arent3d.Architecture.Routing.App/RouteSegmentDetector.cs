using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Detects <see cref="RouteSegment"/>s which passes through an element.
  /// </summary>
  public class RouteSegmentDetector
  {
    public string RouteName { get ; }
    public int SubRouteIndex { get ; }
    private readonly HashSet<IEndPointIndicator> _fromElms = new() ;
    private readonly HashSet<IEndPointIndicator> _toElms = new() ;

    /// <summary>
    /// Create a <see cref="RouteSegmentDetector"/>.
    /// </summary>
    /// <param name="subRoute">A <see cref="SubRoute"/> which can be affected by the passed-through element.</param>
    /// <param name="elementToPassThrough">A passed-through element.</param>
    public RouteSegmentDetector( SubRoute subRoute, Element elementToPassThrough )
    {
      RouteName = subRoute.Route.RouteName ;
      SubRouteIndex = subRoute.SubRouteIndex ;

      CollectEndPoints( elementToPassThrough, true, _fromElms ) ;
      CollectEndPoints( elementToPassThrough, false, _toElms ) ;
    }

    private static void CollectEndPoints( Element element, bool isFrom, HashSet<IEndPointIndicator> foundElms )
    {
      foundElms.UnionWith( element.GetNearestEndPointIndicators( isFrom ) ) ;
    }

    /// <summary>
    /// Returns a pass point index which is after the pass-through element
    /// </summary>
    /// <param name="info">Route info.</param>
    /// <returns>
    /// <para>Pass point index.</para>
    /// <para>0: The passed-through element is between the from-side connector and the first pass point (when no pass points, the to-side connector).</para>
    /// <para>1 to (info.PassPoints.Length - 1): The passed-through element is between the (k-1)-th pass point and the k-th pass point.</para>
    /// <para>info.PassPoints.Length: The passed-through element is between the last pass point and the to-side connector.</para>
    /// <para>-1: Not passed through.</para>
    /// </returns>
    public bool IsPassingThrough( RouteSegment info )
    {
      return ( _fromElms.Contains( info.FromId ) && _toElms.Contains( info.ToId ) ) ;
    }
  }
}