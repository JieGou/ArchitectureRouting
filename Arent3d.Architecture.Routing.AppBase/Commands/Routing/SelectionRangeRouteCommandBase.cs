using System ;
using System.Collections.Generic ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class SelectionRangeRouteCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var selectedElements = uiDocument.Selection.PickElementsByRectangle( "ドラックで複数コネクタを選択して下さい。" ) ;

        var result = document.Transaction(
          "TransactionName.Commands.Routing.SelectionRangeRoute".GetAppStringByKeyOrDefault( "Selection Range Route" ), _ =>
          {
            SelectionRangeRoute( document, selectedElements, uiDocument.ActiveView.GenLevel ) ;

            return Result.Succeeded ;
          } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void SelectionRangeRoute( Document document, IList<Element> selectedElements, Level level, bool isCeiling = false )
    {
      
    }
    
  }
}