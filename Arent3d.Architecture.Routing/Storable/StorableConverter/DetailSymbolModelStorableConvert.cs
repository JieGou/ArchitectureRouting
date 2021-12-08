using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( DetailSymbolModel ) )]
  public class DetailSymbolModelStorableConvert : StorableConverterBase<DetailSymbolModel>
  {
    private enum SerializeField
    {
      DetailSymbol,
      ConduitId,
      FromConnectorId,
      ToConnectorId,
      Code
    }
    
    protected override DetailSymbolModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var detailSymbol = deserializer.GetString( SerializeField.DetailSymbol ) ;
      var conduitId = deserializer.GetString( SerializeField.ConduitId ) ;
      var fromConnectorId = deserializer.GetString( SerializeField.FromConnectorId ) ;
      var toConnectorId = deserializer.GetString( SerializeField.ToConnectorId ) ;
      var code = deserializer.GetString( SerializeField.Code ) ;

      return new DetailSymbolModel( detailSymbol, conduitId, fromConnectorId, toConnectorId, code ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, DetailSymbolModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.DetailSymbol, customTypeValue.DetailSymbol ) ;
      serializerObject.AddNonNull( SerializeField.ConduitId, customTypeValue.ConduitId ) ;
      serializerObject.AddNonNull( SerializeField.FromConnectorId, customTypeValue.FromConnectorId ) ;
      serializerObject.AddNonNull( SerializeField.ToConnectorId, customTypeValue.ToConnectorId ) ;
      serializerObject.AddNonNull( SerializeField.Code, customTypeValue.Code ) ;

      return serializerObject ;
    }
  }
}