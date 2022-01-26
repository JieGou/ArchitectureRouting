﻿using System.Linq ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( CeedModel ) )]
  public class CeedModelStorableConvert : StorableConverterBase<CeedModel>
  {
    private enum SerializeField
    {
      CeeDModelNumber,
      CeeDSetCode,
      GeneralDisplayDeviceSymbol,
      ModelNumber,
      FloorPlanSymbol,
      InstrumentationSymbol,
      Name,
      Condition,
      Base64InstrumentationImageString,
      Base64FloorPlanImages,
      FamilyTypeName
    }

    protected override ISerializerObject Serialize( Element storedElement, CeedModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.CeeDModelNumber, customTypeValue.CeeDModelNumber ) ;
      serializerObject.AddNonNull( SerializeField.CeeDSetCode, customTypeValue.CeeDSetCode ) ;
      serializerObject.AddNonNull( SerializeField.GeneralDisplayDeviceSymbol, customTypeValue.GeneralDisplayDeviceSymbol ) ;
      serializerObject.AddNonNull( SerializeField.ModelNumber, customTypeValue.ModelNumber ) ;
      serializerObject.AddNonNull( SerializeField.FloorPlanSymbol, customTypeValue.FloorPlanSymbol ) ;
      serializerObject.AddNonNull( SerializeField.InstrumentationSymbol, customTypeValue.InstrumentationSymbol ) ;
      serializerObject.AddNonNull( SerializeField.Name, customTypeValue.Name ) ;
      serializerObject.AddNonNull( SerializeField.Condition, customTypeValue.Condition ) ;
      serializerObject.AddNonNull( SerializeField.Base64InstrumentationImageString, customTypeValue.Base64InstrumentationImageString ) ;
      serializerObject.AddNonNull( SerializeField.Base64FloorPlanImages, customTypeValue.Base64FloorPlanImages ) ;
      serializerObject.AddNullable( SerializeField.FamilyTypeName, customTypeValue.FamilyTypeName ) ;

      return serializerObject ;
    }

    protected override CeedModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var ceeDModelNumber = deserializer.GetString( SerializeField.CeeDModelNumber ) ;
      var ceeDSetCode = deserializer.GetString( SerializeField.CeeDSetCode ) ;
      var generalDisplayDeviceSymbol = deserializer.GetString( SerializeField.GeneralDisplayDeviceSymbol ) ;
      var modelNumber = deserializer.GetString( SerializeField.ModelNumber ) ;
      var floorPlanSymbol = deserializer.GetString( SerializeField.FloorPlanSymbol ) ;
      var instrumentationSymbol = deserializer.GetString( SerializeField.InstrumentationSymbol ) ;
      var name = deserializer.GetString( SerializeField.Name ) ;
      var condition = deserializer.GetString( SerializeField.Condition ) ;
      var base64InstrumentationImageString = deserializer.GetString( SerializeField.Base64InstrumentationImageString ) ;
      var base64FloorPlanImages = deserializer.GetString( SerializeField.Base64FloorPlanImages ) ;
      var familyTypeName = deserializer.GetString( SerializeField.FamilyTypeName ) ;

      return new CeedModel( ceeDModelNumber!, ceeDSetCode!, generalDisplayDeviceSymbol!, modelNumber!, floorPlanSymbol!, instrumentationSymbol!, name!, condition!, base64InstrumentationImageString!, base64FloorPlanImages!, familyTypeName! ) ;
    }
  }
}
