using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
//using Arent3d.Architecture.Routing.AppBase.Manager;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class FromToTree : Page, IDockablePaneProvider
  {
    private static Document? Doc { get ; set ; }
    private static UIDocument? UiDoc { get ; set ; }
    private static IReadOnlyCollection<Route>? AllRoutes { get ; set ; }

    private SortedDictionary<string, TreeViewItem>? ItemDictionary { get ; }

    private bool IsLeftMouseClick { get ; set ; }

    private FromToItem? selectedLeftItem { get ; set ; }

    public bool IsConnectorVisibility { get ; set ; }

    public bool IsRouterVisibility { get ; set ; }

    private bool IsPassPointVisibility { get ; set ; }

    public string CoordinatesX { get ; }

    public string CoordinatesY { get ;  }

    public string CoordinatesZ { get ;  }
    
    public string TitleLabel { get ; init ; }
    
    public IPostCommandExecutorBase PostCommandExecutor { get ; }
    

    public FromToTree( string titleLabel, IPostCommandExecutorBase postCommandExecutor)
    {
      this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
      this.CoordinatesX = "X1" ;
      this.CoordinatesY = "Y1" ;
      this.CoordinatesZ = "Z1" ;
      InitializeComponent() ;
      ItemDictionary = new SortedDictionary<string, TreeViewItem>() ;
      TitleLabel = titleLabel ;
      PostCommandExecutor = postCommandExecutor ;
      SelectedFromTo.ParentFromToTree = this ;
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
      this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
    }

    /// <summary>
    /// initialize page
    /// </summary>
    /// <param name="uiApplication"></param>
    public void CustomInitiator( UIApplication uiApplication, AddInType addInType )
    {
      Doc = uiApplication.ActiveUIDocument.Document ;
      UiDoc = uiApplication.ActiveUIDocument ;
      AllRoutes = UiDoc.Document.CollectRoutes(addInType).ToList() ;
      // call the treeview display method
      DisplayTreeViewItem( uiApplication, addInType ) ;
      IsLeftMouseClick = false ;
    }

    private void DisplayTreeViewItem( UIApplication uiApp, AddInType addInType )
    {
      ClearSelection();
      var fromToVm = new FromToTreeViewModel() ;

      fromToVm.FromToModel = new FromToModel( uiApp ) ;
      fromToVm.SetFromToItems(addInType) ;

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
    /// Set SelectedFromtTo Dialog by Selected Route in Selecting TreeView
    /// </summary>
    /// <param name="route"></param>
    private void DisplaySelectedFromTo( PropertySource.RoutePropertySource propertySource )
    {
      SelectedFromTo.UpdateFromToParameters( propertySource.Diameters, propertySource.SystemTypes, propertySource.CurveTypes ) ;
      
      SelectedFromTo.DiameterOrg = SelectedFromTo.Diameter = propertySource?.Diameter ;

      SelectedFromTo.SystemTypeOrg = SelectedFromTo.SystemType = propertySource?.SystemType ;

      SelectedFromTo.CurveTypeOrg = SelectedFromTo.CurveType = propertySource?.CurveType ;
      
      
      if ( SelectedFromTo.CurveType is { } curveType ) {
        SelectedFromTo.CurveTypeLabel = SelectedFromTo.GetTypeLabel( curveType.GetType().Name ) ;
      }

      SelectedFromTo.CurrentOrgDirect = SelectedFromTo.CurrentDirect = propertySource?.IsDirect ;

      // Set min, max value
      if ( propertySource?.Diameter is { } diameter ) {
        SelectedFromTo.CurrentMinValue = Math.Round( ( diameter / 2 ).RevitUnitsToMillimeters(), 2, MidpointRounding.AwayFromZero ) ;
        SelectedFromTo.CurrentMaxValue = Math.Round( ( SelectedFromToViewModel.GetUpLevelHeightFromLevel() - diameter / 2 ).RevitUnitsToMillimeters(), 2, MidpointRounding.AwayFromZero ) ;
      }

      SelectedFromTo.CurrentOrgHeightSetting = SelectedFromTo.CurrentHeightSetting = propertySource?.OnHeightSetting ;
      if ( propertySource?.OnHeightSetting != null ) {
        SelectedFromTo.SetHeightTextVisibility( (bool) propertySource.OnHeightSetting ) ;
        if ( propertySource.FixedHeight is { } fixedHeight ) {
          var heightValue = SelectedFromToViewModel.GetRouteHeightFromFloor( fixedHeight ).RevitUnitsToMillimeters() ;
          var round = Math.Round( heightValue, 2, MidpointRounding.AwayFromZero ) ; 
          SelectedFromTo.FixedOrgHeight = SelectedFromTo.FixedHeight = round ;
        }
        else {
          SelectedFromTo.FixedOrgHeight = SelectedFromTo.FixedHeight = SelectedFromTo.CurrentMinValue ;
        }
      }
      else {
        SelectedFromTo.SetHeightTextVisibility( false ) ;
      }

      if ( propertySource?.AvoidType is { } avoidType ) {
        SelectedFromTo.AvoidTypeOrgKey = SelectedFromTo.AvoidTypeKey = avoidType ;
      }

      SelectedFromTo.ResetDialog() ;
    }

    private static int? NegativeToNull( int? index ) => ( index < 0 ? null : index ) ;

    /// <summary>
    /// event on selected FromToTreeView to select FromTo Element
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FromToTreeView_OnSelectedItemChanged( object sender, RoutedPropertyChangedEventArgs<object> e )
    {
      if ( FromToTreeView.SelectedItem == null ) return ;
      var selectedItem = FromToTreeView.SelectedItem ;
      
      if ( selectedItem is FromToItem selectedFromToItem ) {
        selectedFromToItem.OnSelected() ;
        SelectedFromToViewModel.FromToItem = selectedFromToItem ;

        if ( selectedFromToItem.PropertySourceType is PropertySource.RoutePropertySource routePropertySource && selectedFromToItem.ItemTag == "Route" ) {
          this.DataContext = new { IsRouterVisibility = true, IsConnectorVisibility = false, IsEnableSystemType = selectedFromToItem.IsRootRoute, IsEnableCurveType = selectedFromToItem.IsRootRoute } ;
          // show SelectedFromTo 
          DisplaySelectedFromTo( routePropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is PropertySource.RoutePropertySource routeSubPropertySource && selectedFromToItem.ItemTypeName == "Section" ) {
          // show Connector UI
          this.DataContext = new { IsRouterVisibility = true, IsConnectorVisibility = false, IsEnableSystemType = false, IsEnableCurveType = true } ;
          DisplaySelectedFromTo( routeSubPropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is ConnectorPropertySource connectorPropertySource ) {
          // show Connector UI
          var transform = connectorPropertySource.ConnectorTransform ;
          SelectedFromTo.ClearDialog() ;
          this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
        }
        else if ( selectedFromToItem.PropertySourceType is PassPointPropertySource passPointPropertySource ) {
          // show Connector UI
          var (x, y, z) = passPointPropertySource.PassPointPosition ;
          SelectedFromTo.ClearDialog() ;
          var coordinateX = x.RevitUnitsToMillimeters() ;
          var coordinateY = y.RevitUnitsToMillimeters() ;
          var coordinateZ = z.RevitUnitsToMillimeters() ;
          this.DataContext = new
          {
            IsRouterVisibility = false,
            IsConnectorVisibility = true,
            CoordinatesX = "X:" + ( Math.Floor( coordinateX * 10 ) ) / 10 + " mm",
            CoordinatesY = "Y:" + ( Math.Floor( coordinateY * 10 ) ) / 10 + " mm",
            CoordinatesZ = "Z:" + ( Math.Floor( coordinateZ * 10 ) ) / 10 + " mm",
          } ;
        }
        else if ( selectedFromToItem.PropertySourceType is TerminatePointPropertySource terminatePointPropertySource ) {
          // show Connector UI
          var (x, y, z) = terminatePointPropertySource.TerminatePointPosition ;
          SelectedFromTo.ClearDialog() ;
          var coordinateX = x.RevitUnitsToMillimeters() ;
          var coordinateY = y.RevitUnitsToMillimeters() ;
          var coordinateZ = z.RevitUnitsToMillimeters() ;
          this.DataContext = new
          {
            IsRouterVisibility = false,
            IsConnectorVisibility = true,
            CoordinatesX = "X:" + ( Math.Floor( coordinateX * 10 ) ) / 10 + " mm",
            CoordinatesY = "Y:" + ( Math.Floor( coordinateY * 10 ) ) / 10 + " mm",
            CoordinatesZ = "Z:" + ( Math.Floor( coordinateZ * 10 ) ) / 10 + " mm",
          } ;
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
    /// SelecteViewItem from Element id
    /// </summary>
    /// <param name="elementId"></param>
    public void SelectTreeViewItem( ElementId? elementId )
    {
      var targetItem = GetTreeViewItemFromElementId( FromToTreeView, FromToTreeView.Items, elementId ) ;
      if ( targetItem != null ) {
        // Select in TreeView
        targetItem.IsSelected = true ;
        // Scroll to Item
        targetItem.BringIntoView() ;
      }
    }

    /// <summary>
    /// Clear selection in treeview
    /// </summary>
    private void ClearSelectedItem()
    {
      var selectedFromToItem = FromToTreeView.SelectedItem as FromToItem ;
      if ( GetTreeViewItemFromElementId( FromToTreeView, FromToTreeView.Items,  selectedFromToItem?.ElementId) is TreeViewItem selectedItem) {
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

        // Find in Children
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
    /// <param name="control"></param>
    /// <param name="collection"></param>
    /// <param name="elementId"></param>
    /// <returns></returns>
    private TreeViewItem? GetTreeViewItemFromElementId( ItemsControl control, ItemCollection collection, ElementId? elementId )
    {
      // this method is in developing. This works only in Parent Item
      foreach ( var item in collection ) {
        if ( item is FromToItem fromToItem && fromToItem.ElementId != null ) {
          FromToTreeView.UpdateLayout() ;
          TreeViewItem? treeViewItem = control.ItemContainerGenerator.ContainerFromItem( item ) as TreeViewItem ;
          // Find in current
          if ( fromToItem.ElementId.Equals( elementId ) ) {
            return treeViewItem ;
          }

          // Find in Children
          if ( treeViewItem != null ) {
            treeViewItem.IsExpanded = true ;
            TreeViewItem? childItem = this.GetTreeViewItemFromElementId( treeViewItem, treeViewItem.Items, elementId ) ;
            if ( childItem != null ) {
              return childItem ;
            }
          }
        }
      }

      return null ;
    }

    /// <summary>
    /// TextBlock_PreviewMouseLeftButtonDown event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void TextBlock_PreviewMouseLeftButtonDown( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (FromToItem) FromToTreeView.SelectedItem ;
      if ( selectedItem != null ) {
        IsLeftMouseClick = true ;
        selectedLeftItem = selectedItem ;
      }
    }

    /// <summary>
    /// TextBox_LostFocus event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void TextBox_LostFocus( object sender, RoutedEventArgs e )
    {
    }

    /// <summary>
    /// TextBlock_PreviewMouseLeftButtonUp event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void TextBlock_PreviewMouseLeftButtonUp( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (FromToItem) FromToTreeView.SelectedItem ;
      if ( IsLeftMouseClick && selectedItem == selectedLeftItem ) {
        if ( selectedItem != null && selectedItem.ItemTag == "Route" ) {
          selectedItem.IsEditing = true ;
        }
      }

      IsLeftMouseClick = false ;
    }

    /// <summary>
    /// FromToTreeView_PreviewKeyDown event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void FromToTreeView_PreviewKeyDown( object sender, KeyEventArgs e )
    {
      var selectedItem = (FromToItem) FromToTreeView.SelectedItem ;
      if ( selectedItem != null && e.Key.Equals( Key.F2 ) && selectedItem.ItemTag == "Route" ) {
        selectedItem.IsEditing = true ;
      }
    }

    /// <summary>
    /// TextBox_KeyDown event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void TextBox_KeyDown( object sender, KeyEventArgs e )
    {
      var selectedItem = (FromToItem) FromToTreeView.SelectedItem ;
      System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox) sender ;
      if ( e.Key.Equals( Key.Escape ) ) {
        tb.Text = selectedItem.ItemTypeName ;
        selectedItem.IsEditing = false ;
      }
      else if ( e.Key.Equals( Key.Enter ) ) {
        if ( tb.Text == "" ) {
          tb.Text = selectedItem.ItemTypeName ;
          selectedItem.IsEditing = false ;
        }
        else if ( ExistsRouteName( tb.Text ) ) {
          MessageBox.Show( "Dialog.Forms.FromToTree.Rename".GetAppStringByKeyOrDefault( "Since the name is duplicated, please change it to another name." ) ) ;
          tb.Focus() ;
        }
        else {
          selectedItem.IsEditing = false ;
          selectedItem.ItemTypeName = tb.Text ;
          if ( SelectedFromToViewModel.UiApp is { } app ) {
            PostCommandExecutor.ChangeRouteNameCommand(app);
          }
        }
      }
    }

    /// <summary>
    /// Check if route exists.
    /// </summary>
    /// <param name="routName"></param>
    /// <returns></returns>
    private bool ExistsRouteName( string routName )
    {
      bool result = false ;

      var selectedItem = (FromToItem) FromToTreeView.SelectedItem ;
      for ( int x = 0 ; x < FromToTreeView.Items.Count ; x++ ) {
        var fromItem = (FromToItem) FromToTreeView.Items[ x ] ;
        if ( fromItem.ItemTypeName == routName && fromItem.ItemTag == "Route" && fromItem != selectedItem ) {
          result = true ;
          break ;
        }
      }

      return result ;
    }

    /// <summary>
    /// TextBox_PreviewLostKeyboardFocus event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void TextBox_PreviewLostKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e )
    {
      System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox) sender ;
      var selectedItem = (FromToItem) FromToTreeView.SelectedItem ;


      if ( tb.Text == "" ) {
        tb.Text = selectedItem.ItemTypeName ;
        selectedItem.IsEditing = false ;
      }
      else if ( ExistsRouteName( tb.Text ) ) {
        MessageBox.Show( "Dialog.Forms.FromToTree.Rename".GetAppStringByKeyOrDefault( "Since the name is duplicated, please change it to another name." ) ) ;
        tb.Focus() ;
      }
      else {
        selectedItem.IsEditing = false ;
        selectedItem.ItemTypeName = tb.Text ;
        if ( SelectedFromToViewModel.UiApp is { } app ) {
          PostCommandExecutor.ChangeRouteNameCommand(app);
        }
      }
    }
  }
}