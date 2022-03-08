using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Windows.Documents ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ImportDwgMappingViewModel : ViewModelBase
  {
    public ObservableCollection<ImportDwgMappingModel> ImportDwgMappingModels { get ; set ; }
    
    public ICommand SaveCommand { get ; set ; }
    
    public ImportDwgMappingViewModel(List<ImportDwgMappingModel> importDwgMappingModels)
    {
      ImportDwgMappingModels = new ObservableCollection<ImportDwgMappingModel>( importDwgMappingModels ) ;
      
      SaveCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { ImportDwgData() ; } // Execute()
      ) ;
    }

    private void ImportDwgData()
    {
      
    }
  }
}