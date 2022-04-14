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
      TextNoteId,
      BorderIds
    }
  
    protected override BorderTextNoteModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
  
      var textNoteId = deserializer.GetInt( SerializeField.TextNoteId ) ;
      var borderIds = deserializer.GetString( SerializeField.BorderIds ) ;
  
      return new BorderTextNoteModel( textNoteId, borderIds ) ;
    }
  
    protected override ISerializerObject Serialize( Element storedElement, BorderTextNoteModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
  
      serializerObject.Add( SerializeField.TextNoteId, customTypeValue.TextNoteId ) ;
      serializerObject.AddNonNull( SerializeField.BorderIds, customTypeValue.BorderIds ) ;;
  
      return serializerObject ;
    }
  }
}