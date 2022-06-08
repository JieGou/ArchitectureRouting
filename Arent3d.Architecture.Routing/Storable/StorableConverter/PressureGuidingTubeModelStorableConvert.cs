using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( PressureGuidingTubeModel ) )]
  public class PressureGuidingTubeModelStorableConvert : StorableConverterBase<PressureGuidingTubeModel>
  {
    private enum SerializeField
    {
      Height,
      TubeType,
      CreationMode,
    }

    protected override PressureGuidingTubeModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var creationMode = deserializer.GetString( SerializeField.CreationMode ) ;
      var tubeType = deserializer.GetString( SerializeField.TubeType ) ;
      var height = deserializer.GetDouble( SerializeField.Height ) ;

      return new PressureGuidingTubeModel( height, tubeType, creationMode ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, PressureGuidingTubeModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.Height, customTypeValue.Height ) ;
      serializerObject.AddNonNull( SerializeField.CreationMode, customTypeValue.CreationMode ) ;
      serializerObject.AddNonNull( SerializeField.TubeType, customTypeValue.TubeType ) ;

      return serializerObject ;
    }
  }
}