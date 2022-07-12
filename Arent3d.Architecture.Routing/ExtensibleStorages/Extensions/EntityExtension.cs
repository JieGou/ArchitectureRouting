using System.Collections.Generic ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Extensions
{
  public static class EntityExtension
  {
    public static void SetWrapper<T>(this Entity entity, Field field, IList<T> value)
    {            
      entity.Set(field, value);
    }

    public static void SetWrapper<TKey,TValue>(this Entity entity, Field field, IDictionary<TKey,TValue> value)
    {
      entity.Set(field, value);
    }
  }
}