using System ;
using System.Reflection ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
{
    public class ArrayFieldCreator : IFieldFactory
    {
        public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyModel )
        {
            FieldBuilder fieldBuilder ;

            var genericType = propertyModel.PropertyType.GetGenericArguments()[ 0 ] ;
            var dataModelType = genericType.GetInterface( nameof( IDataModel ) ) ;
            if ( null != dataModelType ) {
                fieldBuilder = schemaBuilder.AddArrayField( propertyModel.Name, typeof( Entity ) ) ;

                var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
                var subSchemaAttribute = schemaAttributeExtractor.GetAttribute( genericType ) ;
                fieldBuilder.SetSubSchemaGUID( subSchemaAttribute.GUID ) ;
            }
            else {
                if ( ! genericType.IsAcceptValueType() )
                    throw new NotSupportedException( $"Type {genericType.Name} is not accepted." ) ;

                fieldBuilder = schemaBuilder.AddArrayField( propertyModel.Name, genericType ) ;
            }

            return fieldBuilder ;
        }
    }
}