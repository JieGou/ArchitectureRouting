using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit.EntityFields
{
  internal static class NativeFieldWriter
  {
    public static void SetNativeValue( this Entity entity, string name, Type nativeType, object obj )
    {
      if ( false == _singleWriter.TryGetValue( nativeType, out var setter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      setter( entity, name, obj ) ;
    }
    
    public static void SetNativeArray( this Entity entity, string name, Type nativeType, IEnumerable<object> objs )
    {
      if ( false == _arrayWriter.TryGetValue( nativeType, out var setter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      setter( entity, name, objs ) ;
    }

    private static readonly IReadOnlyDictionary<Type, Action<Entity, string, object>> _singleWriter = new Dictionary<Type, Action<Entity, string, object>>
    {
      { typeof( bool ), ( e, name, obj ) => e.Set( name, (bool) obj ) },
      { typeof( byte ), ( e, name, obj ) => e.Set( name, (byte) obj ) },
      { typeof( short ), ( e, name, obj ) => e.Set( name, (short) obj ) },
      { typeof( int ), ( e, name, obj ) => e.Set( name, (int) obj ) },
      { typeof( float ), ( e, name, obj ) => e.Set( name, (float) obj ) },
      { typeof( double ), ( e, name, obj ) => e.Set( name, (double) obj ) },
      { typeof( ElementId ), ( e, name, obj ) => e.Set( name, (ElementId) obj ) },
      { typeof( Guid ), ( e, name, obj ) => e.Set( name, (Guid) obj ) },
      { typeof( string ), ( e, name, obj ) => e.Set( name, (string) obj ) },
      { typeof( XYZ ), ( e, name, obj ) => e.Set( name, (XYZ) obj ) },
      { typeof( UV ), ( e, name, obj ) => e.Set( name, (UV) obj ) },
      { typeof( Entity ), ( e, name, obj ) => e.Set( name, (Entity) obj ) },
    } ;

    private static readonly IReadOnlyDictionary<Type, Action<Entity, string, IEnumerable<object>>> _arrayWriter = new Dictionary<Type, Action<Entity, string, IEnumerable<object>>>
    {
      { typeof( bool ), ( e, name, obj ) => e.Set( name, ToList<bool>( obj ) ) },
      { typeof( byte ), ( e, name, obj ) => e.Set( name, ToList<byte>( obj ) ) },
      { typeof( short ), ( e, name, obj ) => e.Set( name, ToList<short>( obj ) ) },
      { typeof( int ), ( e, name, obj ) => e.Set( name, ToList<int>( obj ) ) },
      { typeof( float ), ( e, name, obj ) => e.Set( name, ToList<float>( obj ) ) },
      { typeof( double ), ( e, name, obj ) => e.Set( name, ToList<double>( obj ) ) },
      { typeof( ElementId ), ( e, name, obj ) => e.Set( name, ToList<ElementId>( obj ) ) },
      { typeof( Guid ), ( e, name, obj ) => e.Set( name, ToList<Guid>( obj ) ) },
      { typeof( string ), ( e, name, obj ) => e.Set( name, ToList<string>( obj ) ) },
      { typeof( XYZ ), ( e, name, obj ) => e.Set( name, ToList<XYZ>( obj ) ) },
      { typeof( UV ), ( e, name, obj ) => e.Set( name, ToList<UV>( obj ) ) },
      { typeof( Entity ), ( e, name, obj ) => e.Set( name, ToList<Entity>( obj ) ) },
    } ;

    private static IList<T> ToList<T>( IEnumerable<object> values )
    {
      return values.Cast<T>().ToList() ;
    }
  }
}