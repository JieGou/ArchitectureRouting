using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( PickUpNumberSettingModel ) )]
  public class PickUpNumberSettingStorableConverter : StorableConverterBase<PickUpNumberSettingModel>
  {
    private enum SerializeField
    {
      LevelId,
      LevelName,
      IsPickUpNumberSetting
    }

    protected override PickUpNumberSettingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
      var isPickUpNumberSetting = deserializer.GetBool( SerializeField.IsPickUpNumberSetting ) ;
      var levelId = deserializer.GetInt( SerializeField.LevelId ) ;
      var levelName = deserializer.GetString( SerializeField.LevelName ) ;

      return new PickUpNumberSettingModel( levelId, levelName, isPickUpNumberSetting ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, PickUpNumberSettingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.LevelId, customTypeValue.LevelId ) ;
      serializerObject.AddNonNull( SerializeField.LevelName, customTypeValue.LevelName ) ;
      serializerObject.Add( SerializeField.IsPickUpNumberSetting, customTypeValue.IsPickUpNumberSetting ) ;

      return serializerObject ;
    }
  }
}