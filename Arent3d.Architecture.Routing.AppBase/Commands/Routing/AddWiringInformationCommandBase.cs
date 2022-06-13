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
        var hiroiSetCdMasterEcoModelData = csvStorable.HiroiSetCdMasterEcoModelData ;
        var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
        var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
        var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
        var cnsStorable = document.GetCnsSettingStorable() ;
        var detailSymbolStorable = document.GetDetailSymbolStorable() ;
        var wiringStore = document.GetWiringStorable() ;
        
        var pickInfo = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick a point on a route to get info.", AddInType.Electrical ) ;
        //Get all route name related to selected conduit
        var wiringList = GetAllConduitRelated( document, pickInfo.Element, hiroiSetMasterEcoModelData, hiroiSetMasterNormalModelData, hiroiMasterModelData, wiresAndCablesModelData, hiroiSetCdMasterEcoModelData, hiroiSetCdMasterNormalModelData ) ;

        //Create Detail table from wiringlist
        var detailTableModels = CreateDetailTableModelsFromWiringList( wiringList ) ;
        var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
        var conduitTypes = ( from conduitTypeName in conduitTypeNames select new DetailTableModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
        conduitTypes.Add( new DetailTableModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;

        var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
        var constructionItems = constructionItemNames.Any()
          ? ( from constructionItemName in constructionItemNames select new DetailTableModel.ComboboxItemType( constructionItemName, constructionItemName ) ).ToList()
          : new List<DetailTableModel.ComboboxItemType>() { new(DefaultConstructionItems, DefaultConstructionItems) } ;

        var levelNames = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).Select( l => l.Name ).ToList() ;
        var levels = ( from levelName in levelNames select new DetailTableModel.ComboboxItemType( levelName, levelName ) ).ToList() ;

        var wireTypeNames = wiresAndCablesModelData.Select( w => w.WireType ).Distinct().ToList() ;
        var wireTypes = ( from wireType in wireTypeNames select new DetailTableModel.ComboboxItemType( wireType, wireType ) ).ToList() ;

        var earthTypes = new List<DetailTableModel.ComboboxItemType>() { new("IV", "IV"), new("EM-IE", "EM-IE") } ;

        var numbers = new List<DetailTableModel.ComboboxItemType>() ;
        for ( var i = 1 ; i <= 10 ; i++ ) {
          numbers.Add( new DetailTableModel.ComboboxItemType( i.ToString(), i.ToString() ) ) ;
        }

        var constructionClassificationTypeNames = hiroiSetCdMasterNormalModelData.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
        var constructionClassificationTypes = ( from constructionClassification in constructionClassificationTypeNames select new DetailTableModel.ComboboxItemType( constructionClassification, constructionClassification ) ).ToList() ;

        var signalTypes = ( from signalType in (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType ) ) select new DetailTableModel.ComboboxItemType( signalType.GetFieldName(), signalType.GetFieldName() ) ).ToList() ;

        var viewModel = new DetailTableViewModel( detailTableModels, new ObservableCollection<DetailTableModel>(), conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes ) ;
        var dialog = new DetailTableDialog( document, viewModel, conduitsModelData, wiresAndCablesModelData, false, true ) ;
         if ( dialog.ShowDialog() == true ) {
        //   foreach ( var detailTableModel in viewModel.DetailTableModels ) {
        //     if(wiringStore.WiringData.FirstOrDefault(x=>x.IdOfToConnector == detailTableModel.id))
        //   }
        //   wiringStore.Save();
         }
 

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

    private ObservableCollection<DetailTableModel> CreateDetailTableModelsFromWiringList( List<WiringModel> wiringList )
    {
      ObservableCollection<DetailTableModel> detailTableModels = new ObservableCollection<DetailTableModel>() ;
      foreach ( var wiring in wiringList ) {
        var detailTableModel = new DetailTableModel( false, wiring.Floor, wiring.SetCode, "*", null, wiring.WireType, wiring.WireSize, wiring.WireStrip, null, null, null, null, wiring.PipingType, wiring.PipingSize, wiring.NumberOfPlumbing, wiring.ConstructionClassification, wiring.SignalType,
          wiring.ConstructionItems, wiring.PlumbingItems, wiring.Remark, null, null, wiring.RouteName, wiring.IsEcoModel.ToString(), null, null, null, null, null, null, null ) ;
        detailTableModels.Add( detailTableModel ) ;
      }

      return detailTableModels ;
    }

    private List<WiringModel> GetAllConduitRelated( Document doc, Element pickConduit, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData,
      List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData )
    {
      const string defaultPlumbingType = "配管なし" ;
      var ceedStorable = doc.GetCeedStorable() ;
      var wiringStorable = doc.GetWiringStorable() ;
      var representativeRouteName = ( (Conduit) pickConduit ).GetRepresentativeRouteName() ;
      var routeNameSamePosition = GetRouteNameSamePosition( doc, representativeRouteName!, pickConduit ) ;
      List<WiringModel> selectWiringModels = new() ;

      foreach ( var routeName in routeNameSamePosition ) { 
        var toConnector = ConduitUtil.GetConnectorOfRoute( doc, routeName!, false ) ;
        if ( null == toConnector ) return selectWiringModels ;

        var existedConduitInWiringStorable = wiringStorable.WiringData.FirstOrDefault( x => x.IdOfToConnector == toConnector.UniqueId ) ;
        if ( null != existedConduitInWiringStorable ) {
          selectWiringModels.Add( existedConduitInWiringStorable ) ;
        }

        var floor = doc.GetAllElements<Level>().FirstOrDefault( l => l.Id == toConnector.LevelId )?.Name ?? string.Empty ; 
        string constructionItem =  DefaultConstructionItems ;

        toConnector.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? isEcoMode ) ;
        var isEco = false ;
        if ( ! string.IsNullOrEmpty( isEcoMode ) )
          Boolean.TryParse( isEcoMode, out isEco ) ;

        toConnector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedSetCodeModel ) ;
        if ( string.IsNullOrEmpty( ceedSetCodeModel ) ) {
          selectWiringModels.Add( new WiringModel( toConnector.Id.ToString(), toConnector.UniqueId, routeName!, floor, "*", null, null, null, defaultPlumbingType, "", string.Empty, null, null, constructionItem, constructionItem, "*", null, null, isEco ) ) ;
        }
        else {
          var ceedSetCode = ceedSetCodeModel!.Split( ':' ).ToList() ;
          if ( ceedSetCode.Count < 3 ) return selectWiringModels ;

          var toConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == ceedSetCode[ 0 ] && x.GeneralDisplayDeviceSymbol == ceedSetCode[ 1 ] && x.ModelNumber == ceedSetCode[ 2 ] ) ;
          if ( toConnectorCeedModel == null ) return selectWiringModels ;

          var hiroiSetModels = isEco ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( toConnectorCeedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( toConnectorCeedModel.CeedModelNumber ) ).Skip( 1 ) ;
          var hiroiCdModel = isEco ? hiroiSetCdMasterEcoModelData.FirstOrDefault( x => x.SetCode == ceedSetCode[ 1 ] ) : hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedSetCode[ 1 ] ) ;
          var constructionClassification = hiroiCdModel?.ConstructionClassification ;
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
              var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w =>
                w.WireType == master.Type && w.DiameterOrNominal == master.Size1 && ( ( w.NumberOfHeartsOrLogarithm == "0" && master.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && master.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
              if ( wiresAndCablesModel == null ) continue ;
              var wireType = master.Type ;
              var wireSize = master.Size1 ;
              var wireStrip = string.IsNullOrEmpty( master.Size2 ) || master.Size2 == "0" ? "-" : master.Size2 ;
              var signalType = wiresAndCablesModel.Classification ;

              selectWiringModels.Add( new WiringModel( toConnector.Id.ToString(), toConnector.UniqueId, routeName!, floor, toConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, defaultPlumbingType, "", string.Empty, constructionClassification, signalType, constructionItem,
                constructionItem, toConnectorCeedModel.GeneralDisplayDeviceSymbol, item.ParentPartModelNumber, ceedSetCode[ 0 ], isEco ) ) ;
            }
          }
        }
      } 
      return selectWiringModels ;
    }

    public List<string> GetRouteNameSamePosition( Document doc, string representativeRouteName, Element pickConduit )
    {
      List<string> routeNames = new List<string>() ;
      if ( pickConduit is Conduit ) {
        var conduits = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).ToList() ;
        var location = ( pickConduit.Location as LocationCurve ) ! ;
        var line = ( location.Curve as Line ) ! ;
        var origin = line.Origin ;
        var direction = line.Direction ;
        foreach ( var conduit in conduits ) {
          var anotherLocation = ( conduit.Location as LocationCurve ) ! ;
          var anotherLine = ( anotherLocation.Curve as Line ) ! ;
          var anotherOrigin = anotherLine.Origin ;
          var anotherDirection = anotherLine.Direction ;
          if ( anotherOrigin.DistanceTo( origin ) == 0 && anotherDirection.DistanceTo( direction ) == 0 && ! routeNames.Contains( conduit.GetRouteName()! ) )
            routeNames.Add( conduit.GetRouteName()! ) ;
        }
      }
      else {
        var routeNamesSamePosition = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.GetRepresentativeRouteName() == representativeRouteName ).Select( c => c.GetRouteName() ).Distinct().ToList() ;
        var conduitFittingsOfRoutes = doc.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_ConduitFitting ).Where( c => routeNamesSamePosition.Contains( c.GetRouteName() ) ).ToList() ;
        var pickConduitFitting = doc.GetElementById<FamilyInstance>( pickConduit.Id )! ;
        var location = ( pickConduitFitting.Location as LocationPoint )! ;
        var origin = location.Point ;
        var direction = pickConduitFitting.FacingOrientation ;
        foreach ( var conduitFitting in conduitFittingsOfRoutes ) {
          var anotherConduitFitting = doc.GetElementById<FamilyInstance>( conduitFitting.Id )! ;
          var anotherLocation = ( anotherConduitFitting.Location as LocationPoint )! ;
          var anotherOrigin = anotherLocation.Point ;
          var anotherDirection = anotherConduitFitting.FacingOrientation ;
          if ( anotherOrigin.DistanceTo( origin ) == 0 && anotherDirection.DistanceTo( direction ) == 0 && ! routeNames.Contains( conduitFitting.GetRouteName()! ) )
            routeNames.Add( conduitFitting.GetRouteName()! ) ;
        }
      }

      return routeNames ;
    }
  }
}