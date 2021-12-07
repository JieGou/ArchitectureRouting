using Arent3d.Architecture.Routing.Storable.Model;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class CeeDDetailInformationViewModel : ViewModelBase
    {
        public CeeDDetailInformationModel CeeDDetailInformationModel { get; }

        public CeeDDetailInformationViewModel(CeeDDetailInformationModel ceeDDetailInformationModel)
        {
            CeeDDetailInformationModel = ceeDDetailInformationModel;
        }
    }
}
