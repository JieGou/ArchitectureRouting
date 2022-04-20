using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Utility.Serialization;
using Autodesk.Revit.DB;

namespace Arent3d.Architecture.Routing.Storable.StorableConverter
{
    [StorableConverterOf( typeof( RegistrationOfBoardDataModel ) )]
    
    
    public class RegistrationOfBoardDataModelStorableConvert : StorableConverterBase<RegistrationOfBoardDataModel>
    {
        private enum SerializeField
        {
            AutoControlPanel,
            SignalDestination,
            StatusType,      
            StatusNumber,    
            MeasurementType, 
            MeasurementNumber,
            Remark,
            MaterialCode1,
            MaterialCode2
        }

        protected override RegistrationOfBoardDataModel Deserialize(Element storedElement, IDeserializerObject deserializerObject)
        {
            var deserializer = deserializerObject.Of<SerializeField>() ;

            var autoControlPanel = deserializer.GetString( SerializeField.AutoControlPanel ) ;
            var signalDestination = deserializer.GetString( SerializeField.SignalDestination ) ;
            var statusType = deserializer.GetString( SerializeField.StatusType ) ;
            var statusNumber = deserializer.GetString( SerializeField.StatusNumber ) ;
            var measurementType = deserializer.GetString( SerializeField.MeasurementType ) ;
            var measurementNumber = deserializer.GetString( SerializeField.MeasurementNumber ) ;
            var remark = deserializer.GetString( SerializeField.Remark ) ;
            var materialCode1 = deserializer.GetString( SerializeField.MaterialCode1 ) ;
            var materialCode2 = deserializer.GetString( SerializeField.MaterialCode2 ) ;

            return new RegistrationOfBoardDataModel( autoControlPanel!, signalDestination!, statusType!, statusNumber!, measurementType!, measurementNumber!, remark!, materialCode1!, materialCode2! ) ;
        }

        protected override ISerializerObject Serialize(Element storedElement, RegistrationOfBoardDataModel customTypeValue)
        {
            var serializerObject = new SerializerObject<SerializeField>() ;

            serializerObject.AddNonNull( SerializeField.AutoControlPanel, customTypeValue.AutoControlPanel ) ;
            serializerObject.AddNonNull( SerializeField.SignalDestination, customTypeValue.SignalDestination ) ;
            serializerObject.AddNonNull( SerializeField.StatusType, customTypeValue.StatusType ) ;
            serializerObject.AddNonNull( SerializeField.StatusNumber, customTypeValue.StatusNumber ) ;
            serializerObject.AddNonNull( SerializeField.MeasurementType, customTypeValue.MeasurementType ) ;
            serializerObject.AddNonNull( SerializeField.MeasurementNumber, customTypeValue.MeasurementNumber ) ;
            serializerObject.AddNonNull( SerializeField.Remark, customTypeValue.Remark ) ;
            serializerObject.AddNonNull( SerializeField.MaterialCode1, customTypeValue.MaterialCode1 ) ;
            serializerObject.AddNonNull( SerializeField.MaterialCode2, customTypeValue.MaterialCode2 ) ;
            
            return serializerObject ;
        }
    }
}