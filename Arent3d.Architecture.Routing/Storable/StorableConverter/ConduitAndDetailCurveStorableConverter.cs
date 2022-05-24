using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( ConduitAndDetailCurveModel ) )]
  public class ConduitAndDetailCurveStorableConverter : StorableConverterBase<ConduitAndDetailCurveModel>
  {
    private enum SerializeField
    {
      ConduitId,
      DetailCurveId,
      WireType
    }
    
    protected override ConduitAndDetailCurveModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
  
      var conduitId = deserializer.GetString( SerializeField.ConduitId ) ;
      var detailCurveId = deserializer.GetString( SerializeField.DetailCurveId ) ;
      var wireType = deserializer.GetString( SerializeField.WireType ) ;
  
      return new ConduitAndDetailCurveModel( conduitId, detailCurveId, wireType ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, ConduitAndDetailCurveModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
  
      serializerObject.AddNonNull( SerializeField.ConduitId, customTypeValue.ConduitId ) ;
      serializerObject.AddNonNull( SerializeField.DetailCurveId, customTypeValue.DetailCurveId ) ;
      serializerObject.AddNonNull( SerializeField.WireType, customTypeValue.WireType ) ;
  
      return serializerObject ;
    }
  }
}