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
      CeeDModelNumber,
      CeeDSetCode,
      GeneralDisplayDeviceSymbol,
      ModelNumber,
      FloorPlanSymbol
    }

    protected override ISerializerObject Serialize( Element storedElement, CeedModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.CeeDModelNumber, customTypeValue.CeeDModelNumber ) ;
      serializerObject.AddNonNull( SerializeField.CeeDSetCode, customTypeValue.CeeDSetCode ) ;
      serializerObject.AddNonNull( SerializeField.GeneralDisplayDeviceSymbol, customTypeValue.GeneralDisplayDeviceSymbol ) ;
      serializerObject.AddNonNull( SerializeField.ModelNumber, customTypeValue.ModelNumber ) ;
      serializerObject.AddNonNull( SerializeField.FloorPlanSymbol, customTypeValue.FloorPlanSymbol ) ;

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

      return new CeedModel( ceeDModelNumber!, ceeDSetCode!, generalDisplayDeviceSymbol!, modelNumber!, floorPlanSymbol! ) ;
    }
  }
}