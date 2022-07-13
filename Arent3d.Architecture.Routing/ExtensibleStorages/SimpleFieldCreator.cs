using System ;
using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public class SimpleFieldCreator : IFieldFactory
  {
    public FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyInfo )
    {
      FieldBuilder fieldBuilder ;

      var dataModelType = propertyInfo.PropertyType.GetInterface( nameof( IDataModel ) ) ;
      if ( null != dataModelType ) {
        fieldBuilder = schemaBuilder.AddSimpleField( propertyInfo.Name, typeof( Entity ) ) ;

        var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
        var subSchemaAttribute = schemaAttributeExtractor.GetAttribute( propertyInfo.PropertyType ) ;
        fieldBuilder.SetSubSchemaGUID( subSchemaAttribute.GUID ) ;
      }
      else {
        if ( ! propertyInfo.PropertyType.IsAcceptValueType() )
          throw new NotSupportedException( $"Type {propertyInfo.PropertyType.Name} is not accepted." ) ;

        fieldBuilder = schemaBuilder.AddSimpleField( propertyInfo.Name, propertyInfo.PropertyType ) ;
      }

      return fieldBuilder ;
    }
  }
}