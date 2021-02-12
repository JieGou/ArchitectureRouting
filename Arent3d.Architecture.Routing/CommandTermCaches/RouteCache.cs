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
      _dic = document.GetAllStorables<Route>().ToDictionary( route => route.RouteId ) ;
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
  }
}