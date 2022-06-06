using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( CsvFileModel ) )]
  public class CsvFileModelStorableConvert : StorableConverterBase<CsvFileModel>
  {
    private enum SerializeField
    {
      CsvName,
      CsvFilePath,
      CsvFileName 
    }
    
    protected override CsvFileModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var csvName = deserializer.GetString( SerializeField.CsvName ) ;
      var csvFilePath = deserializer.GetString( SerializeField.CsvFilePath ) ;
      var csvFileName = deserializer.GetString( SerializeField.CsvFileName ) ;
      
      return new CsvFileModel( csvName, csvFilePath, csvFileName) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, CsvFileModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.CsvName, customTypeValue.CsvName ) ;
      serializerObject.AddNonNull( SerializeField.CsvFilePath, customTypeValue.CsvFilePath ) ;
      serializerObject.AddNonNull( SerializeField.CsvFileName, customTypeValue.CsvFileName ) ;

      return serializerObject ;
    }
  }
}