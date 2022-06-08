﻿using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( WiringModel ) )]
  public class WiringStorableConverter: StorableConverterBase<WiringModel>
  {
    private enum SerializeField
    {
      Id,
      IdOfToConnector,
      RouteName,
      Floor,
      GeneralDisplayDeviceSymbol,
      WireType,
      WireSize,
      WireStrip,
      PipingType,
      PipingSize,
      NumberOfPlumbing,
      ConstructionClassification,
      SignalType,
      ConstructionItems,
      PlumbingItems,
      Remark,
      ParentPartMode,
    }
  
    protected override WiringModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;
  
      var id = deserializer.GetString( SerializeField.Id ) ;
      var idOfToConnector = deserializer.GetString( SerializeField.IdOfToConnector ) ;
      var routeName = deserializer.GetString( SerializeField.RouteName ) ;
      var floor = deserializer.GetString( SerializeField.Floor ) ;
      var generalDisplayDeviceSymbol = deserializer.GetString( SerializeField.GeneralDisplayDeviceSymbol ) ;
      var wireType = deserializer.GetString( SerializeField.WireType ) ;
      var wireSize = deserializer.GetString( SerializeField.WireSize ) ;
      var wireStrip = deserializer.GetString( SerializeField.WireStrip ) ;
      var pipingType = deserializer.GetString( SerializeField.PipingType ) ;
      var numberOfPlumbing = deserializer.GetString( SerializeField.NumberOfPlumbing ) ;
      var pipingSize = deserializer.GetString( SerializeField.PipingSize ) ;
      var constructionClassification = deserializer.GetString( SerializeField.ConstructionClassification ) ;
      var signalType = deserializer.GetString( SerializeField.SignalType ) ;
      var constructionItems = deserializer.GetString( SerializeField.ConstructionItems ) ;
      var plumbingItems = deserializer.GetString( SerializeField.PlumbingItems ) ;
      var remark = deserializer.GetString( SerializeField.Remark ) ;
      var parentPartMode = deserializer.GetString( SerializeField.ParentPartMode ) ;
  
      return new WiringModel( id, idOfToConnector, routeName, floor, generalDisplayDeviceSymbol, wireType, wireSize, wireStrip, pipingType, pipingSize, numberOfPlumbing,constructionClassification, signalType, constructionItems, plumbingItems, remark, parentPartMode  ) ;
    }
  
    protected override ISerializerObject Serialize( Element storedElement, WiringModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;
  
      serializerObject.AddNonNull( SerializeField.Id, customTypeValue.Id ) ; 
      serializerObject.AddNonNull( SerializeField.IdOfToConnector, customTypeValue.IdOfToConnector ) ; 
      serializerObject.AddNonNull( SerializeField.RouteName, customTypeValue.RouteName ) ; 
      serializerObject.AddNonNull( SerializeField.Floor, customTypeValue.Floor ) ; 
      serializerObject.AddNonNull( SerializeField.GeneralDisplayDeviceSymbol, customTypeValue.GeneralDisplayDeviceSymbol ) ; 
      serializerObject.AddNonNull( SerializeField.WireType, customTypeValue.WireType ) ; 
      serializerObject.AddNonNull( SerializeField.WireSize, customTypeValue.WireSize ) ; 
      serializerObject.AddNonNull( SerializeField.WireStrip, customTypeValue.WireStrip ) ; 
      serializerObject.AddNonNull( SerializeField.PipingType, customTypeValue.PipingType ) ; 
      serializerObject.AddNonNull( SerializeField.PipingSize, customTypeValue.PipingSize ) ;  
      serializerObject.AddNonNull( SerializeField.ConstructionClassification, customTypeValue.ConstructionClassification ) ;  
      serializerObject.AddNonNull( SerializeField.SignalType, customTypeValue.SignalType ) ;  
      serializerObject.AddNonNull( SerializeField.ConstructionItems, customTypeValue.ConstructionItems ) ;  
      serializerObject.AddNonNull( SerializeField.PlumbingItems, customTypeValue.PlumbingItems ) ;  
      serializerObject.AddNonNull( SerializeField.Remark, customTypeValue.Remark ) ;  
      serializerObject.AddNonNull( SerializeField.ParentPartMode, customTypeValue.ParentPartMode ) ;  
      serializerObject.AddNonNull( SerializeField.NumberOfPlumbing, customTypeValue.NumberOfPlumbing ) ;  
  
      return serializerObject ;
    }
  }
}