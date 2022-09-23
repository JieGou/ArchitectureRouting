using System ;
using System.Collections.Generic ;
using System.Linq ;
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
    private const string EraseLimitRackTransactionName = "Erase Limit Rack" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      if ( uiDocument.ActiveView is not ViewPlan viewPlan ) {
        TaskDialog.Show( "Arent Inc", "Only support in the view plan!" ) ;
        return Result.Cancelled ;
      }
      
      var racks = FilterRacks( uiDocument).EnumerateAll() ;
      
      using var transaction = new Transaction( uiDocument.Document, EraseLimitRackTransactionName ) ;
      try {
        transaction.Start() ;
        RemoveRacksInStorage( viewPlan.GenLevel, racks ) ;
        RemoveLimitRacks( uiDocument.Document, racks.Select(x => x.UniqueId) ) ;
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

    private static void RemoveRacksInStorage(Level level, IEnumerable<Element> racks )
    {
      var elements = racks as Element[] ?? racks.ToArray() ;
      if(!elements.Any())
        return;

      var routeNames = elements.Select( x => RouteUtil.GetMainRouteName( x.GetRouteName() ) ).Distinct() ;
      var storage = new StorageService<Level, RackForRouteModel>( level ) ;
      storage.Data.RackForRoutes.RemoveAll( x => routeNames.Any( y => y == x.RouteName ) ) ;
      storage.SaveChange();
    }

    private static void RemoveLimitRacks( Document document, IEnumerable<string> allLimitRacks )
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

    private static void RemoveRackNotation( Document document, IEnumerable<string> rackUniqueIds )
    {
      var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ??
                                 document.GetRackNotationStorable() ;
      if ( ! rackNotationStorable.RackNotationModelData.Any() ) return ;
      var rackNotationModels = new List<RackNotationModel>() ;
      foreach ( var rackNotationModel in rackNotationStorable.RackNotationModelData
                 .Where( d => rackUniqueIds.Contains( d.RackId ) ).ToList() ) {
        // delete notation
        var notationId = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes )
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