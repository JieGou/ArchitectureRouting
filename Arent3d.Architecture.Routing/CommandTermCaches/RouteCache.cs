using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.CommandTermCaches
{
  public class RouteCache : CommandTermCache<RouteCache>, IReadOnlyDictionary<string, Route>
  {
    private readonly Dictionary<string, Route> _dic ;

    private RouteCache( Document document ) : base( document )
    {
      _dic = document.GetAllStorables<Route>().ToDictionary( route => route.RouteName ) ;
    }

    public IEnumerator<KeyValuePair<string, Route>> GetEnumerator()
    {
      return _dic.GetEnumerator() ;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;

    public int Count => _dic.Count ;

    public bool ContainsKey( string key ) => _dic.ContainsKey( key ) ;

    public bool TryGetValue( string key, out Route value ) => _dic.TryGetValue( key, out value ) ;

    public Route this[ string key ] => _dic[ key ] ;

    public IEnumerable<string> Keys => _dic.Keys ;

    public IEnumerable<Route> Values => _dic.Values ;

    /// <summary>
    /// Removes all routes from both cache and document data storages.
    /// </summary>
    /// <param name="routeNames">Names of the routes which is to be dropped.</param>
    /// <returns>Count of deleted routes.</returns>
    public int Drop( IEnumerable<string> routeNames )
    {
      return routeNames.Count( Drop ) ;
    }

    /// <summary>
    /// Removes a route from both cache and document data storages.
    /// </summary>
    /// <param name="routeName">Name of the route which is to be dropped.</param>
    /// <returns>True, if the specified route is dropped.</returns>
    public bool Drop( string routeName )
    {
      if ( false == _dic.TryGetValue( routeName, out var route ) ) return false ;

      route.Delete() ;
      return true ;
    }
  }
}