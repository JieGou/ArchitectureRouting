using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Utils ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseRackCommandBase : IExternalCommand
  {
    public const string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;
    private const string EraseRackTransactionName = "ラック削除" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      if ( uiDocument.ActiveView is not ViewPlan viewPlan ) {
        TaskDialog.Show( "Arent Inc", "Only support in the view plan!" ) ;
        return Result.Cancelled ;
      }

      var racks = FilterRacks( uiDocument ).EnumerateAll() ;
      if ( ! racks.Any() )
        return Result.Cancelled ;

      using var transaction = new Transaction( uiDocument.Document, EraseRackTransactionName ) ;
      try {
        transaction.Start() ;
        RemoveRacksInStorage( viewPlan.GenLevel, racks ) ;
        CableRackUtils.ChangeEndLinesVisibilityOfConnectedRacks( racks ) ;
        RemoveRacksAndNotations( uiDocument.Document, racks.Select( x => x.UniqueId ) ) ;
        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        transaction.RollBack() ;
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract IEnumerable<Element> GetRacks( UIDocument uiDocument ) ;

    private IEnumerable<Element> FilterRacks(UIDocument uiDocument)
    {
      var racks = GetRacks( uiDocument ) ;
      foreach ( var rack in racks ) {
        var comment = rack.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( uiDocument.Document, "Rack Type" ) ).AsString() ;
        if ( comment == RackCommandBase.RackTypes[ 1 ] || comment == RackCommandBase.RackTypes[ 0 ])
          yield return rack ;
      }
    }

    private static void RemoveRacksInStorage( Level level, IEnumerable<Element> racks )
    {
      var elements = racks as Element[] ?? racks.ToArray() ;
      if ( ! elements.Any() )
        return ;

      var document = level.Document ;

      // change storage of auto rack
      var deletedIds = elements.Select( e => e.Id ) ;
      var routeNames = elements.Select( x => RouteUtil.GetMainRouteName( x.GetRouteName() ) ).Distinct() ;
      var storage = new StorageService<Level, RackForRouteModel>( level ) ;
      var modifiedRecords = storage.Data.RackForRoutes.Where( x => routeNames.Any( y => y == x.RouteName ) ).ToList() ;
      storage.Data.RackForRoutes.RemoveAll( x => routeNames.Any( y => y == x.RouteName ) ) ;
      foreach ( var record in modifiedRecords ) {
        // remove deleted ids in this record
        record.RackIds.RemoveAll( id => deletedIds.Any( deletedId => deletedId == id ) ) ;
        
        // add modified record back to storage
        if ( record.RackIds.Any() )
          storage.Data.RackForRoutes.Add( record ) ;
      }
      storage.SaveChange() ;

      // change storage of manual rack
      var storageRackFromTo = new StorageService<Level, RackFromToModel>( level ) ;
      var rackFromToList = storageRackFromTo.Data.RackFromToItems.Where( x => x.UniqueIds.All( uniqueId => document.GetElement( uniqueId ) is { } ) ).ToList() ;
      storageRackFromTo.Data.RackFromToItems.Clear() ;

      var deletedUniqueIds = elements.Select( e => e.UniqueId ) ;
      foreach ( var rackFromTo in rackFromToList ) {
        // remove deleted uniqueId
        rackFromTo.UniqueIds.RemoveAll( uniqueId => deletedUniqueIds.Any( deletedUniqueId => deletedUniqueId == uniqueId ) ) ;

        // add modified array of unique Id back to storage
        if ( rackFromTo.UniqueIds.Any() )
          storageRackFromTo.Data.RackFromToItems.Add( new RackFromToItem() { UniqueIds = rackFromTo.UniqueIds } ) ;
      }
      storageRackFromTo.SaveChange() ;
    }

    private static void RemoveRacksAndNotations( Document document, IEnumerable<string> allLimitRacks )
    {
      var rackUniqueIds = allLimitRacks as string[] ?? allLimitRacks.ToArray() ;
      if ( ! rackUniqueIds.Any() ) return ;
      RemoveRackNotation( document, rackUniqueIds ) ;
      var allLimitRackElements = rackUniqueIds.Select( document.GetElement ).Where( x => x != null ) ;
      document.Delete( allLimitRackElements.Select( x => x.Id ).ToList() ) ;
    }

    public static void RemoveRackNotationsByRouteNames( Document document, IEnumerable<string>? routeNames )
    {
      var racksAndElbows = document.GetAllElementsOfRoute<FamilyInstance>().Where( e => e.GetBuiltInCategory() == BuiltInCategory.OST_CableTrayFitting && e.GetRouteName() is { } routeName && routeNames.Contains( routeName ) ) ;
      var racksAndElbowsUniqueIds = racksAndElbows.Select( fi => fi.UniqueId ) ;
      RemoveRackNotation( document, racksAndElbowsUniqueIds ) ;
    }

    public static void RemoveRackNotation( Document document, IEnumerable<string> rackUniqueIds )
    {
      var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ??
                                 document.GetRackNotationStorable() ;
      if ( ! rackNotationStorable.RackNotationModelData.Any() ) return ;
      var rackNotationModels = new List<RackNotationModel>() ;
      foreach ( var rackNotationModel in rackNotationStorable.RackNotationModelData
                 .Where( d => rackUniqueIds.Contains( d.RackId ) ).ToList() ) {
        // delete notation
        var notationId = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_CableTrayFittingTags )
          .Where( e => e.UniqueId == rackNotationModel.NotationId ).Select( t => t.Id ).FirstOrDefault() ;
        if ( notationId != null )
          document.Delete( notationId ) ;

        RemoveDetailLines( document, rackNotationModel ) ;

        rackNotationModels.Add( rackNotationModel ) ;
      }

      if ( ! rackNotationModels.Any() ) return ;
      foreach ( var detailSymbolModel in rackNotationModels ) {
        rackNotationStorable.RackNotationModelData.Remove( detailSymbolModel ) ;
      }

      rackNotationStorable.Save() ;
    }

    private static void RemoveDetailLines( Document document, RackNotationModel rackNotationModel )
    {
      var detailLineUniqueIds = new List<string>() ;

      if ( ! string.IsNullOrEmpty( rackNotationModel.EndLineLeaderId ) )
        detailLineUniqueIds.Add( rackNotationModel.EndLineLeaderId ) ;

      if ( rackNotationModel.OtherLineIds.Count > 0 )
        detailLineUniqueIds.AddRange( rackNotationModel.OtherLineIds ) ;

      if ( detailLineUniqueIds.Count == 0 )
        return ;

      var elementIds = new List<ElementId>() ;
      foreach ( var detailLineUniqueId in detailLineUniqueIds.Distinct() ) {
        if ( document.GetElement( detailLineUniqueId ) is { } element ) {
          elementIds.Add( element.Id ) ;
        }
      }

      if ( elementIds.Count > 0 )
        document.Delete( elementIds ) ;
    }

  }
}