using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.App.Forms ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Model
{
  public class FromToModel
  {
    private UIApplication UiApp ;
    private Application App ;
    private UIDocument UiDoc ;
    private Document Doc ;

    public FromToModel( UIApplication uiApp )
    {
      UiApp = uiApp ;
      App = UiApp.Application ;
      UiDoc = uiApp.ActiveUIDocument ;
      Doc = UiDoc.Document ;
    }

    /// <summary>
    /// return Hierarchical FromToData for TreeView
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<FromToItem> GetFromtToData()
    {
      var allRoutes = UiDoc.Document.CollectRoutes().ToList() ;

      var fromToItems = FromToItem.CreateRouteFromToItems( Doc, UiDoc, allRoutes ) ;

      return fromToItems.ToList() ;
    }
  }
}