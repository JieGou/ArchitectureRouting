﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  public class ConnectorUpdater : IUpdater
  {
    private readonly UpdaterId _updaterId ;

    public ConnectorUpdater( AddInId addInId )
    {
      _updaterId = new UpdaterId( addInId, Guid.NewGuid() ) ;
    }

    public void Execute( UpdaterData updaterData )
    {
      try {
        var document = updaterData.GetDocument() ;
        if ( document.ActiveView is not ViewPlan viewPlan )
          return ;

        var uiDocument = new UIDocument( document ) ;
        if ( uiDocument.Selection.GetElementIds().Count == 0 )
          return ;

        var elementIdSelecteds = uiDocument.Selection.GetElementIds() ;
        if ( updaterData.GetModifiedElementIds().Count == 0 )
          return ;

        var elements = updaterData.GetModifiedElementIds().Where( x => elementIdSelecteds.Any( y => y == x ) ).Select( x => document.GetElement( x ) ).Where( x => BuiltInCategorySets.OtherElectricalElements.Any( y => y == x.GetBuiltInCategory() ) )
          .ToList() ;
        if ( ! elements.Any() )
          return ;

        foreach ( var element in elements.Where( element => element.HasParameter( ElectricalRoutingElementParameter.Quantity ) ) ) {
          var tagFamily = element.Category.GetBuiltInCategory() == BuiltInCategory.OST_ElectricalFixtures ? ElectricalRoutingFamilyType.ElectricalFixtureContentTag : ElectricalRoutingFamilyType.ElectricalEquipmentContentTag ;

          var familyTagName = tagFamily.GetFamilyName() ;
          var tag = element.GetIndependentTagsFromElement().FirstOrDefault( x =>
          {
            if ( document.GetElement( x.GetTypeId() ) is not FamilySymbol type )
              return false ;

            return type.FamilyName == familyTagName ;
          } ) ;
          if ( null == tag )
            continue ;

          var tagTypes = document.GetFamilySymbols( tagFamily ).EnumerateAll() ;
          var showQuantityTagType = tagTypes.FirstOrDefault( x => x.LookupParameter( "Is Show Quantity" ).AsInteger() == 1 ) ;
          if ( null == showQuantityTagType )
            continue ;

          var hideQuantityTagType = tagTypes.FirstOrDefault( x => x.LookupParameter( "Is Hide Quantity" ).AsInteger() == 1 ) ;
          if ( null == hideQuantityTagType )
            return ;

          if ( element.TryGetProperty( ElectricalRoutingElementParameter.Quantity, out int quantity ) && quantity >= 0 ) {
            switch ( quantity ) {
              case 1 :
                tag.ChangeTypeId( hideQuantityTagType.Id ) ;
                break ;
              default :
                tag.ChangeTypeId( showQuantityTagType.Id ) ;
                break ;
            }
          }
          else {
            element.SetProperty( ElectricalRoutingElementParameter.Quantity, 1 ) ;
            tag.ChangeTypeId( hideQuantityTagType.Id ) ;
          }

          ResetDetailTableForRoute( document, element, viewPlan.GenLevel ) ;
        }

        uiDocument.Selection.SetElementIds( new List<ElementId>() ) ;
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
      }
    }

    private static void ResetDetailTableForRoute( Document document, Element connector, Level level )
    {
      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var routeName = routes.Keys.Select( RouteUtil.GetMainRouteName ).Distinct().FirstOrDefault( x => ConduitUtil.GetConnectorOfRoute( document, x, false )?.UniqueId == connector.UniqueId ) ;
      if ( string.IsNullOrEmpty( routeName ) )
        return ;

      var fromConnector = ConduitUtil.GetConnectorOfRoute( document, routeName, true ) ;
      if ( null == fromConnector )
        return ;

      var toConnector = ConduitUtil.GetConnectorOfRoute( document, routeName, false ) ;
      if ( null == toConnector )
        return ;

      var storage = new StorageService<Level, DetailTableModel>( level ) ;
      storage.Data.DetailTableData.RemoveAll( x =>
      {
        if ( RouteUtil.GetMainRouteName( x.RouteName ) != routeName )
          return false ;

        if ( x.FromConnectorUniqueId != fromConnector.UniqueId )
          return false ;

        return x.ToConnectorUniqueId == toConnector.UniqueId ;
      } ) ;
      storage.SaveChange() ;
    }

    public UpdaterId GetUpdaterId()
    {
      return _updaterId ;
    }

    public ChangePriority GetChangePriority()
    {
      return ChangePriority.MEPFixtures ;
    }

    public string GetUpdaterName()
    {
      return "Connector Arent Updater" ;
    }

    public string GetAdditionalInformation()
    {
      return "Arent, " + "https://arent3d.com" ;
    }
  }
}