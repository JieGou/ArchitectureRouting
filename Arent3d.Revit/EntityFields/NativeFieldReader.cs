using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Linq.Expressions ;
using System.Reflection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit.EntityFields
{
  internal static class NativeFieldReader
  {
    public static object GetNativeValue( this Entity entity, string name, Type nativeType )
    {
      if ( false == _singleReaders.TryGetValue( nativeType, out var getter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      return getter( entity, name ) ;
    }
    public static IEnumerable<object> GetNativeArray( this Entity entity, string name, Type nativeType )
    {
      if ( false == _arrayReaders.TryGetValue( nativeType, out var getter ) ) throw new InvalidOperationException( $"IList<{nativeType.FullName}> cannot be stored into an entity." ) ;

      return getter( entity, name ) ;
    }
    public static IEnumerable<KeyValuePair<object, object>> GetNativeMap( this Entity entity, string name, Type nativeKeyType, Type nativeValueType )
    {
      if ( false == _mapReaders.TryGetValue( (nativeKeyType, nativeValueType), out var getter ) ) throw new InvalidOperationException( $"IDictionary<{nativeKeyType.FullName}, {nativeValueType.FullName}> cannot be stored into an entity." ) ;

      return getter( entity, name ) ;
    }

    public static object GetNativeValue( this Entity entity, string name, Type nativeType, DisplayUnitType displayUnitType )
    {
      if ( false == _singleReadersWithDisplayUnit.TryGetValue( nativeType, out var getter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      return getter( entity, name, displayUnitType ) ;
    }
    public static IEnumerable<object> GetNativeArray( this Entity entity, string name, Type nativeType, DisplayUnitType displayUnitType )
    {
      if ( false == _arrayReadersWithDisplayUnit.TryGetValue( nativeType, out var getter ) ) throw new InvalidOperationException( $"IList<{nativeType.FullName}> cannot be stored into an entity." ) ;

      return getter( entity, name, displayUnitType ) ;
    }
    public static IEnumerable<KeyValuePair<object, object>> GetNativeMap( this Entity entity, string name, Type nativeKeyType, Type nativeValueType, DisplayUnitType displayUnitType )
    {
      if ( false == _mapReadersWithDisplayUnit.TryGetValue( (nativeKeyType, nativeValueType), out var getter ) ) throw new InvalidOperationException( $"IDictionary<{nativeKeyType.FullName}, {nativeValueType.FullName}> cannot be stored into an entity." ) ;

      return getter( entity, name, displayUnitType ) ;
    }


    private static readonly IReadOnlyDictionary<Type, Func<Entity, string, object>> _singleReaders ;
    private static readonly IReadOnlyDictionary<Type, Func<Entity, string, IEnumerable<object>>> _arrayReaders ;
    private static readonly IReadOnlyDictionary<(Type, Type), Func<Entity, string, IEnumerable<KeyValuePair<object, object>>>> _mapReaders ;

    private static readonly IReadOnlyDictionary<Type, Func<Entity, string, DisplayUnitType, object>> _singleReadersWithDisplayUnit ;
    private static readonly IReadOnlyDictionary<Type, Func<Entity, string, DisplayUnitType, IEnumerable<object>>> _arrayReadersWithDisplayUnit ;
    private static readonly IReadOnlyDictionary<(Type, Type), Func<Entity, string, DisplayUnitType, IEnumerable<KeyValuePair<object, object>>>> _mapReadersWithDisplayUnit ;
  
    static NativeFieldReader()
    {
      var singleReaders = new Dictionary<Type, Func<Entity, string, object>>() ;
      var arrayReaders = new Dictionary<Type, Func<Entity, string, IEnumerable<object>>>() ;
      var mapReaders = new Dictionary<(Type, Type), Func<Entity, string, IEnumerable<KeyValuePair<object, object>>>>() ;

      var singleReadersWithDisplayUnit = new Dictionary<Type, Func<Entity, string, DisplayUnitType, object>>() ;
      var arrayReadersWithDisplayUnit = new Dictionary<Type, Func<Entity, string, DisplayUnitType, IEnumerable<object>>>() ;
      var mapReadersWithDisplayUnit = new Dictionary<(Type, Type), Func<Entity, string, DisplayUnitType, IEnumerable<KeyValuePair<object, object>>>>() ;

      var (get, getWithDisplayUnit) = GetGetMethods() ;
      if ( null != get ) {
        GenerateReaders( get, singleReaders, arrayReaders, mapReaders ) ;
      }
      if ( null != getWithDisplayUnit ) {
        GenerateReadersWithDisplayUnit( getWithDisplayUnit, singleReadersWithDisplayUnit, arrayReadersWithDisplayUnit, mapReadersWithDisplayUnit ) ;
      }

      _singleReaders = singleReaders ;
      _arrayReaders = arrayReaders ;
      _mapReaders = mapReaders ;
      _singleReadersWithDisplayUnit = singleReadersWithDisplayUnit ;
      _arrayReadersWithDisplayUnit = arrayReadersWithDisplayUnit ;
      _mapReadersWithDisplayUnit = mapReadersWithDisplayUnit ;
    }

    private static void GenerateReaders( MethodInfo get, Dictionary<Type, Func<Entity, string, object>> singleReaders, Dictionary<Type, Func<Entity, string, IEnumerable<object>>> arrayReaders, Dictionary<(Type, Type), Func<Entity, string, IEnumerable<KeyValuePair<object, object>>>> mapReaders )
    {
      var entityParameter = Expression.Parameter( typeof( Entity ) ) ;
      var fieldNameParameter = Expression.Parameter( typeof( string ) ) ;
      var parameters = new[] { entityParameter, fieldNameParameter } ;
      var arguments = new Expression[] { fieldNameParameter } ;

      GenerateReaders( get, parameters, arguments, singleReaders, arrayReaders, mapReaders ) ;
    }

    private static void GenerateReadersWithDisplayUnit( MethodInfo get, Dictionary<Type, Func<Entity, string, DisplayUnitType, object>> singleReaders, Dictionary<Type, Func<Entity, string, DisplayUnitType, IEnumerable<object>>> arrayReaders, Dictionary<(Type, Type), Func<Entity, string, DisplayUnitType, IEnumerable<KeyValuePair<object, object>>>> mapReaders )
    {
      var entityParameter = Expression.Parameter( typeof( Entity ) ) ;
      var fieldNameParameter = Expression.Parameter( typeof( string ) ) ;
      var displayUnitParameter = Expression.Parameter( typeof( DisplayUnitType ) ) ;
      var parameters = new[] { entityParameter, fieldNameParameter, displayUnitParameter } ;
      var arguments = new Expression[] { fieldNameParameter, Expression.Convert( displayUnitParameter, DisplayUnitType.NativeType ) } ;

      GenerateReaders( get, parameters, arguments, singleReaders, arrayReaders, mapReaders ) ;
    }

    private static void GenerateReaders<TSingleReader, TArrayReader, TMapReader>( MethodInfo get, ParameterExpression[] parameters, Expression[] arguments, Dictionary<Type, TSingleReader> singleReaders, Dictionary<Type, TArrayReader> arrayReaders, Dictionary<(Type, Type), TMapReader> mapReaders )
    {
      foreach ( var type in NativeFieldTypes.AcceptableTypes ) {
        singleReaders.Add( type, GetReader<TSingleReader>( get.MakeGenericMethod( type ), parameters, arguments, GetSingleConverter ) ) ;
        arrayReaders.Add( type, GetReader<TArrayReader>( get.MakeGenericMethod( typeof( IList<> ).MakeGenericType( type ) ), parameters, arguments, GetArrayConverter ) ) ;

        foreach ( var keyType in NativeFieldTypes.AcceptableTypesForKey ) {
          mapReaders.Add( (keyType, type), GetReader<TMapReader>( get.MakeGenericMethod( typeof( IDictionary<,> ).MakeGenericType( keyType, type ) ), parameters, arguments, GetMapConverter ) ) ;
        }
      }
    }

    private static T GetReader<T>( MethodInfo method, ParameterExpression[] parameters, Expression[] arguments, Func<Expression, Expression> resultConverter )
    {
      return Expression.Lambda<T>( resultConverter( Expression.Call( parameters[ 0 ], method, arguments ) ), parameters ).Compile() ;
    }

    private static Expression GetSingleConverter( Expression expression )
    {
      return Expression.Convert( expression, typeof( object ) ) ;
    }
    private static Expression GetArrayConverter( Expression expression )
    {
      var method = typeof( NativeFieldConverter ).GetMethod( "FromList" )!.MakeGenericMethod( expression.Type.GetGenericArguments() ) ;
      return Expression.Call( method, expression ) ;
    }
    private static Expression GetMapConverter( Expression expression )
    {
      var method = typeof( NativeFieldConverter ).GetMethod( "FromDictionary" )!.MakeGenericMethod( expression.Type.GetGenericArguments() ) ;
      return Expression.Call( method, expression ) ;
    }

    private static class NativeFieldConverter
    {
      public static IEnumerable<object> FromList<T>( IList<T> values ) where T : notnull
      {
        return values.OfType<object>() ;
      }

      public static IEnumerable<KeyValuePair<object, object>> FromDictionary<TKey, TValue>( IDictionary<TKey, TValue> values ) where TKey : notnull where TValue : notnull
      {
        foreach ( var (key, value) in values ) {
          yield return new KeyValuePair<object, object>( key, value ) ;
        }
      }
    }

    private static (MethodInfo? Get, MethodInfo? GetWithDisplayUnit) GetGetMethods()
    {
      MethodInfo? get = null ;
      MethodInfo? getWithDisplayUnit = null ;

      foreach ( var method in typeof( Entity ).GetMethods( BindingFlags.Instance | BindingFlags.Public ) ) {
        if ( false == method.IsGenericMethod || false == method.IsGenericMethodDefinition || false == method.ContainsGenericParameters || "Get" != method.Name ) continue ;

        var parameters = method.GetParameters() ;
        if ( 1 == parameters.Length ) {
          if ( typeof( string ) != parameters[ 0 ].ParameterType ) continue ;
          get = method ;
        }
        else if ( 2 == parameters.Length ) {
          if ( typeof( string ) != parameters[ 0 ].ParameterType ) continue ;
          if ( DisplayUnitType.NativeType != parameters[ 1 ].ParameterType ) continue ;

          getWithDisplayUnit = method ;
        }
      }

      return ( get, getWithDisplayUnit ) ;
    }
  }
}