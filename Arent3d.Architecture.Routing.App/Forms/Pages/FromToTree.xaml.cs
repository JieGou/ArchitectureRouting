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
using Arent3d.Architecture.Routing.App.Model ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FromToTree : Page, IDockablePaneProvider
  {
    private static Document? Doc { get ; set ; }
    private static UIDocument? UiDoc { get ; set ; }
    private static IReadOnlyCollection<Route>? AllRoutes { get ; set ; }

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
      DisplayTreeViewItem( uiApplication, AllRoutes ) ;
    }

    private void DisplayTreeViewItem( UIApplication uiApp, IReadOnlyCollection<Route> allRoutes )
    {
      var fromToVm = new FromToTreeViewModel() ;

      fromToVm.FromToModel = new FromToModel( uiApp ) ;
      fromToVm.SetFromToItems() ;

      FromToTreeView.DataContext = fromToVm ;

      FromToTreeView.ItemsSource = fromToVm.FromToItems ;
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
    private void DisplaySelectedFromTo()
    {
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
      var selectedItem = FromToTreeView.SelectedItem ;

      if ( selectedItem is FromToItem selectedFromToItem ) {
        
        selectedFromToItem.OnSelected() ;
        
        if ( selectedFromToItem.DisplaySelectedFromTo ) {
          // show SelectedFromTo 
          DisplaySelectedFromTo() ;
        }
        else {
          // don't show SelectedFromTo 
          SelectedFromTo.ClearDialog() ;
        }
      }
    }

    /// <summary>
    /// event on MouseDoubleClicked to focus selected FromTo Element
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FromToTreeView_OnMouseDoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = FromToTreeView.SelectedItem ;
      ( selectedItem as FromToItem )?.OnDoubleClicked() ;
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
          SelectedFromTo.ClearDialog() ;
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