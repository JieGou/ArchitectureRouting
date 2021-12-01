using System.Collections.ObjectModel;
using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class ConduitInformationViewModel : ViewModelBase
    {
        public ObservableCollection<ConduitInformationModel> ConduitInformationModels { get; set; }

        public ConduitInformationViewModel(ObservableCollection<ConduitInformationModel> conduitInformationModels)
        {
            ConduitInformationModels = conduitInformationModels;
        }
    }
}
