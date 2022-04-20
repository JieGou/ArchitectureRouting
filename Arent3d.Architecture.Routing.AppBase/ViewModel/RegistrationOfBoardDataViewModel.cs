using System.Collections.Generic;
using System.Linq;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class RegistrationOfBoardDataViewModel : ViewModelBase
    {
        public List<RegistrationOfBoardDataModel> RegistrationOfBoardDataModels { get ; }
        public RegistrationOfBoardDataStorable RegistrationOfBoardDataStorable { get ; }
        public readonly List<string> AutoControlPanels = new() ;
        public readonly List<string> SignalDestinations = new() ;
        
        public RegistrationOfBoardDataViewModel( RegistrationOfBoardDataStorable registrationOfBoardDataStorable )
        {
            RegistrationOfBoardDataStorable = registrationOfBoardDataStorable ;
            RegistrationOfBoardDataModels = registrationOfBoardDataStorable.RegistrationOfBoardData ;
            AddAutoSignal(RegistrationOfBoardDataModels);
        }
        
        public RegistrationOfBoardDataViewModel( RegistrationOfBoardDataStorable registrationOfBoardDataStorable, List<RegistrationOfBoardDataModel> registrationOfBoardDataModels )
        {
            RegistrationOfBoardDataStorable = registrationOfBoardDataStorable ;
            RegistrationOfBoardDataModels = registrationOfBoardDataModels ;
            AddAutoSignal(registrationOfBoardDataModels);
        }
        
        private void AddAutoSignal( IReadOnlyCollection<RegistrationOfBoardDataModel> registrationOfBoardDataModels )
        {
            foreach ( var registrationOfBoardDataModel in registrationOfBoardDataModels.Where( registrationOfBoardDataModel => ! string.IsNullOrEmpty( registrationOfBoardDataModel.AutoControlPanel ) ) ) {
                if ( ! AutoControlPanels.Contains( registrationOfBoardDataModel.AutoControlPanel ) ) AutoControlPanels.Add( registrationOfBoardDataModel.AutoControlPanel ) ;
            }

            foreach ( var registrationOfBoardDataModel in registrationOfBoardDataModels.Where( registrationOfBoardDataModel => ! string.IsNullOrEmpty( registrationOfBoardDataModel.SignalDestination ) ) ) {
                if ( ! SignalDestinations.Contains( registrationOfBoardDataModel.SignalDestination ) ) SignalDestinations.Add( registrationOfBoardDataModel.SignalDestination ) ;
            }
        }
    }
}