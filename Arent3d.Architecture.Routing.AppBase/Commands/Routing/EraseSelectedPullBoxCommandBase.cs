using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.DB.Electrical ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseSelectedPullBoxCommandBase : RoutingCommandBase<IReadOnlyCollection<Route>>
  {
    protected abstract AddInType GetAddInType() ;
    
    protected override OperationResult<IReadOnlyCollection<Route>> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      return new OperationResult<IReadOnlyCollection<Route>>( SelectRoutes( commandData.Application.ActiveUIDocument ) ) ;
    }

    private IReadOnlyCollection<Route> SelectRoutes( UIDocument uiDocument )
    {
      var document = uiDocument.Document ;
      //var list = PointOnRoutePicker.PickedRoutesFromSelections( uiDocument ).EnumerateAll() ;
      // var routingExecutor = GetRoutingExecutor() ;
      //
      // var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element) ;
      //
      // var element = document.GetElement( pickedObject?.ElementId ) ;
      //
      // var connId = element.GetPropertyInt( RoutingFamilyLinkedParameter.RouteConnectorRelationId ) ;

      //var pullbox = eleConn.GetConnectors().FirstOrDefault( conn => conn.Id == connId ) ;
      // var routeNames = list.Where( r => ! string.IsNullOrEmpty( r.Name ) ).Select( r => r.Name ).ToHashSet() ;
      // if ( routeNames.Any() ) ChangeWireTypeCommand.RemoveDetailLinesByRoutes( uiDocument.Document, routeNames ) ;
      // if ( 0 < list.Count ) return list ;

      // var pullbox = ConnectorPicker.GetPullBox(uiDocument,"Pick pull box", GetAddInType()) ;
      // var all = pullbox.GetAllRelatedElements() ;
      // var routes = pullbox.PickedElement.GetRouteName() ;
      
       PullPoxPickFilter detailSymbolFilter = new() ;

       var pickedPullBox = uiDocument.Selection.PickObject( ObjectType.Element, detailSymbolFilter ) ;
       var element = document.GetElement( pickedPullBox?.ElementId ) ;
      
     
      // var allConduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit )).OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      //
      // var route2 = element.GetRouteName() ;
      //
      //var routes = document.GetAllElements<Element>().OfCategory( ).OfNotElementType().Where( filter ).OfType<TElement>() ;
      //var routes =  document.GetAllElements<Route>().OfCategory( BuiltInCategory.OST_RouteCurve ) ;
      
      var allConduits = document.GetAllElements<Conduit>().OfCategory( BuiltInCategory.OST_Conduit ) ;
      
      //var routeNames = allConduits.Select( conduit =>  conduit.GetRouteName() ).Distinct();
      //  // .GroupBy( conduit => conduit.GetRouteName() ).Select( conduit => conduit.Key ).ToList() ;
        var hashSet = document.CollectRoutes( GetAddInType() ).Select( x=>x.Name ) ;
      //
       var test = GetRoutesByName( hashSet, document ) ;

      // var test1 = document.CollectRoutes( GetAddInType() ).SelectMany( r => r.GetAllConnectors() ) ;
      
      

      //  var list = document.GetAllElementsOfRouteName<Element>(routeNames[0]!).Distinct() ;
      //
      // var from = allConduits[ 1 ].GetRoutingConnectors( true ) ;
      // var to = allConduits[ 1 ].GetRoutingConnectors( false ) ;

      // foreach ( var conduit in allConduits ) {
      //   GetFromConnectorIdAndToConnectorIdOfCable( conduit, document,element  ) ;
      // }

      List<Element> listConduit = new List<Element>() ;

      foreach ( var conduit in allConduits ) {
        var a = ( GetConduitRelatedPullBox( conduit, document, element ) ) ;
         listConduit.AddRange( a );
      }

      var allConduitsRelatedPullBox = listConduit ;
      
    
      var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to delete.", GetAddInType() ) ;
    
      
      var pickRouteNames = new HashSet<string>() { pickInfo.Route.Name } ;
      if ( pickRouteNames.Any() ) ChangeWireTypeCommand.RemoveDetailLinesByRoutes( uiDocument.Document, pickRouteNames ) ;
      return new[] { pickInfo.Route } ;
    }
    
    private IEnumerable<Element> GetConduitRelatedPullBox( Element conduit,  Document document, Element pickedPullBox )
    {
      var fromEndPoint = conduit.GetNearestEndPoints( true ) ;
      var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
      if ( fromEndPointKey != null ) {
        var fromElementUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromElementUniqueId ) && pickedPullBox.UniqueId == fromElementUniqueId) {
          yield return conduit ;
        }
      }
      
      var toEndPoint = conduit.GetNearestEndPoints( false ) ;
      var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
      if ( toEndPointKey != null ) {
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( toElementUniqueId ) &&  pickedPullBox.UniqueId == toElementUniqueId ) {
          yield return conduit ;
        }
      }
    }

    private IReadOnlyCollection<Route> GetRoutesByName( IEnumerable<string> routeNames, Document document )
    {
      var dic = RouteCache.Get( DocumentKey.Get( document ) ) ;
      List<Route> routes = new List<Route>() ;
      foreach ( var routeName in routeNames ) {
        if ( routeName != null ) {
          if ( false == dic.TryGetValue( routeName, out var route )) continue ;
          var subRoute = route.RouteSegments ;
          
        }
      }

      return routes ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, IReadOnlyCollection<Route> routes )
    {
      return GetSelectedRouteSegments( document, routes ) ;
    }

    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSelectedRouteSegments( Document document, IReadOnlyCollection<Route> pickedRoutes )
    {
      var selectedRoutes = Route.CollectAllDescendantBranches( pickedRoutes ) ;

      var recreatedRoutes = Route.GetAllRelatedBranches( selectedRoutes ) ;
      recreatedRoutes.ExceptWith( selectedRoutes ) ;
      RouteGenerator.EraseRoutes( document, selectedRoutes.ConvertAll( route => route.RouteName ), true ) ;

      // Returns affected but not deleted routes to recreate them.
      return recreatedRoutes.ToSegmentsWithName().EnumerateAll() ;
    }
    
    private class PullPoxPickFilter : ISelectionFilter
    {
      
      public bool AllowElement( Element e )
      {
        return ( ((FamilyInstance) e).GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true ;
      }
    }
  }
}