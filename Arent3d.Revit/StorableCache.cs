using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Events ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Expression = System.Linq.Expressions.Expression ;

namespace Arent3d.Revit
{
  internal static class StorableCache
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
  public abstract class StorableCache<TCache, TStorable> : IReadOnlyDictionary<string, TStorable>
    where TCache : StorableCache<TCache, TStorable> where TStorable : StorableBase
  {
    private static readonly CacheByDocuments _caches = new() ;

    static StorableCache()
    {
      StorableCache.AddCacheDictionary( _caches ) ;
    }

    /// <summary>
    /// A document related to this cache.
    /// </summary>
    public Document Document { get ; }

    private readonly Dictionary<string, TStorable> _dic ;

    /// <summary>
    /// Cache constructor.
    /// </summary>
    /// <remarks>All caches must have a constructor with only one Document parameter (may be non-public).</remarks>
    /// <param name="document"></param>
    protected StorableCache( Document document )
    {
      Document = document ;

      _dic = document.GetAllStorables<TStorable>().ToDictionary( storable => storable.Name ) ;
    }

    public static TCache Get( Document document )
    {
      if ( false == _caches.TryGetValue( document, out var cache ) ) {
        cache = CreateCache( document ) ;
        _caches.Add( document, cache ) ;
      }

      return cache ;
    }

    private static TCache CreateCache( Document document )
    {
      if ( 0 == _caches.Count ) {
        AddEvents( document.Application ) ;
      }

      return CacheGenerator<TCache, TStorable>.Generate( document ) ;
    }

    private class CacheByDocuments : Dictionary<Document, TCache>, StorableCache.ICacheByDocuments
    {
      void StorableCache.ICacheByDocuments.Remove( Document document )
      {
        if ( false == Remove( document ) ) return ;

        if ( 0 == _caches.Count ) {
          RemoveEvents( document.Application ) ;
        }
      }
    }

    /// <summary>
    /// Generate and register new storable object if needed.
    /// </summary>
    /// <param name="name">Name of the storable object.</param>
    /// <returns>Storable object.</returns>
    public TStorable FindOrCreate( string name )
    {
      if ( _dic.TryGetValue( name, out var storable ) && storable.OwnerElement.GetValidId().IsValid() ) return storable ;

      storable = CreateNewStorable( Document, name ) ;
      _dic[ name ] = storable ;
      return storable ;
    }

    protected abstract TStorable CreateNewStorable( Document document, string name ) ;

    /// <summary>
    /// Removes all storable objects from both cache and document data storages.
    /// </summary>
    /// <param name="names">Names of the storable objects which is to be dropped.</param>
    /// <returns>Count of deleted storable objects.</returns>
    public int Drop( IEnumerable<string> names )
    {
      return names.Count( Drop ) ;
    }

    /// <summary>
    /// Removes a storable object from both cache and document data storages.
    /// </summary>
    /// <param name="name">Name of the storable object which is to be dropped.</param>
    /// <returns>True, if the specified storable object is dropped.</returns>
    public bool Drop( string name )
    {
      if ( false == _dic.TryGetValue( name, out var storable ) ) return false ;

      storable.Delete() ;
      return true ;
    }

    #region Auto update

    private static bool _documentChangedEventRegistered = false ;
    
    private static void AddEvents( Application application )
    {
      if ( false == _documentChangedEventRegistered ) {
        _documentChangedEventRegistered = true ;
        application.DocumentChanged += ModifyStorableCache ;
      }
    }

    private static void RemoveEvents( Application application )
    {
      // Do nothing
    }

    private static void ModifyStorableCache( object sender, DocumentChangedEventArgs e )
    {
      var document = e.GetDocument() ;
      if ( e.GetAddedElementIds().Concat( e.GetModifiedElementIds() ).Any( elmId => ExistsStorable( document, elmId ) ) ) {
        _caches.Remove( document ) ;
      }
    }

    private static bool ExistsStorable( Document document, ElementId elementId )
    {
      return document.GetElementById<DataStorage>( elementId )?.HasStorable<TStorable>() ?? false ;
    }

    #endregion

    #region IDictionary

    public IEnumerator<KeyValuePair<string, TStorable>> GetEnumerator()
    {
      return _dic.GetEnumerator() ;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator() ;

    public int Count => _dic.Count ;

    public bool ContainsKey( string key ) => _dic.ContainsKey( key ) ;

    public bool TryGetValue( string key, out TStorable value ) => _dic.TryGetValue( key, out value ) ;

    public TStorable this[ string key ] => _dic[ key ] ;

    public IEnumerable<string> Keys => _dic.Keys ;

    public IEnumerable<TStorable> Values => _dic.Values ;

    #endregion
  }

  internal static class CacheGenerator<TCache, TStorable>
    where TCache : StorableCache<TCache, TStorable> where TStorable : StorableBase
  {
    private static readonly Func<Document, TCache> _cacheGenerator = CreateCacheGenerator() ;

    private static Func<Document, TCache> CreateCacheGenerator()
    {
      var ctor = typeof( TCache ).GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, StorableCache.ConstructorTypes, Array.Empty<ParameterModifier>() ) ;
      if ( null == ctor ) throw new ArgumentException() ;

      var documentParam = Expression.Parameter( typeof( Document ) ) ;
      return Expression.Lambda<Func<Document, TCache>>( Expression.New( ctor, documentParam ), documentParam ).Compile() ;
    }

    public static TCache Generate( Document document )
    {
      return _cacheGenerator( document ) ;
    }
  }
}