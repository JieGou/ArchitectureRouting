using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
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
      data.InitialState = new DockablePaneState
      {
        DockPosition = DockPosition.Tabbed,
        TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
      } ;
    }
    
    /// <summary>
    /// initialize page
    /// </summary>
    /// <param name="uiApplication"></param>
    public void CustomInitiator( UIApplication uiApplication )
    {
      doc = uiApplication.ActiveUIDocument.Document ;
      uiDoc = uiApplication.ActiveUIDocument ;

      // get the current document name
      docName.Text = doc.PathName.ToString().Split( '\\' ).Last() ;
      // get the active view name
      viewName.Text = doc.ActiveView.Name ;
      // call the treeview display method
      DisplayTreeViewItem() ;
    }

    /// <summary>
    /// set TreeViewItems
    /// </summary>
    public void DisplayTreeViewItem()
    {
      // clear items first
      FromToTreeView.Items.Clear() ;

      // viewtypename and treeviewitem dictionary
      SortedDictionary<string, TreeViewItem> viewTypeDictionary = new SortedDictionary<string, TreeViewItem>() ;

      //viewtypename
      List<string> viewTypeNames = new List<string>() ;

      //collect view type
      List<Element> elements = new FilteredElementCollector( doc ).OfClass( typeof( View ) ).ToList() ;

      foreach ( var element in elements ) {
        View? view = element as View ;

        if ( view != null ) {
          viewTypeNames.Add( view.ViewType.ToString() ) ;
        }
      }

      //create treeviewitem for viewtype
      foreach ( var viewTypeName in viewTypeNames.Distinct().OrderBy( name => name ).ToList() ) {
        // create viewtype treeviewitem
        TreeViewItem viewTypeItem = new TreeViewItem() { Header = viewTypeName } ;
        // store in dict
        viewTypeDictionary[ viewTypeName ] = viewTypeItem ;
        // add to treeview
        FromToTreeView.Items.Add( viewTypeItem ) ;
      }

      foreach ( var element in elements ) {
        // view
        View? view = element as View ;
        if ( view != null ) {
          // viewname
          string viewName = view.Name ;
          // view typename
          string viewTypeName = view.ViewType.ToString() ;
          // create view treeviewitem
          TreeViewItem viewItem = new TreeViewItem() { Header = viewName } ;
          // view item add to view type
          viewTypeDictionary[ viewTypeName ].Items.Add( viewItem ) ;
        }
      }
    }
  }
}