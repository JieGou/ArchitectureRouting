using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
{
    public class StorageService<TDataModel> where TDataModel : class, IDataModel
    {
        public DataStorage Owner { get ; }
        public TDataModel Data { get ;  }

        private IEnumerable<(Element Owner, TDataModel Data)>? _dataStorages ;
        private IEnumerable<(Element Owner, TDataModel Data)> DataStorages
        {
            get { return _dataStorages ??= Owner.Document.GetAllDataStorage<TDataModel>() ; }
        }

        public StorageService(Document document, bool isForUser)
        {
            Owner = document.FindOrCreateDataStorage<TDataModel>( isForUser ) ;
            Data = Owner.GetData<TDataModel>() ?? Activator.CreateInstance<TDataModel>() ;
        }

        public void SaveChange()
        {
            Owner.SetData(Data);
        }
        
    }
}