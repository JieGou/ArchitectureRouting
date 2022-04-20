using System.Diagnostics.CodeAnalysis;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    [SuppressMessage( "ReSharper", "ConvertToUsingDeclaration" )]
    
    public class RegistrationOfBoardDataModel
    {
        public string AutoControlPanel { get ; set ; }
        public string SignalDestination { get ; set ; }
        public string StatusType { get ; set ; }
        public string StatusNumber { get ; set ; }
        public string MeasurementType { get ; set ; }
        public string MeasurementNumber { get ; set ; }
        public string Remark { get ; set ; }
        public string MaterialCode1 { get ; set ; }
        public string MaterialCode2 { get ; set ; }

        public RegistrationOfBoardDataModel(string autoControlPanel, string signalDestination, string statusType, string statusNumber, string measurementType, string measurementNumber, string remark, string materialCode1, string materialCode2)
        {
            AutoControlPanel = autoControlPanel;
            SignalDestination = signalDestination;
            StatusType = statusType;
            StatusNumber = statusNumber;
            MeasurementType = measurementType;
            MeasurementNumber = measurementNumber;
            Remark = remark;
            MaterialCode1 = materialCode1;
            MaterialCode2 = materialCode2;
        }
    }
}