using System ;
using System.Reflection ;
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

      _builder.AddSimpleField( fieldName, converter.GetNativeType() ) ;
    }

    public void SetArray<TFieldType>( string fieldName )
    {
      var converter = typeof( TFieldType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TFieldType ).FullName} is not acceptable." ) ;

      _builder.AddArrayField( fieldName, converter.GetNativeType() ) ;
    }

    public void SetMap<TKeyType, TValueType>( string fieldName )
    {
      var keyConverter = typeof( TKeyType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TKeyType ).FullName} is not acceptable." ) ;
      var valueConverter = typeof( TValueType ).GetStorableConverter() ?? throw new InvalidOperationException( $"Type {typeof( TValueType ).FullName} is not acceptable." ) ;

      _builder.AddMapField( fieldName, keyConverter.GetNativeType(), valueConverter.GetNativeType() ) ;
    }
  }
}