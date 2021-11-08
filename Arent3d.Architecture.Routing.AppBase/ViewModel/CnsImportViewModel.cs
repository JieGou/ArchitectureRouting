using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using System.Collections.Generic ;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CnsImportViewModel : ViewModelBase
  {
    public ObservableCollection<CnsImportModel> CnsImportModels { get; set; }

    public CnsImportStorable CnsImportStorable { get; set; }
    
    public CnsImportViewModel( CnsImportStorable cnsStorables )
    {
      CnsImportModels = new ObservableCollection<CnsImportModel>();
      CnsImportStorable = cnsStorables ;
      cnsStorables.CnsImportData.ForEach(x => CnsImportModels.Add(x));

    }
  }
}