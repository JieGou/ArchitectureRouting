using System ;
using System.Collections.Generic ;
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

          document.Delete( allLimitRack ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }
}