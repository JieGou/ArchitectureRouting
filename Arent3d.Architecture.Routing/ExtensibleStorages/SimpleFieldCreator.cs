using System.Reflection ;
using Arent3d.Architecture.Routing.ExtensibleStorages.Attributes ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public class SimpleFieldCreator: IFieldFactory
  {
    public FieldBuilder CreateField(SchemaBuilder schemaBuilder, PropertyInfo propertyInfo)
    {
      FieldBuilder fieldBuilder;

      if (propertyInfo.PropertyType.GetInterface(nameof(IModelEntity)) is not null)
      {
        fieldBuilder = schemaBuilder.AddSimpleField(propertyInfo.Name, typeof(Entity));
        
        var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>();
        var subSchemaAttribute = schemaAttributeExtractor.GetAttribute(propertyInfo.PropertyType);
        fieldBuilder.SetSubSchemaGUID(subSchemaAttribute.GUID);
      }
      else
      {
        fieldBuilder = schemaBuilder.AddSimpleField(propertyInfo.Name, propertyInfo.PropertyType);
      }

      return fieldBuilder;
    }
  }  
}