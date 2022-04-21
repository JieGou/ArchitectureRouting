using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
  [StorableConverterOf( typeof( RegistrationOfBoardDataModel ) )]
  public class RegistrationOfBoardDataModelStorableConvert : StorableConverterBase<RegistrationOfBoardDataModel>
  {
    private enum SerializeField
    {
      AutoControlPanel,
      SignalDestination,
      Kind1,
      Number1,
      Kind2,
      Number2,
      Remark,
      MaterialCode1,
      MaterialCode2
    }

    protected override RegistrationOfBoardDataModel Deserialize( Element storedElement, IDeserializerObject deserializerObject )
    {
      var deserializer = deserializerObject.Of<SerializeField>() ;

      var autoControlPanel = deserializer.GetString( SerializeField.AutoControlPanel ) ;
      var signalDestination = deserializer.GetString( SerializeField.SignalDestination ) ;
      var kind1 = deserializer.GetString( SerializeField.Kind1 ) ;
      var number1 = deserializer.GetString( SerializeField.Number1 ) ;
      var kind2 = deserializer.GetString( SerializeField.Kind2 ) ;
      var number2 = deserializer.GetString( SerializeField.Number2 ) ;
      var remark = deserializer.GetString( SerializeField.Remark ) ;
      var materialCode1 = deserializer.GetString( SerializeField.MaterialCode1 ) ;
      var materialCode2 = deserializer.GetString( SerializeField.MaterialCode2 ) ;

      return new RegistrationOfBoardDataModel( autoControlPanel!, signalDestination!, kind1!, number1!, kind2!, number2!, remark!, materialCode1!, materialCode2! ) ;
    }

    protected override ISerializerObject Serialize( Element storedElement, RegistrationOfBoardDataModel customTypeValue )
    {
      var serializerObject = new SerializerObject<SerializeField>() ;

      serializerObject.AddNonNull( SerializeField.AutoControlPanel, customTypeValue.AutoControlPanel ) ;
      serializerObject.AddNonNull( SerializeField.SignalDestination, customTypeValue.SignalDestination ) ;
      serializerObject.AddNonNull( SerializeField.Kind1, customTypeValue.Kind1 ) ;
      serializerObject.AddNonNull( SerializeField.Number1, customTypeValue.Number1 ) ;
      serializerObject.AddNonNull( SerializeField.Kind2, customTypeValue.Kind2 ) ;
      serializerObject.AddNonNull( SerializeField.Number2, customTypeValue.Number2 ) ;
      serializerObject.AddNonNull( SerializeField.Remark, customTypeValue.Remark ) ;
      serializerObject.AddNonNull( SerializeField.MaterialCode1, customTypeValue.MaterialCode1 ) ;
      serializerObject.AddNonNull( SerializeField.MaterialCode2, customTypeValue.MaterialCode2 ) ;

      return serializerObject ;
    }
  }
}