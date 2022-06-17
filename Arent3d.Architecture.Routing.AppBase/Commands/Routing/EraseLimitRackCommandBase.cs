using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseLimitRackCommandBase : IExternalCommand
  {
    private const string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;
    private const string EraseLimitRackTransactionName = "Erase Limit Rack" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var limitRackStorable = document.GetAllStorables<LimitRackStorable>().FirstOrDefault() ??
                              document.GetLimitRackStorable() ;
      var allLimitRackIds = GetLimitRackIds( uiDocument, document, limitRackStorable ) ;

      try {
        using var transaction = new Transaction( document, EraseLimitRackTransactionName ) ;
        transaction.Start() ;
        RemoveLimitRacks( document, allLimitRackIds.rackIds ) ;
        RemoveBoundaryCableTray( document, allLimitRackIds.detailCurverIds ) ;
        RemoveLimitRackModels( limitRackStorable, allLimitRackIds.limitRackModels ) ;
        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract (IEnumerable<string> rackIds, IEnumerable<string> detailCurverIds, IEnumerable<LimitRackModel>
      limitRackModels) GetLimitRackIds( UIDocument ui, Document doc, LimitRackStorable limitRackStorable ) ;

    protected abstract void RemoveLimitRackModels( LimitRackStorable limitRackStorable,
      IEnumerable<LimitRackModel> selectedLimitRackModels ) ;

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

      if ( rackNotationModel.OrtherLineId.Count > 0 )
        detailLineUniqueIds.AddRange( rackNotationModel.OrtherLineId ) ;

      if ( detailLineUniqueIds.Count == 0 )
        return ;

      var eleIds = new List<ElementId>() ;
      foreach ( var detailLineUniqueId in detailLineUniqueIds.Distinct() ) {
        if ( document.GetElement( detailLineUniqueId ) is { } element ) {
          eleIds.Add( element.Id ) ;
        }
      }

      if ( eleIds.Count > 0 )
        document.Delete( eleIds ) ;
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
  }
}