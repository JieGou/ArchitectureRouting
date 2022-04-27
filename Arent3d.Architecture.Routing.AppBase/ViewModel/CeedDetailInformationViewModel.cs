using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
    public class CeedDetailInformationViewModel : NotifyPropertyChanged
    {

        private readonly Document _document ;

        public ObservableCollection<QueryData> QueryData { get ; set ; } = new() ;
        
        private CeedDetailInformationModel? _ceedDetailInformationModel ;
        public CeedDetailInformationModel CeedDetailInformationModel
        {
            get { return _ceedDetailInformationModel ??= new CeedDetailInformationModel( QueryData, string.Empty ) ; }
            set
            {
                _ceedDetailInformationModel = value ;
                OnPropertyChanged();
            }
        }

        private List<CeedModel>? _ceedModels ;
        public List<CeedModel> CeedModels => _ceedModels ??= _document.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>() ;

        private CsvStorable? _csvStorable ;
        public CsvStorable CsvStorable => _csvStorable ??= _document.GetCsvStorable() ;
        
        private List<HiroiSetMasterModel>? _hiroiSetMasterNormalModels ;
        public List<HiroiSetMasterModel> HiroiSetMasterNormalModels => _hiroiSetMasterNormalModels ??= CsvStorable.HiroiSetMasterNormalModelData ;
        
        private List<HiroiMasterModel>? _hiroiMasterModels ;
        public List<HiroiMasterModel> HiroiMasterModels => _hiroiMasterModels ??= CsvStorable.HiroiMasterModelData ;
        
        private List<HiroiSetCdMasterModel>? _hiroiSetCdMasterModels ;
        public List<HiroiSetCdMasterModel> HiroiSetCdMasterModels => _hiroiSetCdMasterModels ??= CsvStorable.HiroiSetCdMasterNormalModelData ;
        
        private string _setCode ;

        public string SetCode
        {
            get => _setCode ;
            set
            {
                _setCode = value ;
                OnPropertyChanged();
            }
        }
        
        public CeedDetailInformationViewModel(Document document, string pickedText)
        {
            _document = document ;
            _setCode = pickedText ;
        }
    }
}
