using System ;
using System.Collections ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Frame = Autodesk.Revit.DB.Frame ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public partial class FromToTree : Page, IDisposable, IDockablePaneProvider
  {
    public ExternalCommandData? eData = null ;
    public Document? doc = null ;
    public UIDocument? uiDoc = null ;
    public FromToTreeViewModel? vm = null ;

    public FromToTree()
    {
      InitializeComponent() ;

      vm = new FromToTreeViewModel() ;
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

      // get the active view name
      viewName.Text = doc.ActiveView.Name ;
      // call the treeview display method
      DisplayTreeViewItem( uiDoc ) ;
    }

    /// <summary>
    /// set TreeViewItems
    /// </summary>
    public void DisplayTreeViewItem( UIDocument uiDoc )
    {
      // clear items first
      FromToTreeView.Items.Clear() ;

      // routename and childrenname dictionary
      SortedDictionary<string, TreeViewItem> routeDictionary = new SortedDictionary<string, TreeViewItem>() ;

      // routenames
      List<string> routeNames = new List<string>() ;

      List<string> childBranchNames = new List<string>() ;
      List<SubRoute> subRoutes = new List<SubRoute>() ; //change to List<Route> childBranches = new List<Route>()

      //collect view type
      List<Element> elements = new FilteredElementCollector( doc ).OfClass( typeof( View ) ).ToList() ;

      // collect Routes
      ObservableCollection<Route> allRoutes = new ObservableCollection<Route>( uiDoc.Document.CollectRoutes() ) ;


      foreach ( var r in allRoutes ) {
        routeNames.Add( r.RouteName ) ;

        //get ChildBranches
        //foreach ( var c in r.GetChildBranches() ) {
        foreach ( var c in r.SubRoutes ) {
          childBranchNames.Add( c.ToString() ) ;
          subRoutes.Add( c ) ;
        }
      }

      // create Route tree
      foreach ( var routeName in routeNames.Distinct().OrderBy( name => name ).ToList() ) {
        // create route treeviewitem
        TreeViewItem routeItem = new TreeViewItem() { Header = routeName } ;
        // store in dict
        routeDictionary[ routeName ] = routeItem ;
        // add to treeview
        FromToTreeView.Items.Add( routeItem ) ;
      }
      
      // create branches tree
      /* foreach ( var c in childBranchNames ) {
   TreeViewItem viewItem = new TreeViewItem() { Header = c } ;
   // 
   routeDictionary[c].Items.Add( viewItem ) ;
 }*/

      foreach ( var c in subRoutes ) {
        TreeViewItem viewItem = new TreeViewItem() { Header = c } ;
        // 
        routeDictionary[ c.Route.RouteName ].Items.Add( viewItem ) ;
      }
    }
  }
}