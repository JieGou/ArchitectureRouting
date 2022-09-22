using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseLimitRackCommandBase : IExternalCommand
  {
    public const string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;
    private const string EraseLimitRackTransactionName = "Erase Limit Rack" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      
      var limitRackStorable = document.GetLimitRackStorable() ;
      var limitRacks = GetLimitRackIds( uiDocument, document,limitRackStorable ) ;
      var boundaryCableTrayIds = GetBoundaryCableTrays( document, limitRacks.limitRackModels ).EnumerateAll() ;
      
      using var transaction = new Transaction( document, EraseLimitRackTransactionName ) ;
      try {
        transaction.Start() ;
        RemoveLimitRacks( document, limitRacks.limitRackIds ) ;
        RemoveBoundaryCableTray( document, boundaryCableTrayIds ) ;
        RemoveLimitRackModelInStorable( limitRackStorable, limitRacks.limitRackModels ) ;
        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        transaction.RollBack() ;
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract (IReadOnlyCollection<string> limitRackIds,IReadOnlyCollection<LimitRackModel> limitRackModels) GetLimitRackIds( UIDocument ui, Document doc, LimitRackStorable limitRackStorable ) ;

    protected abstract IEnumerable<string> GetBoundaryCableTrays( Document doc,IReadOnlyCollection<LimitRackModel> limitRackModels ) ;

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

    private static void RemoveBoundaryCableTray( Document document, IEnumerable<string>? limitRackDetailIds )
    {
      if ( limitRackDetailIds is null ) {
        var curveFilterIds = new FilteredElementCollector( document ).OfClass( typeof( CurveElement ) )
          .OfType<CurveElement>()
          .Where( x => null != x.LineStyle && ( x.LineStyle as GraphicsStyle )!.GraphicsStyleCategory.Name ==
            BoundaryCableTrayLineStyleName ).Select( x => x.Id ).ToList() ;
        if ( curveFilterIds.Any() )
          document.Delete( curveFilterIds ) ;
        return ;
      }

      document.Delete( limitRackDetailIds.Select( document.GetElement ).Select( x => x.Id ).ToList() ) ;
    }

    private static void RemoveLimitRackModelInStorable( LimitRackStorable limitRackStorable, IReadOnlyCollection<LimitRackModel> limitRackModels )
    {
      if ( ! limitRackStorable.LimitRackModels.Any() ) return ;
      foreach ( var limitRackModel in limitRackModels ) {
        limitRackStorable.LimitRackModels.Remove( limitRackModel ) ;
      }

      limitRackStorable.Save() ;
    }

    protected static IEnumerable<FamilyInstance> GetAllLimitRackInstances(Document doc)
    {
      var cableTrays = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTray ) ;
      var cableTrayFittings = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTrayFitting ) ;

      foreach ( var cableTray in cableTrays ) {
        var comment = cableTray.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ) ).AsString() ;
        if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
          yield return cableTray ;
      }

      foreach ( var cableTrayFitting in cableTrayFittings ) {
        var comment = cableTrayFitting.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ) ).AsString() ;
        if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
          yield return cableTrayFitting ;
      }
    }
    
  }
}