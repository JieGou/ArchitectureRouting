using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Extensions
{
    public static class DataStorageExtension
    {
        public static DataStorage GetDataStorageForUser( this Document document )
        {
            if ( string.IsNullOrEmpty( document.Application.Username ) )
                throw new Exception( "Please login to Revit." ) ;
        
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
    }
}