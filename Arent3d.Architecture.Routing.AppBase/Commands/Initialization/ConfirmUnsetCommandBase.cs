using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ConfirmUnsetCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Unset" ), _ =>
        {
          var conduitIdNotConstruction = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.TryGetProperty( RoutingFamilyLinkedParameter.ConstructionItem, out string? constructionItem ) == true && string.IsNullOrEmpty( constructionItem ) ).Select( c => c.Id ).ToList() ;
          var connectorIdNotConstruction = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).Where( c => c.TryGetProperty( RoutingFamilyLinkedParameter.ConstructionItem, out string? constructionItem ) == true && string.IsNullOrEmpty( constructionItem ) ).Select( c => c.Id ).ToList() ;
          ChangeConduitAndConnectorColor( document, conduitIdNotConstruction, connectorIdNotConstruction ) ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private void ChangeConduitAndConnectorColor( Document document, List<ElementId> conduitIds, List<ElementId> connectorIds )
    {
      OverrideGraphicSettings ogs = new OverrideGraphicSettings() ;
      ogs.SetProjectionLineColor( new Color( 255, 0, 0 ) ) ;
      foreach ( var conduitId in conduitIds ) {
        document.ActiveView.SetElementOverrides( conduitId, ogs ) ;
      }

      foreach ( var connectorId in connectorIds ) {
        document.ActiveView.SetElementOverrides( connectorId, ogs ) ;
      }
    }
  }
}