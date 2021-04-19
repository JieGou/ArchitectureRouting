using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Data ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public abstract class FromToItem
  {
    public string ItemTypeName { get ; init ; }
    private string ItemTag { get ; init ; }
    public ElementId? ElementId { get ; init ; }
    public IReadOnlyList<FromToItem> Children => ChildrenList ;

    private List<FromToItem> ChildrenList { get ; }
    public abstract BitmapImage? Icon { get ; }

    private static SortedDictionary<string, FromToItem>? ItemDictionary { get ; set ; }

    private bool _selected ;

    public bool Selected
    {
      get => this._selected ;
      set
      {
        this._selected = value ;
        if ( value == true ) {
          OnSelected() ;
        }
      }
    }

    public bool DisplaySelectedFromTo { get ; set ; }
    private IReadOnlyCollection<Route> AllRoutes { get ; set ; }
    private Document Doc { get ; }
    private UIDocument UiDoc { get ; }


    protected FromToItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes )
    {
      ItemTypeName = "" ;
      ItemTag = "" ;
      ElementId = null ;
      ChildrenList = new List<FromToItem>() ;
      DisplaySelectedFromTo = false ;
      AllRoutes = allRoutes ;
      Doc = doc ;
      UiDoc = uiDoc ;
    }

    public abstract void OnSelected() ;

    public abstract void OnDoubleClicked() ;

    /// <summary>
    /// Create Hierarchical FromToData from allRoutes
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="uiDoc"></param>
    /// <param name="allRoutes"></param>
    /// <returns></returns>
    public static IEnumerable<FromToItem> CreateRouteFromToItems( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes )
    {
      var childBranches = new List<Route>() ;

      var parentFromTos = new List<Route>() ;

      ItemDictionary = new SortedDictionary<string, FromToItem>() ;

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

        // Create and add ChildItems
        routeItem.CreateChildItems( routeItem ) ;
        yield return routeItem ;
      }

      foreach ( var c in childBranches ) {
        var branchItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, c ) { ItemTypeName = c.RouteName, ElementId = c.OwnerElement?.Id, ItemTag = "Route", DisplaySelectedFromTo = true } ;
        var parentRouteName = c.GetParentBranches().ToList().Last().RouteName ;
        // search own parent TreeViewItem
        if ( ItemDictionary != null ) {
          ItemDictionary[ parentRouteName ].ChildrenList.Add( branchItem ) ;
          ItemDictionary[ c.RouteName ] = branchItem ;
        }

        // Create and add ChildItems
        branchItem.CreateChildItems( branchItem ) ;
      }
    }

    /// <summary>
    /// Create EndPointItem for Children
    /// </summary>
    /// <param name="routeItem"></param>
    private void CreateEndPointItem( RouteItem routeItem, IEndPointIndicator endPoint )
    {
      switch ( endPoint ) {
        // Create ConnectorItem
        case ConnectorIndicator connectorIndicator :
        {
          var connector = connectorIndicator.GetConnector( Doc ) ;
          if ( connector?.Owner is FamilyInstance familyInstance ) {
            var connectorItem = new FromToItem.ConnectorItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes ) { ItemTypeName = familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name, ElementId = connector.Owner.Id, ItemTag = "Connector", DisplaySelectedFromTo = false } ;
            routeItem?.ChildrenList.Add( connectorItem ) ;
          }
          break ;
        }
        // Create PassPointItem
        case PassPointEndIndicator passPointEndIndicator :
        {
          var passPointItem = new FromToItem.PassPointItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes )
          {
            ItemTypeName = "PassPoint", ElementId = new ElementId( passPointEndIndicator.ElementId ), ItemTag = "PassPoint", DisplaySelectedFromTo = false,
          } ;
          routeItem?.ChildrenList.Add( passPointItem ) ;
          break ;
        }
      }
    }

    /// <summary>
    /// Create SubRouteItem
    /// </summary>
    /// <param name="routeItem"></param>
    /// <param name="subRoute"></param>
    private void CreateSubRouteItem( RouteItem routeItem, SubRoute subRoute )
    {
      var subRouteItem = new FromToItem.SubRouteItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes )
      {
        ItemTypeName = "Section",
        ElementId = Doc.GetAllElementsOfSubRoute<Element>( subRoute.Route.RouteName, subRoute.SubRouteIndex ).FirstOrDefault()?.Id,
        ItemTag = "SubRoute",
        DisplaySelectedFromTo = true,
        Route = subRoute.Route,
        SubRouteIndex = subRoute.SubRouteIndex
      } ;
      routeItem?.ChildrenList.Add( subRouteItem ) ;
    }

    /// <summary>
    /// Create and add ChildItems to RouteItem
    /// </summary>
    /// <param name="routeItem"></param>
    private void CreateChildItems( RouteItem routeItem )
    {
      foreach ( var subRoute in routeItem.SubRoutes ) {
        // if no PassPoint
        if ( routeItem.SubRoutes.Count() < 2 ) {
          foreach ( var endPoint in subRoute.AllEndPointIndicators ) {
            if ( endPoint == subRoute.AllEndPointIndicators.LastOrDefault() ) {
              routeItem.CreateSubRouteItem( routeItem, subRoute ) ;
            }

            routeItem.CreateEndPointItem( routeItem, endPoint ) ;
          }
        }
        // if with PassPoint
        else {
          if ( subRoute.AllEndPointIndicators.FirstOrDefault() is { } endPointIndicator ) {
            routeItem.CreateEndPointItem( routeItem, endPointIndicator ) ;
            routeItem.CreateSubRouteItem( routeItem, subRoute ) ;
          }

          // Add last EndPoint
          if ( subRoute == routeItem.SubRoutes.LastOrDefault() ) {
            if ( subRoute.AllEndPointIndicators.LastOrDefault() is { } toIndicator ) {
              routeItem.CreateEndPointItem( routeItem, toIndicator ) ;
            }
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    private class RouteItem : FromToItem
    {
      private Route? _selectedRoute ;
      private IEnumerable<Route> _childBranches ;
      public readonly IEnumerable<Connector> Connectors ;

      public readonly IEnumerable<SubRoute> SubRoutes ;


      private List<ElementId>? _targetElements ;

      private static BitmapImage RouteItemIcon { get ; } = new BitmapImage( new Uri( "../../resources/MEP.ico", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public RouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, Route ownRoute ) : base( doc, uiDoc, allRoutes )
      {
        _childBranches = allRoutes.Where( r => r.HasParent() && r.GetParentBranches().ToList().Last().RouteName == ownRoute.RouteName ) ;
        Connectors = ownRoute.GetAllConnectors( doc ) ;
        SubRoutes = ownRoute.SubRoutes ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        _selectedRoute = AllRoutes?.ToList().Find( r => r.OwnerElement?.Id == ElementId ) ;

        if ( _selectedRoute != null ) {
          // set SelectedRoute to SelectedFromToViewModel
          SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, _selectedRoute ) ;

          _targetElements = Doc?.GetAllElementsOfRouteName<Element>( _selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
          // Select targetElements
          if ( _targetElements != null ) {
            UiDoc?.Selection.SetElementIds( _targetElements ) ;
          }
        }
      }

      public override void OnDoubleClicked()
      {
        if ( _selectedRoute != null ) {
          // Select targetElements
          UiDoc?.ShowElements( _targetElements ) ;
        }
      }
    }

    private class ConnectorItem : FromToItem
    {
      private List<ElementId>? _targetElements ;

      private static BitmapImage RouteItemIcon { get ; } = new BitmapImage( new Uri( "../../resources/InsertBranchPoint.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

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
      public Route? Route { get ; init ; }
      public int SubRouteIndex { get ; init ; }

      private List<ElementId>? _targetElements ;
      private static BitmapImage RouteItemIcon { get ; } = new BitmapImage( new Uri( "../../resources/PickFrom-To.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public SubRouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes ) : base( doc, uiDoc, allRoutes )
      {
        SubRouteIndex = 0 ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;
        // set SelectedRoute to SelectedFromToViewModel
        SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, Route ) ;

        if ( Route != null ) {
          _targetElements = Doc.GetAllElementsOfSubRoute<Element>( Route.RouteName, SubRouteIndex ).Select( e => e.Id ).ToList() ;
          // Select targetElements
          if ( _targetElements != null ) {
            UiDoc?.Selection.SetElementIds( _targetElements ) ;
          }
        }
      }

      public override void OnDoubleClicked()
      {
        UiDoc?.ShowElements( _targetElements ) ;
      }
    }

    private class PassPointItem : FromToItem
    {
      private List<ElementId>? _targetElements ;
      private static BitmapImage RouteItemIcon { get ; } = new BitmapImage( new Uri( "../../resources/InsertPassPoint.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public PassPointItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes ) : base( doc, uiDoc, allRoutes )
      {
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

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