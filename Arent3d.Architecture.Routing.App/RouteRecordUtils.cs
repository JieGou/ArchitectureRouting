using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  public static class RouteRecordUtils
  {
    public static IEnumerable<RouteRecord> ToRouteRecords( Route route )
    {
      var routeName = route.RouteId ;

      foreach ( var subRoute in route.SubRoutes ) {
        var fromList = subRoute.FromEndPointIndicators.OfType<ConnectorIndicator>().ToList() ;
        var toList = subRoute.ToEndPointIndicators.OfType<ConnectorIndicator>().ToList() ;

        foreach ( var record in ToRouteRecords( routeName, fromList, toList ) ) {
          yield return record ;
        }
      }
    }

    public static IEnumerable<RouteRecord> ToRouteRecords( string routeName, IList<ConnectorIndicator> fromList, IList<ConnectorIndicator> toList )
    {
      if ( 0 == fromList.Count || 0 == toList.Count ) yield break ;

      var from1 = fromList[ 0 ] ;
      var to1 = toList[ 0 ] ;
      foreach ( var to in toList ) {
        yield return new RouteRecord( routeName, from1, to ) ; // TODO: Pass points
      }

      foreach ( var from in fromList.Skip( 1 ) ) {
        yield return new RouteRecord( routeName, from, to1 ) ; // TODO: Pass points
      }
    }
  }
}