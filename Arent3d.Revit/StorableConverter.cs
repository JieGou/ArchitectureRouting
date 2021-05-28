using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Linq.Expressions ;
using System.Reflection ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using MathLib ;

namespace Arent3d.Revit
{
  public interface IStorableConverter
  {
    Type GetNativeType() ;
    object NativeToCustom( Element storedElement, object nativeTypeValue ) ;
    object CustomToNative( Element storedElement, object customTypeValue ) ;
  }

  public abstract class StorableConverterBase<TCustomTypeValue, TNativeTypeValue> : IStorableConverter
  {
    Type IStorableConverter.GetNativeType() => typeof( TNativeTypeValue ) ;

    object IStorableConverter.NativeToCustom( Element storedElement, object nativeTypeValue ) => NativeToCustom( storedElement, (TNativeTypeValue) nativeTypeValue )! ;
    object IStorableConverter.CustomToNative( Element storedElement, object customTypeValue ) => CustomToNative( storedElement, (TCustomTypeValue) customTypeValue )! ;

    protected abstract TCustomTypeValue NativeToCustom( Element storedElement, TNativeTypeValue nativeTypeValue ) ;
    protected abstract TNativeTypeValue CustomToNative( Element storedElement, TCustomTypeValue customTypeValue ) ;
  }

  public abstract class StorableConverterBase<TCustomTypeValue> : StorableConverterBase<TCustomTypeValue, string>
  {
    protected sealed override TCustomTypeValue NativeToCustom( Element storedElement, string nativeTypeValue ) => Parse( storedElement, new Parser( nativeTypeValue ) ) ;
    protected sealed override string CustomToNative( Element storedElement, TCustomTypeValue customTypeValue ) => Stringify( storedElement, customTypeValue ).ToString() ;
    protected abstract TCustomTypeValue Parse( Element storedElement, Parser parser ) ;
    protected abstract Stringifier Stringify( Element storedElement, TCustomTypeValue customTypeValue ) ;
  }

  public static class StorableConverter
  {
    public static IStorableConverter? GetStorableConverter( this Type type )
    {
      if ( false == _converters.TryGetValue( type, out var converter ) ) {
        var assembly = type.Assembly ;
        if ( _loadedAssemblies.Contains( assembly ) ) return null ;

        // registering and retry
        RegisterAssembly( type.Assembly ) ;
        if ( false == _converters.TryGetValue( type, out converter ) ) return null ;
      }

      return converter ;
    }

    private static readonly HashSet<Assembly> _loadedAssemblies = new() ;

    private static readonly Dictionary<Type, IStorableConverter> _converters = new()
    {
      { typeof( bool ), new NoConverter<bool>() },
      { typeof( byte ), new NoConverter<byte>() },
      { typeof( short ), new NoConverter<short>() },
      { typeof( int ), new NoConverter<int>() },
      { typeof( float ), new NoConverter<float>() },
      { typeof( double ), new NoConverter<double>() },
      { typeof( ElementId ), new NoConverter<ElementId>() },
      { typeof( Guid ), new NoConverter<Guid>() },
      { typeof( string ), new NoConverter<string>() },
      { typeof( XYZ ), new NoConverter<XYZ>() },
      { typeof( UV ), new NoConverter<UV>() },
      { typeof( Entity ), new NoConverter<Entity>() },
      { typeof( Element ), new ElementStorableConverter() },
      { typeof( Vector3d ), new Vector3dStorableConverter() },
      { typeof( Vector2d ), new Vector2dStorableConverter() },
    } ;

    static StorableConverter()
    {
      RegisterAssembly( Assembly.GetExecutingAssembly() ) ;
    }

    private static void RegisterAssembly( Assembly assembly )
    {
      if ( false == _loadedAssemblies.Add( assembly ) ) return ;

      foreach ( var type in GetAllTypes( assembly ) ) {
        if ( false == typeof( IStorableConverter ).IsAssignableFrom( type ) ) continue ;
        if ( type.GetCustomAttribute<StorableConverterOfAttribute>() is not { } attr ) continue ;

        var converter = Instantiate( type ) ;
        if ( null == converter ) throw new InvalidOperationException( $"{type.FullName} cannot be instantiated by a constructor with no parameter." ) ;

        _converters.Add( attr.TargetType, converter ) ;
      }
    }

    private static IEnumerable<Type> GetAllTypes( Assembly assembly )
    {
      return assembly.GetTypes().SelectMany( GetAllTypes ) ;
    }

    private static IEnumerable<Type> GetAllTypes( Type type )
    {
      yield return type ;

      foreach ( var subType in type.GetNestedTypes( BindingFlags.Public | BindingFlags.NonPublic ).SelectMany( GetAllTypes ) ) {
        yield return subType ;
      }
    }

    private static IStorableConverter Instantiate( Type converterType )
    {
      IStorableConverter? conv = null ;
      foreach ( var ctor in converterType.GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ) ) {
        if ( 0 != ctor.GetParameters().Length ) continue ;

        conv = CreateConverter( ctor ) ;
      }

      if ( null == conv ) {
        throw new InvalidOperationException( $"No constructor with no args is found in {converterType.FullName}." ) ;
      }

      return conv ;
    }

    private static IStorableConverter? CreateConverter( ConstructorInfo ctor )
    {
      return Expression.Lambda<Func<IStorableConverter>>( Expression.New( ctor ) ).Compile()() ;
    }

    #region Built-in storable converters

    private class NoConverter<T> : IStorableConverter
    {
      public Type GetNativeType() => typeof( T ) ;

      public object NativeToCustom( Element storedElement, object nativeTypeValue ) => nativeTypeValue ;

      public object CustomToNative( Element storedElement, object customTypeValue ) => customTypeValue ;
    }

    private class ElementStorableConverter : StorableConverterBase<Element, ElementId>
    {
      protected override Element NativeToCustom( Element storedElement, ElementId nativeTypeValue )
      {
        return storedElement.Document.GetElement( nativeTypeValue ) ;
      }

      protected override ElementId CustomToNative( Element storedElement, Element customTypeValue )
      {
        return customTypeValue.Id ;
      }
    }

    private class Vector3dStorableConverter : StorableConverterBase<Vector3d, XYZ>
    {
      protected override Vector3d NativeToCustom( Element storedElement, XYZ nativeTypeValue )
      {
        return new Vector3d( nativeTypeValue.X, nativeTypeValue.Y, nativeTypeValue.Z ) ;
      }

      protected override XYZ CustomToNative( Element storedElement, Vector3d customTypeValue )
      {
        return new XYZ( customTypeValue.x, customTypeValue.y, customTypeValue.z ) ;
      }
    }

    private class Vector2dStorableConverter : StorableConverterBase<Vector2d, UV>
    {
      protected override Vector2d NativeToCustom( Element storedElement, UV nativeTypeValue )
      {
        return new Vector2d( nativeTypeValue.U, nativeTypeValue.V ) ;
      }

      protected override UV CustomToNative( Element storedElement, Vector2d customTypeValue )
      {
        return new UV( customTypeValue.x, customTypeValue.y ) ;
      }
    }

    #endregion
  }
}