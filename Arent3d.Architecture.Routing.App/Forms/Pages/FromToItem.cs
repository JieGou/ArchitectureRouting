using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Data ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.App.Commands ;
using Arent3d.Architecture.Routing.App.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Forms
{
  public abstract class FromToItem
  {
    public string ItemTypeName { get ; private init ; }
    private string ItemTag { get ; init ; }
    public ElementId? ElementId { get ; private init ; }
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
    private IReadOnlyCollection<Route> AllRoutes { get ; set ; }
    private Document Doc { get ; }
    private UIDocument UiDoc { get ; }

    // Property source for UI
    public PropertySource? PropertySourceType { get ; private init ; }

    protected FromToItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes )
    {
      ItemTypeName = "" ;
      ItemTag = "" ;
      ElementId = null ;
      ChildrenList = new List<FromToItem>() ;
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
        var routeItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, route ) { ItemTypeName = route.RouteName, ElementId = route.OwnerElement?.Id, ItemTag = "Route" } ;
        // store in dict
        if ( ItemDictionary != null ) {
          ItemDictionary[ route.RouteName ] = routeItem ;
        }

        // Create and add ChildItems
        routeItem.CreateChildItems( routeItem ) ;
        yield return routeItem ;
      }

      foreach ( var c in childBranches ) {
        var branchItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, c ) { ItemTypeName = c.RouteName, ElementId = c.OwnerElement?.Id, ItemTag = "Route"} ;
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
    /// <param name="endPoint"></param>
    private void CreateEndPointItem( RouteItem routeItem, IEndPoint endPoint )
    {
      switch ( endPoint ) {
        // Create ConnectorItem
        case ConnectorEndPoint connectorEndPoint :
        {
          var connector = connectorEndPoint.GetConnector() ;
          if ( connector?.Owner is FamilyInstance familyInstance ) {
            var connectorItem = new ConnectorItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, connector )
            {
              ItemTypeName = familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name,
              ElementId = connectorEndPoint.EquipmentId,
              ItemTag = "Connector",
            } ;
            routeItem.ChildrenList.Add( connectorItem ) ;
          }

          break ;
        }
        // Create PassPointItem
        case PassPointEndPoint passPointEndPoint :
        {
          var passPointItem = new PassPointItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, passPointEndPoint )
          {
            ItemTypeName = "PassPoint", ElementId = passPointEndPoint.PassPointId, ItemTag = "PassPoint",
          } ;
          routeItem.ChildrenList.Add( passPointItem ) ;
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
      var subRouteItem = new FromToItem.SubRouteItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, subRoute ) { ItemTypeName = "Section", ElementId = Doc.GetAllElementsOfSubRoute<Element>( subRoute.Route.RouteName, subRoute.SubRouteIndex ).FirstOrDefault()?.Id, ItemTag = "SubRoute" } ;
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
          foreach ( var endPoint in subRoute.AllEndPoints ) {
            if ( endPoint == subRoute.AllEndPoints.LastOrDefault() ) {
              routeItem.CreateSubRouteItem( routeItem, subRoute ) ;
            }

            routeItem.CreateEndPointItem( routeItem, endPoint ) ;
          }
        }
        // if with PassPoint
        else {
          if ( subRoute.AllEndPoints.FirstOrDefault() is { } endPointIndicator ) {
            routeItem.CreateEndPointItem( routeItem, endPointIndicator ) ;
            routeItem.CreateSubRouteItem( routeItem, subRoute ) ;
          }

          // Add last EndPoint
          if ( subRoute == routeItem.SubRoutes.LastOrDefault() ) {
            if ( subRoute.AllEndPoints.LastOrDefault() is { } toIndicator ) {
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

      public readonly IEnumerable<SubRoute> SubRoutes ;


      private List<ElementId>? _targetElements ;

      private static BitmapImage RouteItemIcon { get ; } = new BitmapImage( new Uri( "../../resources/MEP.ico", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public RouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, Route ownRoute ) : base( doc, uiDoc, allRoutes )
      {
        SubRoutes = ownRoute.SubRoutes ;
        PropertySourceType = new PropertySource.RoutePropertySource( doc, ownRoute.SubRoutes ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        _selectedRoute = AllRoutes.FirstOrDefault( r => r.OwnerElement?.Id == ElementId ) ;

        if ( _selectedRoute == null ) return ;
        // set SelectedRoute to SelectedFromToViewModel
        SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, _selectedRoute.SubRoutes.ToList(), this ) ;

        _targetElements = Doc?.GetAllElementsOfRouteName<Element>( _selectedRoute.RouteName ).Select( elem => elem.Id ).ToList() ;
        // Select targetElements
        if ( _targetElements != null ) {
          UiDoc?.Selection.SetElementIds( _targetElements ) ;
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

      public ConnectorItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, Connector connector ) : base( doc, uiDoc, allRoutes )
      {
        PropertySourceType = new ConnectorPropertySource( doc, connector ) ;
      }

      public override void OnSelected()
      {
        if ( ElementId == null ) return ;
        _targetElements = new List<ElementId>() { ElementId } ;
        UiDoc?.Selection.SetElementIds( _targetElements ) ;
      }

      public override void OnDoubleClicked()
      {
        UiDoc?.ShowElements( _targetElements ) ;
      }
    }


    private class SubRouteItem : FromToItem
    {
      private Route? Route { get ; init ; }
      private int SubRouteIndex { get ; init ; }

      private List<ElementId>? _targetElements ;
      private static BitmapImage RouteItemIcon { get ; } = new BitmapImage( new Uri( "../../resources/PickFrom-To.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public SubRouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, SubRoute ownSubRoute ) : base( doc, uiDoc, allRoutes )
      {
        SubRouteIndex = 0 ;
        Route = ownSubRoute.Route ;
        SubRouteIndex = ownSubRoute.SubRouteIndex ;
        PropertySourceType = new PropertySource.RoutePropertySource( Doc, new List<SubRoute>() { ownSubRoute } ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        if ( Route == null ) return ;
        _targetElements = Doc.GetAllElementsOfSubRoute<Element>( Route.RouteName, SubRouteIndex ).Select( e => e.Id ).ToList() ;
        // Select targetElements
        if ( _targetElements == null ) return ;
        UiDoc?.Selection.SetElementIds( _targetElements ) ;
        // set SelectedRoute to SelectedFromToViewModel
        var targetSubRoutes = new List<SubRoute> { Route.SubRoutes.ElementAt( SubRouteIndex ) } ;
        if ( UiDoc != null ) SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, targetSubRoutes, this ) ;
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

      public PassPointItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, PassPointEndPoint passPointEndPoint ) : base( doc, uiDoc, allRoutes )
      {
        PropertySourceType = new PassPointPropertySource( doc, passPointEndPoint ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        if ( ElementId == null ) return ;
        _targetElements = new List<ElementId>() { ElementId } ;
        UiDoc?.Selection.SetElementIds( _targetElements ) ;
      }

      public override void OnDoubleClicked()
      {
        UiDoc?.ShowElements( _targetElements ) ;
      }
    }
  }
}