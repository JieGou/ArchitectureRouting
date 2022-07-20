using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;


namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( PickUpNumberSettingModel ) )]
  public class PickUpNumberSettingStorableConverter: StorableConverterBase<PickUpNumberSettingModel>
  {
    private enum SerializeField
    {
      Level,
      IsPickUpNumberSetting
    }
  
    protected override PickUpNumberSettingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
      var isPickUpNumberSetting = deserializer.GetBool( SerializeField.IsPickUpNumberSetting ) ;
      var level = deserializer.GetString( SerializeField.Level ) ;

      return new PickUpNumberSettingModel( level, isPickUpNumberSetting ) ;
    }
  
    protected override ISerializerObject Serialize( Element storedElement, PickUpNumberSettingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
      
      serializerObject.AddNonNull( SerializeField.Level, customTypeValue.Level ) ;
      serializerObject.Add( SerializeField.IsPickUpNumberSetting, customTypeValue.IsPickUpNumberSetting ) ;

      return serializerObject ;
    }
  }
}