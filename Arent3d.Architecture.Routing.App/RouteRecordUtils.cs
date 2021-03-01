using System.Collections.Generic ;

namespace Arent3d.Architecture.Routing.App
{
  public static class RouteRecordUtils
  {
    public static IEnumerable<RouteRecord> ToRouteRecords( Route route )
    {
      var routeName = route.RouteName ;

      foreach ( var routeInfo in route.RouteInfos ) {
        yield return new RouteRecord( routeName, routeInfo ) ;
      }
    }
  }
}