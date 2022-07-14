using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Extensions
{
    public static class DataStorageExtension
    {
        public static DataStorage FindOrCreateDataStorageForUser( this Document document )
        {
            if ( string.IsNullOrEmpty( document.Application.Username ) )
                throw new InvalidDataException( "Please login to Revit." ) ;
        
            var dataStorage = document.GetAllInstances<DataStorage>().SingleOrDefault( x => x.Name == $"{AppInfo.VendorId}_{document.Application.Username}" ) ;
            if ( null != dataStorage )
                return dataStorage ;

            dataStorage = DataStorage.Create( document ) ;
            dataStorage.Name = $"{AppInfo.VendorId}_{document.Application.Username}" ;
            return dataStorage ;
        }

        public static IEnumerable<DataStorage> GetDataStorageUsers( this Document document )
        {
            return document.GetAllInstances<DataStorage>(x => x.Name.StartsWith(AppInfo.VendorId)) ;
        }

        public static IEnumerable<TDataModel> GetAllData<TDataModel>(this Document document) where TDataModel : class, IDataModel
        {
            var dataStorages = GetDataStorageUsers( document ).ToList() ;
            return !dataStorages.Any() ? Enumerable.Empty<TDataModel>() : dataStorages.Select( x => x.GetData<TDataModel>() ).OfType<TDataModel>() ;
        }
    }
}