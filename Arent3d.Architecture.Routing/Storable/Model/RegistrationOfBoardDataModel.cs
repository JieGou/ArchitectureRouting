using System.Diagnostics.CodeAnalysis;

namespace Arent3d.Architecture.Routing.Storable.Model
{
    [SuppressMessage( "ReSharper", "ConvertToUsingDeclaration" )]
    
    public class RegistrationOfBoardDataModel
    {
        public string AutoControlPanel { get ; set ; }
        public string SignalDestination { get ; set ; }
        
        public string Kind1 { get ; set ; }
        public string Number1 { get ; set ; }
        public string Kind2 { get ; set ; }
        public string Number2 { get ; set ; }
        public string Remark { get ; set ; }
        public string MaterialCode1 { get ; set ; }
        public string MaterialCode2 { get ; set ; }

        public RegistrationOfBoardDataModel()
        {
            AutoControlPanel = string.Empty ;
            SignalDestination = string.Empty ;
            Kind1 = string.Empty ;
            Number1 = string.Empty ;
            Kind2 = string.Empty ;
            Number2 = string.Empty ;
            Remark = string.Empty ;
            MaterialCode1 = string.Empty ;
            MaterialCode2 = string.Empty ;
        }

        public RegistrationOfBoardDataModel(string autoControlPanel, string signalDestination, string kind1, string number1, string kind2, string number2, string remark, string materialCode1, string materialCode2)
        {
            AutoControlPanel = autoControlPanel;
            SignalDestination = signalDestination;
            Kind1 = kind1;
            Number1 = number1;
            Kind2 = kind2;
            Number2 = number2;
            Remark = remark;
            MaterialCode1 = materialCode1;
            MaterialCode2 = materialCode2;
        }
    }
}