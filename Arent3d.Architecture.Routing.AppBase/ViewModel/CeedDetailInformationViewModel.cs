using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class CeedDetailInformationViewModel : ViewModelBase
    {
        public CeedDetailInformationModel CeedDetailInformationModel { get; }

        public CeedDetailInformationViewModel(CeedDetailInformationModel ceedDetailInformationModel)
        {
            CeedDetailInformationModel = ceedDetailInformationModel;
        }
    }
}
