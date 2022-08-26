using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( SymbolInformationModel ) )]
  public class SymbolInformationModelStorableConverter : StorableConverterBase<SymbolInformationModel>
  {
    private enum SerializeField
    {
      SymbolUniqueId,
      TagUniqueId,
      SymbolKind,
      SymbolCoordinate,
      Height,
      Percent,
      Color,
      Description,
      CharacterHeight,
      IsShowText,
      IsEco,
      Floor,
    }

    protected override SymbolInformationModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var symbolUniqueId = deserializer.GetString( SerializeField.SymbolUniqueId ) ;
      var tagUniqueId = deserializer.GetString( SerializeField.TagUniqueId ) ;
      var symbolKind = deserializer.GetString( SerializeField.SymbolKind ) ;
      var symbolCoordinate = deserializer.GetString( SerializeField.SymbolCoordinate ) ;
      var height = deserializer.GetDouble( SerializeField.Height ) ;
      var percent = deserializer.GetDouble( SerializeField.Percent ) ;
      var color = deserializer.GetString( SerializeField.Color ) ;
      var description = deserializer.GetString( SerializeField.Description ) ;
      var floor = deserializer.GetString( SerializeField.Floor ) ;
      var characterHeight = deserializer.GetDouble( SerializeField.CharacterHeight ) ;
      var isShowText = deserializer.GetBool( SerializeField.IsShowText ) ; 

      return new SymbolInformationModel(symbolUniqueId, tagUniqueId, symbolKind, symbolCoordinate, height, percent, color, isShowText, description, characterHeight, floor ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, SymbolInformationModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.SymbolUniqueId, customTypeValue.SymbolUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.TagUniqueId, customTypeValue.TagUniqueId ) ;
      serializerObject.AddNonNull( SerializeField.SymbolKind, customTypeValue.SymbolKind ) ;
      serializerObject.AddNonNull( SerializeField.SymbolCoordinate, customTypeValue.SymbolCoordinate ) ;
      serializerObject.Add( SerializeField.Height, customTypeValue.Height ) ;
      serializerObject.Add( SerializeField.Percent, customTypeValue.Percent ) ;
      serializerObject.AddNonNull( SerializeField.Color, customTypeValue.Color ) ;
      serializerObject.AddNonNull( SerializeField.Description, customTypeValue.Description ) ;
      serializerObject.AddNonNull( SerializeField.Floor, customTypeValue.Floor ) ;
      serializerObject.Add( SerializeField.CharacterHeight, customTypeValue.CharacterHeight ) ;
      serializerObject.Add( SerializeField.IsShowText, customTypeValue.IsShowText ) ;   

      return serializerObject ;
    }
  }
}