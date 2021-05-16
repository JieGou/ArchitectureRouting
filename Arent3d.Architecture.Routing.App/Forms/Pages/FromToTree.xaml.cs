using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Diagnostics ;
using System.Globalization ;
using System.IO.Packaging ;
using System.Linq ;
using System.Threading.Tasks ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Media ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.App.Model ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.UI;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FromToTree : Page, IDockablePaneProvider
  {
    private static Document? Doc { get ; set ; }
    private static UIDocument? UiDoc { get ; set ; }
    private static IReadOnlyCollection<Route>? AllRoutes { get ; set ; }

    private SortedDictionary<string, TreeViewItem>? ItemDictionary { get ; set ; }

    private bool IsLeftMouseClick { get; set; }

    private FromToItem? selectedLeftItem { get; set; }

    public bool IsConnectorVisibility { get; set; }

    public bool IsRouterVisibility { get; set; }

    private bool IsPassPointVisibility { get; set; }

    public string CoordinatesX { get; set; }

    public string CoordinatesY { get; set; }

    public string CoordinatesZ { get; set; }




        public FromToTree()
    {
        this.DataContext = new { IsRouterVisibility = true, IsConnectorVisibility = false};
        IsConnectorVisibility = true;
        IsRouterVisibility = false;
            this.CoordinatesX = "X1";

            this.CoordinatesY = "Y1";
            this.CoordinatesZ = "Z1";
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
      IsLeftMouseClick = false;
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
    /// Set SelectedFromtTo Dialog by Selected Route in TreeView
    /// </summary>
    /// <param name="route"></param>
    private void DisplaySelectedFromTo( PropertySource.RoutePropertySource propertySource )
    {
      SelectedFromTo.UpdateFromToParameters( propertySource.Diameters, propertySource.SystemTypes, propertySource.CurveTypes ) ;

      SelectedFromTo.DiameterOrgIndex = SelectedFromTo.DiameterIndex = NegativeToNull( propertySource.DiameterIndex ) ;

      SelectedFromTo.SystemTypeOrgIndex = SelectedFromTo.SystemTypeIndex = NegativeToNull( propertySource.SystemTypeIndex ) ;

      SelectedFromTo.CurveTypeOrgIndex = SelectedFromTo.CurveTypeIndex = NegativeToNull( propertySource.CurveTypeIndex ) ;

      if ( SelectedFromTo.CurveTypeIndex is { } index ) {
         SelectedFromTo.CurveTypeLabel = SelectedFromTo.GetTypeLabel( SelectedFromTo.CurveTypes[ index ].GetType().Name ) ;
      }

      SelectedFromTo.CurrentOrgDirect =  SelectedFromTo.CurrentDirect = propertySource.IsDirect ;
      SelectedFromTo.CurrentOrgHeightSetting = SelectedFromTo.CurrentHeightSetting = propertySource.OnHeightSetting ;

      if ( propertySource.FixedHeight is { } fixedHeight ) {
        SelectedFromTo.FixedOrgHeight = SelectedFromTo.FixedHeight = UnitUtils.ConvertFromInternalUnits
          (SelectedFromToViewModel.GetHeightFromFloor(fixedHeight), UnitTypeId.Millimeters).ToString( CultureInfo.InvariantCulture ) ;
      }
      else {
        SelectedFromTo.FixedOrgHeight = SelectedFromTo.FixedHeight = "" ;
      }

      SelectedFromTo.ResetDialog() ;
    }

    private static int? NegativeToNull( int index ) => ( index < 0 ? null : index ) ;

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
        
        if ( selectedFromToItem.PropertySourceType is PropertySource.RoutePropertySource routePropertySource && selectedFromToItem.ItemTag == "Route") {

          this.DataContext = new { IsRouterVisibility = true, IsConnectorVisibility = false, IsEnableSystemType = selectedFromToItem.IsRootRoute, IsEnableCurveType = selectedFromToItem.IsRootRoute };
          // show SelectedFromTo 
          DisplaySelectedFromTo( routePropertySource ) ;
        }
        else if ( selectedFromToItem.PropertySourceType is PropertySource.RoutePropertySource routeSubPropertySource && selectedFromToItem.ItemTypeName == "Section" ) {
          // show Connector UI
          this.DataContext = new { IsRouterVisibility = true, IsConnectorVisibility = false, IsEnableSystemType = false, IsEnableCurveType = true };
          DisplaySelectedFromTo( routeSubPropertySource );
        }
        else if ( selectedFromToItem.PropertySourceType is ConnectorPropertySource connectorPropertySource ) {
          // show Connector UI
          var transform = connectorPropertySource.ConnectorTransform ;
          SelectedFromTo.ClearDialog() ;
          this.DataContext = new { IsRouterVisibility = false, IsConnectorVisibility = false };
        }
        else if ( selectedFromToItem.PropertySourceType is PassPointPropertySource passPointPropertySource ) {
          // show Connector UI
          var (x, y, z) = passPointPropertySource.PassPointPosition ;
          SelectedFromTo.ClearDialog() ;
          var coordinateX = UnitUtils.ConvertFromInternalUnits( x, UnitTypeId.Millimeters ) ;
          var coordinateY = UnitUtils.ConvertFromInternalUnits( y, UnitTypeId.Millimeters ) ;
          var coordinateZ = UnitUtils.ConvertFromInternalUnits( z, UnitTypeId.Millimeters ) ;
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
          var coordinateX = UnitUtils.ConvertFromInternalUnits( x, UnitTypeId.Millimeters ) ;
          var coordinateY = UnitUtils.ConvertFromInternalUnits( y, UnitTypeId.Millimeters ) ;
          var coordinateZ = UnitUtils.ConvertFromInternalUnits( z, UnitTypeId.Millimeters ) ;
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
        // Scorll to Item
        targetItem.BringIntoView() ;
        // this code is in developing
        /*if ( targetItem.Tag.ToString() == "Connector" ) {
          SelectedFromTo.ClearDialog() ;
        }*/
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
    private TreeViewItem? GetTreeViewItemFromElementId( ItemsControl control, ItemCollection collection, ElementId? elementId )
    {
      // this method is in developing. This works only in Parent Item
      foreach ( var item in collection ) {
        if ( item is FromToItem fromToItem && fromToItem.ElementId != null ) {
          FromToTreeView.UpdateLayout();
          TreeViewItem? treeViewItem = control.ItemContainerGenerator.ContainerFromItem( item as object ) as TreeViewItem ;
          // Find in current
          if ( fromToItem.ElementId.Equals( elementId ) ) {
            return treeViewItem ;
          }

          // Find in Childs
          if ( treeViewItem != null ) {
            treeViewItem.IsExpanded = true;
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
        var selectedItem = (FromToItem) FromToTreeView.SelectedItem;
        if ( selectedItem != null ) {
            IsLeftMouseClick = true;
            selectedLeftItem = selectedItem;
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
        System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox) sender;
        var selectedItem = (FromToItem) FromToTreeView.SelectedItem;
        if ( IsExsitRouteName( tb.Text ) ) {
            MessageBox.Show( "名前が重複しているため、他の値に変更してください。" );
            tb.Focus();
        }
        else {
            selectedItem.IsEditing = false;
            selectedItem.ItemTypeName = tb.Text;
            UiDoc?.Application.PostCommand<Commands.PostCommands.ApplyChangeRouteNameCommand>();
        }
    }

    /// <summary>
    /// TextBlock_PreviewMouseLeftButtonUp event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void TextBlock_PreviewMouseLeftButtonUp( object sender, MouseButtonEventArgs e )
    {
        var selectedItem = (FromToItem) FromToTreeView.SelectedItem;
        if ( IsLeftMouseClick && selectedItem == selectedLeftItem ) { 
                
            if ( selectedItem != null && selectedItem.ItemTag == "Route" ) {
                selectedItem.IsEditing = true;
            }
                
        }
        IsLeftMouseClick = false;
    }

    /// <summary>
    /// FromToTreeView_PreviewKeyDown event
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private void FromToTreeView_PreviewKeyDown( object sender, KeyEventArgs e )
    {
        var selectedItem = (FromToItem) FromToTreeView.SelectedItem;
        if ( selectedItem != null && e.Key.Equals( Key.F2 ) && selectedItem.ItemTag == "Route" ) {
            selectedItem.IsEditing = true;
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
        var selectedItem = (FromToItem) FromToTreeView.SelectedItem;
        System.Windows.Controls.TextBox tb = (System.Windows.Controls.TextBox) sender;
        if ( e.Key.Equals( Key.Escape ) ) {
            tb.Text = selectedItem.ItemTypeName;
            selectedItem.IsEditing = false;
                
        }
        else if ( e.Key.Equals( Key.Enter ) ) {
            if( IsExsitRouteName( tb.Text ) ) {
                MessageBox.Show( "名前が重複しているため、他の値に変更してください。" );
                tb.Focus();
            }
            else { 
                selectedItem.IsEditing = false;
                selectedItem.ItemTypeName = tb.Text;
                //UiDoc?.Application.PostCommand<Commands.PostCommands.ApplyChangeRouteNameCommand>();
            }
        }
    }

    /// <summary>
    /// IsExsitRouteName
    /// </summary>
    /// <param name="routName"></param>
    /// <returns></returns>
    private bool IsExsitRouteName(string routName)
    {
        bool result = false;
        var selectedItem = (FromToItem) FromToTreeView.SelectedItem;
        for ( int x = 0 ; x < FromToTreeView.Items.Count ; x++ ) {
            var fromItem = (FromToItem) FromToTreeView.Items[x];
            if( fromItem.ItemTypeName == routName && fromItem.ItemTag == "Route" && fromItem != selectedItem ) {
                result = true;
                break;
            }
        }

        return result;
    }
  }
}