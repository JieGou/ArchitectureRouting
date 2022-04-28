using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( EcoSettingModel ) )]
  public class EcoSettingModelStorableConvert : StorableConverterBase<EcoSettingModel>
  {
    private enum SerializeField
    {
      IsEcoMode
    }

    protected override ISerializerObject Serialize( Element storedElement, EcoSettingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.IsEcoMode, customTypeValue.IsEcoMode ) ;

      return serializerObject ;
    }

    protected override EcoSettingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var isEcoMode = deserializer.GetBool( SerializeField.IsEcoMode ) ;

      return new EcoSettingModel( isEcoMode! ) ;
    }
  }
}