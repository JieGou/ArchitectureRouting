using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( WiringInformationChangedModel ) )]
  public class WiringInformationChangedStorableConverter: StorableConverterBase<WiringInformationChangedModel>
  {
    private enum SerializeField
    {
      ConnectorUniqueId,
      MaterialCode
    }
  
    protected override WiringInformationChangedModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
  
      var connectorUniqueId = deserializer.GetString( SerializeField.ConnectorUniqueId ) ;
      var materialCode = deserializer.GetString( SerializeField.MaterialCode ) ;
  
      return new WiringInformationChangedModel( connectorUniqueId, materialCode ) ;
    }
  
    protected override ISerializerObject Serialize( Element storedElement, WiringInformationChangedModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
  
      serializerObject.AddNonNull( SerializeField.ConnectorUniqueId, customTypeValue.ConnectorUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.MaterialCode, customTypeValue.MaterialCode ) ;;
  
      return serializerObject ;
    }
  }
}