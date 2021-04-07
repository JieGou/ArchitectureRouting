using System.Collections.Generic ;
using Arent3d.Architecture.Routing.App.Forms ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.ViewModel
{
  public class FromToTreeViewModel : ViewModelBase
  {
    //FromToTree
    public static FromToTree? FromToTreePanel { get ; set ; }
    
    
    public static void GetSelectedRouteName(string selectedRouteName)
    {
      if ( FromToTreePanel != null ) {
        FromToTreePanel.SelectTreeViewItem(selectedRouteName);
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
    
    
  }
}