using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;


namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( WireLengthNotationModel ) )]
  public class WireLengthNotationStorableConverter: StorableConverterBase<WireLengthNotationModel>
  {
    private enum SerializeField
    {
      TextNoteId,
      Level
    }
  
    protected override WireLengthNotationModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
      var textNoteId = deserializer.GetString( SerializeField.TextNoteId ) ;
      var level = deserializer.GetString( SerializeField.Level ) ;

      return new WireLengthNotationModel( textNoteId, level ) ;
    }
  
    protected override ISerializerObject Serialize( Element storedElement, WireLengthNotationModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
  
      serializerObject.AddNonNull( SerializeField.TextNoteId, customTypeValue.TextNoteId ) ;
      serializerObject.AddNonNull( SerializeField.Level, customTypeValue.Level ) ;

      return serializerObject ;
    }
  }
}