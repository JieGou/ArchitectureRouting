using System ;
using System.Collections.Generic ;
using System.Reflection ;
using System.Text ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
{
    public class FieldFactory : IFieldFactory
    {
        public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyModel )
        {
            IFieldFactory? fieldFactory = null ;

            var propertyType = propertyModel.PropertyType ;
            if ( propertyType.IsGenericType ) {
                foreach ( var interfaceType in propertyType.GetInterfaces() ) {
                    if ( ! interfaceType.IsGenericType )
                        continue ;

                    if ( interfaceType.GetGenericTypeDefinition() == typeof( IList<> ) ) {
                        fieldFactory = new ArrayFieldCreator() ;
                        break ;
                    }

                    if ( interfaceType.GetGenericTypeDefinition() != typeof( IDictionary<,> ) )
                        continue ;

                    fieldFactory = new MapFieldCreator() ;
                    break ;
                }
            }
            else {
                fieldFactory = new SimpleFieldCreator() ;
            }

            if ( fieldFactory != null )
                return fieldFactory.CreateField( schemaBuilder, propertyModel ) ;

            var sb = new StringBuilder() ;
            sb.AppendLine( $"Type {propertyType.Name} does not supported." ) ;
            sb.AppendLine( "Only IList<T> and IDictionary<TKey, TValue> generic types are supported." ) ;
            throw new NotSupportedException( sb.ToString() ) ;
        }
    }
}