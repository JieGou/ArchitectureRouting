using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class FromToTreeViewModel : ViewModelBase
  {
    public FromToModel? FromToModel { get ; set ; }

    //FromToTree
    public static FromToTree? FromToTreePanel { get ; set ; }

    public IReadOnlyCollection<FromToItem>? FromToItems { get ; set ; }

    public FromToTreeViewModel()
    {
    }

    /// <summary>
    /// set FromToItems to create TreeView
    /// </summary>
    public void SetFromToItems( AddInType addInType, FromToItemsUiBase fromToItemsUiBase )
    {
      FromToItems = FromToModel?.GetFromToData( addInType, fromToItemsUiBase ) ;
    }

    public static void GetSelectedElementId( string? elementUniqueId )
    {
      if ( FromToTreePanel != null ) {
        FromToTreePanel.SelectTreeViewItem( elementUniqueId ) ;
      }
      else {
        return ;
      }
    }

    public static void ClearSelection()
    {
      if ( FromToTreePanel != null ) {
        FromToTreePanel.ClearSelection() ;
      }
      else {
        return ;
      }
    }
  }
}