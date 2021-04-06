using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FromToTree : Page, IDockablePaneProvider
  {
    private Document? Doc { get ; set ; }
    private UIDocument? UiDoc { get ; set ; }

    private IReadOnlyCollection<Route>? AllRoutes { get ; set ; }

    public FromToTree()
    {
      InitializeComponent() ;
    }


    public class FromTo
    {
      public Route? Route { get ; set ; }
    }


    public void SetupDockablePane( DockablePaneProviderData data )
    {
      data.FrameworkElement = this as FrameworkElement ;
      // Define initial pane position in Revit ui
      data.InitialState = new DockablePaneState { DockPosition = DockPosition.Tabbed, TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser } ;
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
      SelectedFromTo.ClearDialog();
    }

    /// <summary>
    /// set TreeViewItems
    /// </summary>
    private void DisplayTreeViewItem( IReadOnlyCollection<Route> allRoutes )
    {
      // clear items first
      FromToTreeView.Items.Clear() ;

      // routename and childrenname dictionary
      var routeDictionary = new SortedDictionary<string, TreeViewItem>() ;

      var childBranches = new List<Route>() ;

      // collect Routes
      //List<Route> routes = new List<Route>( allRoutes ) ;

      foreach ( var r in allRoutes ) {
        //get ChildBranches
        childBranches.AddRange( r.GetChildBranches().ToList() ) ;
      }

      // create Route tree
      foreach ( var route in allRoutes.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
        // create route treeviewitem
        TreeViewItem routeItem = new TreeViewItem() { Header = route.RouteName } ;
        // store in dict
        routeDictionary[ route.RouteName ] = routeItem ;
        // add to treeview
        FromToTreeView.Items.Add( routeItem ) ;
      }

      // create branches tree
      foreach ( var c in childBranches ) {
        TreeViewItem viewItem = new TreeViewItem() { Header = c.RouteName } ;

        routeDictionary[ c.GetParentBranches().ToList()[ 0 ].RouteName ].Items.Add( viewItem ) ;
      }
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

      var selectedRoute = AllRoutes?.ToList().Find( r => r.RouteName == selectedItem.Header.ToString() ) ;

      if ( selectedRoute != null ) {
        var targetElements = Doc?.GetAllElementsOfRouteName<Element>( selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
        //Select targetElements
        UiDoc?.Selection.SetElementIds( targetElements ) ;

        DisplaySelectedFromTo( selectedRoute ) ;
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

      var selectedRoute = AllRoutes?.ToList().Find( r => r.RouteName == selectedItem.Header.ToString() ) ;

      if ( selectedRoute != null ) {
        var targetElements = Doc?.GetAllElementsOfRouteName<Element>( selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
        //Show targetElements
        UiDoc?.ShowElements( targetElements ) ;
      }
    }


    /// <summary>
    /// SelecteViewItem from RouteName
    /// </summary>
    /// <param name="selectedRouteName"></param>
    public void SelectTreeViewItem( string selectedRouteName )
    {
      var targetItem = GetTreeViewItemFromName( selectedRouteName ) ;
      var tvi = FromToTreeView.ItemContainerGenerator.ContainerFromItem(targetItem) 
        as TreeViewItem;

      if (tvi != null)
      {
        tvi.IsSelected = true;
      }
    }

    /// <summary>
    /// Get TreeViewItemObject from RouteName
    /// </summary>
    /// <param name="routeName"></param>
    /// <returns></returns>
    private object? GetTreeViewItemFromName( string routeName )
    {
      foreach ( var item  in FromToTreeView.Items ) {
        if ( item is TreeViewItem treeViewItem ) {
          if ( treeViewItem.Header.ToString() == routeName ) {
            return item ;
          }
          else {
            continue;
          }
        }
        else {
          continue ;
        }
      }

      return null ;
    }
    
    private object? GetTreeViewItemFromIndex( int index )
    {
      var targetItem = FromToTreeView.Items.GetItemAt( index ) ;
      if ( targetItem != null ) {
        return targetItem ;
      }
      return null ;
    }
    
    /// <summary>
    /// Recursively search for an item in this subtree.
    /// </summary>
    /// <param name="container">
    /// The parent ItemsControl. This can be a TreeView or a TreeViewItem.
    /// </param>
    /// <param name="item">
    /// The item to search for.
    /// </param>
    /// <returns>
    /// The TreeViewItem that contains the specified item.
    /// </returns>
    public TreeViewItem? GetTreeViewItem( ItemsControl? container, object item )
    {
      if ( container != null ) {
        if ( container.DataContext == item ) {
          return container as TreeViewItem ;
        }

        // Expand the current container
        if ( container is TreeViewItem && ! ( (TreeViewItem) container ).IsExpanded ) {
          container.SetValue( TreeViewItem.IsExpandedProperty, true ) ;
        }

        // Try to generate the ItemsPresenter and the ItemsPanel.
        // by calling ApplyTemplate.  Note that in the 
        // virtualizing case even if the item is marked 
        // expanded we still need to do this step in order to 
        // regenerate the visuals because they may have been virtualized away.

        container.ApplyTemplate() ;
        ItemsPresenter? itemsPresenter = (ItemsPresenter?) container.Template.FindName( "ItemsHost", container ) ;
        if ( itemsPresenter != null ) {
          itemsPresenter.ApplyTemplate() ;
        }
        else {
          // The Tree template has not named the ItemsPresenter, 
          // so walk the descendents and find the child.
          itemsPresenter = FindVisualChild<ItemsPresenter>( container );
          if ( itemsPresenter == null ) {
            container.UpdateLayout() ;

            itemsPresenter = FindVisualChild<ItemsPresenter>( container ) ;
          }
        }

        System.Windows.Controls.Panel itemsHostPanel = (System.Windows.Controls.Panel) VisualTreeHelper.GetChild( itemsPresenter, 0 ) ;


        // Ensure that the generator for this panel has been created.
        UIElementCollection children = itemsHostPanel.Children ;

        FromToVirtualizingStackPanel? virtualizingPanel = itemsHostPanel as FromToVirtualizingStackPanel ;

        for ( int i = 0, count = container.Items.Count ; i < count ; i++ ) {
          TreeViewItem subContainer ;
          if ( virtualizingPanel != null ) {
            // Bring the item into view so 
            // that the container will be generated.
            virtualizingPanel.BringIntoView( i ) ;

            subContainer = (TreeViewItem) container.ItemContainerGenerator.ContainerFromIndex( i ) ;
          }
          else {
            subContainer = (TreeViewItem) container.ItemContainerGenerator.ContainerFromIndex( i ) ;

            // Bring the item into view to maintain the 
            // same behavior as with a virtualizing panel.
            subContainer.BringIntoView() ;
          }

          if ( subContainer != null ) {
            // Search the next level for the object.
            TreeViewItem? resultContainer = GetTreeViewItem( subContainer, item ) ;
            if ( resultContainer != null ) {
              return resultContainer ;
            }
            else {
              // The object is not under this TreeViewItem
              // so collapse it.
              subContainer.IsExpanded = false ;
            }
          }
        }
      }

      return null ;
    }

    /// <summary>
    /// Search for an element of a certain type in the visual tree.
    /// </summary>
    /// <typeparam name="T">The type of element to find.</typeparam>
    /// <param name="visual">The parent element.</param>
    /// <returns></returns>
    private T? FindVisualChild<T>( Visual visual ) where T : Visual
    {
      for ( int i = 0 ; i < VisualTreeHelper.GetChildrenCount( visual ) ; i++ ) {
        Visual? child = (Visual?) VisualTreeHelper.GetChild( visual, i ) ;
        if ( child != null ) {
          T? correctlyTyped = child as T ;
          if ( correctlyTyped != null ) {
            return correctlyTyped ;
          }

          T? descendent = FindVisualChild<T>( child ) ;
          if ( descendent != null ) {
            return descendent ;
          }
        }
      }

      return null ;
    }
  }
}