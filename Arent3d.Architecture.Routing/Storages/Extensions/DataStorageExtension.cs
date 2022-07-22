using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages.Attributes ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storages.Extensions
{
    public static class DataStorageExtension
    {
        /// <summary>
        /// Find or create DataStorage
        /// </summary>
        /// <param name="document">Document</param>
        /// <param name="isForUser">True - Each user owns a DataStorage. False - All users share a DataStorage</param>
        /// <typeparam name="TDataModel">A class that inherits from IDataModel</typeparam>
        /// <returns>DataStorage</returns>
        public static DataStorage FindOrCreateDataStorage<TDataModel>( this Document document, bool isForUser ) where TDataModel : class, IDataModel
        {
            string dataStorageName ;
            if ( isForUser ) {
                if ( string.IsNullOrEmpty( document.Application.LoginUserId ) )
                    throw new InvalidDataException( "Please login to Revit." ) ;
                
                dataStorageName = $"{AppInfo.VendorId}-{document.Application.Username}-{document.Application.LoginUserId}" ;
            }
            else {
                var dataModelType = typeof( TDataModel ) ;
                var schemaAttributeExtractor = new AttributeExtractor<SchemaAttribute>() ;
                var schemaAttribute = schemaAttributeExtractor.GetAttribute( dataModelType ) ;

                dataStorageName = $"{AppInfo.VendorId}-{schemaAttribute.GUID}" ;
            }
            

            DataStorage? dataStorage ;
            if ( isForUser ) {
                dataStorage = document.GetAllInstances<DataStorage>().SingleOrDefault( x => x.Name.StartsWith( AppInfo.VendorId ) && x.Name.EndsWith( document.Application.LoginUserId ) ) ;
            }
            else {
                dataStorage = document.GetAllInstances<DataStorage>().SingleOrDefault( x => x.Name == dataStorageName ) ;
            }
             
            if ( null != dataStorage )
                return dataStorage ;

            if ( !document.IsModifiable ) {
                using var transaction = new Transaction( document ) ;
                transaction.Start( "Create Storage" ) ;
                
                dataStorage = document.CreateDataStorage( dataStorageName ) ;
                
                transaction.Commit() ;
            }
            else {
                dataStorage = document.CreateDataStorage( dataStorageName ) ;
            }
            
            return dataStorage ;
        }

        public static DataStorage CreateDataStorage( this Document document, string dataStorageName )
        {
            var dataStorage = DataStorage.Create( document ) ;
            dataStorage.Name = dataStorageName ;
            return dataStorage ;
        }

        public static IEnumerable<(TOwner Owner, TDataModel Data)> GetAllDatas<TOwner, TDataModel>( this Document document ) where TOwner : Element where TDataModel : class, IDataModel
        {
            var datas = new List<(TOwner Owner, TDataModel Data)>() ;
            
            var owners = document.GetAllInstances<TOwner>( o => o is not DataStorage dataStorage || dataStorage.Name.StartsWith(AppInfo.VendorId) ) ;
            foreach ( var owner in owners ) {
                if(owner.GetData<TDataModel>() is not { } data)
                    continue;
                
                datas.Add((owner, data));
            }

            return datas ;
        }
    }
}