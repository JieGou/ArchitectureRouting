using System ;
using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
    public class ArrayFieldCreator : IFieldFactory
    {
        public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyInfo )
        {
            FieldBuilder fieldBuilder ;

            var genericType = propertyInfo.PropertyType.GetGenericArguments()[ 0 ] ;
            var dataModelType = genericType.GetInterface( nameof( IDataModel ) ) ;
            if ( null != dataModelType ) {
                fieldBuilder = schemaBuilder.AddArrayField( propertyInfo.Name, typeof( Entity ) ) ;

                var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
                var subSchemaAttribute = schemaAttributeExtractor.GetAttribute( genericType ) ;
                fieldBuilder.SetSubSchemaGUID( subSchemaAttribute.GUID ) ;
            }
            else {
                if ( ! genericType.IsAcceptValueType() )
                    throw new NotSupportedException( $"Type {genericType.Name} is not accepted." ) ;

                fieldBuilder = schemaBuilder.AddArrayField( propertyInfo.Name, genericType ) ;
            }

            return fieldBuilder ;
        }
    }
}