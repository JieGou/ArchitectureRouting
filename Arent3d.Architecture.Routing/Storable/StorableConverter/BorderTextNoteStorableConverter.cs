using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( BorderTextNoteModel ) )]
  public class BorderTextNoteStorableConverter: StorableConverterBase<BorderTextNoteModel>
  {
    private enum SerializeField
    {
      TextNoteUniqueId,
      BorderUniqueIds
    }
  
    protected override BorderTextNoteModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
  
      var textNoteUniqueId = deserializer.GetString( SerializeField.TextNoteUniqueId ) ;
      var borderUniqueIds = deserializer.GetString( SerializeField.BorderUniqueIds ) ;
  
      return new BorderTextNoteModel( textNoteUniqueId, borderUniqueIds ) ;
    }
  
    protected override ISerializerObject Serialize( Element storedElement, BorderTextNoteModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
  
      serializerObject.AddNonNull( SerializeField.TextNoteUniqueId, customTypeValue.TextNoteUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.BorderUniqueIds, customTypeValue.BorderUniqueIds ) ;;
  
      return serializerObject ;
    }
  }
}