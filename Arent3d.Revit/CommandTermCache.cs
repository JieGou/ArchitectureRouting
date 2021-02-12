using System ;
using System.Collections.Generic ;
using System.Reflection ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  internal interface ICommandTermCache
  {
    Document Document { get ; }
  }

  internal static class CommandTermCache
  {
    public static readonly Type[] ConstructorTypes = { typeof( Document ) } ;

    private static readonly List<ICacheByDocuments> AllCaches = new() ;


    public static void AddCacheDictionary( ICacheByDocuments caches )
    {
      AllCaches.Add( caches ) ;
    }

    public static void ReleaseCaches( Document document )
    {
      AllCaches.ForEach( dic => dic.Remove( document ) ) ;
    }

    public interface ICacheByDocuments
    {
      void Remove( Document document ) ;
    }
  }

  /// <summary>
  /// Base class of a caches which is active while a command is running.
  /// </summary>
  public abstract class CommandTermCache<TCache> : ICommandTermCache where TCache : CommandTermCache<TCache>
  {
    private static readonly CacheByDocuments _caches = new() ;

    static CommandTermCache()
    {
      CommandTermCache.AddCacheDictionary( _caches ) ;
    }

    /// <summary>
    /// A document related to this cache.
    /// </summary>
    public Document Document { get ; }

    /// <summary>
    /// Cache constructor.
    /// </summary>
    /// <remarks>All caches must have a constructor with only one Document parameter (may be non-public).</remarks>
    /// <param name="document"></param>
    protected CommandTermCache( Document document )
    {
      Document = document ;
    }

    public static TCache Get( Document document )
    {
      if ( false == _caches.TryGetValue( document, out var cache ) ) {
        cache = CreateCache( document ) ;
        _caches.Add( document, cache ) ;
      }

      return cache ;
    }

    public void Invalidate()
    {
      _caches.Remove( Document ) ;
    }

    private static TCache CreateCache( Document document )
    {
      var ctor = typeof( TCache ).GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, CommandTermCache.ConstructorTypes, Array.Empty<ParameterModifier>() ) ;
      if ( null == ctor ) throw new ArgumentException() ;

      return (TCache) ctor.Invoke( new object[] { document } ) ;
    }

    private class CacheByDocuments : Dictionary<Document, TCache>, CommandTermCache.ICacheByDocuments
    {
      void CommandTermCache.ICacheByDocuments.Remove( Document document ) => Remove( document ) ;
    }
  }
}