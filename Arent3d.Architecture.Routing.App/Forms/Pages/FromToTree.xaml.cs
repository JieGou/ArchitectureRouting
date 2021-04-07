﻿using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Diagnostics ;
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
      SelectedFromTo.ClearDialog() ;
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

      var parentFromTos = new List<Route>() ;


      foreach ( var route in allRoutes ) {
        //get ChildBranches
        if ( route.HasParent() ) {
          childBranches.Add( route ) ;
        }
        else {
          parentFromTos.Add( route ) ;
        }
      }

      // create Route tree
      foreach ( var route in parentFromTos.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
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
      var targetItem = GetTreeViewItemFromName( FromToTreeView.Items , selectedRouteName ) ;
      
      if ( targetItem != null ) {
        // Select in TreeView
        targetItem.IsSelected = true ;
        // Scorll to Item
        targetItem.BringIntoView();
      }
    }

    /// <summary>
    /// Get TreeViewItemObject from RouteName
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="targetName"></param>
    /// <returns></returns>
    private TreeViewItem? GetTreeViewItemFromName(ItemCollection collection, String targetName)
    {
      foreach(TreeViewItem item in collection)
      {
        // Find in current
        if (item.Header.Equals(targetName))
        {
          return item;
        }
        // Find in Childs
        TreeViewItem? childItem = this.GetTreeViewItemFromName(item.Items, targetName);
          if (childItem != null)
          {
            item.IsExpanded = true;
            return childItem;
          }
      }
      return null;
    }
  }
}