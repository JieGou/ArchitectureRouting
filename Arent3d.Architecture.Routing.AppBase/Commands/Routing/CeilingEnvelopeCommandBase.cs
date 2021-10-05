using System ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class CeilingEnvelopeCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var (originX, originY, _) = uiDocument.Selection.PickPoint( "Envelopeの配置場所を選択して下さい。" ) ;

        var result = document.Transaction(
          "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Pipe Spaces" ), _ =>
          {
            NewEnvelopeCommandBase.GenerateEnvelope( document, originX, originY, uiDocument.ActiveView.GenLevel, true ) ;

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
  }
}