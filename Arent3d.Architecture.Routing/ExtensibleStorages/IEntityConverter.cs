using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages
{
  public interface IEntityConverter
  {
    /// <summary>
    /// Convert object from IModelEntity to a Entity object
    /// </summary>
    /// <param name="modelEntity">IModelEntity object to convert</param>
    /// <returns>Entity</returns>
    Entity Convert(IModelEntity modelEntity);
    
    /// <summary>
    /// Convert Entity to the TModelEntity object
    /// </summary>
    /// <param name="entity">Entity to convert</param>
    /// <typeparam name="TModelEntity">The type of the TModelEntity</typeparam>
    /// <returns>TModelEntity</returns>
    TModelEntity Convert<TModelEntity>(Entity entity) where TModelEntity : class, IModelEntity;
  }
}