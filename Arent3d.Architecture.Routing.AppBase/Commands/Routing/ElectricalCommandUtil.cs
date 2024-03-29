﻿using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public static class ElectricalCommandUtil
  {
    private const string DefaultConstructionItem = "未設定" ;

    public static void SetPropertyForCable( Document document, IReadOnlyCollection<Route> routes )
    {
      using Transaction t = new Transaction( document, "Set Construction item." ) ;
      t.Start() ;
      var defaultIsEcoModeValue = document.GetDefaultSettingStorable().EcoSettingData.IsEcoMode.ToString() ;
      foreach ( var route in routes ) {
        if ( ! route.SubRoutes.Any() ) continue ;
        
        var subRoute = route.SubRoutes.Last() ;
        var segment = subRoute.Segments.FirstOrDefault() ;
        if ( segment == null ) continue ;

        var fromConstructionItem = DefaultConstructionItem ;
        var fromIsEcoMode = defaultIsEcoModeValue ;
        var fromEndPointKey = segment.FromEndPoint.Key ;
        var fromEndPointId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromEndPointId ) ) {
          var fromConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == fromEndPointId ) ;
          if ( fromConnector != null && ( fromConnector.IsTerminatePoint() || fromConnector.IsPassPoint() ) ) {
            fromConnector.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorId ) ;
            if ( ! string.IsNullOrEmpty( fromConnectorId ) ) {
              fromConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == fromConnectorId ) ;
            }
          }

          if ( fromConnector != null ) {
            fromConnector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
            fromConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
            if ( ! string.IsNullOrEmpty( constructionItem ) ) fromConstructionItem = constructionItem! ;
            if ( ! string.IsNullOrEmpty( isEcoMode ) ) fromIsEcoMode = isEcoMode! ;

            if ( fromConnector.HasParameter( ElectricalRoutingElementParameter.ConstructionItem ) && string.IsNullOrEmpty( constructionItem ) ) fromConnector.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
            if ( fromConnector.HasParameter( ElectricalRoutingElementParameter.IsEcoMode ) && string.IsNullOrEmpty( isEcoMode ) ) fromConnector.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
          }
        }

        var toEndPointKey = segment.ToEndPoint.Key ;
        var toEndPointId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toEndPointId ) ) continue ;
        var toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == toEndPointId ) ;
        if ( toConnector != null && ( toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) ) {
          toConnector.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? connectorId ) ;
          if ( ! string.IsNullOrEmpty( connectorId ) ) {
            toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == connectorId ) ;
          }
        }

        if ( toConnector == null ) continue ;
        {
          toConnector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
          toConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;

          if ( toConnector.HasParameter( ElectricalRoutingElementParameter.ConstructionItem ) && string.IsNullOrEmpty( constructionItem ) ) {
            toConnector.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
            constructionItem = DefaultConstructionItem ;
          }

          if ( toConnector.HasParameter( ElectricalRoutingElementParameter.IsEcoMode ) && string.IsNullOrEmpty( isEcoMode ) ) {
            toConnector.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
            isEcoMode = defaultIsEcoModeValue ;
          }

          var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
          foreach ( var conduit in conduits ) {
            conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, toConnector.Name == ElectricalRoutingFamilyType.ToJboxConnector.GetFamilyName() ? fromConstructionItem : constructionItem! ) ;
            conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, toConnector.Name == ElectricalRoutingFamilyType.ToJboxConnector.GetFamilyName() ? fromIsEcoMode : isEcoMode! ) ;
          }
        }
      }

      t.Commit() ;
    }

    public static void GroupConnector( Document document, Dictionary<ElementId, List<ElementId>> connectorGroups )
    {
      using Transaction t = new Transaction( document ) ;
      t.Start( "Group connector" ) ;
      foreach ( var (connectorId, textNoteIds) in connectorGroups ) {
        // create group for updated connector (with new property) and related text note if any
        var groupIds = new List<ElementId> { connectorId } ;
        groupIds.AddRange( textNoteIds ) ;
        document.Create.NewGroup( groupIds ) ;
      }

      t.Commit() ;
    }
    
    public static Element? GetConnectorOfRoute( Document document, IReadOnlyCollection<Element> allConnectors, string routeName, bool isFrom = false )
    {
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
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
    
    public static List<string> GetCeedSetCodeOfElement( Element element )
    {
      element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCode ) ;
      return ! string.IsNullOrEmpty( ceedSetCode ) ? ceedSetCode!.Split( ':' ).ToList() : new List<string>() ;
    }
    
    public static ( string, string ) GetCeedCodeAndDeviceSymbolOfElement( Element element )
    {
      element.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCodeModel ) ;
      if ( string.IsNullOrEmpty( ceedSetCodeModel ) ) return ( string.Empty, string.Empty ) ;
      var ceedSetCode = ceedSetCodeModel!.Split( ':' ).ToList() ;
      var ceedCode = ceedSetCode.FirstOrDefault() ;
      var deviceSymbol = ceedSetCode.Count > 1 ? ceedSetCode.ElementAt( 1 ) : string.Empty ;
      return ( ceedCode ?? string.Empty, deviceSymbol ?? string.Empty ) ;
    }

  }
}