using System ;
using System.Collections.Generic ;
using Arent3d.Revit.EntityFields ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  public class FieldReader
  {
    private readonly Element _element ;
    private readonly Entity _entity ;
    
    internal FieldReader( Element element, Entity entity )
    {
      _element = element ;
      _entity = entity ;
    }

    public TFieldType GetSingle<TFieldType>( string name )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      return (TFieldType) converter.NativeToCustom( _element, _entity.GetNativeValue( name, converter.GetNativeType() ) ) ;
    }
    public IEnumerable<TFieldType> GetArray<TFieldType>( string name )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      foreach ( var value in _entity.GetNativeArray( name, converter.GetNativeType() ) ) {
        yield return (TFieldType) converter.NativeToCustom( _element, value ) ;
      }
    }
    public IEnumerable<KeyValuePair<TKeyType, TValueType>> GetMap<TKeyType, TValueType>( string name )
    {
      var keyConverter = typeof( TKeyType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} is not acceptable." ) ;
      if ( false == NativeFieldTypes.IsAcceptableForKey( keyConverter.GetNativeType() ) ) throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} cannot be a key." ) ;

      var valueConverter = typeof( TValueType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TValueType ).FullName} is not acceptable." ) ;

      foreach ( var (key, value) in _entity.GetNativeMap( name, keyConverter.GetNativeType(), valueConverter.GetNativeType() ) ) {
        yield return new KeyValuePair<TKeyType, TValueType>( (TKeyType) keyConverter.NativeToCustom( _element, key ), (TValueType) valueConverter.NativeToCustom( _element, value ) ) ;
      }
    }

    public TFieldType GetSingle<TFieldType>( string name, DisplayUnitType displayUnitType )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      return (TFieldType) converter.NativeToCustom( _element, _entity.GetNativeValue( name, converter.GetNativeType(), displayUnitType ) ) ;
    }
    public IEnumerable<TFieldType> GetArray<TFieldType>( string name, DisplayUnitType displayUnitType )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      foreach ( var value in _entity.GetNativeArray( name, converter.GetNativeType(), displayUnitType ) ) {
        yield return (TFieldType) converter.NativeToCustom( _element, value ) ;
      }
    }
    public IEnumerable<KeyValuePair<TKeyType, TValueType>> GetMap<TKeyType, TValueType>( string name, DisplayUnitType displayUnitType )
    {
      var keyConverter = typeof( TKeyType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} is not acceptable." ) ;
      if ( false == NativeFieldTypes.IsAcceptableForKey( keyConverter.GetNativeType() ) ) throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} cannot be a key." ) ;

      var valueConverter = typeof( TValueType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TValueType ).FullName} is not acceptable." ) ;

      foreach ( var (key, value) in _entity.GetNativeMap( name, keyConverter.GetNativeType(), valueConverter.GetNativeType(), displayUnitType ) ) {
        yield return new KeyValuePair<TKeyType, TValueType>( (TKeyType) keyConverter.NativeToCustom( _element, key ), (TValueType) valueConverter.NativeToCustom( _element, value ) ) ;
      }
    }
  }
}