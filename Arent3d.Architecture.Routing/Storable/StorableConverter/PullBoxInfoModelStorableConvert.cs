using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( PullBoxInfoModel ) )]
  public class PullBoxInfoModelStorableConvert : StorableConverterBase<PullBoxInfoModel>
  {
    private enum SerializeField
    {
      PullBoxUniqueId,
      TextNoteUniqueId
    }

    protected override PullBoxInfoModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var PullBoxUniqueId = deserializer.GetString( SerializeField.PullBoxUniqueId ) ;
      var TextNoteUniqueId = deserializer.GetString( SerializeField.TextNoteUniqueId ) ;

      return new PullBoxInfoModel( PullBoxUniqueId, TextNoteUniqueId ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, PullBoxInfoModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.PullBoxUniqueId, customTypeValue.PullBoxUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.TextNoteUniqueId, customTypeValue.TextNoteUniqueId ) ;

      return serializerObject ;
    }
  }
}