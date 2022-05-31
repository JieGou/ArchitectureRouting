using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( ImportDwgMappingModel ) )]
  public class ImportDwgMappingModelStorableConverter : StorableConverterBase<ImportDwgMappingModel>
  {
    private enum SerializeField
    {
      Id,
      FullFilePath,
      FileName,
      FloorName,
      FloorHeight,
      Scale
    }
    
    protected override ImportDwgMappingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var id = deserializer.GetString( SerializeField.Id ) ;
      var fullFilePath = deserializer.GetString( SerializeField.FullFilePath ) ;
      var fileName = deserializer.GetString( SerializeField.FileName ) ;
      var floorName = deserializer.GetString( SerializeField.FloorName ) ;
      var floorHeight = deserializer.GetDouble( SerializeField.FloorHeight ) ;
      var scale = deserializer.GetInt( SerializeField.Scale ) ;

      return new ImportDwgMappingModel( id, fullFilePath, fileName, floorName, floorHeight, scale ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, ImportDwgMappingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.Id, customTypeValue.Id ) ;
      serializerObject.AddNonNull( SerializeField.FullFilePath, customTypeValue.FullFilePath ) ;
      serializerObject.AddNonNull( SerializeField.FileName, customTypeValue.FileName ) ;
      serializerObject.AddNonNull( SerializeField.FloorName, customTypeValue.FloorName ) ;
      serializerObject.Add( SerializeField.FloorHeight, customTypeValue.FloorHeight ) ;
      serializerObject.Add( SerializeField.Scale, customTypeValue.Scale ) ;

      return serializerObject ;
    }
  }
}