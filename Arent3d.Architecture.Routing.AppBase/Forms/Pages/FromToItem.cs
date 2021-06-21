using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.Data ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows.Media ;
using System.Windows.Media.Imaging ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public abstract class FromToItem : INotifyPropertyChanged
  {
    private string _itemTypeName { get ; set ; }
    public string ItemTag { get ; init ; }
    public ElementId? ElementId { get ; private init ; }
    public IReadOnlyList<FromToItem> Children => ChildrenList ;
    private bool _isEditing { get ; set ; }

    public bool IsEditing
    {
      get { return this._isEditing ; }
      set
      {
        if ( value != this._isEditing ) {
          this._isEditing = value ;
          NotifyPropertyChanged() ;
        }
      }
    }

    public string ItemTypeName
    {
      get { return this._itemTypeName ; }
      set
      {
        if ( value != this._itemTypeName ) {
          this._itemTypeName = value ;
          NotifyPropertyChanged() ;
        }
      }
    }

    public string? ItemFloor { get ; set ; }

    public bool IsRootRoute { get ; set ; }

    public SolidColorBrush TextColor { get ; set ; }
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
      ItemFloor = "" ;
      TextColor = (SolidColorBrush) ( new BrushConverter().ConvertFrom( "#000000" ) ) ;
      _itemTypeName = "" ;
      IsEditing = false ;
      ItemTag = "" ;
      ElementId = null ;
      ChildrenList = new List<FromToItem>() ;
      AllRoutes = allRoutes ;
      Doc = doc ;
      UiDoc = uiDoc ;
      IsRootRoute = false ;
    }

    public event PropertyChangedEventHandler? PropertyChanged ;

    public abstract void OnSelected() ;

    public abstract void OnDoubleClicked() ;

    private void NotifyPropertyChanged( [CallerMemberName] String propertyName = "" )
    {
      if ( PropertyChanged != null ) {
        PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) ) ;
      }
    }

    /// <summary>
    /// Create Hierarchical FromToData from allRoutes
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="uiDoc"></param>
    /// <param name="allRoutes"></param>
    /// <returns></returns>
    public static IEnumerable<FromToItem> CreateRouteFromToItems( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, FromToItemsUiBase fromToItemsUiBase )
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

      ViewFamilyType vft = (ViewFamilyType) doc.GetElement( doc.ActiveView.GetTypeId() ) ;

      foreach ( var route in parentFromTos.Distinct().OrderBy( r => r.RouteName ).ToList() ) {
        var routeItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, route, fromToItemsUiBase.FromToTreeIcons?[ "RouteItem" ] ) { ItemTypeName = route.RouteName, ElementId = route.OwnerElement?.Id, ItemTag = "Route" } ;
        routeItem.IsRootRoute = true ;
        List<ElementId> elements = doc.GetAllElementsOfRouteName<Element>( route.RouteName ).Select( elem => elem.Id ).ToList() ;

        Element? targetElement = null ;
        foreach ( ElementId elemntId in elements ) {
          Element element = doc.GetElement( elemntId ) ;
          if ( ! element.Name.Equals( "Routing Pass Point" ) ) {
            targetElement = element ;
            break ;
          }
        }

        Parameter? levelName = targetElement?.get_Parameter( BuiltInParameter.RBS_START_LEVEL_PARAM ) ;
        routeItem.ItemFloor = "(" + levelName?.AsValueString() + ")" ;

        if ( ! vft.Name.Equals( "3D ビュー" ) ) {
          string viewLevelName = routeItem.Doc.ActiveView.get_Parameter( BuiltInParameter.PLAN_VIEW_LEVEL ).AsString() ;
          if ( viewLevelName != levelName?.AsValueString() ) {
            SolidColorBrush scb = (SolidColorBrush) ( new BrushConverter().ConvertFrom( "#808080" ) ) ;
            scb.Opacity = 0.75 ;
            routeItem.TextColor = scb ;
          }
        }

        // store in dict
        if ( ItemDictionary != null ) {
          ItemDictionary[ route.RouteName ] = routeItem ;
        }

        // Create and add ChildItems
        routeItem.CreateChildItems( routeItem, fromToItemsUiBase ) ;
        yield return routeItem ;
      }

      foreach ( var c in childBranches ) {
        var branchItem = new FromToItem.RouteItem( doc, uiDoc, allRoutes, c, fromToItemsUiBase.FromToTreeIcons?[ "SubRouteItem" ] ) { ItemTypeName = c.RouteName, ElementId = c.OwnerElement?.Id, ItemTag = "Route" } ;
        var parentRouteName = c.GetParentBranches().ToList().Last().RouteName ;


        List<ElementId> elements = doc.GetAllElementsOfRouteName<Element>( c.RouteName ).Select( elem => elem.Id ).ToList() ;

        Element? targetElement = null ;
        foreach ( ElementId elemntId in elements ) {
          Element element = doc.GetElement( elemntId ) ;
          if ( ! element.Name.Equals( "Routing Pass Point" ) ) {
            targetElement = element ;
            break ;
          }
        }

        Parameter? levelName = targetElement?.get_Parameter( BuiltInParameter.RBS_START_LEVEL_PARAM ) ;
        branchItem.ItemFloor = "(" + levelName?.AsValueString() + ")" ;
        if ( ! vft.Name.Equals( "3D ビュー" ) ) {
          string viewLevelName = branchItem.Doc.ActiveView.get_Parameter( BuiltInParameter.PLAN_VIEW_LEVEL ).AsString() ;
          if ( viewLevelName != levelName?.AsValueString() ) {
            SolidColorBrush scb = (SolidColorBrush) ( new BrushConverter().ConvertFrom( "#808080" ) ) ;
            scb.Opacity = 0.75 ;
            branchItem.TextColor = scb ;
          }
        }

        // search own parent TreeViewItem
        if ( ItemDictionary != null ) {
          ItemDictionary[ parentRouteName ].ChildrenList.Add( branchItem ) ;
          ItemDictionary[ c.RouteName ] = branchItem ;
        }

        // Create and add ChildItems
        branchItem.CreateChildItems( branchItem, fromToItemsUiBase ) ;
      }
    }

    /// <summary>
    /// Create EndPointItem for Children
    /// </summary>
    /// <param name="routeItem"></param>
    /// <param name="endPoint"></param>
    private void CreateEndPointItem( RouteItem routeItem, IEndPoint endPoint, FromToItemsUiBase fromToItemsUiBase )
    {
      switch ( endPoint ) {
        // Create ConnectorItem
        case ConnectorEndPoint connectorEndPoint :
        {
          var connector = connectorEndPoint.GetConnector() ;
          if ( connector?.Owner is FamilyInstance familyInstance ) {
            var connectorItem = new ConnectorItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, connector, fromToItemsUiBase.FromToTreeIcons?[ "ConnectorItem" ] ) { ItemTypeName = familyInstance.Symbol.Family.Name + ":" + connector.Owner.Name, ElementId = connectorEndPoint.EquipmentId, ItemTag = "Connector", } ;
            var level = routeItem.Doc.GetElementById<Level>( familyInstance.LevelId ) ;
            connectorItem.ItemFloor = "(" + level?.Name + ")" ;
            if ( routeItem.Doc.GetElementById<ViewFamilyType>( routeItem.Doc.ActiveView.GetTypeId() ) is { } vft && ! vft.Name.Contains( "3D" ) ) {
              string viewLevelName = routeItem.Doc.ActiveView.get_Parameter( BuiltInParameter.PLAN_VIEW_LEVEL ).AsString() ;
              if ( viewLevelName != level?.Name ) {
                connectorItem.TextColor.Opacity = 0.75 ;
              }
            }

            routeItem.ChildrenList.Add( connectorItem ) ;
          }

          break ;
        }
        // Create PassPointItem
        case PassPointEndPoint passPointEndPoint :
        {
          var passPointItem = new PassPointItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, passPointEndPoint, fromToItemsUiBase.FromToTreeIcons?[ "PassPointItem" ] ) { ItemTypeName = "PassPoint", ElementId = passPointEndPoint.PassPointId, ItemTag = "PassPoint", } ;
          routeItem.ChildrenList.Add( passPointItem ) ;
          break ;
        }
        // Create PassPointItem
        case TerminatePointEndPoint terminatePointEndPoint :
        {
          var passPointItem = new TerminatePointItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, terminatePointEndPoint, fromToItemsUiBase.FromToTreeIcons?[ "TerminatePointItem" ] ) { ItemTypeName = "TerminatePoint", ElementId = terminatePointEndPoint.TerminatePointId, ItemTag = "TerminatePoint", } ;
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
    private void CreateSubRouteItem( RouteItem routeItem, SubRoute subRoute, FromToItemsUiBase fromToItemsUiBase )
    {
      var subRouteItem = new FromToItem.SubRouteItem( routeItem.Doc, routeItem.UiDoc, routeItem.AllRoutes, subRoute, fromToItemsUiBase.FromToTreeIcons?[ "SubRouteItem" ] ) { ItemTypeName = "Section", ElementId = Doc.GetAllElementsOfSubRoute<Element>( subRoute.Route.RouteName, subRoute.SubRouteIndex ).FirstOrDefault()?.Id, ItemTag = "SubRoute" } ;
      List<ElementId> elements = routeItem.Doc.GetAllElementsOfRouteName<Element>( subRoute.Route.RouteName ).Select( elem => elem.Id ).ToList() ;
      Element element = routeItem.Doc.GetElement( elements[ 0 ] ) ;
      Parameter? levelName = element.get_Parameter( BuiltInParameter.RBS_START_LEVEL_PARAM ) ;
      ViewFamilyType vft = (ViewFamilyType) routeItem.Doc.GetElement( routeItem.Doc.ActiveView.GetTypeId() ) ;

      //if ( !vft.Name.Equals( "3D ビュー" ) ) {
      //    string viewLevelName = routeItem.Doc.ActiveView.get_Parameter( BuiltInParameter.PLAN_VIEW_LEVEL ).AsString();
      //    if ( viewLevelName != levelName?.AsValueString() ) {
      //        SolidColorBrush scb = (SolidColorBrush) (new BrushConverter().ConvertFrom( "#808080" ));
      //        scb.Opacity = 0.75;
      //        subRouteItem.TextColor = scb;
      //   }
      //}
      routeItem?.ChildrenList.Add( subRouteItem ) ;
    }

    /// <summary>
    /// Create and add ChildItems to RouteItem
    /// </summary>
    /// <param name="routeItem"></param>
    private void CreateChildItems( RouteItem routeItem, FromToItemsUiBase fromToItemsUiBase )
    {
      foreach ( var subRoute in routeItem.SubRoutes ) {
        // if no PassPoint
        if ( routeItem.SubRoutes.Count() < 2 ) {
          foreach ( var endPoint in subRoute.AllEndPoints ) {
            if ( endPoint == subRoute.AllEndPoints.LastOrDefault() ) {
              routeItem.CreateSubRouteItem( routeItem, subRoute, fromToItemsUiBase ) ;
            }

            routeItem.CreateEndPointItem( routeItem, endPoint, fromToItemsUiBase ) ;
          }
        }
        // if with PassPoint
        else {
          if ( subRoute.AllEndPoints.FirstOrDefault() is { } endPointIndicator ) {
            routeItem.CreateEndPointItem( routeItem, endPointIndicator, fromToItemsUiBase ) ;
            routeItem.CreateSubRouteItem( routeItem, subRoute, fromToItemsUiBase ) ;
          }

          // Add last EndPoint
          if ( subRoute == routeItem.SubRoutes.LastOrDefault() ) {
            if ( subRoute.AllEndPoints.LastOrDefault() is { } toIndicator ) {
              routeItem.CreateEndPointItem( routeItem, toIndicator, fromToItemsUiBase ) ;
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

      private static BitmapImage RouteItemIcon { get ; set ; } = new BitmapImage( new Uri( "../../resources/MEP.ico", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public RouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, Route ownRoute, BitmapImage? bitmapImage ) : base( doc, uiDoc, allRoutes )
      {
        SubRoutes = ownRoute.SubRoutes ;
        /*if ( bitmapImage is { } ) { 
          RouteItemIcon = bitmapImage ;
        }*/ // TODO
        PropertySourceType = new PropertySource.RoutePropertySource( doc, ownRoute.SubRoutes ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        _selectedRoute = AllRoutes.FirstOrDefault( r => r.OwnerElement?.Id == ElementId ) ;

        if ( _selectedRoute == null ) return ;
        // set SelectedRoute to SelectedFromToViewModel
        SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, _selectedRoute.SubRoutes.ToList(), this ) ;

        _targetElements = Doc?.GetAllElementsOfRouteName<Element>( ItemTypeName ).Select( elem => elem.Id ).ToList() ;
        // Select targetElements
        if ( _targetElements != null ) {
          UiDoc.Selection.SetElementIds( _targetElements ) ;
        }
      }

      public override void OnDoubleClicked()
      {
        if ( _selectedRoute != null ) {
          // Select targetElements
          UiDoc.ShowElements( _targetElements ) ;
        }
      }
    }

    private class ConnectorItem : FromToItem
    {
      private List<ElementId>? _targetElements ;

      private static BitmapImage RouteItemIcon { get ; set ; } = new BitmapImage( new Uri( "../../resources/InsertBranchPoint.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public ConnectorItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, Connector connector, BitmapImage? bitmapImage ) : base( doc, uiDoc, allRoutes )
      {
        /*if ( bitmapImage is { } ) {
          RouteItemIcon = bitmapImage ;
        }*/
        PropertySourceType = new ConnectorPropertySource( doc, connector ) ;
      }

      public override void OnSelected()
      {
        if ( ElementId == null ) return ;
        _targetElements = new List<ElementId>() { ElementId } ;
        UiDoc.Selection.SetElementIds( _targetElements ) ;
      }

      public override void OnDoubleClicked()
      {
        UiDoc.ShowElements( _targetElements ) ;
      }
    }


    private class SubRouteItem : FromToItem
    {
      private Route? Route { get ; init ; }
      private int SubRouteIndex { get ; init ; }

      private List<ElementId>? _targetElements ;
      private static BitmapImage RouteItemIcon { get ; set ; } = new BitmapImage( new Uri( "../../resources/PickFrom-To.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public SubRouteItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, SubRoute ownSubRoute, BitmapImage? bitmapImage ) : base( doc, uiDoc, allRoutes )
      {
        SubRouteIndex = 0 ;
        Route = ownSubRoute.Route ;
        SubRouteIndex = ownSubRoute.SubRouteIndex ;
        /*if ( bitmapImage is { } ) {
          RouteItemIcon = bitmapImage ;
        }*/ // TODO
        PropertySourceType = new PropertySource.RoutePropertySource( Doc, new List<SubRoute>() { ownSubRoute } ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        if ( Route == null ) return ;
        _targetElements = Doc.GetAllElementsOfSubRoute<Element>( Route.RouteName, SubRouteIndex ).Select( e => e.Id ).ToList() ;
        // Select targetElements
        if ( _targetElements == null ) return ;
        UiDoc.Selection.SetElementIds( _targetElements ) ;
        // set SelectedRoute to SelectedFromToViewModel
        var targetSubRoutes = new List<SubRoute> { Route.SubRoutes.ElementAt( SubRouteIndex ) } ;
        if ( UiDoc != null ) SelectedFromToViewModel.SetSelectedFromToInfo( UiDoc, Doc, targetSubRoutes, this ) ;
      }

      public override void OnDoubleClicked()
      {
        UiDoc.ShowElements( _targetElements ) ;
      }
    }

    private class PassPointItem : FromToItem
    {
      private List<ElementId>? _targetElements ;
      private static BitmapImage RouteItemIcon { get ; set ; } = new BitmapImage( new Uri( "../../resources/InsertPassPoint.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public PassPointItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, PassPointEndPoint passPointEndPoint, BitmapImage? bitmapImage ) : base( doc, uiDoc, allRoutes )
      {
        /*if ( bitmapImage is { } ) {
          RouteItemIcon = bitmapImage ;
        }*/ // TODO
        PropertySourceType = new PassPointPropertySource( doc, passPointEndPoint ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        if ( ElementId == null ) return ;
        _targetElements = new List<ElementId>() { ElementId } ;
        UiDoc.Selection.SetElementIds( _targetElements ) ;
      }

      public override void OnDoubleClicked()
      {
        UiDoc.ShowElements( _targetElements ) ;
      }
    }

    private class TerminatePointItem : FromToItem
    {
      private List<ElementId>? _targetElements ;
      private static BitmapImage RouteItemIcon { get ; set ; } = new BitmapImage( new Uri( "../../resources/InsertPassPoint.png", UriKind.Relative ) ) ;
      public override BitmapImage Icon => RouteItemIcon ;

      public TerminatePointItem( Document doc, UIDocument uiDoc, IReadOnlyCollection<Route> allRoutes, TerminatePointEndPoint terminatePointEndPoint, BitmapImage? bitmapImage ) : base( doc, uiDoc, allRoutes )
      {
        /*if ( bitmapImage is { } ) {
          RouteItemIcon = bitmapImage ;
        }*/ // TODO
        PropertySourceType = new TerminatePointPropertySource( doc, terminatePointEndPoint ) ;
      }

      public override void OnSelected()
      {
        _targetElements = new List<ElementId>() ;

        if ( ElementId == null ) return ;
        _targetElements = new List<ElementId>() { ElementId } ;
        UiDoc.Selection.SetElementIds( _targetElements ) ;
      }

      public override void OnDoubleClicked()
      {
        UiDoc.ShowElements( _targetElements ) ;
      }
    }
  }
}