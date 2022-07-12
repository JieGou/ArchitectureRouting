using System ;
using System.IO ;
using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  /// <summary>
  /// Create a schema from a type
  /// </summary>
  public class SchemaCreator : ISchemaCreator
  {
    private readonly AttributeExtractor<SchemaAttribute> _schemaAttributeExtractor = new() ;
    private readonly AttributeExtractor<FieldAttribute> _fieldAttributeExtractor = new() ;
    private readonly IFieldFactory _fieldFactory = new FieldFactory() ;

    public Schema FindOrCreate( Type type )
    {
      var schemaAttribute = _schemaAttributeExtractor.GetAttribute( type ) ;
      if ( Schema.Lookup( schemaAttribute.GUID ) is { } schema )
        return schema ;

      if ( ! SchemaBuilder.GUIDIsValid( schemaAttribute.GUID ) )
        throw new InvalidDataException( $"GUID of the type {nameof( type )} is invalid." ) ;

      var schemaBuilder = new SchemaBuilder( schemaAttribute.GUID ) ;
      if ( ! schemaBuilder.AcceptableName( schemaAttribute.SchemaName ) )
        throw new InvalidDataException( $"The schema name {schemaAttribute.SchemaName} is invalid." ) ;

      schemaBuilder.SetSchemaName( schemaAttribute.SchemaName ) ;

      if ( schemaAttribute.ApplicationGUID != Guid.Empty )
        schemaBuilder.SetApplicationGUID( schemaAttribute.ApplicationGUID ) ;

      if ( ! string.IsNullOrEmpty( schemaAttribute.Documentation ) )
        schemaBuilder.SetDocumentation( schemaAttribute.Documentation ) ;

      if ( schemaAttribute.ReadAccessLevel != default )
        schemaBuilder.SetReadAccessLevel( schemaAttribute.ReadAccessLevel ) ;

      if ( schemaAttribute.WriteAccessLevel != default )
        schemaBuilder.SetWriteAccessLevel( schemaAttribute.WriteAccessLevel ) ;

      if ( ! string.IsNullOrEmpty( schemaAttribute.VendorId ) )
        schemaBuilder.SetVendorId( schemaAttribute.VendorId ) ;

      var propertyInfos = type.GetProperties( BindingFlags.Public | BindingFlags.Instance ) ;
      foreach ( var propertyInfo in propertyInfos ) {
        var propertyAttributes = propertyInfo.GetCustomAttributes( typeof( FieldAttribute ), true ) ;
        if ( propertyAttributes.Length == 0 )
          continue ;

        var fieldAttribute = _fieldAttributeExtractor.GetAttribute( propertyInfo ) ;
        var fieldBuilder = _fieldFactory.CreateField( schemaBuilder, propertyInfo ) ;

        if ( ! string.IsNullOrEmpty( fieldAttribute.Documentation ) )
          fieldBuilder.SetDocumentation( fieldAttribute.Documentation ) ;

        if ( fieldBuilder.NeedsUnits() )
          fieldBuilder.SetSpec( new ForgeTypeId( fieldAttribute.SpecTypeId ) ) ;
      }

      return schemaBuilder.Finish() ;
    }
  }
}