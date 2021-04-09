using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Diagnostics ;
using System.IO.Packaging ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Media ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FromToTree : Page, IDockablePaneProvider
  {
    private Document? Doc { get ; set ; }
    private UIDocument? UiDoc { get ; set ; }
    private IReadOnlyCollection<Route>? AllRoutes { get ; set ; }

    private SortedDictionary<string, TreeViewItem>? ItemDictionary { get ; set ; }


    public FromToTree()
    {
      InitializeComponent() ;
      ItemDictionary = new SortedDictionary<string, TreeViewItem>() ;
    }


    public void SetupDockablePane( DockablePaneProviderData data )
    {
      data.FrameworkElement = this as FrameworkElement ;
      // Define initial pane position in Revit ui
      data.InitialState = new DockablePaneState { DockPosition = DockPosition.Tabbed, TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser } ;
    }

    /// <summary>
    /// clear selection and SelectedFromToDialog
    /// </summary>
    public void ClearSelection()
    {
      ClearSelectedItem() ;
      SelectedFromTo.ClearDialog() ;
    }

    /// <summary>
    /// initialize page
    /// </summary>
    /// <param name="uiApplication"></param>
    public void CustomInitiator( UIApplication uiApplication )
    {
      Doc = uiApplication.ActiveUIDocument.Document ;
      UiDoc = uiApplication.ActiveUIDocument ;
      AllRoutes = UiDoc.Document.CollectRoutes().ToList() ;
      // call the treeview display method
      DisplayTreeViewItem( AllRoutes ) ;
    }

    /// <summary>
    /// set TreeViewItems
    /// </summary>
    private void DisplayTreeViewItem( IReadOnlyCollection<Route> allRoutes )
    {
      if ( FromToTreeView != null ) {
        //clear selection;
        ClearSelectedItem() ;
        // clear items first
        FromToTreeView.Items.Clear() ;

        if ( ItemDictionary != null ) {
          ItemDictionary.Clear() ;

          var childBranches = new List<Route>() ;

          var parentFromTos = new List<Route>() ;


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

          // create Route tree
          foreach ( var route in parentFromTos.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
            // create route treeviewitem/
            TreeViewItem routeItem = new TreeViewItem() { Uid = route.OwnerElement?.Id.ToString(), Tag = "Route" } ;
            SetHeaderStackPanel( routeItem, route.RouteName ) ;
            // store in dict
            ItemDictionary[ route.RouteName ] = routeItem ;
            // add to treeview
            FromToTreeView.Items.Add( routeItem ) ;
            // add connector to branch treeviewitem
            AddConnectorItemToTreeViewItem( Doc, routeItem, route ) ;
            /*foreach ( var subRoute in route.SubRoutes ) {
              TreeViewItem subrouteItem = new TreeViewItem() { Uid = route.OwnerElement?.Id.ToString(), Tag = "Route" } ;
            }*/
          }

          // create branches tree
          foreach ( var c in childBranches ) {
            TreeViewItem branchItem = new TreeViewItem() { Uid = c.OwnerElement?.Id.ToString(), Tag = "Route" } ;
            SetHeaderStackPanel( branchItem, c.RouteName ) ;
            var parentRouteName = c.GetParentBranches().ToList().Last().RouteName ;
            // search own parent TreeViewItem
            ItemDictionary[ parentRouteName ].Items.Add( branchItem ) ;
            ItemDictionary[ c.RouteName ] = branchItem ;
            // add connector to branch treeviewitem
            AddConnectorItemToTreeViewItem( Doc, branchItem, c ) ;
          }
        }
      }
    }

    /// <summary>
    /// GetParentTree recursively
    /// </summary>
    /// <param name="targetItem"></param>
    /// <param name="parentName"></param>
    /// <returns></returns>
    private TreeViewItem? GetParentTreeViewItem( TreeViewItem targetItem, string parentName )
    {
      foreach ( var item in targetItem.Items ) {
        // sreach in current
        if ( item is TreeViewItem treeViewItem ) {
          if ( treeViewItem.Header.Equals( parentName ) ) {
            return treeViewItem ;
          }

          // search in childs
          TreeViewItem? childItem = this.GetParentTreeViewItem( treeViewItem, parentName ) ;
          if ( childItem != null ) {
            return childItem ;
          }
        }
      }

      return null ;
    }

    /// <summary>
    /// Add Conncecotor to FromTo TreeVieItem
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="targetItem"></param>
    /// <param name="targetRoute"></param>
    private void AddConnectorItemToTreeViewItem( Document? doc, TreeViewItem targetItem, Route targetRoute )
    {
      if ( Doc != null ) {
        var connectors = targetRoute.GetAllConnectors( Doc ) ;
        foreach ( var connector in connectors ) {
          if ( connector.Owner is FamilyInstance familyInstance ) {
            TreeViewItem viewItem = new TreeViewItem() { Uid = connector.Owner.Id.ToString(), Tag = "Connector" } ;
            SetHeaderStackPanel( viewItem, familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name ) ;
            targetItem.Items.Add( viewItem ) ;
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

    /// <summary>
    /// set headerstackpanel to treeviewitem
    /// </summary>
    /// <param name="treeViewItem"></param>
    /// <param name="routeName"></param>
    private void SetHeaderStackPanel( TreeViewItem treeViewItem, string routeName )
    {
      BitmapImage bmi = new BitmapImage() ;

      switch ( treeViewItem.Tag ) {
        case "Route" :
          bmi = new BitmapImage( new Uri( "../../resources/MEP.ico", UriKind.Relative ) ) ;
          break ;
        case "Connector" :
          bmi = new BitmapImage( new Uri( "../../resources/InsertBranchPoint.png", UriKind.Relative ) ) ;
          break ;
      }

      // create stackpanel
      StackPanel sp = new StackPanel() ;
      sp.Orientation = Orientation.Horizontal ;
      Image img = new Image() ;
      img.Source = bmi ;
      img.Height = 15 ;
      img.Width = 15 ;
      sp.Children.Add( img ) ;
      TextBlock tb = new TextBlock() ;
      tb.Text = routeName ;
      sp.Children.Add( tb ) ;
      treeViewItem.Header = sp ;
    }

    /// <summary>
    /// Set SelectedFromtTo Dialog by Selected Route
    /// </summary>
    /// <param name="route"></param>
    private void DisplaySelectedFromTo( Route route )
    {
      if ( Doc != null && UiDoc != null ) {
        SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, route ) ;
      }

      SelectedFromTo.UpdateFromToParameters( SelectedFromToViewModel.Diameters, SelectedFromToViewModel.SystemTypes, SelectedFromToViewModel.CurveTypes ) ;

      SelectedFromTo.DiameterIndex = SelectedFromToViewModel.DiameterIndex ;

      SelectedFromTo.SystemTypeIndex = SelectedFromToViewModel.SystemTypeIndex ;

      SelectedFromTo.CurveTypeIndex = SelectedFromToViewModel.CurveTypeIndex ;

      SelectedFromTo.CurveTypeLabel = SelectedFromTo.GetTypeLabel( SelectedFromTo.CurveTypes[ (int) SelectedFromTo.CurveTypeIndex ].GetType().Name ) ;

      SelectedFromTo.CurrentDirect = SelectedFromToViewModel.IsDirect ;

      SelectedFromTo.ResetDialog() ;
    }

    /// <summary>
    /// event on selected FromToTreeView to select FromTo Element
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FromToTreeView_OnSelectedItemChanged( object sender, RoutedPropertyChangedEventArgs<object> e )
    {
      if ( FromToTreeView.SelectedItem is not TreeViewItem selectedItem ) {
        return ;
      }

      List<ElementId>? targetElements = new List<ElementId>() ;
      switch ( selectedItem.Tag ) {
        case "Route" :
          var selectedRoute = AllRoutes?.ToList().Find( r => r.OwnerElement?.Id == UIHelper.GetElementIdFromViewItem( selectedItem ) ) ;

          if ( selectedRoute != null ) {
            targetElements = Doc?.GetAllElementsOfRouteName<Element>( selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
            //Select targetElements
            UiDoc?.Selection.SetElementIds( targetElements ) ;
            DisplaySelectedFromTo( selectedRoute ) ;
          }

          break ;
        case "Connector" :
          targetElements.Add( UIHelper.GetElementIdFromViewItem( selectedItem ) ) ;
          UiDoc?.Selection.SetElementIds( targetElements ) ;
          SelectedFromTo.ClearDialog();
          break ;
      }
    }

    /// <summary>
    /// event on MouseDoubleClicked to focus selected FromTo Element
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FromToTreeView_OnMouseDoubleClick( object sender, MouseButtonEventArgs e )
    {
      if ( FromToTreeView.SelectedItem is not TreeViewItem selectedItem ) {
        return ;
      }

      selectedItem.IsExpanded = false ;

      List<ElementId>? targetElements = new List<ElementId>() ;
      switch ( selectedItem.Tag ) {
        case "Route" :
          var selectedRoute = AllRoutes?.ToList().Find( r => r.OwnerElement?.Id == UIHelper.GetElementIdFromViewItem( selectedItem ) ) ;

          if ( selectedRoute != null ) {
            targetElements = Doc?.GetAllElementsOfRouteName<Element>( selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
            //Select targetElements
            UiDoc?.ShowElements( targetElements ) ;
          }

          break ;
        case "Connector" :
          targetElements.Add( UIHelper.GetElementIdFromViewItem( selectedItem ) ) ;
          UiDoc?.ShowElements( targetElements ) ;
          break ;
      }
    }


    /// <summary>
    /// SelecteViewItem from RouteName
    /// </summary>
    /// <param name="selectedRouteName"></param>
    public void SelectTreeViewItem( ElementId? elementId )
    {
      var targetItem = GetTreeViewItemFromElementId( FromToTreeView.Items, elementId ) ;

      if ( targetItem != null ) {
        // Select in TreeView
        targetItem.IsSelected = true ;
        // Scorll to Item
        targetItem.BringIntoView() ;
        if ( targetItem.Tag.ToString() == "Connector" ) {
          SelectedFromTo.ClearDialog();
        }
      }
    }

    /// <summary>
    /// Clear selection it treeview
    /// </summary>
    private void ClearSelectedItem()
    {
      if ( FromToTreeView.SelectedItem is TreeViewItem selectedItem ) {
        selectedItem.IsSelected = false ;
      }
    }

    /// <summary>
    /// Get TreeViewItemObject from RouteName
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="targetName"></param>
    /// <returns></returns>
    private TreeViewItem? GetTreeViewItemFromName( ItemCollection collection, String targetName )
    {
      foreach ( TreeViewItem item in collection ) {
        // Find in current
        if ( item.Header.Equals( targetName ) ) {
          return item ;
        }

        // Find in Childs
        TreeViewItem? childItem = this.GetTreeViewItemFromName( item.Items, targetName ) ;
        if ( childItem != null ) {
          item.IsExpanded = true ;
          return childItem ;
        }
      }

      return null ;
    }

    /// <summary>
    /// Get TreeViewItemObject from elementId
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="targetName"></param>
    /// <returns></returns>
    private TreeViewItem? GetTreeViewItemFromElementId( ItemCollection collection, ElementId? elementId )
    {
      foreach ( var item in collection ) {
        if ( item is TreeViewItem treeViewItem ) {
          // Find in current
          if ( treeViewItem.Uid.Equals( elementId?.ToString() ) ) {
            return treeViewItem ;
          }

          // Find in Childs
          TreeViewItem? childItem = this.GetTreeViewItemFromElementId( treeViewItem.Items, elementId ) ;
          if ( childItem != null ) {
            treeViewItem.IsExpanded = true ;
            return childItem ;
          }
        }
      }

      return null ;
    }
  }
}