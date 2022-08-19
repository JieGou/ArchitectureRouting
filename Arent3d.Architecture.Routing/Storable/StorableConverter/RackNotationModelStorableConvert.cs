using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( RackNotationModel ) )]
  public class RackNotationModelStorableConvert : StorableConverterBase<RackNotationModel>
  {
    private enum SerializeField
    {
      RackId,
      NotationId,
      RackNotationId,
      FromConnectorId,
      IsDirectionX,
      RackWidth,
      EndLineLeaderId,
      EndPoint,
      OrtherLineId
    }

    protected override RackNotationModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var rackId = deserializer.GetString( SerializeField.RackId ) ;
      var notationId = deserializer.GetString( SerializeField.NotationId ) ;
      var rackNotationId = deserializer.GetString( SerializeField.RackNotationId ) ;
      var fromConnectorId = deserializer.GetString( SerializeField.FromConnectorId ) ;
      var isDirectionX = deserializer.GetBool( SerializeField.IsDirectionX ) ;
      var rackWidth = deserializer.GetDouble( SerializeField.RackWidth ) ;
      var endLineLeaderId = deserializer.GetString( SerializeField.EndLineLeaderId ) ;
      var endPoint = deserializer.GetInt( SerializeField.EndPoint ) ;
      var ortherLineId = deserializer.GetNonNullStringArray( SerializeField.OrtherLineId ) ;

      return new RackNotationModel( rackId, notationId, rackNotationId, fromConnectorId, isDirectionX, rackWidth, endLineLeaderId, endPoint, ortherLineId ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, RackNotationModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.RackId, customTypeValue.RackId ) ;
      serializerObject.AddNonNull( SerializeField.NotationId, customTypeValue.NotationId ) ;
      serializerObject.AddNonNull( SerializeField.RackNotationId, customTypeValue.RackNotationId ) ;
      serializerObject.AddNonNull( SerializeField.FromConnectorId, customTypeValue.FromConnectorId ) ;
      serializerObject.Add( SerializeField.IsDirectionX, customTypeValue.IsDirectionX ) ;
      serializerObject.Add( SerializeField.RackWidth, customTypeValue.RackWidth ) ;
      serializerObject.AddNonNull(SerializeField.EndLineLeaderId, customTypeValue.EndLineLeaderId ) ;
      serializerObject.Add(SerializeField.EndPoint, customTypeValue.EndPoint ) ;
      serializerObject.AddNonNull(SerializeField.OrtherLineId, customTypeValue.OtherLineIds ) ;

      return serializerObject ;
    }
  }
}