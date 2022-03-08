using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Windows.Documents ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ImportDwgMappingViewModel : ViewModelBase
  {
    public ObservableCollection<ImportDwgMappingModel> ImportDwgMappingModels { get ; set ; }
    public List<FileComboboxItemType> FileItems { get ; set ; }
    
    public ICommand SaveCommand { get ; set ; }
    
    public ImportDwgMappingViewModel(List<ImportDwgMappingModel> importDwgMappingModels, List<FileComboboxItemType> fileItems)
    {
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( importDwgMappingModels ) ;
      FileItems = fileItems ;
      
      SaveCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { ImportDwgData() ; } // Execute()
      ) ;
    }

    private void ImportDwgData()
    {
      
    }
  }
}