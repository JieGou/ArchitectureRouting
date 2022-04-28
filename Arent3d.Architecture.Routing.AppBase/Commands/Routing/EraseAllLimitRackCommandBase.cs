using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class EraseAllLimitRackCommandBase : IExternalCommand
  {
    public static string BoundaryCableTrayLineStyleName = "BoundaryCableTray" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      try {
        return document.Transaction( "TransactionName.Commands.Routing.EraseAllLimitRack".GetAppStringByKeyOrDefault( null ), _ =>
        {
          var cableTrays = document.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTray ) ;
          var cableTrayFittings = document.GetAllFamilyInstances( ElectricalRoutingFamilyType.CableTrayFitting ) ;
          var allLimitRack = new List<string>() ;
          foreach ( var cableTray in cableTrays ) {
            var comment = cableTray.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ) ).AsString() ;
            if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
              allLimitRack.Add( cableTray.UniqueId ) ;
          }

          foreach ( var cableTrayFitting in cableTrayFittings ) {
            var comment = cableTrayFitting.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ) ).AsString() ;
            if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
              allLimitRack.Add( cableTrayFitting.UniqueId ) ;
          }

          if ( allLimitRack.Any() ) {
            RemoveRackNotation( document, allLimitRack ) ;
            document.Delete( allLimitRack.Select(x => document.GetElement(x)).Select(x => x.Id).ToList() ) ;
          }
          RemoveBoundaryCableTray( document ) ;
          
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void RemoveBoundaryCableTray(Document document)
    {
      var curveFilterIds = new FilteredElementCollector( document )
        .OfClass( typeof( CurveElement ) )
        .OfType<CurveElement>()
        .Where( x => null != x.LineStyle && ( x.LineStyle as GraphicsStyle )!.GraphicsStyleCategory.Name == BoundaryCableTrayLineStyleName )
        .Select( x => x.Id )
        .ToList() ;
      if ( curveFilterIds.Any() )
        document.Delete( curveFilterIds ) ;
    }
    
    private static void RemoveRackNotation( Document document, IEnumerable<string> rackUniqueIds )
    {
      var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? document.GetRackNotationStorable() ;
      if ( ! rackNotationStorable.RackNotationModelData.Any() ) return ;
      var rackNotationModels = new List<RackNotationModel>() ;
      foreach ( var rackNotationModel in rackNotationStorable.RackNotationModelData.Where( d => rackUniqueIds.Contains( d.RackId ) ).ToList() ) {
        // delete notation
        var notationId = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.UniqueId == rackNotationModel.NotationId ).Select( t => t.Id ).FirstOrDefault() ;
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
    
    private static void RemoveDetailLines(Document document, RackNotationModel rackNotationModel)
    {
      var detailLineUniqueIds = new List<string>() ;
      
      if(!string.IsNullOrEmpty(rackNotationModel.EndLineLeaderId))
        detailLineUniqueIds.Add(rackNotationModel.EndLineLeaderId);
        
      if(rackNotationModel.OrtherLineId.Count > 0)
        detailLineUniqueIds.AddRange(rackNotationModel.OrtherLineId);

      if ( detailLineUniqueIds.Count == 0 ) 
        return;
      
      var eleIds = new List<ElementId>() ;
      foreach ( var detailLineUniqueId in detailLineUniqueIds.Distinct() ) {
        if ( document.GetElement( detailLineUniqueId ) is { } element ) {
          eleIds.Add(element.Id);
        }
      }

      if ( eleIds.Count > 0 )
        document.Delete( eleIds ) ;
    }
  }
}