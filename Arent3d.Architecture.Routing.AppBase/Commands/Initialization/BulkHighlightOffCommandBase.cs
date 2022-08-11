using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class BulkHighlightOffCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.BulkHighlightOff".GetAppStringByKeyOrDefault( "Bulk Highlight Off" ), _ =>
        {
          //Highlight off below features
          List<Element> highlightOffElements = new() ;
          
          //Confirm Unset Command
          var elementNotConstruction = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.ConstructionItems ).Where( c => c.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) && string.IsNullOrEmpty( constructionItem ) ).ToList() ;
          highlightOffElements.AddRange( elementNotConstruction );
          
          // Confirm not connecting Command
          var elementNotConnect = ConfirmNotConnectingCommandBase.GetElementsNotConnect( document ) ;
          highlightOffElements.AddRange( elementNotConnect );

          // Highlight off automatic calculation size of pull box
          var textNotesHighlightOff =
            PullBoxRouteManager.GetTextNotesOfPullBox( document, true ) ;
          highlightOffElements.AddRange( textNotesHighlightOff );
          
          ResetElementColor( document, highlightOffElements ) ;
          
          // Show Fall Mark Command
          ShowFallMarkCommandBase.RemoveDisplayingFallMark( document ) ;
          
          // Show Open EndPoint Mark Command
          ShowOpenEndPointMarkCommandBase.RemoveDisplayingOpenEndPointMark( document ) ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    public static void ChangeElementColor( Document document, IEnumerable<Element> elements, Color color )
    {
      OverrideGraphicSettings ogs = new() ;
      ogs.SetProjectionLineColor( color ) ;
      foreach ( var element in elements ) {
        document.ActiveView.SetElementOverrides( element.Id, ogs ) ;
      }
    }
    
    public static void ResetElementColor( Document document, IEnumerable<Element> elements )
    {
      OverrideGraphicSettings ogs = new() ;
      foreach ( var element in elements ) {
        document.ActiveView.SetElementOverrides( element.Id, ogs ) ;
      }
    }
  }
}