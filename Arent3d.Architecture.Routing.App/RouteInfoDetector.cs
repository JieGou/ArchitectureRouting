using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoint ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Detects <see cref="RouteInfo"/>s which passes through an element.
  /// </summary>
  public class RouteInfoDetector
  {
    public string RouteName { get ; }
    public int SubRouteIndex { get ; }
    private readonly HashSet<IEndPointIndicator> _fromElms = new() ;
    private readonly HashSet<IEndPointIndicator> _toElms = new() ;

    /// <summary>
    /// Create a <see cref="RouteInfoDetector"/>.
    /// </summary>
    /// <param name="subRoute">A <see cref="SubRoute"/> which can be affected by the passed-through element.</param>
    /// <param name="elementToPassThrough">A passed-through element.</param>
    public RouteInfoDetector( SubRoute subRoute, Element elementToPassThrough )
    {
      RouteName = subRoute.Route.RouteName ;
      SubRouteIndex = subRoute.SubRouteIndex ;

      CollectEndPoints( elementToPassThrough, true, _fromElms ) ;
      CollectEndPoints( elementToPassThrough, false, _toElms ) ;
    }

    private static void CollectEndPoints( Element element, bool isFrom, HashSet<IEndPointIndicator> foundElms )
    {
      foreach ( var indicator in element.GetNearestEndPointIndicators( isFrom ) ) {
        switch ( indicator ) {
          case PassPointEndIndicator pp :
            foundElms.Add( new PassPointEndIndicator( pp.ElementId, PassPointEndSide.Forward ) ) ;
            break ;
          default :
            foundElms.Add( indicator ) ;
            break ;
        }
      }
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
    public int GetPassedThroughPassPointIndex( RouteInfo info )
    {
      var list = new List<IEndPointIndicator>() ;
      list.Add( info.FromId ) ;
      list.AddRange( info.PassPoints.Select( id => (IEndPointIndicator) new PassPointEndIndicator( id, PassPointEndSide.Forward ) ) ) ;
      list.Add( info.ToId ) ;

      for ( var i = list.Count - 2 ; i >= 0 ; --i ) {
        if ( _fromElms.Contains( list[ i ] ) && _toElms.Contains( list[ i + 1 ] ) ) {
          return i ;
        }
      }

      return -1 ;
    }
  }
}