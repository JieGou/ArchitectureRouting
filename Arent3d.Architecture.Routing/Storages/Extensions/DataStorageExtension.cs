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

        public static IEnumerable<(Element Owner, TDataModel Data)> GetAllDataStorage<TDataModel>( this Document document ) where TDataModel : class, IDataModel
        {
            var allDatas = new List<(Element Owner, TDataModel Data)>() ;
            
            var dataStorages = document.GetAllInstances<DataStorage>( x => x.Name.StartsWith( AppInfo.VendorId ) ) ;
            foreach ( var dataStorage in dataStorages ) {
                if(dataStorage.GetData<TDataModel>() is not { } data)
                    continue;
                
                allDatas.Add((dataStorage, data));
            }

            return allDatas ;
        }
    }
}