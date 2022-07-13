﻿using System ;
using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
    public class MapFieldCreator : IFieldFactory
    {
        public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyInfo )
        {
            FieldBuilder fieldBuilder ;

            var genericKeyType = propertyInfo.PropertyType.GetGenericArguments()[ 0 ] ;
            var genericValueType = propertyInfo.PropertyType.GetGenericArguments()[ 1 ] ;

            var dataModelType = genericValueType.GetInterface( nameof( IDataModel ) ) ;
            if ( null != dataModelType ) {
                fieldBuilder = schemaBuilder.AddMapField( propertyInfo.Name, genericKeyType, typeof( Entity ) ) ;

                var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
                var subSchemaAttribute = schemaAttributeExtractor.GetAttribute( genericValueType ) ;
                fieldBuilder.SetSubSchemaGUID( subSchemaAttribute.GUID ) ;
            }
            else {
                if ( ! genericValueType.IsAcceptValueType() )
                    throw new NotSupportedException( $"The value type {genericValueType.Name} is not accepted." ) ;

                if ( ! genericKeyType.IsAcceptKeyType() )
                    throw new NotSupportedException( $"The key type {genericKeyType.Name} is not accepted." ) ;

                fieldBuilder = schemaBuilder.AddMapField( propertyInfo.Name, genericKeyType, genericValueType ) ;
            }

            return fieldBuilder ;
        }
    }
}