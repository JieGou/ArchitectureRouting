using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Diagnostics ;
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

    private SortedDictionary<string, FromToItem>? ItemDictionary { get ; set ; }

    public FromToModel( UIApplication uiApp )
    {
      UiApp = uiApp ;
      App = UiApp.Application ;
      UiDoc = uiApp.ActiveUIDocument ;
      Doc = UiDoc.Document ;
      ItemDictionary = new SortedDictionary<string, FromToItem>() ;
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
        FromToItem.AllRoutes = allRoutes ;
        FromToItem.Doc = Doc ;
        FromToItem.UiDoc = UiDoc ;

        foreach ( var route in allRoutes ) {
          // get ChildBranches
          if ( route.HasParent() ) {
            childBranches.Add( route ) ;
          }
          // get ChildBranches
          else {
            parentFromTos.Add( route ) ;
          }
        }

        //create route fromtoitem
        foreach ( var route in parentFromTos.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
          var routeItem = new FromToItem.ParentItem() { Name = route.RouteName, ElementId = route.OwnerElement?.Id, ItemTag = "Route", DisplaySelectedFromTo = true } ;
          // store in dict
          if ( ItemDictionary != null ) {
            ItemDictionary[ route.RouteName ] = routeItem ;
          }

          // add to fromToItems
          fromToItems.Add( routeItem ) ;
          // add connector to Parent FromToItem
          AddConnectorItemToFromToItem( Doc, routeItem, route ) ;
        }

        // create branches fromtoitem
        foreach ( var c in childBranches ) {
          var branchItem = new FromToItem.BranchItem() { Name = c.RouteName, ElementId = c.OwnerElement?.Id, ItemTag = "Route", DisplaySelectedFromTo = true } ;
          var parentRouteName = c.GetParentBranches().ToList().Last().RouteName ;
          // search own parent TreeViewItem
          if ( ItemDictionary != null ) {
            ItemDictionary[ parentRouteName ].Children?.Add( branchItem ) ;
            ItemDictionary[ c.RouteName ] = branchItem ;
          }

          // add connector to branch treeviewitem
          AddConnectorItemToFromToItem( Doc, branchItem, c ) ;
        }
      }

      return fromToItems ;
    }

    /// <summary>
    /// add connector item to FromToItem's Children
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="targetItem"></param>
    /// <param name="targetRoute"></param>
    private void AddConnectorItemToFromToItem( Document? doc, FromToItem targetItem, Route targetRoute )
    {
      if ( Doc != null ) {
        var connectors = targetRoute.GetAllConnectors( Doc ) ;
        foreach ( var connector in connectors ) {
          if ( connector.Owner is FamilyInstance familyInstance ) {
            var connectorItem = new FromToItem.ConnectorItem() { Name = familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name, ElementId = connector.Owner.Id, ItemTag = "Connector", DisplaySelectedFromTo = false } ;
            targetItem?.Children?.Add( connectorItem ) ;
          }
          else {
            continue ;
          }
        }
      }
      else {
        return ;
      }
    }
  }
}