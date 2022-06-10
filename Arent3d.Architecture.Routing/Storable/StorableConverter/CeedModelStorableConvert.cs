using System.Linq ;
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
      LegendDisplay,
      CeedModelNumber,
      CeedSetCode,
      GeneralDisplayDeviceSymbol,
      ModelNumber,
      FloorPlanSymbol,
      InstrumentationSymbol,
      Name,
      Base64InstrumentationImageString,
      Base64FloorPlanImages,
      FloorPlanType,
      IsAdded,
      IsEditFloorPlan,
      IsEditInstrumentation,
      IsEditCondition,
    }

    protected override ISerializerObject Serialize( Element storedElement, CeedModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.LegendDisplay, customTypeValue.LegendDisplay ) ;
      serializerObject.AddNonNull( SerializeField.CeedModelNumber, customTypeValue.CeedModelNumber ) ;
      serializerObject.AddNonNull( SerializeField.CeedSetCode, customTypeValue.CeedSetCode ) ;
      serializerObject.AddNonNull( SerializeField.GeneralDisplayDeviceSymbol, customTypeValue.GeneralDisplayDeviceSymbol ) ;
      serializerObject.AddNonNull( SerializeField.ModelNumber, customTypeValue.ModelNumber ) ;
      serializerObject.AddNonNull( SerializeField.FloorPlanSymbol, customTypeValue.FloorPlanSymbol ) ;
      serializerObject.AddNonNull( SerializeField.InstrumentationSymbol, customTypeValue.InstrumentationSymbol ) ;
      serializerObject.AddNonNull( SerializeField.Name, customTypeValue.Name ) ;
      serializerObject.AddNonNull( SerializeField.Base64InstrumentationImageString, customTypeValue.Base64InstrumentationImageString ) ;
      serializerObject.AddNonNull( SerializeField.Base64FloorPlanImages, customTypeValue.Base64FloorPlanImages ) ;
      serializerObject.AddNullable( SerializeField.FloorPlanType, customTypeValue.FloorPlanType ) ;
      serializerObject.Add( SerializeField.IsAdded, customTypeValue.IsAdded ) ;
      serializerObject.Add( SerializeField.IsEditFloorPlan, customTypeValue.IsEditFloorPlan ) ;
      serializerObject.Add( SerializeField.IsEditInstrumentation, customTypeValue.IsEditInstrumentation ) ;
      serializerObject.Add( SerializeField.IsEditCondition, customTypeValue.IsEditCondition ) ;

      return serializerObject ;
    }

    protected override CeedModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var legendDisplay = deserializer.GetString( SerializeField.LegendDisplay ) ;
      var ceedModelNumber = deserializer.GetString( SerializeField.CeedModelNumber ) ;
      var ceedSetCode = deserializer.GetString( SerializeField.CeedSetCode ) ;
      var generalDisplayDeviceSymbol = deserializer.GetString( SerializeField.GeneralDisplayDeviceSymbol ) ;
      var modelNumber = deserializer.GetString( SerializeField.ModelNumber ) ;
      var floorPlanSymbol = deserializer.GetString( SerializeField.FloorPlanSymbol ) ;
      var instrumentationSymbol = deserializer.GetString( SerializeField.InstrumentationSymbol ) ;
      var name = deserializer.GetString( SerializeField.Name ) ;
      var base64InstrumentationImageString = deserializer.GetString( SerializeField.Base64InstrumentationImageString ) ;
      var base64FloorPlanImages = deserializer.GetString( SerializeField.Base64FloorPlanImages ) ;
      var floorPlanType = deserializer.GetString( SerializeField.FloorPlanType ) ;
      var isAdded = deserializer.GetBool( SerializeField.IsAdded ) ;
      var isEditFloorPlan = deserializer.GetBool( SerializeField.IsEditFloorPlan ) ;
      var isEditInstrumentation = deserializer.GetBool( SerializeField.IsEditInstrumentation ) ;
      var isEditCondition = deserializer.GetBool( SerializeField.IsEditCondition ) ;

      return new CeedModel( legendDisplay!, ceedModelNumber!, ceedSetCode!, generalDisplayDeviceSymbol!, modelNumber!, floorPlanSymbol!, instrumentationSymbol!, name!, base64InstrumentationImageString!, base64FloorPlanImages!, floorPlanType!, isAdded!, isEditFloorPlan!, isEditInstrumentation!, isEditCondition! ) ;
    }
  }
}
