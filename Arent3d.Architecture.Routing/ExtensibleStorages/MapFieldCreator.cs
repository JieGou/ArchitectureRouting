using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  internal class MapFieldCreator: IFieldFactory
  {
    public FieldBuilder CreateField(SchemaBuilder schemaBuilder, PropertyInfo propertyInfo)
    {
      FieldBuilder fieldBuilder;

      var genericKeyType = propertyInfo.PropertyType.GetGenericArguments()[0];
      var genericValueType = propertyInfo.PropertyType.GetGenericArguments()[1];

      if (genericValueType.GetInterface(nameof(IModelEntity)) is not null)
      {
        fieldBuilder = schemaBuilder.AddMapField(propertyInfo.Name, genericKeyType, typeof(Entity));

        var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>();
        var subSchemaAttribute = schemaAttributeExtractor.GetAttribute(genericValueType);
        fieldBuilder.SetSubSchemaGUID(subSchemaAttribute.GUID);
      }
      else
      {
        fieldBuilder = schemaBuilder.AddMapField(propertyInfo.Name, genericKeyType, genericValueType);                
      }

      return fieldBuilder;
    }
  }
}