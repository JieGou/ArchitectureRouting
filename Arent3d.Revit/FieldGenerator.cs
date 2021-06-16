using System ;
using System.Reflection ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Revit
{
  public class FieldGenerator
  {
    private readonly SchemaBuilder _builder ;
    private Schema? _schema = null ;

    public FieldGenerator( Type type )
    {
      _builder = new SchemaBuilder( type.GUID ) ;
      _builder.SetSchemaName( type.FullName.Replace( ".", "_" ) ) ;

      foreach ( var attr in type.GetCustomAttributes<StorableVisibilityAttribute>() ) {
        if ( attr.ForRead ) {
          _builder.SetReadAccessLevel( attr.AccessLevel ) ;
        }
        if ( attr.ForWrite ) {
          _builder.SetWriteAccessLevel( attr.AccessLevel ) ;
        }

        if ( false == string.IsNullOrEmpty( attr.VendorId ) ) {
          _builder.SetVendorId( attr.VendorId ) ;
        }

        if ( Guid.Empty != attr.ApplicationGuid ) {
          _builder.SetApplicationGUID( attr.ApplicationGuid ) ;
        }
      }
    }
    
    internal Schema CreateSchema()
    {
      return _schema ??= _builder.Finish() ;
    }

    public void SetSingle<TFieldType>( string fieldName )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      var field = _builder.AddSimpleField( fieldName, converter.GetNativeType() ) ;
      if ( field.NeedsUnits() ) throw new InvalidOperationException( "Needs units. Use SetArray<TFieldType>( string, SpecType )" ) ;
    }

    public void SetArray<TFieldType>( string fieldName )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      var field = _builder.AddArrayField( fieldName, converter.GetNativeType() ) ;
      if ( field.NeedsUnits() ) throw new InvalidOperationException( "Needs units. Use SetArray<TFieldType>( string, SpecType )" ) ;
    }

    public void SetMap<TKeyType, TValueType>( string fieldName )
    {
      if ( typeof( bool ) != typeof( TKeyType ) && typeof( byte ) != typeof( TKeyType ) && typeof( short ) != typeof( TKeyType ) && typeof( long ) != typeof( TKeyType ) && typeof( Guid ) != typeof( TKeyType ) && typeof( string ) != typeof( TKeyType ) && typeof( ElementId ) != typeof( TKeyType ) ) {
        throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} cannot be a key." ) ;
      }

      var keyConverter = typeof( TKeyType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} is not acceptable." ) ;
      var valueConverter = typeof( TValueType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TValueType ).FullName} is not acceptable." ) ;

      var field = _builder.AddMapField( fieldName, keyConverter.GetNativeType(), valueConverter.GetNativeType() ) ;
      if ( field.NeedsUnits() ) throw new InvalidOperationException( "Needs units. Use SetMap<TKeyType, TValueType>( string, SpecType )") ;
    }

    public void SetSingle<TFieldType>( string fieldName, SpecType specType )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      var field = _builder.AddSimpleField( fieldName, converter.GetNativeType() ) ;
      if ( false == field.NeedsUnits() ) throw new InvalidOperationException( "Needs units. Use SetArray<TFieldType>( string )" ) ;
      SetSpec( field, specType ) ;
    }

    public void SetArray<TFieldType>( string fieldName, SpecType specType )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      var field = _builder.AddArrayField( fieldName, converter.GetNativeType() ) ;
      if ( false == field.NeedsUnits() ) throw new InvalidOperationException( "Needs units. Use SetArray<TFieldType>( string )" ) ;
      SetSpec( field, specType ) ;
    }

    public void SetMap<TKeyType, TValueType>( string fieldName, SpecType specType )
    {
      if ( typeof( bool ) != typeof( TKeyType ) && typeof( byte ) != typeof( TKeyType ) && typeof( short ) != typeof( TKeyType ) && typeof( long ) != typeof( TKeyType ) && typeof( Guid ) != typeof( TKeyType ) && typeof( string ) != typeof( TKeyType ) && typeof( ElementId ) != typeof( TKeyType ) ) {
        throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} cannot be a key." ) ;
      }

      var keyConverter = typeof( TKeyType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} is not acceptable." ) ;
      var valueConverter = typeof( TValueType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TValueType ).FullName} is not acceptable." ) ;
      
      var field = _builder.AddMapField( fieldName, keyConverter.GetNativeType(), valueConverter.GetNativeType() ) ;
      if ( false == field.NeedsUnits() ) throw new InvalidOperationException( "Needs units. Use SetMap<TKeyType, TValueType>( string )") ;
      SetSpec( field, specType ) ;
    }

    private void SetSpec( FieldBuilder field, SpecType specType )
    {
#if REVIT2019 || REVIT2020
      field.SetUnitType( specType ) ;
#else
      field.SetSpec( specType ) ;
#endif
    }
  }
}