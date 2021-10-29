using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( OffsetSettingModel ) )]
  internal class OffsetSettingModelStorableConverter : StorableConverterBase<OffsetSettingModel>
  {
    private enum SerializeField
    {
      LevelId,
      LevelName,
      Elevation,
      Underfloor,
      HeightOfLevel,
      HeightOfConnectors
    }

    protected override OffsetSettingModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var levelId = deserializer.GetInt( SerializeField.LevelId ) ;
      var levelName = deserializer.GetString( SerializeField.LevelName ) ;
      var elevation = deserializer.GetDouble( SerializeField.Elevation ) ;
      var underfloor = deserializer.GetDouble( SerializeField.Underfloor ) ;
      var heightOfLevel = deserializer.GetDouble( SerializeField.HeightOfLevel ) ;
      var heightOfConnectors = deserializer.GetDouble( SerializeField.HeightOfConnectors ) ;

      return new OffsetSettingModel( levelId, levelName, elevation, underfloor, heightOfLevel, heightOfConnectors ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, OffsetSettingModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.Add( SerializeField.LevelId, customTypeValue.LevelId ) ;
      serializerObject.Add( SerializeField.Underfloor, customTypeValue.Underfloor ) ;
      serializerObject.Add( SerializeField.HeightOfLevel, customTypeValue.HeightOfLevel ) ;
      serializerObject.Add( SerializeField.HeightOfConnectors, customTypeValue.HeightOfConnectors ) ;

      return serializerObject ;
    }
  }
}