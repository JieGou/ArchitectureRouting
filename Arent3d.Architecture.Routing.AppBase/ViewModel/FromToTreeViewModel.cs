using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

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
    public void SetFromToItems(AddInType addInType)
    {
      FromToItems = FromToModel?.GetFromtToData(addInType) ;
    }

    public static void GetSelectedElementId( ElementId? elementId )
    {
      if ( FromToTreePanel != null ) {
        FromToTreePanel.SelectTreeViewItem( elementId ) ;
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