using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
  public static class ConduitUtil
  {
    public static List<Element> GetConduitRelated( Document doc, List<Element> elements )
    {
      var relatedConduits = new List<Element>() ;
      var connectorIds = elements
        .Where( e => e is FamilyInstance && e.Category.GetBuiltInCategory() is BuiltInCategory.OST_ElectricalFixtures or BuiltInCategory.OST_ElectricalEquipment )
        .Select( c => c.UniqueId ).ToHashSet() ;
      var conduits = elements.Where( e => e is Conduit ) ;
      var allRouteNames = conduits.Where( c => {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        return rNameArray.Length == 2 ;
      } ).Select( c => c.GetRouteName() ).Distinct() ;
      foreach ( var routeName in allRouteNames ) {
        var conduitsOfRoute = GetConduitsOfRoute( doc, connectorIds, routeName! ) ;
        relatedConduits.AddRange( conduitsOfRoute ) ;
      }

      return relatedConduits ;
    }

    private static IEnumerable<Element> GetConduitsOfRoute( Document document, ICollection<string> connectorIds, string routeName )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).Where( e => e.Name != ElectricalRoutingFamilyType.PullBox.GetFamilyName() ).EnumerateAll() ;
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits )
        .Where( c => {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == routeName ;
      } ).EnumerateAll() ;
      string fromConnectorId = string.Empty ;
      string toConnectorId = string.Empty ;
      foreach ( var conduit in conduitsOfRoute ) {
        if ( string.IsNullOrEmpty( fromConnectorId ) ) {
          var fromConnector = GetConnectorOfRoute( allConnectors, conduit, true ) ;
          if ( fromConnector != null ) fromConnectorId = fromConnector.UniqueId ;
        }

        if ( ! string.IsNullOrEmpty( toConnectorId ) ) continue ;
        var toConnector = GetConnectorOfRoute( allConnectors, conduit, false ) ;
        if ( toConnector != null ) toConnectorId = toConnector.UniqueId ;
      }
      
      if ( string.IsNullOrEmpty( fromConnectorId ) || string.IsNullOrEmpty( toConnectorId ) || ! connectorIds.Contains( fromConnectorId ) || ! connectorIds.Contains( toConnectorId ) ) return new List<Element>() ;
      return conduitsOfRoute ;
    }

    private static Element? GetConnectorOfRoute( IEnumerable<Element> allConnectors, Element conduit, bool isFrom )
    {
      var endPoint = conduit.GetNearestEndPoints( isFrom ).ToList() ;
      if ( ! endPoint.Any() ) return null ;
      var endPointKey = endPoint.First().Key ;
      var elementId = endPointKey.GetElementUniqueId() ;
      if ( string.IsNullOrEmpty( elementId ) ) return null ;
      var connector = allConnectors.SingleOrDefault( c => c.UniqueId == elementId ) ;
      if ( connector == null || connector.IsTerminatePoint() || connector.IsPassPoint() ) return null ;
      return connector ;
    }
    
    public static Element? GetConnectorOfRoute( Document document, string routeName, bool isFrom )
    {
      var routeNameArray = routeName.Split( '_' ) ;
      routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).Where( e => e.Name != ElectricalRoutingFamilyType.PullBox.GetFamilyName() ).ToList() ;
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => {
        if ( c.GetRouteName() is not { } rName ) return false ;
        var rNameArray = rName.Split( '_' ) ;
        var strRouteName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
        return strRouteName == routeName ;
      } ).ToList() ;
      foreach ( var conduit in conduitsOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( isFrom ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
        return toConnector ;
      }

      return null ;
    }

    public static IEnumerable<DetailTableItemModel> GetDetailTableItemsFromConduits(this IEnumerable<Element> allConduits,Document doc)
    {
      var csvStorable = doc.GetCsvStorable() ;
      var storageService = new StorageService<Level, DetailSymbolModel>( ( (ViewPlan) doc.ActiveView ).GenLevel ) ;
      var allConduitIds = allConduits.Select( p => p.UniqueId ).ToList() ;
      var (detailTableModels, _, _) =
        CreateDetailTableCommandBase.CreateDetailTableItem( doc, csvStorable, storageService, allConduits.ToList(),
          allConduitIds, false ) ;

      return detailTableModels ;
    }

    public static ( IEndPoint? FromEndPoint, IEndPoint? ToEndPoint ) GetFromElementIdAndToElementIdOfConduit( Element conduit )
    {
      IEndPoint? fromEndPoint = null, toEndPoint = null ;
      var fromEndPoints = conduit.GetNearestEndPoints( true ).ToList() ;
      if ( fromEndPoints.Any() )
        fromEndPoint = fromEndPoints.First() ;
      var toEndPoints = conduit.GetNearestEndPoints( false ).ToList() ;
      if ( toEndPoints.Any() )
        toEndPoint = toEndPoints.First() ;
      return ( fromEndPoint, toEndPoint ) ;
    }
  }
}