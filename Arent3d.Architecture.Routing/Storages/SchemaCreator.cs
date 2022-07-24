using System ;
using System.IO ;
using System.Reflection ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
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

            var propertyModels = type.GetProperties( BindingFlags.Public | BindingFlags.Instance ) ;
            if (propertyModels.Length > 256)
                throw new ArgumentOutOfRangeException($"Exceeds 256 fields in type {type.Name}.");
            
            foreach ( var propertyModel in propertyModels ) {
                if (propertyModel.PropertyType.IsFloatingPoint())
                    throw new MissingMemberException($"{nameof(FieldAttribute.SpecTypeId)} & {nameof(FieldAttribute.UnitTypeId)} is required for property {propertyModel.Name}.");
                
                var propertyAttributes = propertyModel.GetCustomAttributes( typeof( FieldAttribute ), true ) ;
                if ( propertyAttributes.Length == 0 )
                    continue ;

                var fieldAttribute = _fieldAttributeExtractor.GetAttribute( propertyModel ) ;
                var fieldBuilder = _fieldFactory.CreateField( schemaBuilder, propertyModel ) ;

                if ( ! string.IsNullOrEmpty( fieldAttribute.Documentation ) )
                    fieldBuilder.SetDocumentation( fieldAttribute.Documentation ) ;

                if ( fieldBuilder.NeedsUnits() ) {
                    if ( string.IsNullOrEmpty( fieldAttribute.SpecTypeId ) || string.IsNullOrEmpty( fieldAttribute.UnitTypeId ) )
                        throw new MissingMemberException( $"{nameof( FieldAttribute.SpecTypeId )} and {nameof( FieldAttribute.UnitTypeId )} for the property {propertyModel.Name} is required." ) ;

                    fieldBuilder.SetSpec( new ForgeTypeId( fieldAttribute.SpecTypeId ) ) ;
                }
                else if ( ! string.IsNullOrEmpty( fieldAttribute.SpecTypeId ) ) {
                    if ( string.IsNullOrEmpty( fieldAttribute.UnitTypeId ) )
                        throw new MissingMemberException( $"{nameof( FieldAttribute.UnitTypeId )} for the property {propertyModel.Name} is required." ) ;

                    fieldBuilder.SetSpec( new ForgeTypeId( fieldAttribute.SpecTypeId ) ) ;
                }
            }

            return schemaBuilder.Finish() ;
        }
    }
}