using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class FromToTree : Page, IDockablePaneProvider
  {
    private static readonly DependencyPropertyKey DisplayUnitSystemPropertyKey = DependencyProperty.RegisterReadOnly( "DisplayUnitSystem", typeof( DisplayUnit ), typeof( FromToTree ), new PropertyMetadata( DisplayUnit.IMPERIAL ) ) ;
    private static readonly DependencyPropertyKey IsConnectorVisiblePropertyKey = DependencyProperty.RegisterReadOnly( "IsConnectorVisible", typeof( bool ), typeof( FromToTree ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsPassPointVisiblePropertyKey = DependencyProperty.RegisterReadOnly( "IsPassPointVisible", typeof( bool ), typeof( FromToTree ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey IsRouterVisiblePropertyKey = DependencyProperty.RegisterReadOnly( "IsRouterVisible", typeof( bool ), typeof( FromToTree ), new PropertyMetadata( false ) ) ;
    private static readonly DependencyPropertyKey CoordinatesXPropertyKey = DependencyProperty.RegisterReadOnly( "CoordinatesX", typeof( string ), typeof( FromToTree ), new PropertyMetadata( string.Empty ) ) ;
    private static readonly DependencyPropertyKey CoordinatesYPropertyKey = DependencyProperty.RegisterReadOnly( "CoordinatesY", typeof( string ), typeof( FromToTree ), new PropertyMetadata( string.Empty ) ) ;
    private static readonly DependencyPropertyKey CoordinatesZPropertyKey = DependencyProperty.RegisterReadOnly( "CoordinatesZ", typeof( string ), typeof( FromToTree ), new PropertyMetadata( string.Empty ) ) ;

    public DisplayUnit DisplayUnitSystem
    {
      get { return (DisplayUnit)GetValue( DisplayUnitSystemPropertyKey.DependencyProperty ) ; }
      private set { SetValue( DisplayUnitSystemPropertyKey, value ) ; }
    }

    public bool IsPassPointVisible
    {
      get { return (bool)GetValue( IsPassPointVisiblePropertyKey.DependencyProperty ) ; }
      private set { SetValue( IsPassPointVisiblePropertyKey, value ) ; }
    }
    public bool IsConnectorVisible
    {
      get { return (bool)GetValue( IsConnectorVisiblePropertyKey.DependencyProperty ) ; }
      private  set { SetValue( IsConnectorVisiblePropertyKey, value ) ; }
    }
    public bool IsRouterVisible
    {
      get { return (bool)GetValue( IsRouterVisiblePropertyKey.DependencyProperty ) ; }
      private   set { SetValue( IsRouterVisiblePropertyKey, value ) ; }
    }

    public string CoordinatesX
    {
      get { return (string)GetValue( CoordinatesXPropertyKey.DependencyProperty ) ; }
      private set { SetValue( CoordinatesXPropertyKey, value ) ; }
    }

    public string CoordinatesY
    {
      get { return (string)GetValue( CoordinatesYPropertyKey.DependencyProperty ) ; }
      private set { SetValue( CoordinatesYPropertyKey, value ) ; }
    }

    public string CoordinatesZ
    {
      get { return (string)GetValue( CoordinatesZPropertyKey.DependencyProperty ) ; }
      private set { SetValue( CoordinatesZPropertyKey, value ) ; }
    }

    private IReadOnlyCollection<Route>? AllRoutes { get ; set ; }

    private SortedDictionary<string, TreeViewItem>? ItemDictionary { get ; }

    private bool IsLeftMouseClick { get ; set ; }

    private FromToItem? SelectedLeftItem { get ; set ; }
    
    public string TitleLabel { get ; }
    
    public IPostCommandExecutorBase PostCommandExecutor { get ; }
    
    public FromToItemsUiBase FromToItemsUi { get ; }

    public FromToTree( string titleLabel, IPostCommandExecutorBase postCommandExecutor, FromToItemsUiBase fromToItemsUi)
    {
      InitializeComponent() ;
      ItemDictionary = new SortedDictionary<string, TreeViewItem>() ;
      TitleLabel = titleLabel ;
      PostCommandExecutor = postCommandExecutor ;
      FromToItemsUi = fromToItemsUi ;
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
      SelectedFromTo.EditingSource = null ;
      this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false } ;
    }

    public bool HasInvalidRoute()
    {
      return AllRoutes?.Any( route => route.OwnerElement is not { } ownerElement || false == ownerElement.IsValidObject ) ?? false ;
    }

    /// <summary>
    /// initialize page
    /// </summary>
    /// <param name="uiApplication"></param>
    /// <param name="addInType"></param>
    public void CustomInitiator( UIApplication uiApplication, AddInType addInType )
    {
      var document = uiApplication.ActiveUIDocument.Document ;
      AllRoutes = document.CollectRoutes(addInType).ToList() ;
      DisplayUnitSystem = document.DisplayUnitSystem ;
      // call the treeview display method
      DisplayTreeViewItem( uiApplication, addInType ) ;
      IsLeftMouseClick = false ;
    }

    private void DisplayTreeViewItem( UIApplication uiApp, AddInType addInType )
    {
      ClearSelection();
      var fromToVm = new FromToTreeViewModel() ;

      fromToVm.FromToModel = new FromToModel( uiApp ) ;
      fromToVm.SetFromToItems(addInType,  FromToItemsUi) ;

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
    /// Set SelectedFromTo Dialog by Selected Route in Selecting TreeView
    /// </summary>
    /// <param name="propertySource"></param>
    private void DisplaySelectedFromTo( RoutePropertySource propertySource )
    {
      IsRouterVisible = true ;
      IsConnectorVisible = false ;
      IsPassPointVisible = false ;

      SelectedFromTo.EditingSource = propertySource ;
    }

    private void DisplaySelectedConnector( ConnectorPropertySource propertySource )
    {
      IsRouterVisible = false ;
      IsConnectorVisible = true ;
      IsPassPointVisible = false ;

      SelectedFromTo.EditingSource = null ;
    }

    private void DisplaySelectedPassPoint( PassPointPropertySource propertySource )
    {
      IsRouterVisible = false ;
      IsConnectorVisible = false ;
      IsPassPointVisible = true ;

      SelectedFromTo.EditingSource = null ;
      
      var (x, y, z) = propertySource.PassPointPosition ;
      var coordinateX = x.RevitUnitsToMillimeters() ;
      var coordinateY = y.RevitUnitsToMillimeters() ;
      var coordinateZ = z.RevitUnitsToMillimeters() ;
      CoordinatesX = "X:" + ( Math.Floor( coordinateX * 10 ) ) / 10 + " mm" ;
      CoordinatesY = "Y:" + ( Math.Floor( coordinateY * 10 ) ) / 10 + " mm" ;
      CoordinatesZ = "Z:" + ( Math.Floor( coordinateZ * 10 ) ) / 10 + " mm" ;
    }

    private void DisplaySelectedTerminatePoint( TerminatePointPropertySource propertySource )
    {
      IsRouterVisible = false ;
      IsConnectorVisible = false ;
      IsPassPointVisible = true ;

      SelectedFromTo.EditingSource = null ;
      
      var (x, y, z) = propertySource.TerminatePointPosition ;
      var coordinateX = x.RevitUnitsToMillimeters() ;
      var coordinateY = y.RevitUnitsToMillimeters() ;
      var coordinateZ = z.RevitUnitsToMillimeters() ;
      CoordinatesX = "X:" + ( Math.Floor( coordinateX * 10 ) ) / 10 + " mm" ;
      CoordinatesY = "Y:" + ( Math.Floor( coordinateY * 10 ) ) / 10 + " mm" ;
      CoordinatesZ = "Z:" + ( Math.Floor( coordinateZ * 10 ) ) / 10 + " mm" ;
    }

    private void HideControls()
    {
      IsRouterVisible = false ;
      IsConnectorVisible = false ;
      IsPassPointVisible = false ;

      SelectedFromTo.EditingSource = null ;
    }

    private static int? NegativeToNull( int? index ) => ( index < 0 ? null : index ) ;

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
        SelectedFromTo.TargetFromToItem = selectedFromToItem ;

        if ( selectedFromToItem.PropertySourceType is RoutePropertySource routePropertySource && selectedFromToItem.ItemTag == "Route" ) {
          DisplaySelectedFromTo( routePropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is RoutePropertySource routeSubPropertySource && selectedFromToItem.ItemTypeName == "Section" ) {
          DisplaySelectedFromTo( routeSubPropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is ConnectorPropertySource connectorPropertySource ) {
          DisplaySelectedConnector( connectorPropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is PassPointPropertySource passPointPropertySource ) {
          DisplaySelectedPassPoint( passPointPropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is TerminatePointPropertySource terminatePointPropertySource ) {
          DisplaySelectedTerminatePoint( terminatePointPropertySource ) ;
        }
        else {
          HideControls() ;
        }
      }
      else {
        HideControls() ;
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
    /// <param name="elementUniqueId"></param>
    public void SelectTreeViewItem( string? elementUniqueId )
    {
      var targetItem = GetTreeViewItemFromElementId( FromToTreeView, FromToTreeView.Items, elementUniqueId ) ;
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
      if ( GetTreeViewItemFromElementId( FromToTreeView, FromToTreeView.Items, selectedFromToItem?.ElementUniqueId ) is TreeViewItem selectedItem) {
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
    private TreeViewItem? GetTreeViewItemFromElementId( ItemsControl control, ItemCollection collection, string? elementUniqueId )
    {
      // this method is in developing. This works only in Parent Item
      foreach ( var item in collection ) {
        if ( item is FromToItem fromToItem ) {
          FromToTreeView.UpdateLayout() ;
          TreeViewItem? treeViewItem = control.ItemContainerGenerator.ContainerFromItem( item ) as TreeViewItem ;
          // Find in current
          if ( fromToItem.ElementUniqueId == elementUniqueId ) {
            return treeViewItem ;
          }

          // Find in Children
          if ( treeViewItem != null ) {
            treeViewItem.IsExpanded = true ;
            TreeViewItem? childItem = this.GetTreeViewItemFromElementId( treeViewItem, treeViewItem.Items, elementUniqueId ) ;
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
        SelectedLeftItem = selectedItem ;
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
      if ( IsLeftMouseClick && selectedItem == SelectedLeftItem ) {
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
          tb.Text = "" ;
          if ( ( selectedItem.PropertySourceType as RoutePropertySource )?.TargetRoute is { } route ) {
            PostCommandExecutor.ChangeRouteNameCommand( route, selectedItem.ItemTypeName ) ;
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
      if ( FromToTreeView.SelectedItem is not FromToItem selectedItem ) return ;

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
        tb.Text = "" ;
        if ( ( selectedItem.PropertySourceType as RoutePropertySource )?.TargetRoute is { } route ) {
          PostCommandExecutor.ChangeRouteNameCommand( route, selectedItem.ItemTypeName ) ;
        }
      }
    }
  }
}