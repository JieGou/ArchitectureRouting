using System.Collections.ObjectModel ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class CnsSettingApplyConstructionItem
  {
    public int ItemIndex { get ; set ; }
    public string OldConstructionItem { get ; set ; }
    public string UniqueId { get ; set ; }
    public string NewConstructionItem { get ; set ; }
    
    public ObservableCollection<string> AvailiableConstructionItem { get ; set ; }

    public CnsSettingApplyConstructionItem(string uniqueId, int itemIndex, string oldConstructionItem, string newConstructionItem, ObservableCollection<string> availiableConstructionItem )
    {
      UniqueId = uniqueId ;
      ItemIndex = itemIndex ;
      OldConstructionItem = oldConstructionItem ;
      NewConstructionItem = newConstructionItem ;
      AvailiableConstructionItem = availiableConstructionItem ;
    }
  }
}