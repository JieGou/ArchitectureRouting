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
    private const string DefaultConstructionItems = "未設定" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      Document document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Unset" ), _ =>
        {
          var elementNotConstruction = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.ConstructionItems ).Where( c => c.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) && ( string.IsNullOrEmpty( constructionItem ) || constructionItem == DefaultConstructionItems )).ToList() ;
          var color = new Color( 255, 0, 0 ) ;
          ChangeElementColor( document, elementNotConstruction, color ) ;

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