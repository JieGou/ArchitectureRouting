using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storages
{
    public class StorageService<TOwner, TDataModel> where TOwner : Element where TDataModel : class, IDataModel
    {
        public TOwner Owner { get ; }
        public TDataModel Data { get ; set ; }

        private IEnumerable<(TOwner Owner, TDataModel Data)>? _allDatas ;
        public IEnumerable<(TOwner Owner, TDataModel Data)> AllDatas
        {
            get { return _allDatas ??= Owner.Document.GetAllDatas<TOwner, TDataModel>() ; }
        }

        public StorageService( TOwner owner )
        {
            Owner = owner ;
            Data = Owner.GetData<TDataModel>() ?? Activator.CreateInstance<TDataModel>() ;
        }

        public void SaveChange()
        {
            Owner.SetData(Data);
        }
        
    }
}