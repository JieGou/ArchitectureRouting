using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.App
{
  public static class RouteRecordUtils
  {
    public static IEnumerable<(string RouteName, RouteSegment Segment)> ToSegmentsWithName( this IEnumerable<RouteSegment> segments, string routeName )
    {
      return segments.Select( seg => ( routeName, seg ) ) ;
    }
    public static IEnumerable<(string RouteName, RouteSegment Segment)> ToSegmentsWithName( this Route route )
    {
      return route.RouteSegments.ToSegmentsWithName( route.RouteName ) ;
    }
    public static List<(string RouteName, RouteSegment Segment)> ToSegmentsWithNameList( this Route route )
    {
      var list = new List<(string RouteName, RouteSegment Segment)>( route.RouteSegments.Count ) ;
      list.AddRange( route.RouteSegments.ToSegmentsWithName( route.RouteName ) ) ;
      return list ;
    }
    public static IEnumerable<(string RouteName, RouteSegment Segment)> ToSegmentsWithName( this IEnumerable<Route> routes )
    {
      return routes.SelectMany( ToSegmentsWithName ) ;
    }

    public static async IAsyncEnumerable<(string RouteName, RouteSegment Segment)> ToSegmentsWithName( this IAsyncEnumerable<RouteRecord> routeRecords, Document document )
    {
      var indDic = new EndPointIndicatorDictionaryForImport( document ) ;

      await foreach ( var record in routeRecords ) {
        var fromIndicator = indDic.GetIndicator( record.RouteName, record.FromKey, record.FromIndicator ) ;
        var toIndicator = indDic.GetIndicator( record.RouteName, record.FromKey, record.ToIndicator ) ;
        if ( null == fromIndicator || null == toIndicator ) continue ;
        yield return ( record.RouteName, new RouteSegment( fromIndicator, toIndicator, record.NominalDiameter, record.IsRoutingOnPipeSpace ) ) ;
      }
    }

    public static IEnumerable<RouteRecord> ToRouteRecords( this IEnumerable<(string RouteName, RouteSegment Segment)> segments, Document document )
    {
      var indDic = new EndPointIndicatorDictionaryForExport( document ) ;

      foreach ( var (routeName, segment) in segments ) {
        var (fromKey, fromIndicator) = indDic.GetIndicator( segment.FromId ) ;
        var (toKey, toIndicator) = indDic.GetIndicator( segment.ToId ) ;

        yield return new RouteRecord
        {
          RouteName = routeName,
          FromKey = fromKey,
          FromIndicator = fromIndicator,
          ToKey = toKey,
          ToIndicator = toIndicator
        } ;
      }
    }
  }
}