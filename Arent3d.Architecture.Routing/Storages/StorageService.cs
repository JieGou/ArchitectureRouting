using System ;
using Arent3d.Architecture.Routing.Storages.Extensions ;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storages
{
    public class StorageService<TOwner, TDataModel> where TOwner : Element where TDataModel : class, IDataModel
    {
        public TOwner Owner { get ; }
        public TDataModel Data { get ; set ; }

        public StorageService( TOwner owner )
        {
            Owner = owner ;
            Data = Owner.GetData<TDataModel>() ?? Activator.CreateInstance<TDataModel>() ;
        }

        public void SaveChange()
        {
            using var transaction = new Transaction(Owner.Document);
            transaction.OpenTransactionIfNeed(Owner.Document, "Save Change Data", () =>
            {
                Owner.SetData(Data);
            });
        }
    }
}