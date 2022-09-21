using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseLimitRackCommandBase : IExternalCommand
  {
    private const string EraseLimitRackTransactionName = "Erase Limit Rack" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var limitRacks = GetLimitRackUniqueIds( uiDocument, document ) ;
      
      using var transaction = new Transaction( document, EraseLimitRackTransactionName ) ;
      try {
        transaction.Start() ;
        RemoveLimitRacks( document, limitRacks ) ;
        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        transaction.RollBack() ;
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract IEnumerable<string> GetLimitRackUniqueIds( UIDocument uiDocument, Document document ) ;

    private static void RemoveLimitRacks( Document document, IEnumerable<string> allLimitRacks )
    {
      var rackUniqueIds = allLimitRacks as string[] ?? allLimitRacks.ToArray() ;
      if ( ! rackUniqueIds.Any() ) return ;
      RemoveRackNotation( document, rackUniqueIds ) ;
      var allLimitRackElements = rackUniqueIds.Select( document.GetElement ).Where( x => x != null ) ;
      document.Delete( allLimitRackElements.Select( x => x.Id ).ToList() ) ;
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

    protected static IEnumerable<FamilyInstance> GetAllLimitRackInstances(Document doc)
    {
      var cableTrays = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTray ) ;
      var cableTrayFittings = doc.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTrayFitting ) ;

      foreach ( var cableTray in cableTrays ) {
        var comment = cableTray.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ) ).AsString() ;
        if ( comment == RackCommandBase.RackTypes[ 1 ] )
          yield return cableTray ;
      }

      foreach ( var cableTrayFitting in cableTrayFittings ) {
        var comment = cableTrayFitting.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( doc, "Rack Type" ) ).AsString() ;
        if ( comment == RackCommandBase.RackTypes[ 1 ] )
          yield return cableTrayFitting ;
      }
    }
    
  }
}