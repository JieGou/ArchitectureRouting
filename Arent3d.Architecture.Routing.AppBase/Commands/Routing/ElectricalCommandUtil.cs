using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class ElectricalCommandUtil
  {
    public static void SetConstructionItemForCable( Document document, IReadOnlyCollection<Route> routes )
    {
      using Transaction t = new Transaction( document, "Set Construction item." ) ;
      t.Start() ;
      foreach ( var route in routes ) {
        var subRoute = route.SubRoutes.Last() ;
        var segment = subRoute.Segments.FirstOrDefault() ;
        if ( segment == null ) continue ;
        var toEndPointKey = segment.ToEndPoint.Key ;
        if ( toEndPointKey == null ) continue ;
        var toEndPointId = toEndPointKey.GetElementId() ;
        if ( string.IsNullOrEmpty( toEndPointId ) ) continue ;
        var toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.Id.IntegerValue.ToString() == toEndPointId ) ;
        if ( toConnector != null && ( toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) ) {
          toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? connectorId ) ;
          if ( ! string.IsNullOrEmpty( connectorId ) ) {
            toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.Id.IntegerValue.ToString() == connectorId ) ;
          }
        }

        if ( toConnector == null ) continue ;
        {
          toConnector.TryGetProperty( RoutingFamilyLinkedParameter.ConstructionItem, out string? constructionItem ) ;
          if ( string.IsNullOrEmpty( constructionItem ) ) continue ;
          var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
          foreach ( var conduit in conduits ) {
            conduit.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, constructionItem! ) ;
          }
        }
      }

      t.Commit() ;
    }
  }
}