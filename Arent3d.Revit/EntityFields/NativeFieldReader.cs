using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit.EntityFields
{
  internal static class NativeFieldReader
  {
    public static object GetNativeValue( this Entity entity, string name, Type nativeType )
    {
      if ( false == _singleReader.TryGetValue( nativeType, out var getter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      return getter( entity, name ) ;
    }
    
    public static IEnumerable<object> GetNativeArray( this Entity entity, string name, Type nativeType )
    {
      if ( false == _arrayReader.TryGetValue( nativeType, out var getter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      return getter( entity, name ).Cast<object>() ;
    }

    private static readonly IReadOnlyDictionary<Type, Func<Entity, string, object>> _singleReader = new Dictionary<Type, Func<Entity, string, object>>
    {
      { typeof( bool ), ( e, name ) => e.Get<bool>( name ) },
      { typeof( byte ), ( e, name ) => e.Get<byte>( name ) },
      { typeof( short ), ( e, name ) => e.Get<short>( name ) },
      { typeof( int ), ( e, name ) => e.Get<int>( name ) },
      { typeof( float ), ( e, name ) => e.Get<float>( name ) },
      { typeof( double ), ( e, name ) => e.Get<double>( name ) },
      { typeof( ElementId ), ( e, name ) => e.Get<ElementId>( name ) },
      { typeof( Guid ), ( e, name ) => e.Get<Guid>( name ) },
      { typeof( string ), ( e, name ) => e.Get<string>( name ) },
      { typeof( XYZ ), ( e, name ) => e.Get<XYZ>( name ) },
      { typeof( UV ), ( e, name ) => e.Get<UV>( name ) },
      { typeof( Entity ), ( e, name ) => e.Get<Entity>( name ) },
    } ;

    private static readonly IReadOnlyDictionary<Type, Func<Entity, string, IEnumerable>> _arrayReader = new Dictionary<Type, Func<Entity, string, IEnumerable>>
    {
      { typeof( bool ), ( e, name ) => e.Get<IList<bool>>( name ) },
      { typeof( byte ), ( e, name ) => e.Get<IList<byte>>( name ) },
      { typeof( short ), ( e, name ) => e.Get<IList<short>>( name ) },
      { typeof( int ), ( e, name ) => e.Get<IList<int>>( name ) },
      { typeof( float ), ( e, name ) => e.Get<IList<float>>( name ) },
      { typeof( double ), ( e, name ) => e.Get<IList<double>>( name ) },
      { typeof( ElementId ), ( e, name ) => e.Get<IList<ElementId>>( name ) },
      { typeof( Guid ), ( e, name ) => e.Get<IList<Guid>>( name ) },
      { typeof( string ), ( e, name ) => e.Get<IList<string>>( name ) },
      { typeof( XYZ ), ( e, name ) => e.Get<IList<XYZ>>( name ) },
      { typeof( UV ), ( e, name ) => e.Get<IList<UV>>( name ) },
      { typeof( Entity ), ( e, name ) => e.Get<IList<Entity>>( name ) },
    } ;
  }
}