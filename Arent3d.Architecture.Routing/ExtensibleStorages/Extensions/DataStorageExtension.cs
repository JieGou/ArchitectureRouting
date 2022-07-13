using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.ExtensibleStorages.Extensions
{
  public static class DataStorageExtension
  {
    public static DataStorage GetDataStorage(this Document document)
    {
      var dataStorage = document.GetAllInstances<DataStorage>().SingleOrDefault(x => x.Name == nameof(DataStorage));
      if ( null != dataStorage ) 
        return dataStorage ;
      
      dataStorage = DataStorage.Create(document);
      dataStorage.Name = nameof( DataStorage ) ;
      return dataStorage;
    }
  }
}