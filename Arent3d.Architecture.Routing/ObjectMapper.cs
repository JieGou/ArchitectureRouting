using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Linq.Expressions ;
using System.Reflection ;

namespace Arent3d.Architecture.Routing
{
  /// <summary>
  /// Abstract class of Revit-object to auto-routing-object mapper.
  /// </summary>
  /// <typeparam name="TMapper">A derived class itself.</typeparam>
  /// <typeparam name="TRevitObject">A Revit class.</typeparam>
  /// <typeparam name="TRoutingObject">An auto routing class.</typeparam>
  public class ObjectMapper<TMapper, TRevitObject, TRoutingObject>
    where TMapper : ObjectMapper<TMapper, TRevitObject, TRoutingObject>
    where TRevitObject : class
    where TRoutingObject : IMappedObject<TRevitObject>
  {
    private static TMapper? _instance ;

    /// <summary>
    /// Returns the unique instance of an object mapper.
    /// </summary>
    public static TMapper Instance
    {
      get
      {
        if ( null == _instance ) {
          _instance = CreateInstance() ;
        }

        return _instance ;
      }
    }

    private static TMapper CreateInstance()
    {
      // Do not use `new TMapper()` because TMapper's constructors is to be a private constructor.
      var constructor = typeof( TMapper ).GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null ) ;
      if ( null == constructor ) {
        throw new InvalidOperationException( $"{typeof( TMapper ).FullName}: Constructor with no parameters is not found. Cannot use this class." ) ;
      }

      return (TMapper) constructor.Invoke( null ) ;
    }

    private readonly Dictionary<TRevitObject, TRoutingObject> _dic = new() ;

    protected ObjectMapper()
    {
    }

    /// <summary>
    /// Gets related auto-routing object from a Revit object. Not found, this method generates a new auto-routing object from it.
    /// </summary>
    /// <param name="revitObject">Revit object.</param>
    /// <returns>An auto-routing object related from <see cref="revitObject"/>.</returns>
    public TRoutingObject Get( TRevitObject revitObject )
    {
      if ( false == _dic.TryGetValue( revitObject, out var routingObject ) ) {
        routingObject = CreateRoutingObjectFrom( revitObject ) ;
        _dic.Add( revitObject, routingObject ) ;
      }

      return routingObject ;
    }

    private static Func<TRevitObject, TRoutingObject>? _generator ;

    private static Func<TRevitObject, TRoutingObject> GetGenerator()
    {
      if ( null == _generator ) {
        _generator = CreateGenerator() ;
      }

      return _generator ;
    }

    private static Func<TRevitObject, TRoutingObject> CreateGenerator()
    {
      // Do not use `new TMapper()` because TMapper's constructors is to be a private constructor.
      var constructor = typeof( TRoutingObject ).GetConstructor( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] { typeof( TRevitObject ) }, null ) ;
      if ( null == constructor ) {
        throw new InvalidOperationException( $"{typeof( TRoutingObject ).FullName}: Constructor with only one {typeof( TRevitObject ).FullName} parameters is not found. Cannot use this class." ) ;
      }

      var param = Expression.Parameter( typeof( TRevitObject ) ) ;
      return Expression.Lambda<Func<TRevitObject, TRoutingObject>>( Expression.New( constructor, param ), param ).Compile() ;
    }

    /// <summary>
    /// Generates a new auto-routing object from a Revit object.
    /// </summary>
    /// <param name="revitObject">Revit object</param>
    /// <returns>A new auto-routing object related from <see cref="revitObject"/>.</returns>
    protected virtual TRoutingObject CreateRoutingObjectFrom( TRevitObject revitObject )
    {
      return GetGenerator()( revitObject ) ;
    }
  }
}