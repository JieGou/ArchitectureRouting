using System ;
using System.Reflection ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
{
    public class SimpleFieldCreator : IFieldFactory
    {
        public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyModel )
        {
            FieldBuilder fieldBuilder ;

            var dataModelType = propertyModel.PropertyType.GetInterface( nameof( IDataModel ) ) ;
            if ( null != dataModelType ) {
                fieldBuilder = schemaBuilder.AddSimpleField( propertyModel.Name, typeof( Entity ) ) ;

                var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
                var subSchemaAttribute = schemaAttributeExtractor.GetAttribute( propertyModel.PropertyType ) ;
                fieldBuilder.SetSubSchemaGUID( subSchemaAttribute.GUID ) ;
            }
            else {
                if ( ! propertyModel.PropertyType.IsAcceptValueType() )
                    throw new NotSupportedException( $"Type {propertyModel.PropertyType.Name} is not accepted." ) ;

                fieldBuilder = schemaBuilder.AddSimpleField( propertyModel.Name, propertyModel.PropertyType ) ;
            }

            return fieldBuilder ;
        }
    }
}