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
    private UIApplication? UiApp = null ;
    private Application? App = null ;
    private UIDocument? UiDoc = null ;
    private Document? Doc = null ;

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
    public ObservableCollection<FromToItem> GetFromtToData()
    {
      // codes below are in developping
      var childBranches = new List<Route>() ;

      var parentFromTos = new List<Route>() ;

      var fromToItems = new ObservableCollection<FromToItem>() ;
      if ( UiDoc != null ) {
        var allRoutes = UiDoc.Document.CollectRoutes().ToList() ;
        

        foreach ( var route in allRoutes ) {
          // get ChildBranches
          if ( route.HasParent() ) {
            childBranches.Add( route ) ;
          }
          // get ChildBranches
          else {
            parentFromTos.Add( route ) ;
            var parentFromToItem = new FromToItem { Name = route.RouteName, Children = new List<FromToItem>()} ;
            fromToItems.Add( parentFromToItem ) ;
            //fromToItems.Add();
            if ( Doc != null ) {
              var connectors = route.GetAllConnectors( Doc ) ;
              foreach ( var connector in connectors ) {
                if ( connector.Owner is FamilyInstance familyInstance ) {
                  parentFromToItem.Children?.Add( new FromToItem { Name = familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name } ) ;
                }
                else {
                  continue ;
                }
              }
            }
          }
          //return fromToItems ;
        }
      }
      return fromToItems ;
    }
  }
}
  