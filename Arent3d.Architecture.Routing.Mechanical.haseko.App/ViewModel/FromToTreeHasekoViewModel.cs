using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Mechanical.Haseko.App.Forms ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.ViewModel
{
  public class FromToTreeHasekoViewModel : ViewModelBase
  {
    public FromToModel? FromToModel { get ; set ; }

    //FromToTreeHaseko
    public static FromToTreeHaseko? FromToTreeHasekoPanel { get ; set ; }

    public IReadOnlyCollection<FromToItem>? FromToItems { get ; set ; }

    public FromToTreeHasekoViewModel() 
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
      if ( FromToTreeHasekoPanel != null ) {
        FromToTreeHasekoPanel.SelectTreeViewItem( elementUniqueId ) ;
      }
      else {
        return ;
      }
    }

    public static void ClearSelection()
    {
      if ( FromToTreeHasekoPanel != null ) {
        FromToTreeHasekoPanel.ClearSelection() ;
      }
      else {
        return ;
      }
    }
  }
}