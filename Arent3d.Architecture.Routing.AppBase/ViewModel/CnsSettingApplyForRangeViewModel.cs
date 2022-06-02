using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Data ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CnsSettingApplyForRangeViewModel : NotifyPropertyChanged
  {
    public ObservableCollection<string> ConstructionItemList { get ; set ; }
    public List<CnsSettingApplyConstructionItem> CnsSettingApplyConstructionItems { get ; set ; }
    
    public CnsSettingApplyForRangeViewModel( List<CnsSettingApplyConstructionItem> cnsSettingApplyConstructionItems, ObservableCollection<string> constructionItemList )
    {
      ConstructionItemList = constructionItemList ;
      CnsSettingApplyConstructionItems = cnsSettingApplyConstructionItems ;
    }
  }
}