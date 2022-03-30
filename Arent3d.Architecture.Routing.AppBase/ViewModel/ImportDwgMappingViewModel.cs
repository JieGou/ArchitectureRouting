using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ImportDwgMappingViewModel : ViewModelBase
  {
    public ObservableCollection<ImportDwgMappingModel> ImportDwgMappingModels { get ; }
    public List<FileComboboxItemType> FileItems { get ; }

    public ImportDwgMappingViewModel( List<ImportDwgMappingModel> importDwgMappingModels, List<FileComboboxItemType> fileItems )
    {
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( importDwgMappingModels ) ;
      FileItems = fileItems ;
    }
  }
}