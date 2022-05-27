using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AddWiringInformationCommandBase: IExternalCommand
  {
    private const string NoPlumping = "配管なし" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        Document document = uiDocument.Document ;

        var csvStorable = document.GetCsvStorable() ;
        var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
        var cnsStorable = document.GetCnsSettingStorable() ;
        var detailSymbolStorable = document.GetDetailSymbolStorable() ;
        
        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to get info.", AddInType.Electrical ) ;
        var pickedObjectIds = new List<string>(){pickInfo.Element.UniqueId} ;
        var ( detailTableModels, isMixConstructionItems, isExistDetailTableModelRow ) = CreateDetailTableUtil.CreateDetailTable( document, csvStorable, detailSymbolStorable, new List<Element>() { pickInfo.Element }, pickedObjectIds, false ) ;
        
        var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
        var conduitTypes = new ObservableCollection<string>( conduitTypeNames ) { NoPlumping } ;

        var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
        var constructionItems = new ObservableCollection<string>( constructionItemNames ) ;

        var levelNames = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).Select( l => l.Name ).ToList() ;
        var levels = new ObservableCollection<string>( levelNames ) ;

        var wireTypeNames = wiresAndCablesModelData.Select( w => w.WireType ).Distinct().ToList() ;
        var wireTypes = new ObservableCollection<string>( wireTypeNames ) ;

        var earthTypes = new ObservableCollection<string>() {  "IV" , "EM-IE" } ;

        var numbers = new ObservableCollection<string>() {  "1" , "2" , "3", "4" , "5", "6" , "7", "8" , "9", "10" } ;
         
        var constructionClassificationTypeNames = hiroiSetCdMasterNormalModelData.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
        var constructionClassificationTypes = new ObservableCollection<string>(constructionClassificationTypeNames) ;

        var signalTypes = new ObservableCollection<string>((from signalType in (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType )) select signalType.GetFieldName()).ToList()) ; 
         
        
        var viewModel = new AddWiringInformationViewModel( document, detailTableModels.FirstOrDefault()!, conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes   ) ;
        var dialog = new AddWiringInformationDialog( viewModel ) ;
        dialog.ShowDialog() ;
        return Result.Succeeded ; 
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