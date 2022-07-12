using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public class ArrayFieldCreator : IFieldFactory
  {
    public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyInfo )
    {
      FieldBuilder fieldBuilder ;

      var genericType = propertyInfo.PropertyType.GetGenericArguments()[ 0 ] ;
      if ( genericType.GetInterface( nameof( IModelEntity ) ) is not null ) {
        fieldBuilder = schemaBuilder.AddArrayField( propertyInfo.Name, typeof( Entity ) ) ;

        var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
        var subSchemaAttribute = schemaAttributeExtractor.GetAttribute( genericType ) ;
        fieldBuilder.SetSubSchemaGUID( subSchemaAttribute.GUID ) ;
      }
      else {
        fieldBuilder = schemaBuilder.AddArrayField( propertyInfo.Name, genericType ) ;
      }

      return fieldBuilder ;
    }
  }
}