using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storages.Models
{
    public class CeedUserStorage
    {
        public DataStorage DataStorage { get ; set ; }
        public CeedUserModel CeedUserModel { get ; set ; }

        public CeedUserStorage(Document document)
        {
            DataStorage = document.FindOrCreateDataStorageForUser() ;
            CeedUserModel = DataStorage.GetData<CeedUserModel>() ?? new CeedUserModel() ;
        }
        
    }
}