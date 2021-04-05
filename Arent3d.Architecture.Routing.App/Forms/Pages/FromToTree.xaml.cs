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
  public partial class FromToTree : Page, IDisposable, IDockablePaneProvider
  {
    public ExternalCommandData? eData = null ;
    public Document? doc = null ;
    public UIDocument? uiDoc = null ;

    public List<Route> AllRoutes = new List<Route>() ;

    public FromToTree()
    {
      InitializeComponent() ;
    }


    public class FromTo
    {
      public Route? Route { get ; set ; }
      // public 
    }

    public void Dispose()
    {
      this.Dispose() ;
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
      doc = uiApplication.ActiveUIDocument.Document ;
      uiDoc = uiApplication.ActiveUIDocument ;
      AllRoutes = uiDoc.Document.CollectRoutes().ToList() ;
      // call the treeview display method
      DisplayTreeViewItem( AllRoutes ) ;

      //DisplaySelectedFromTo( allRoutes.ToList()[ 0 ] ) ;
    }

    /// <summary>
    /// set TreeViewItems
    /// </summary>
    public void DisplayTreeViewItem( IEnumerable<Route> allRoutes )
    {
      // clear items first
      var test = FromToTreeView ;
      var test2 = FromToTreeView.Items ;
      FromToTreeView.Items.Clear() ;

      // routename and childrenname dictionary
      SortedDictionary<string, TreeViewItem> routeDictionary = new SortedDictionary<string, TreeViewItem>() ;

      List<Route> childBranches = new List<Route>() ;
      List<SubRoute> subRoutes = new List<SubRoute>() ; //test for subroutes

      //collect view type
      List<Element> elements = new FilteredElementCollector( doc ).OfClass( typeof( View ) ).ToList() ;

      // collect Routes
      List<Route> routes = new List<Route>( allRoutes ) ;


      foreach ( var r in routes ) {
        //get ChildBranches

        childBranches.AddRange( r.GetChildBranches().ToList() ) ;
        foreach ( var c in r.SubRoutes ) {
          subRoutes.Add( c ) ;
        }
      }

      // create Route tree
      //foreach ( var route in routes.Where(r => r.IsParentBranch(r) == true).Distinct().OrderBy( r => r.RouteName ).ToList() ) {
      foreach ( var route in routes.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
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

      /*foreach ( var c in subRoutes ) {
        TreeViewItem viewItem = new TreeViewItem() { Header = c } ;
        // 
        routeDictionary[ c.Route.RouteName ].Items.Add( viewItem ) ;
      }*/
    }

    /// <summary>
    /// Set SelectedFromtTo Dialog by Selected Route
    /// </summary>
    /// <param name="route"></param>
    public void DisplaySelectedFromTo( Route route )
    {
      if ( doc != null && uiDoc != null ) {
        SelectedFromToViewModel.SetSelectedFromToInfo( uiDoc, doc, route ) ;
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
        SelectedFromTo.CurveTypeLabel = SelectedFromTo.CurveTypes[ (int) SelectedFromTo.CurveTypeIndex ].GetType().Name.Split( 'T' )[ 0 ] + " Type" ;
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

      var selectedRoute = AllRoutes.Find( r => r.RouteName == selectedItem.Header.ToString() ) ;
      var targetElements = doc?.GetAllElementsOfRouteName<Element>( selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;

      //Select targetElements
      uiDoc?.Selection.SetElementIds( targetElements ) ;
      //Show targetElements
      uiDoc?.ShowElements( targetElements ) ;

      DisplaySelectedFromTo( selectedRoute ) ;
    }
  }
}