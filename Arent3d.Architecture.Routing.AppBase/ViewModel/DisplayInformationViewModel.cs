using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class DisplayInformationViewModel : ViewModelBase
    {
        public DisplayInformationModel DisplayInformationModel { get; }

        public DisplayInformationViewModel(DisplayInformationModel displayInformationModels)
        {
            DisplayInformationModel = displayInformationModels;
        }
    }
}