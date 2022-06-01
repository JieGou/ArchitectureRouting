using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ElectricalSymbolAggregationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        Document document = uiDocument.Document ;
        var level = uiDocument.ActiveView.GenLevel ;
        
        var ceedStoreable = document.GetCeedStorable() ;
        var listCeedModel = ceedStoreable.CeedModelData ;
        var selectedConnectors = uiDocument.Selection.PickElementsByRectangle( ConnectorFamilySelectionFilter.Instance, "ドラックで複数コネクタを選択して下さい。" ).OfType<FamilyInstance>() ;
        List<ElectricalSymbolAggregationModel> listElectricalSymbolAggregation = new() ;
        foreach ( var connector in selectedConnectors ) {
          if ( false == connector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) || string.IsNullOrEmpty( ceedCode ) )
            continue ;
          var detailCode = ceedCode!.Split( ':' ).ToList() ;
          var ceedSetCode = detailCode.First() ;
          var symbol = detailCode.Count > 1 ? detailCode.ElementAt( 1 ) : string.Empty ;
          var modelNumber = detailCode.Count > 2 ? detailCode.ElementAt( 2 ) : string.Empty ;

          var ceedModel = listCeedModel.FirstOrDefault( x => x.CeedSetCode == ceedSetCode && x.GeneralDisplayDeviceSymbol == symbol && x.ModelNumber == modelNumber ) ;

          if ( null == ceedModel ) continue ;

          connector.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? constructionItem ) ;
          var exitedModel = listElectricalSymbolAggregation.FirstOrDefault( x => x.ProductCode == ceedModel.CeedModelNumber ) ;
          if ( null != exitedModel ) {
            exitedModel.Number += 1 ;
          }
          else {
            var detail = ceedModel.GeneralDisplayDeviceSymbol ;
            if ( ! string.IsNullOrEmpty( ceedModel.Condition ) )
              detail += " (" + ceedModel.Condition + ")" ;
            
            listElectricalSymbolAggregation.Add( new ElectricalSymbolAggregationModel(level.Name, ceedSetCode, constructionItem ?? string.Empty, ceedModel.CeedModelNumber, detail, 1, "個" ) ) ;
          }
        }

        ElectricalSymbolAggregationViewModel viewModel = new(listElectricalSymbolAggregation) ;
        var dialog = new ElectricalSymbolAggregationDialog( viewModel ) ;

        dialog.ShowDialog() ;

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }
  }
}