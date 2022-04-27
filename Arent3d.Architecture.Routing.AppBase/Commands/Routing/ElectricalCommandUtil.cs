using System.Collections.Generic ;
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
      var connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      using Transaction t = new Transaction( document, "Set Construction item." ) ;
      t.Start() ;
      var defaultIsEcoModeValue = document.GetEcoSettingStorable().EcoSettingData.IsEcoMode.ToString() ;
      foreach ( var route in routes ) {
        var subRoute = route.SubRoutes.Last() ;
        var segment = subRoute.Segments.FirstOrDefault() ;
        if ( segment == null ) continue ;

        var fromEndPointKey = segment.FromEndPoint.Key ;
        var fromEndPointId = fromEndPointKey.GetElementUniqueId() ;
        if ( ! string.IsNullOrEmpty( fromEndPointId ) ) {
          var fromConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == fromEndPointId ) ;
          if ( fromConnector != null && ( fromConnector.IsTerminatePoint() || fromConnector.IsPassPoint() ) ) {
            fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorId ) ;
            if ( ! string.IsNullOrEmpty( fromConnectorId ) ) {
              fromConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == fromConnectorId ) ;
            }
          }

          if ( fromConnector != null ) {
            fromConnector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
            fromConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
            if ( string.IsNullOrEmpty( constructionItem ) || string.IsNullOrEmpty( isEcoMode ) ) {
              UnGroupConnector( document, fromConnector, ref connectorGroups ) ;
            }

            if ( string.IsNullOrEmpty( constructionItem ) ) fromConnector.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
            if ( string.IsNullOrEmpty( isEcoMode ) ) fromConnector.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
          }
        }

        var toEndPointKey = segment.ToEndPoint.Key ;
        var toEndPointId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toEndPointId ) ) continue ;
        var toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == toEndPointId ) ;
        if ( toConnector != null && ( toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) ) {
          toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? connectorId ) ;
          if ( ! string.IsNullOrEmpty( connectorId ) ) {
            toConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).FirstOrDefault( c => c.UniqueId == connectorId ) ;
          }
        }

        if ( toConnector == null ) continue ;
        {
          toConnector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
          toConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;

          if ( string.IsNullOrEmpty( constructionItem ) || string.IsNullOrEmpty( isEcoMode ) ) {
            UnGroupConnector( document, toConnector, ref connectorGroups ) ;
          }

          if ( string.IsNullOrEmpty( constructionItem ) ) {
            toConnector.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
            constructionItem = DefaultConstructionItem ;
          }

          if ( string.IsNullOrEmpty( isEcoMode ) ) {
            toConnector.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
            isEcoMode = defaultIsEcoModeValue ;
          }

          var conduits = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == route.RouteName ).ToList() ;
          foreach ( var conduit in conduits ) {
            conduit.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem! ) ;
            conduit.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, defaultIsEcoModeValue ) ;
          }
        }
      }

      t.Commit() ;
      GroupConnector( document, connectorGroups ) ;
    }

    private static void UnGroupConnector( Document document, Element connector, ref Dictionary<ElementId, List<ElementId>> connectorGroups )
    {
      var parentGroup = document.GetElement( connector.GroupId ) as Group ;
      if ( parentGroup == null ) return ;
      // ungroup before set property
      var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
      List<ElementId> listTextNoteIds = new List<ElementId>() ;
      // ungroup textNote before ungroup connector
      foreach ( var group in attachedGroup ) {
        var ids = @group.GetMemberIds() ;
        listTextNoteIds.AddRange( ids ) ;
        @group.UngroupMembers() ;
      }

      if ( ! connectorGroups.ContainsKey( connector.Id ) ) connectorGroups.Add( connector.Id, listTextNoteIds ) ;
      parentGroup.UngroupMembers() ;
    }

    private static void GroupConnector( Document document, Dictionary<ElementId, List<ElementId>> connectorGroups )
    {
      using Transaction t = new Transaction( document ) ;
      t.Start( "Group connector" ) ;
      foreach ( var (connectorId, textNoteIds) in connectorGroups ) {
        // create group for updated connector (with new property) and related text note if any
        List<ElementId> groupIds = new List<ElementId> { connectorId } ;
        groupIds.AddRange( textNoteIds ) ;
        document.Create.NewGroup( groupIds ) ;
      }

      t.Commit() ;
    }
  }
}