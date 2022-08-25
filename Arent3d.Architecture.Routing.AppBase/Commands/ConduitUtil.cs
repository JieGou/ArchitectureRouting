using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
  public static class ConduitUtil
  {
    public static List<Element> GetConduitRelated( Document doc, List<Element> conduits )
    {
      var result = new List<Element>() ;
      var allConduits = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ) ;
      foreach ( var conduit in conduits ) {
        bool hasStartElement = false ;
        bool hasEndElement = false ;
        string startTerminateId = string.Empty ;
        string endTerminateId = string.Empty ;
        var conduitRouteName = conduit.GetRouteName() ;
        var startPoint = conduit.GetNearestEndPoints( true ) ;
        var startPointKey = startPoint.FirstOrDefault()?.Key ;
        if ( startPointKey != null ) {
          startTerminateId = startPointKey.GetElementUniqueId() ;
        }

        var endPoint = conduit.GetNearestEndPoints( false ) ;
        var endPointKey = endPoint.FirstOrDefault()?.Key ;
        if ( endPointKey != null ) {
          endTerminateId = endPointKey.GetElementUniqueId() ;
        }

        if ( ! string.IsNullOrEmpty( startTerminateId ) && ! string.IsNullOrEmpty( endTerminateId ) ) {
          var (startConnectorId, endConnectorId) = GetFromConnectorIdAndToConnectorId( doc, startTerminateId, endTerminateId ) ;
          hasStartElement = conduits.Any( c => c.UniqueId == startConnectorId ) ;
          hasEndElement = conduits.Any( c => c.UniqueId == endConnectorId ) ;
        }

        if ( ! string.IsNullOrEmpty( conduitRouteName ) && hasStartElement && hasEndElement ) {
          var relateConduits = allConduits.Where( x => x.GetRouteName() == conduitRouteName ).ToList() ;
          bool isNotFull = relateConduits.Any( x => conduits.All( y => y.Id != x.Id ) ) ;
          if ( ! isNotFull ) {
            result.AddRange( relateConduits ) ;
          }
        }
      }

      return result ;
    }

    private static (string, string) GetFromConnectorIdAndToConnectorId( Document document, string fromElementId, string toElementId )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;

      if ( ! string.IsNullOrEmpty( fromElementId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromElementId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorId ) ;
          if ( ! string.IsNullOrEmpty( fromConnectorId ) )
            fromElementId = fromConnectorId! ;
        }
      }

      if ( string.IsNullOrEmpty( toElementId ) ) return ( fromElementId, toElementId ) ;
      {
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementId ) ;
        if ( ! toConnector!.IsTerminatePoint() && ! toConnector!.IsPassPoint() ) return ( fromElementId, toElementId ) ;
        toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? toConnectorId ) ;
        if ( ! string.IsNullOrEmpty( toConnectorId ) )
          toElementId = toConnectorId! ;
      }

      return ( fromElementId, toElementId ) ;
    }
    
    public  static  Element? GetConnectorOfRoute( Document document, string routeName, bool isFrom )
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

    public static ( string, string ) GetFromElementIdAndToElementIdOfConduit( Element conduit )
    {
      var fromElementId = string.Empty ;
      var toElementId = string.Empty ;
      var fromEndPoints = conduit.GetNearestEndPoints( true ).ToList() ;
      if ( fromEndPoints.Any() )
        fromElementId = fromEndPoints.First().Key.GetElementUniqueId() ;
      var toEndPoints = conduit.GetNearestEndPoints( false ).ToList() ;
      if ( toEndPoints.Any() )
        toElementId = toEndPoints.First().Key.GetElementUniqueId() ;
      return ( fromElementId, toElementId ) ;
    }
  }
}