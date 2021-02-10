using System.Collections.Generic ;
using System.Linq ;
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
    private readonly HashSet<(int, int)> _fromElms = new() ;
    private readonly HashSet<(int, int)> _toElms = new() ;

    /// <summary>
    /// Create a <see cref="RouteInfoDetector"/>.
    /// </summary>
    /// <param name="subRoute">A <see cref="SubRoute"/> which can be affected by the passed-through element.</param>
    /// <param name="elementToPassThrough">A passed-through element.</param>
    public RouteInfoDetector( SubRoute subRoute, Element elementToPassThrough )
    {
      RouteName = subRoute.Route.RouteId ;
      SubRouteIndex = subRoute.SubRouteIndex ;

      var passPointDic = CreatePassPointDictionary( elementToPassThrough.Document ) ;

      CollectEndPoints( elementToPassThrough, true, passPointDic, _fromElms ) ;
      CollectEndPoints( elementToPassThrough, false, passPointDic, _toElms ) ;
    }

    private static IReadOnlyDictionary<(int, int), int> CreatePassPointDictionary( Document document )
    {
      var dic = new Dictionary<(int, int), int>() ;

      foreach ( var passPoint in document.GetAllFamilyInstances( RoutingFamilyType.PassPoint ) ) {
        var passPointId = passPoint.Id.IntegerValue ;
        foreach ( var con in passPoint.GetPassPointConnectors( true ).Concat( passPoint.GetPassPointConnectors( false ) ) ) {
          dic.Add( ( con.ElementId, con.ConnectorId ), passPointId ) ;
        }
      }

      return dic ;
    }

    private void CollectEndPoints( Element element, bool isFrom, IReadOnlyDictionary<(int, int), int> passPointDictionary, HashSet<(int, int)> foundElms )
    {
      var stack = new Stack<Element>() ;
      stack.Push( element ) ;

      var done = new HashSet<int> { element.Id.IntegerValue } ;

      while ( 0 < stack.Count ) {
        var e = stack.Pop() ;
        foreach ( var connector in e.GetRoutingConnectors( isFrom ) ) {
          var passPoint = GetPassPoint( passPointDictionary, connector ) ;
          if ( passPoint.HasValue ) {
            // register pass point and finish searching
            foundElms.Add( ( passPoint.Value, 0 ) ) ;
          }
          else {
            foreach ( var c in connector.GetLogicallyConnectedConnectors() ) {
              var passPoint2 = GetPassPoint( passPointDictionary, c ) ;
              if ( passPoint2.HasValue ) {
                // register pass point and finish searching
                foundElms.Add( ( passPoint2.Value, 0 ) ) ;
              }
              else if ( false == done.Add( c.Owner.Id.IntegerValue ) ) {
                // already added (do nothing)
              }
              else if ( IsTargetSubRoute( c.Owner ) ) {
                // add into searching targets.
                stack.Push( c.Owner ) ;
              }
              else {
                // end point
                foundElms.Add( ( c.Owner.Id.IntegerValue, c.Id ) ) ;
              }
            }
          }
        }
      }
    }

    private bool IsTargetSubRoute( Element elm )
    {
      return ( elm.GetRouteName() == RouteName ) && ( elm.GetSubRouteIndex() == SubRouteIndex ) ;
    }

    private static int? GetPassPoint( IReadOnlyDictionary<(int, int), int> passPointDictionary, Connector connector )
    {
      if ( passPointDictionary.TryGetValue( ( connector.Owner.Id.IntegerValue, connector.Id ), out var i ) ) return i ;
      return null ;
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
      var list = new List<(int, int)>() ;
      list.Add( ( info.FromId.ElementId, info.FromId.ConnectorId ) ) ;
      list.AddRange( info.PassPoints.Select( id => ( id, 0 ) ) ) ;
      list.Add( ( info.ToId.ElementId, info.ToId.ConnectorId ) ) ;

      for ( var i = list.Count - 2 ; i >= 0 ; --i ) {
        if ( _fromElms.Contains( list[ i ] ) && _toElms.Contains( list[ i + 1 ] ) ) {
          return i ;
        }
      }

      return -1 ;
    }
  }
}