using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( ChangePlumbingInformationModel ) )]
  public class ChangePlumbingInformationModelStorableConverter : StorableConverterBase<ChangePlumbingInformationModel>
  {
    private enum SerializeField
    {
      ConduitId,
      PlumbingType,
      PlumbingSize,
      NumberOfPlumbing,
      ConstructionClassification,
      ConstructionItems, 
      WireCrossSectionalArea,
      IsExposure
    }
    
    protected override ChangePlumbingInformationModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var conduitId = deserializer.GetString( SerializeField.ConduitId ) ;
      var plumbingType = deserializer.GetString( SerializeField.PlumbingType ) ;
      var plumbingSize = deserializer.GetString( SerializeField.PlumbingSize ) ;
      var numberOfPlumbing = deserializer.GetString( SerializeField.NumberOfPlumbing ) ;
      var constructionClassification = deserializer.GetString( SerializeField.ConstructionClassification ) ;
      var constructionItems = deserializer.GetString( SerializeField.ConstructionItems ) ;
      var wireCrossSectionalArea = deserializer.GetDouble( SerializeField.WireCrossSectionalArea ) ;
      var isExposure = deserializer.GetBool( SerializeField.IsExposure ) ;

      return new ChangePlumbingInformationModel( conduitId, plumbingType, plumbingSize, numberOfPlumbing, constructionClassification, constructionItems, wireCrossSectionalArea, isExposure ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, ChangePlumbingInformationModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.ConduitId, customTypeValue.ConduitId ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingType, customTypeValue.PlumbingType ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingSize, customTypeValue.PlumbingSize ) ;
      serializerObject.AddNonNull( SerializeField.NumberOfPlumbing, customTypeValue.NumberOfPlumbing ) ;
      serializerObject.AddNonNull( SerializeField.ConstructionClassification, customTypeValue.ConstructionClassification ) ;
      serializerObject.AddNonNull( SerializeField.ConstructionItems, customTypeValue.ConstructionItems ) ;
      serializerObject.Add( SerializeField.WireCrossSectionalArea, customTypeValue.WireCrossSectionalArea ) ;
      serializerObject.Add( SerializeField.IsExposure, customTypeValue.IsExposure ) ;

      return serializerObject ;
    }
  }
}