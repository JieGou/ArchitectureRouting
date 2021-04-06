using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
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
    }

    /// <summary>
    /// set TreeViewItems
    /// </summary>
    private void DisplayTreeViewItem( IReadOnlyCollection<Route> allRoutes )
    {
      // clear items first
      var test = FromToTreeView ;
      var test2 = FromToTreeView.Items ;
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
      //foreach ( var route in routes.Where(r => r.IsParentBranch(r) == true).Distinct().OrderBy( r => r.RouteName ).ToList() ) {
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

      if ( SelectedFromToViewModel.Diameters != null ) {
        SelectedFromTo.Diameters = new ObservableCollection<string>( SelectedFromToViewModel.Diameters.Select( d => UnitUtils.ConvertFromInternalUnits( d, UnitTypeId.Millimeters ) + " mm" ) ) ;
      }

      SelectedFromTo.DiameterIndex = SelectedFromToViewModel.DiameterIndex ;

      if ( SelectedFromToViewModel.SystemTypes != null ) {
        SelectedFromTo.SystemTypes = new ObservableCollection<MEPSystemType>( SelectedFromToViewModel.SystemTypes ) ;
      }

      SelectedFromTo.SystemTypeIndex = SelectedFromToViewModel.SystemTypeIndex ;

      SelectedFromTo.CurveTypeIndex = SelectedFromToViewModel.CurveTypeIndex ;

      if ( SelectedFromToViewModel.CurveTypes != null ) {
        SelectedFromTo.CurveTypes = new ObservableCollection<MEPCurveType>( SelectedFromToViewModel.CurveTypes ) ;
        SelectedFromTo.CurveTypeLabel = SelectedFromTo.GetTypeLabel(SelectedFromTo.CurveTypes[ (int) SelectedFromTo.CurveTypeIndex ].GetType().Name);
        ;
      }

      SelectedFromTo.CurrentDirect = SelectedFromToViewModel.IsDirect ;

      SelectedFromTo.ResetDialog() ;
    }

    /// <summary>
    /// event on selected FromToTreeView
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
        //Show targetElements
        UiDoc?.ShowElements( targetElements ) ;
        
        DisplaySelectedFromTo( selectedRoute ) ;
      }

    }
  }
}