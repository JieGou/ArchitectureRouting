using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AddWiringInformationCommandBase : IExternalCommand
  {
    private const string NoPlumping = "配管なし" ;
    private const string DefaultConstructionItems = "未設定" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        Document document = uiDocument.Document ;

        var csvStorable = document.GetCsvStorable() ;
        var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
        var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
        var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
        var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
        var cnsStorable = document.GetCnsSettingStorable() ;
        var detailSymbolStorable = document.GetDetailSymbolStorable() ;

        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to get info.", AddInType.Electrical ) ;
        //Get all route name related to selected conduit
        var wiringList = GetAllConduitRelated( document, pickInfo.Element, hiroiSetMasterEcoModelData, hiroiSetMasterNormalModelData, hiroiMasterModelData, wiresAndCablesModelData) ;
        SelectWiringViewModel wiringViewModel = new SelectWiringViewModel( wiringList ) ;
        SelectWiringDialog selectWiringDialog = new SelectWiringDialog( wiringViewModel ) ;
        if ( selectWiringDialog.ShowDialog() == false ) return Result.Cancelled ;


        var pickedObjectIds = new List<string>() { pickInfo.Element.UniqueId } ;
        var (detailTableModels, isMixConstructionItems, isExistDetailTableModelRow) = CreateDetailTableCommandBase.CreateDetailTable( document, csvStorable, detailSymbolStorable, new List<Element>() { pickInfo.Element }, pickedObjectIds, false ) ;
        var detailTableModel = detailTableModels.FirstOrDefault( x => x.RouteName == pickInfo.Route.RouteName ) ;
        if ( null == detailTableModel ) {
          MessageBox.Show( "Item info can't be found!", "Info" ) ;
          return Result.Cancelled ;
        }
        //detailTableModels = new ObservableCollection<DetailTableModel>() { detailTableModel } ;

        var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
        var conduitTypes = new ObservableCollection<string>( conduitTypeNames ) { NoPlumping } ;

        var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
        if ( ! constructionItemNames.Any() )
          constructionItemNames.Add( DefaultConstructionItems ) ;
        var constructionItems = new ObservableCollection<string>( constructionItemNames ) ;

        var levelNames = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).Select( l => l.Name ).ToList() ;
        var levels = new ObservableCollection<string>( levelNames ) ;

        var wireTypeNames = wiresAndCablesModelData.Select( w => w.WireType ).Distinct().ToList() ;
        var wireTypes = new ObservableCollection<string>( wireTypeNames ) ;

        var earthTypes = new ObservableCollection<string>() { "IV", "EM-IE" } ;

        var numbers = new ObservableCollection<string>()
        {
          "1",
          "2",
          "3",
          "4",
          "5",
          "6",
          "7",
          "8",
          "9",
          "10"
        } ;

        var constructionClassificationTypeNames = hiroiSetCdMasterNormalModelData.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
        var constructionClassificationTypes = new ObservableCollection<string>( constructionClassificationTypeNames ) ;

        var signalTypes = new ObservableCollection<string>( ( from signalType in (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType ) ) select signalType.GetFieldName() ).ToList() ) ;


        var viewModel = new AddWiringInformationViewModel( document, detailTableModel!, conduitsModelData, conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes, isMixConstructionItems ) ;
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

    private List<SelectWiringModel> GetAllConduitRelated( Document doc, Element pickConduit, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
    {
      const string defaultPlumbingType = "配管なし" ;
      var ceedStorable = doc.GetCeedStorable() ;
      var representativeRouteName = ( (Conduit) pickConduit ).GetRepresentativeRouteName() ;
      var conduits = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).ToList() ;
      List<SelectWiringModel> selectWiringModels = new() ;
      foreach ( var conduit in conduits ) {
        var routeName = conduit.GetRouteName() ;
        if ( string.IsNullOrEmpty( routeName ) ) continue ;

        var toConnector = ConduitUtil.GetConnectorOfRoute( doc, routeName!, false ) ;
        if ( null == toConnector ) continue ;

        toConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCodeModel ) ;
        if ( string.IsNullOrEmpty( ceedSetCodeModel ) ) continue ;

        toConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;

        var ceedSetCode = ceedSetCodeModel!.Split( ':' ).ToList() ;
        if ( ceedSetCode.Count < 3 ) continue ;

        var toConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == ceedSetCode[ 0 ] && x.GeneralDisplayDeviceSymbol == ceedSetCode[ 1 ] && x.ModelNumber == ceedSetCode[ 2 ] ) ;
        if ( toConnectorCeedModel == null ) continue ;

        var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode )
          ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( toConnectorCeedModel.CeedModelNumber ) ).Skip( 1 )
          : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( toConnectorCeedModel.CeedModelNumber ) ).Skip( 1 ) ;
        
        foreach ( var item in hiroiSetModels ) {
          List<string> listMaterialCode = new() ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode1 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode2 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode2 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode3 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode3 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode4 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode4 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode5 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode5 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode6 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode6 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode7 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode7 ).ToString() ) ;
          if ( ! string.IsNullOrEmpty( item.MaterialCode8 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode8 ).ToString() ) ;

          if ( ! listMaterialCode.Any() ) continue ;
          var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
          foreach ( var master in masterModels ) {
            var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == master.Type && w.DiameterOrNominal == master.Size1 && ( ( w.NumberOfHeartsOrLogarithm == "0" && master.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && master.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel == null ) continue ;
            var wireType = master.Type ;
            var wireSize = master.Size1 ;
            var wireStrip = string.IsNullOrEmpty( master.Size2 ) || master.Size2 == "0" ? "-" : master.Size2 ;
 
            selectWiringModels.Add( new SelectWiringModel( conduit.Id.ToString(), routeName!, toConnectorCeedModel.FloorPlanType, toConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, defaultPlumbingType, "" ) ) ;
          }
        }

        
      }

      return selectWiringModels ;

      //return ( from conduit in conduits select new SelectWiringModel( conduit.Id.ToString(), conduit.GetRouteName() ?? string.Empty ) ).ToList() ;
    }
  }
}