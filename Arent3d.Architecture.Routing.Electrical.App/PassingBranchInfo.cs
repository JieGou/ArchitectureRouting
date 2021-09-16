using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  internal class PassingBranchInfo
  {
    public static IReadOnlyDictionary<SubRoute, PassingBranchInfo> CreateBranchMap( IEnumerable<Route> routes )
    {
      var dicPrevNext = CreatePrevNextSubRouteDictionary( routes ) ;

      var dic = new Dictionary<SubRoute, PassingBranchInfo>() ;
      foreach ( var (route, subRouteSequence) in CollectAllSubRouteSequences( dicPrevNext ) ) {
        var representativeSubRoute = subRouteSequence.First( subRoute => subRoute.Route == route ) ;
        foreach ( var subRoute in subRouteSequence ) {
          if ( false == dic.TryGetValue( subRoute, out var passingBranchInfo ) ) {
            passingBranchInfo = new PassingBranchInfo() ;
            dic.Add( subRoute, passingBranchInfo ) ;
          }

          if ( subRoute.Route == route ) {
            representativeSubRoute = subRoute ; // update when owner Route is matched
          }

          passingBranchInfo.AddFromToEndPoint( representativeSubRoute, GetFromEndPoint( subRoute ), GetToEndPoint( subRoute ) ) ;
        }
      }

      return dic ;
    }

    private static IEndPoint GetFromEndPoint( SubRoute subRoute ) => subRoute.Segments.First().FromEndPoint ;
    private static IEndPoint GetToEndPoint( SubRoute subRoute ) => subRoute.Segments.First().ToEndPoint ;

    private static SubRoute GetPreviousSubRoute( SubRoute subRoute ) => subRoute.PreviousSubRoute ?? throw new InvalidOperationException() ;
    private static SubRoute GetNextSubRoute( SubRoute subRoute ) => subRoute.NextSubRoute ?? throw new InvalidOperationException() ;

    private static IReadOnlyDictionary<SubRoute, (List<SubRoute> Prev, List<SubRoute> Next)> CreatePrevNextSubRouteDictionary( IEnumerable<Route> routes )
    {
      var subRoutes = routes.SelectMany( route => route.SubRoutes ) ;

      var dicPrevNext = new Dictionary<SubRoute, (List<SubRoute> Prev, List<SubRoute> Next)>() ;
      foreach ( var subRoute in subRoutes ) {
        switch ( GetFromEndPoint( subRoute ) ) {
        case PassPointEndPoint :
          AddLink( dicPrevNext, GetPreviousSubRoute( subRoute ), subRoute ) ;
          break ;
        case PassPointBranchEndPoint fromPassPoint :
          AddLink( dicPrevNext, fromPassPoint.GetSubRoute( true ) ?? throw new InvalidOperationException(), subRoute ) ;
          break ;
        }

        switch ( GetToEndPoint( subRoute ) ) {
        case PassPointEndPoint :
          AddLink( dicPrevNext, subRoute, GetNextSubRoute( subRoute ) ) ;
          break ;
        case PassPointBranchEndPoint toPassPoint :
          AddLink( dicPrevNext, subRoute, toPassPoint.GetSubRoute( false ) ?? throw new InvalidOperationException() ) ;
          break ;
        }

        if ( false == dicPrevNext.ContainsKey( subRoute ) ) {
          dicPrevNext.Add( subRoute, ( new List<SubRoute>(), new List<SubRoute>() ) ) ;
        }
      }

      return dicPrevNext ;

      static void AddLink( Dictionary<SubRoute, (List<SubRoute> Prev, List<SubRoute> Next)> dicPrevNext, SubRoute prevSubRoute, SubRoute nextSubRoute )
      {
        if ( false == dicPrevNext.TryGetValue( prevSubRoute, out var tuple1 ) ) {
          tuple1 = ( new List<SubRoute>(), new List<SubRoute>() ) ;
          dicPrevNext.Add( prevSubRoute, tuple1 ) ;
        }
        tuple1.Next.Add( nextSubRoute ) ;

        if ( false == dicPrevNext.TryGetValue( nextSubRoute, out var tuple2 ) ) {
          tuple2 = ( new List<SubRoute>(), new List<SubRoute>() ) ;
          dicPrevNext.Add( nextSubRoute, tuple2 ) ;
        }
        tuple2.Prev.Add( prevSubRoute ) ;
      }
    }

    private static IEnumerable<KeyValuePair<Route, IReadOnlyList<SubRoute>>> CollectAllSubRouteSequences( IReadOnlyDictionary<SubRoute, (List<SubRoute> Prev, List<SubRoute> Next)> dicPrevNext )
    {
      var dicSequences = new Dictionary<Route, IReadOnlyList<SubRoute>>() ;
      foreach ( var subRoute in dicPrevNext.Keys ) {
        var route = subRoute.Route ;
        if ( dicSequences.ContainsKey( route ) ) continue ; // already created

        var list = new List<SubRoute> { subRoute } ;
        GetSubRouteList( list, dicPrevNext, subRoute, true, null ) ;
        list.Reverse() ;
        GetSubRouteList( list, dicPrevNext, subRoute, false, null ) ;

        dicSequences.Add( route, list ) ;
      }

      return dicSequences ;

      static void GetSubRouteList( List<SubRoute> subRouteList, IReadOnlyDictionary<SubRoute, (List<SubRoute> Prev, List<SubRoute> Next)> dicPrevNext, SubRoute subRoute, bool trailFromSide, EndPointKey? target )
      {
        while ( true ) {
          var nextEndPoint = trailFromSide ? GetFromEndPoint( subRoute ) : GetToEndPoint( subRoute ) ;

          if ( null == target ) {
            switch ( nextEndPoint ) {
              case PassPointEndPoint :
                subRoute = trailFromSide ? GetPreviousSubRoute( subRoute ) : GetNextSubRoute( subRoute ) ;
                subRouteList.Add( subRoute ) ;
                continue ; // tail recursion

              case PassPointBranchEndPoint passPointBranchEndPoint :
                target = passPointBranchEndPoint.EndPointKeyOverSubRoute ;
                subRoute = passPointBranchEndPoint.GetSubRoute( trailFromSide ) ?? throw new InvalidOperationException() ;
                subRouteList.Add( subRoute ) ;
                continue ; // tail recursion

              default : return ; // finished
            }
          }

          var list = SeekToEnd( dicPrevNext, subRoute, target, trailFromSide ) ;
          if ( null == list ) throw new InvalidOperationException() ;

          subRouteList.AddRange( list ) ;
          return ;
        }
      }

      static IEnumerable<SubRoute>? SeekToEnd( IReadOnlyDictionary<SubRoute, (List<SubRoute> Prev, List<SubRoute> Next)> dicPrevNext, SubRoute subRoute, EndPointKey target, bool trailFromSide )
      {
        var tuple = dicPrevNext[ subRoute ] ;
        var nextCandidate = trailFromSide ? tuple.Prev : tuple.Next ;

        if ( 0 == nextCandidate.Count ) {
          if ( ( trailFromSide ? GetFromEndPoint( subRoute ) : GetToEndPoint( subRoute ) ).Key != target ) return null ;  // not a target branch
          return Enumerable.Empty<SubRoute>() ; // finished
        }

        return nextCandidate.Select( nextSubRoute => SeekToEnd( dicPrevNext, nextSubRoute, target, trailFromSide )?.Prepend( nextSubRoute ) ).FirstOrDefault( list => null != list ) ;
      }
    }



    private readonly List<(SubRoute SubRoute, EndPointKey From, EndPointKey To)> _list = new() ;

    private PassingBranchInfo()
    {
    }

    private void AddFromToEndPoint( SubRoute representativeSubRoute, IEndPoint fromEndPoint, IEndPoint toEndPoint )
    {
      _list.Add( ( representativeSubRoute, fromEndPoint.Key, toEndPoint.Key ) ) ;
    }

    public IEnumerable<(SubRoute SubRoute, EndPointKey From, EndPointKey To)> GetPassingSubRoutes() => _list ;
  }
}