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
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      try {
        return document.Transaction( "TransactionName.Commands.Routing.EraseAllLimitRack".GetAppStringByKeyOrDefault( null ), _ =>
        {
          var cableTrays = document.GetAllFamilyInstances( RoutingFamilyType.CableTray ) ;
          var cableTrayFittings = document.GetAllFamilyInstances( RoutingFamilyType.CableTrayFitting ) ;
          var allLimitRack = new List<ElementId>() ;
          foreach ( var cableTray in cableTrays ) {
            var comment = cableTray.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ) ).AsString() ;
            if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
              allLimitRack.Add( cableTray.Id ) ;
          }

          foreach ( var cableTrayFitting in cableTrayFittings ) {
            var comment = cableTrayFitting.ParametersMap.get_Item( "Revit.Property.Builtin.RackType".GetDocumentStringByKeyOrDefault( document, "Rack Type" ) ).AsString() ;
            if ( comment == NewRackCommandBase.RackTypes[ 1 ] )
              allLimitRack.Add( cableTrayFitting.Id ) ;
          }

          RemoveRackNotation( document, allLimitRack ) ;
          document.Delete( allLimitRack ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    private static void RemoveRackNotation( Document document, List<ElementId> elementIds )
    {
      var rackNotationStorable = document.GetAllStorables<RackNotationStorable>().FirstOrDefault() ?? document.GetRackNotationStorable() ;
      if ( ! rackNotationStorable.RackNotationModelData.Any() ) return ;
      var rackNotationModels = new List<RackNotationModel>() ;
      List<string> rackIds = elementIds.Select( i => i.IntegerValue.ToString() ).ToList() ;
      foreach ( var rackNotationModel in rackNotationStorable.RackNotationModelData.Where( d => rackIds.Contains( d.RackId ) ).ToList() ) {
        // delete notation
        var notationId = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_TextNotes ).Where( e => e.Id.IntegerValue.ToString() == rackNotationModel.NotationId ).Select( t => t.Id ).FirstOrDefault() ;
        if ( notationId != null ) document.Delete( notationId ) ;
        foreach ( var lineId in rackNotationModel.LineIds.Split( ',' ) ) {
          var id = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Lines ).Where( e => e.Id.IntegerValue.ToString() == lineId ).Select( e => e.Id ).FirstOrDefault() ;
          if ( id != null ) document.Delete( id ) ;
        }

        rackNotationModels.Add( rackNotationModel ) ;
      }

      if ( ! rackNotationModels.Any() ) return ;
      foreach ( var detailSymbolModel in rackNotationModels ) {
        rackNotationStorable.RackNotationModelData.Remove( detailSymbolModel ) ;
      }

      rackNotationStorable.Save() ;
    }
  }
}