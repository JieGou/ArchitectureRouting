using System ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  /// <summary>
  /// Create a schema from a type
  /// </summary>
  public interface ISchemaCreator
  {
    Schema FindOrCreate(Type type);
  }
}