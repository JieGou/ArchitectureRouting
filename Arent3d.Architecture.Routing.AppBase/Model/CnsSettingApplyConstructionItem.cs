namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class CnsSettingApplyConstructionItem
  {
    public int ItemIndex { get ; set ; }
    public string OldConstructionItem { get ; set ; }
    public string NewConstructionItem { get ; set ; }

    public CnsSettingApplyConstructionItem(int itemIndex, string oldConstructionItem, string newConstructionItem )
    {
      ItemIndex = itemIndex ;
      OldConstructionItem = oldConstructionItem ;
      NewConstructionItem = newConstructionItem ;
    }
  }
}