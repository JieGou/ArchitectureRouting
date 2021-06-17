using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Linq.Expressions ;
using System.Reflection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Expression = System.Linq.Expressions.Expression ;

namespace Arent3d.Revit.EntityFields
{
  internal static class NativeFieldWriter
  {
    public static void SetNativeValue( this Entity entity, string name, Type nativeType, object obj )
    {
      if ( false == _singleWriters.TryGetValue( nativeType, out var setter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      setter( entity, name, obj ) ;
    }
    public static void SetNativeArray( this Entity entity, string name, Type nativeType, IEnumerable<object> objs )
    {
      if ( false == _arrayWriters.TryGetValue( nativeType, out var setter ) ) throw new InvalidOperationException( $"IList<{nativeType.FullName}> cannot be stored into an entity." ) ;

      setter( entity, name, objs ) ;
    }
    public static void SetNativeMap( this Entity entity, string name, Type nativeKeyType, Type nativeValueType, IEnumerable<KeyValuePair<object, object>> objs )
    {
      if ( false == _mapWriters.TryGetValue( (nativeKeyType, nativeValueType), out var setter ) ) throw new InvalidOperationException( $"IDictionary<{nativeKeyType.FullName}, {nativeValueType.FullName}> cannot be stored into an entity." ) ;

      setter( entity, name, objs ) ;
    }

    public static void SetNativeValue( this Entity entity, string name, Type nativeType, object obj, DisplayUnitType displayUnitType )
    {
      if ( false == _singleWritersWithDisplayUnit.TryGetValue( nativeType, out var setter ) ) throw new InvalidOperationException( $"{nativeType.FullName} cannot be stored into an entity." ) ;

      setter( entity, name, obj, displayUnitType ) ;
    }
    public static void SetNativeArray( this Entity entity, string name, Type nativeType, IEnumerable<object> objs, DisplayUnitType displayUnitType )
    {
      if ( false == _arrayWritersWithDisplayUnit.TryGetValue( nativeType, out var setter ) ) throw new InvalidOperationException( $"IList<{nativeType.FullName}> cannot be stored into an entity." ) ;

      setter( entity, name, objs, displayUnitType ) ;
    }
    public static void SetNativeMap( this Entity entity, string name, Type nativeKeyType, Type nativeValueType, IEnumerable<KeyValuePair<object, object>> objs, DisplayUnitType displayUnitType )
    {
      if ( false == _mapWritersWithDisplayUnit.TryGetValue( (nativeKeyType, nativeValueType), out var setter ) ) throw new InvalidOperationException( $"IDictionary<{nativeKeyType.FullName}, {nativeValueType.FullName}> cannot be stored into an entity." ) ;

      setter( entity, name, objs, displayUnitType ) ;
    }



    private static readonly IReadOnlyDictionary<Type, Action<Entity, string, object>> _singleWriters ;
    private static readonly IReadOnlyDictionary<Type, Action<Entity, string, IEnumerable<object>>> _arrayWriters ;
    private static readonly IReadOnlyDictionary<(Type, Type), Action<Entity, string, IEnumerable<KeyValuePair<object, object>>>> _mapWriters ;

    private static readonly IReadOnlyDictionary<Type, Action<Entity, string, object, DisplayUnitType>> _singleWritersWithDisplayUnit ;
    private static readonly IReadOnlyDictionary<Type, Action<Entity, string, IEnumerable<object>, DisplayUnitType>> _arrayWritersWithDisplayUnit ;
    private static readonly IReadOnlyDictionary<(Type, Type), Action<Entity, string, IEnumerable<KeyValuePair<object, object>>, DisplayUnitType>> _mapWritersWithDisplayUnit ;
  
    static NativeFieldWriter()
    {
      var singleWriters = new Dictionary<Type, Action<Entity, string, object>>() ;
      var arrayWriters = new Dictionary<Type, Action<Entity, string, IEnumerable<object>>>() ;
      var mapWriters = new Dictionary<(Type, Type), Action<Entity, string, IEnumerable<KeyValuePair<object, object>>>>() ;

      var singleWritersWithDisplayUnit = new Dictionary<Type, Action<Entity, string, object, DisplayUnitType>>() ;
      var arrayWritersWithDisplayUnit = new Dictionary<Type, Action<Entity, string, IEnumerable<object>, DisplayUnitType>>() ;
      var mapWritersWithDisplayUnit = new Dictionary<(Type, Type), Action<Entity, string, IEnumerable<KeyValuePair<object, object>>, DisplayUnitType>>() ;

      var (set, setWithDisplayUnit) = GetSetMethods() ;
      if ( null != set ) {
        GenerateWriters( set, singleWriters, arrayWriters, mapWriters ) ;
      }
      if ( null != setWithDisplayUnit ) {
        GenerateWritersWithDisplayUnit( setWithDisplayUnit, singleWritersWithDisplayUnit, arrayWritersWithDisplayUnit, mapWritersWithDisplayUnit ) ;
      }

      _singleWriters = singleWriters ;
      _arrayWriters = arrayWriters ;
      _mapWriters = mapWriters ;
      _singleWritersWithDisplayUnit = singleWritersWithDisplayUnit ;
      _arrayWritersWithDisplayUnit = arrayWritersWithDisplayUnit ;
      _mapWritersWithDisplayUnit = mapWritersWithDisplayUnit ;
    }

    private static void GenerateWriters( MethodInfo set, Dictionary<Type, Action<Entity, string, object>> singleWriters, Dictionary<Type, Action<Entity, string, IEnumerable<object>>> arrayWriters, Dictionary<(Type, Type), Action<Entity, string, IEnumerable<KeyValuePair<object, object>>>> mapWriters )
    {
      var entityParameter = Expression.Parameter( typeof( Entity ) ) ;
      var fieldNameParameter = Expression.Parameter( typeof( string ) ) ;
      var parameters = new[] { entityParameter, fieldNameParameter, null! } ;
      var arguments = new Expression[] { fieldNameParameter, null! } ;

      GenerateWriters( set, parameters, arguments, singleWriters, arrayWriters, mapWriters ) ;
    }

    private static void GenerateWritersWithDisplayUnit( MethodInfo get, Dictionary<Type, Action<Entity, string, object, DisplayUnitType>> singleWriters, Dictionary<Type, Action<Entity, string, IEnumerable<object>, DisplayUnitType>> arrayWriters, Dictionary<(Type, Type), Action<Entity, string, IEnumerable<KeyValuePair<object, object>>, DisplayUnitType>> mapWriters )
    {
      var entityParameter = Expression.Parameter( typeof( Entity ) ) ;
      var fieldNameParameter = Expression.Parameter( typeof( string ) ) ;
      var displayUnitParameter = Expression.Parameter( typeof( DisplayUnitType ) ) ;
      var parameters = new[] { entityParameter, fieldNameParameter, null!, displayUnitParameter } ;
      var arguments = new Expression[] { fieldNameParameter, null!, Expression.Convert( displayUnitParameter, DisplayUnitType.NativeType ) } ;

      GenerateWriters( get, parameters, arguments, singleWriters, arrayWriters, mapWriters ) ;
    }

    private static void GenerateWriters<TSingleWriter, TArrayWriter, TMapWriter>( MethodInfo set, ParameterExpression[] parameters, Expression[] arguments, Dictionary<Type, TSingleWriter> singleWriters, Dictionary<Type, TArrayWriter> arrayWriters, Dictionary<(Type, Type), TMapWriter> mapWriters )
    {
      var parametersForSingle = ReplaceParameter( parameters, typeof( object ) ) ;
      var parametersForArray = ReplaceParameter( parameters, typeof( IEnumerable<object> ) ) ;
      var parametersForMap = ReplaceParameter( parameters, typeof( IEnumerable<KeyValuePair<object, object>> ) ) ;

      var argumentsForSingle = (Expression[]) arguments.Clone() ;
      var argumentsForArray = (Expression[]) arguments.Clone() ;
      var argumentsForMap = (Expression[]) arguments.Clone() ;
      
      foreach ( var type in NativeFieldTypes.AcceptableTypes ) {
        argumentsForSingle[ 1 ] = ModifySingleArgument( parametersForSingle[ 2 ], type ) ;
        singleWriters.Add( type, GetWriter<TSingleWriter>( set.MakeGenericMethod( type ), parametersForSingle, argumentsForSingle ) ) ;
        argumentsForArray[ 1 ] = ModifyArrayArgument( parametersForArray[ 2 ], type ) ;
        arrayWriters.Add( type, GetWriter<TArrayWriter>( set.MakeGenericMethod( typeof( IList<> ).MakeGenericType( type ) ), parametersForArray, argumentsForArray ) ) ;

        foreach ( var keyType in NativeFieldTypes.AcceptableTypesForKey ) {
          argumentsForMap[ 1 ] = ModifyMapArgument( parametersForMap[ 2 ], keyType, type ) ;
          mapWriters.Add( (keyType, type), GetWriter<TMapWriter>( set.MakeGenericMethod( typeof( IDictionary<,> ).MakeGenericType( keyType, type ) ), parametersForMap, argumentsForMap ) ) ;
        }
      }
    }

    private static ParameterExpression[] ReplaceParameter( ParameterExpression[] parameter, Type type )
    {
      var result = (ParameterExpression[]) parameter.Clone() ;
      result[ 2 ] = Expression.Parameter( type ) ;
      return result ;
    }

    private static T GetWriter<T>( MethodInfo method, ParameterExpression[] parameters, Expression[] arguments )
    {
      return Expression.Lambda<T>( Expression.Call( parameters[ 0 ], method, arguments ), parameters ).Compile() ;
    }

    private static Expression ModifySingleArgument( ParameterExpression expression, Type type )
    {
      return Expression.Convert( expression, type ) ;
    }
    private static Expression ModifyArrayArgument( ParameterExpression expression, Type type )
    {
      var method = typeof( NativeFieldConverter ).GetMethod( "ToList" )!.MakeGenericMethod( type ) ;
      return Expression.Call( method, expression ) ;
    }
    private static Expression ModifyMapArgument( ParameterExpression expression, Type keyType, Type valueType )
    {
      var method = typeof( NativeFieldConverter ).GetMethod( "ToDictionary" )!.MakeGenericMethod( keyType, valueType ) ;
      return Expression.Call( method, expression ) ;
    }

    private static class NativeFieldConverter
    {
      public static IList<T> ToList<T>( IEnumerable<object> values )
      {
        return values.Cast<T>().ToList() ;
      }

      public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>( IEnumerable<KeyValuePair<object, object>> values )
      {
        var dic = new Dictionary<TKey, TValue>() ;
        foreach ( var (key, value) in values ) {
          dic.Add( (TKey) key, (TValue) value ) ;
        }

        return dic ;
      }
    }

    private static (MethodInfo? Set, MethodInfo? SetWithDisplayUnit) GetSetMethods()
    {
      MethodInfo? set = null ;
      MethodInfo? setWithDisplayUnit = null ;

      foreach ( var method in typeof( Entity ).GetMethods( BindingFlags.Instance | BindingFlags.Public ) ) {
        if ( false == method.IsGenericMethod || false == method.IsGenericMethodDefinition || false == method.ContainsGenericParameters || "Set" != method.Name ) continue ;

        var parameters = method.GetParameters() ;
        if ( 2 == parameters.Length ) {
          if ( typeof( string ) != parameters[ 0 ].ParameterType ) continue ;
          set = method ;
        }
        else if ( 3 == parameters.Length ) {
          if ( typeof( string ) != parameters[ 0 ].ParameterType ) continue ;
          if ( DisplayUnitType.NativeType != parameters[ 2 ].ParameterType ) continue ;

          setWithDisplayUnit = method ;
        }
      }

      return ( set, setWithDisplayUnit ) ;
    }
  }
}