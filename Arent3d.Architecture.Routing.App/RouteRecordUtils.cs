using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
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
      var endPointDictionary = new EndPointDictionaryForImport( document ) ;

      await foreach ( var record in routeRecords ) {
        var fromEndPoint = endPointDictionary.GetEndPoint( record.RouteName, record.FromKey,EndPointExtensions.ParseEndPoint( document, record.FromEndType, record.FromEndParams ) ) ;
        var toEndPoint = endPointDictionary.GetEndPoint( record.RouteName, record.ToKey, EndPointExtensions.ParseEndPoint( document, record.ToEndType, record.ToEndParams ) ) ;
        if ( null == fromEndPoint || null == toEndPoint ) continue ;

        var curveType = GetCurveType( document, record.CurveTypeName ) ;

        yield return ( record.RouteName, new RouteSegment( fromEndPoint, toEndPoint, record.NominalDiameter, record.IsRoutingOnPipeSpace, record.FixedBopHeight, record.AvoidType) { CurveType = curveType } ) ;
      }
    }

    public static IEnumerable<RouteRecord> ToRouteRecords( this IEnumerable<(string RouteName, RouteSegment Segment)> segments, Document document )
    {
      var endPointDictionary = new EndPointDictionaryForExport( document ) ;

      foreach ( var (routeName, segment) in segments ) {
        var (fromKey, fromEndPoint) = endPointDictionary.GetEndPoint( segment.FromEndPoint ) ;
        var (toKey, toEndPoint) = endPointDictionary.GetEndPoint( segment.ToEndPoint ) ;

        yield return new RouteRecord
        {
          RouteName = routeName,
          FromKey = fromKey,
          FromEndType = fromEndPoint.TypeName,
          FromEndParams = fromEndPoint.ParameterString,
          ToKey = toKey,
          ToEndType = toEndPoint.TypeName,
          ToEndParams = toEndPoint.ParameterString,
          NominalDiameter = segment.PreferredNominalDiameter,
          CurveTypeName = GetCurveTypeName( segment.CurveType ),
          FixedBopHeight = segment.FixedBopHeight,
          AvoidType = segment.AvoidType,
        } ;
      }
    }

    private static MEPCurveType? GetCurveType( Document document, string curveTypeName )
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( ct => ct.Name == curveTypeName ) ;
    }

    private static string GetCurveTypeName( MEPCurveType? curveType )
    {
      if ( null == curveType ) return string.Empty ;

      return curveType.Name ;
    }
  }
}