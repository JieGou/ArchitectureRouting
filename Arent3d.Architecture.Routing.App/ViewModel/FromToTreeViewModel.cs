using System.Collections.Generic ;
using Arent3d.Architecture.Routing.App.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FromToTreeViewModel : ViewModelBase
  {
    //FromToTree
    public static FromToTree? FromToTreePanel { get ; set ; }


    public static void GetSelectedElementId( ElementId? elementId )
    {
      if ( FromToTreePanel != null ) {
        FromToTreePanel.SelectTreeViewItem( elementId ) ;
      }
      else {
        return ;
      }
    }

    public IEnumerable<Route> GetAllRoutes( UIDocument uiDocument )
    {
      var allRoutes = uiDocument.Document.CollectRoutes() ;

      return allRoutes ;
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