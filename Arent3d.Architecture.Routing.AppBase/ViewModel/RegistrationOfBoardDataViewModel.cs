using System.Collections.Generic;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class RegistrationOfBoardDataViewModel : ViewModelBase
    {
        public List<RegistrationOfBoardDataModel> RegistrationOfBoardDataModels { get ; }
        public RegistrationOfBoardDataStorable RegistrationOfBoardDataStorable { get ; }
        
        public RegistrationOfBoardDataViewModel( RegistrationOfBoardDataStorable registrationOfBoardDataStorable )
        {
            RegistrationOfBoardDataStorable = registrationOfBoardDataStorable ;
            RegistrationOfBoardDataModels = registrationOfBoardDataStorable.RegistrationOfBoardData ;
        }
        
        public RegistrationOfBoardDataViewModel( RegistrationOfBoardDataStorable registrationOfBoardDataStorable, List<RegistrationOfBoardDataModel> registrationOfBoardDataModels )
        {
            RegistrationOfBoardDataStorable = registrationOfBoardDataStorable ;
            RegistrationOfBoardDataModels = registrationOfBoardDataModels ;
        }
    }
}