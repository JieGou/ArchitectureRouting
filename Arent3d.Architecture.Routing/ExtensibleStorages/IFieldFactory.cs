using System.Reflection ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
    /// <summary>
    /// Create a schema field from a property
    /// </summary>
    public interface IFieldFactory
    {
        FieldBuilder CreateField( SchemaBuilder schemaBuilder, PropertyInfo propertyModel ) ;
    }
}