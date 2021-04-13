using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public abstract class FromToItem
  {
    public string ItemTypeName { get ; init ; }
    public string ItemTag { get ; init ; }
    public ElementId? ElementId { get ; init ; }
    public List<FromToItem>? Children { get ; set ; }
    public abstract BitmapImage? Icon { get ; }

    public static SortedDictionary<string, FromToItem>? ItemDictionary { get ; set ; }

    public bool Selected
    {
      get { return this.Selected ; }
      set
      {
        this.Selected = value ;
        if ( value == true ) {
          OnSelected() ;
        }
      }
    }

    public bool DisplaySelectedFromTo { get ; set ; }
    public IReadOnlyCollection<Route> AllRoutes { get ; set ; }
    private Document Doc { get ; }
    private UIDocument UiDoc { get ; }


    protected FromToItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes )
    {
      ItemTypeName = "" ;
      ItemTag = "" ;
      ElementId = null ;
      Children = new List<FromToItem>() ;
      DisplaySelectedFromTo = false ;
      AllRoutes = allRoutes ;
      Doc = doc ;
      UiDoc = uiDoc ;
    }

    public abstract void OnSelected() ;

    public abstract void OnDoubleClicked() ;

    public static IEnumerable<FromToItem> CreateRouteFromToItems( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes )
    {
      var childBranches = new List<Route>() ;

      var parentFromTos = new List<Route>() ;

      ItemDictionary = new SortedDictionary<string, FromToItem>() ;

      //IEnumerable<FromToItem> fromToItems ;

      foreach ( var route in allRoutes ) {
        if ( route.HasParent() ) {
          childBranches.Add( route ) ;
        }
        else {
          parentFromTos.Add( route ) ;
        }
      }

      foreach ( var route in parentFromTos.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
        var routeItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, route ) { ItemTypeName = route.RouteName, ElementId = route.OwnerElement?.Id, ItemTag = "Route", DisplaySelectedFromTo = true } ;
        // store in dict
        if ( ItemDictionary != null ) {
          ItemDictionary[ route.RouteName ] = routeItem ;
        }

        // add connector to Parent FromToItem
        routeItem.CreateConnectorItems( routeItem ) ;
        // add to fromToItems
        yield return routeItem ;
      }

      foreach ( var c in childBranches ) {
        var branchItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, c ) { ItemTypeName = c.RouteName, ElementId = c.OwnerElement?.Id, ItemTag = "Route", DisplaySelectedFromTo = true } ;
        var parentRouteName = c.GetParentBranches().ToList().Last().RouteName ;
        // search own parent TreeViewItem
        if ( ItemDictionary != null ) {
          ItemDictionary[ parentRouteName ].Children?.Add( branchItem ) ;
          ItemDictionary[ c.RouteName ] = branchItem ;
        }

        // add connector to branch treeviewitem
        branchItem.CreateConnectorItems( branchItem ) ;
      }
    }

    private void CreateConnectorItems( RouteItem routeItem )
    {
      foreach ( var connector in routeItem.Connectors ) {
        if ( connector.Owner is FamilyInstance familyInstance && routeItem != null ) {
          var connectorItem = new FromToItem.ConnectorItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes ) { ItemTypeName = familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name, ElementId = connector.Owner.Id, ItemTag = "Connector", DisplaySelectedFromTo = false } ;
          routeItem?.Children?.Add( connectorItem ) ;
        }
        else {
          return ;
        }
      }
    }


    private class RouteItem : FromToItem
    {
      public Route? SelectedRoute ;
      public IEnumerable<Route> ChildBranches ;
      public IEnumerable<Connector> Connectors ;

      private List<ElementId>? _targetElements ;


      public override BitmapImage Icon { get ; } = new BitmapImage( new Uri( "../../resources/MEP.ico", UriKind.Relative ) ) ;

      public RouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, Route ownRoute ) : base( doc, uiDoc, allRoutes )
      {
        ChildBranches = allRoutes.Where( r => r.HasParent() && r.GetParentBranches().ToList().Last().RouteName == ownRoute.RouteName ) ;
        Connectors = ownRoute.GetAllConnectors( doc ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        SelectedRoute = AllRoutes?.ToList().Find( r => r.OwnerElement?.Id == ElementId ) ;

        if ( SelectedRoute != null ) {
          // set SelectedRoute to SelectedFromToViewModel
          SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, SelectedRoute ) ;

          _targetElements = Doc?.GetAllElementsOfRouteName<Element>( SelectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
          //Select targetElements
          if ( _targetElements != null ) {
            UiDoc?.Selection.SetElementIds( _targetElements ) ;
          }
        }
      }

      public override void OnDoubleClicked()
      {
        if ( SelectedRoute != null ) {
          //Select targetElements
          UiDoc?.ShowElements( _targetElements ) ;
        }
      }
    }

    private class ConnectorItem : FromToItem
    {
      private List<ElementId>? _targetElements ;

      public override BitmapImage Icon { get ; } = new BitmapImage( new Uri( "../../resources/InsertBranchPoint.png", UriKind.Relative ) ) ;

      public ConnectorItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes ) : base( doc, uiDoc, allRoutes )
      {
      }

      public override void OnSelected()
      {
        if ( ElementId != null ) {
          _targetElements = new List<ElementId>() { ElementId } ;
          UiDoc?.Selection.SetElementIds( _targetElements ) ;
        }
      }

      public override void OnDoubleClicked()
      {
        UiDoc?.ShowElements( _targetElements ) ;
      }
    }


    private class SubRouteItem : FromToItem
    {
      private List<ElementId>? _targetElements ;

      public override BitmapImage Icon { get ; } = new BitmapImage( new Uri( "../../resources/InsertPassPoint.png", UriKind.Relative ) ) ;

      public SubRouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes ) : base( doc, uiDoc, allRoutes )
      {
      }

      public override void OnSelected()
      {
        if ( ElementId != null ) {
          _targetElements = new List<ElementId>() { ElementId } ;
          UiDoc?.Selection.SetElementIds( _targetElements ) ;
        }
      }

      public override void OnDoubleClicked()
      {
        UiDoc?.ShowElements( _targetElements ) ;
      }
    }
  }
}