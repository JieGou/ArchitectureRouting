using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Model
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
    public IReadOnlyCollection<FromToItem> GetFromtToData(AddInType addInType, FromToItemsUiBase fromToItemsUiBase)
    {
      var allRoutes = UiDoc.Document.CollectRoutes(addInType).ToList() ;

      var fromToItems = FromToItem.CreateRouteFromToItems( Doc, UiDoc, allRoutes, fromToItemsUiBase ) ;

      return fromToItems.ToList() ;
    }
  }
}