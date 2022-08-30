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
          var elementsNotHavingConstructionItems = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.ConstructionItems ).Where( c => c.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) && ( string.IsNullOrEmpty( constructionItem ) || constructionItem == DefaultConstructionItems )).ToList() ;
          var color = new Color( 255, 0, 0 ) ;
          if ( elementsNotHavingConstructionItems.Any( t =>
              {
                var colorOfElement = t.Document.ActiveView.GetElementOverrides( t.Id ).ProjectionLineColor ;
                if ( ! colorOfElement.IsValid ) return false ;
                
                return colorOfElement.Red == color.Red && colorOfElement.Blue == color.Blue && colorOfElement.Green == color.Green ;
              } ) )
            ResetElementColor( elementsNotHavingConstructionItems ) ;
          else
            ChangeElementColor( elementsNotHavingConstructionItems, color ) ;

          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    public static void ChangeElementColor(IEnumerable<Element> elements, Color color )
    {
      OverrideGraphicSettings ogs = new() ;
      ogs.SetProjectionLineColor( color ) ;
      foreach ( var element in elements ) {
        element.Document.ActiveView.SetElementOverrides( element.Id, ogs ) ;
      }
    }
    
    public static void ResetElementColor(IEnumerable<Element> elements )
    {
      OverrideGraphicSettings ogs = new() ;
      foreach ( var element in elements ) {
        element.Document.ActiveView.SetElementOverrides( element.Id, ogs ) ;
      }
    }
  }
}