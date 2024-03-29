﻿using Arent3d.Architecture.Routing.Storable.Model ;
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
      ConnectorId,
      PlumbingType,
      PlumbingSize,
      NumberOfPlumbing,
      PlumbingName,
      ClassificationOfPlumbing,
      ConstructionItems, 
      WireCrossSectionalArea,
      IsExposure,
      IsInDoor,
      ConduitDirectionZ
    }
    
    protected override ChangePlumbingInformationModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var conduitId = deserializer.GetString( SerializeField.ConduitId ) ;
      var connectorId = deserializer.GetString( SerializeField.ConnectorId ) ;
      var plumbingType = deserializer.GetString( SerializeField.PlumbingType ) ;
      var plumbingSize = deserializer.GetString( SerializeField.PlumbingSize ) ;
      var numberOfPlumbing = deserializer.GetString( SerializeField.NumberOfPlumbing ) ;
      var plumbingName = deserializer.GetString( SerializeField.PlumbingName ) ;
      var classificationOfPlumbing = deserializer.GetString( SerializeField.ClassificationOfPlumbing ) ;
      var constructionItems = deserializer.GetString( SerializeField.ConstructionItems ) ;
      var wireCrossSectionalArea = deserializer.GetDouble( SerializeField.WireCrossSectionalArea ) ;
      var isExposure = deserializer.GetBool( SerializeField.IsExposure ) ;
      var isInDoor = deserializer.GetBool( SerializeField.IsInDoor ) ;
      var conduitDirectionZ = deserializer.GetDouble( SerializeField.ConduitDirectionZ ) ;

      return new ChangePlumbingInformationModel( conduitId, connectorId, plumbingType, plumbingSize, numberOfPlumbing, plumbingName, classificationOfPlumbing, constructionItems, wireCrossSectionalArea, isExposure, isInDoor, conduitDirectionZ ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, ChangePlumbingInformationModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.ConduitId, customTypeValue.ConduitId ) ;
      serializerObject.AddNonNull( SerializeField.ConnectorId, customTypeValue.ConnectorId ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingType, customTypeValue.PlumbingType ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingSize, customTypeValue.PlumbingSize ) ;
      serializerObject.AddNonNull( SerializeField.NumberOfPlumbing, customTypeValue.NumberOfPlumbing ) ;
      serializerObject.AddNonNull( SerializeField.PlumbingName, customTypeValue.PlumbingName ) ;
      serializerObject.AddNonNull( SerializeField.ClassificationOfPlumbing, customTypeValue.ClassificationOfPlumbing ) ;
      serializerObject.AddNonNull( SerializeField.ConstructionItems, customTypeValue.ConstructionItems ) ;
      serializerObject.Add( SerializeField.WireCrossSectionalArea, customTypeValue.WireCrossSectionalArea ) ;
      serializerObject.Add( SerializeField.IsExposure, customTypeValue.IsExposure ) ;
      serializerObject.Add( SerializeField.IsInDoor, customTypeValue.IsInDoor ) ;
      serializerObject.Add( SerializeField.ConduitDirectionZ, customTypeValue.ConduitDirectionZ ) ;

      return serializerObject ;
    }
  }
}