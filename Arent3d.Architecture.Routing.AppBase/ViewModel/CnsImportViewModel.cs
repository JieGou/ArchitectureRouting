using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.Collections.Generic ;
using System.Collections.ObjectModel;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CnsImportViewModel : ViewModelBase
  {
    public ObservableCollection<CnsImportModel> CnsImportModels { get ; }

    public CnsImportStorable CnsImportStorable { get ; }
    
    public CnsImportViewModel( CnsImportStorable cnsStorables )
    {
      CnsImportStorable = cnsStorables ;
      CnsImportModels = new ObservableCollection<CnsImportModel>(cnsStorables.CnsImportData) ;
    }
  }
}