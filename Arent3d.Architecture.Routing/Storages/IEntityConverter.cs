using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
{
    public interface IEntityConverter
    {
        /// <summary>
        /// Convert object from IDataModel to a Entity object
        /// </summary>
        /// <param name="dataModel">IDataModel object to convert</param>
        /// <returns>Entity</returns>
        Entity Convert( IDataModel dataModel ) ;

        /// <summary>
        /// Convert Entity to the IDataModel object
        /// </summary>
        /// <param name="entity">Entity to convert</param>
        /// <typeparam name="TDataModel">The type of the IDataModel</typeparam>
        /// <returns>TDataModel</returns>
        TDataModel Convert<TDataModel>( Entity entity ) where TDataModel : class, IDataModel ;
    }
}