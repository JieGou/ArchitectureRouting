using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Autodesk.Revit.Attributes ;
using NPOI.OpenXmlFormats.Spreadsheet ;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.CreateDetailTableCommandBase ;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.ShowElectricSymbolsCommandBase ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction(TransactionMode.Manual)]
  [DisplayNameKey("Electrical.App.Commands.Initialization.ShowDialogCreateTableByFloorCommand", DefaultString = "Create Table\nBy Floors")]
  [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
  public class ShowDialogCreateTableByFloorCommand : IExternalCommand
  {
    private const string DetailTableType = "Detail Table" ;
    private const string ElectricalSymbolTableType = "Electrical Symbol Table" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;

      var dialog = new CreateTableByFloors( uiDocument.Document ) ;
      dialog.ShowDialog() ;
      if ( ! dialog.DialogResult ?? true )
        return Result.Cancelled ;
      
      var tableType = dialog.SelectedTableType ;
      var levelIds = dialog.LevelList.Where( t => t.IsSelected == true ).Select( p => p.LevelId ).ToList() ;

      return tableType switch
      {
        DetailTableType => CreateDetailTable( uiDocument, levelIds ),
        ElectricalSymbolTableType => CreateElectricalTable( uiDocument, levelIds ),
        _ => Result.Failed
      } ;
    }
    
    private Result CreateDetailTable( UIDocument uiDocument, List<ElementId> levelIds )
    {
      // const string defaultParentPlumbingType = "E" ;
      // var csvStorable = uiDocument.Document.GetCsvStorable() ;
      // var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
      // var conduitsModelData = csvStorable.ConduitsModelData ;
      // var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      // var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      // var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      // var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
      // var hiroiSetCdMasterEcoModelData = csvStorable.HiroiSetCdMasterEcoModelData ;
      // var ceedStorable = uiDocument.Document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      // var detailTableModelsData = uiDocument.Document.GetDetailTableStorable().DetailTableModelData ;
      // var detailTableModels = new List<ObservableCollection<DetailTableModel>>() ;
      // var detailSymbolStorable = uiDocument.Document.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      // var cnsStorable = uiDocument.Document.GetCnsSettingStorable() ;
      // var allConduits = new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( Conduit ) ).OfType<Conduit>().AsEnumerable() ;
      // bool mixConstructionItems ;
      // foreach ( var levelId in levelIds ) {
      //   try {
      //     var detailTableModelsByFloors = new ObservableCollection<DetailTableModel>() ;
      //     var conduitsByFloors = allConduits.Where( x => x.ReferenceLevel.Id == levelId ).ToList() ;
      //     var elementsByFloors = conduitsByFloors.Cast<Element>().ToList() ;
      //     var pickedObjectIds = conduitsByFloors.Select( p => p.UniqueId ).ToList() ;
      //     var detailSymbolModelsByDetailSymbolId = detailSymbolStorable.DetailSymbolModelData
      //       .Where( x => pickedObjectIds.Contains( x.ConduitId ) ).OrderBy( x => x.DetailSymbol )
      //       .ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.IsParentSymbol ).GroupBy(
      //         x => x.DetailSymbolId, ( key, p ) => new { DetailSymbolId = key, DetailSymbolModels = p.ToList() } ) ;
      //     var detailSymbolIds = detailSymbolStorable.DetailSymbolModelData
      //       .Where( x => pickedObjectIds.Contains( x.ConduitId ) ).Select( d => d.DetailSymbolId ).Distinct()
      //       .ToHashSet() ;
      //     mixConstructionItems = IsMixConstructionItems( detailTableModelsData, detailSymbolIds ) ;
      //     foreach ( var detailSymbolModelByDetailSymbolId in detailSymbolModelsByDetailSymbolId ) {
      //       var firstDetailSymbolModelByDetailSymbolId =
      //         detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault() ;
      //       var routeNames = detailSymbolModelByDetailSymbolId.DetailSymbolModels.Select( d => d.RouteName ).Distinct()
      //         .ToList() ;
      //       var parentRouteName = firstDetailSymbolModelByDetailSymbolId!.CountCableSamePosition == 1
      //         ? firstDetailSymbolModelByDetailSymbolId.RouteName
      //         : GetParentRouteName( doc, routeNames ) ;
      //       if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
      //         var parentDetailSymbolModel =
      //           detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault(
      //             d => d.RouteName == parentRouteName ) ;
      //         // AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData,
      //         //   hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData,
      //         //   hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData,
      //         //   detailTableModelsByFloors, elementsByFloors, parentDetailSymbolModel!, true, mixConstructionItems ) ;
      //         // routeNames = routeNames.Where( n => n != parentRouteName ).OrderByDescending( n => n ).ToList() ;
      //       }
      //
      //       foreach ( var childDetailSymbolModel in from routeName in routeNames
      //                select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d =>
      //                  d.RouteName == routeName ) ) {
      //         // AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData,
      //         //   hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData,
      //         //   hiroiMasterModelData, conduitsModelData, wiresAndCablesModelData, detailTableModelsData,
      //         //   detailTableModelsByFloors, elementsByFloors, childDetailSymbolModel, false, mixConstructionItems ) ;
      //       }
      //     }
      //
      //     // SortDetailTableModel( ref detailTableModelsByFloors ) ;
      //     // SetPlumbingData( conduitsModelData, ref detailTableModelsByFloors, defaultParentPlumbingType,
      //       // mixConstructionItems ) ;
      //     detailTableModels.Add( detailTableModelsByFloors ) ;
      //   }
      //   catch {
      //     return Result.Cancelled ;
      //   }
      // }
      //
      // return doc.Transaction(
      //   "TransactionName.Commands.Routing.CreateDetailTable".GetAppStringByKeyOrDefault( "Set detail table" ), _ =>
      //   {
      //     foreach ( var detailTableModelsByFloors in detailTableModels ) {
      //       if ( detailTableModelsByFloors.Count > 0 ) {
      //         var levelName = detailTableModelsByFloors[ 0 ].Floor ;
      //         CreateDetailTableSchedule( doc, detailTableModelsByFloors, levelName ) ;
      //         SaveDetailTableData( detailTableModelsByFloors, doc ) ;
      //       }
      //     }
      //
      //     return Result.Succeeded ;
      //   } ) ;
      return Result.Succeeded ;
    }

    private Result CreateElectricalTable( UIDocument uiDocument, List<ElementId> levelIds )
    {
      // var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      // var csvStorable = doc.GetCsvStorable() ;
      // var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      // var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      // var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      // var detailTableModelData = doc.GetDetailTableStorable().DetailTableModelData ;
      // var allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements )
      //   .ToList() ;
      // var electricalSymbolModels = new List<ElectricalSymbolModel>() ;
      // var errorMess = string.Empty ;
      // try {
      //   var allConduits = new FilteredElementCollector( doc ).OfClass( typeof( Conduit ) )
      //     .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      //   var conduitsByFloors = allConduits.Where( x => levelIds.Contains( x.ReferenceLevel.Id ) ).ToList() ;
      //   var routePicked = conduitsByFloors.Where( x => x.GetRouteName() != null ).Select( x => x.GetRouteName() )
      //     .Distinct().ToList() ;
      //
      //   foreach ( var routeName in routePicked ) {
      //     // var fromConnectorInfoAndToConnectorInfo =
      //     //   GetFromConnectorInfoAndToConnectorInfo( doc, allConnectors, routeName!, ref errorMess ) ;
      //     // if ( ! string.IsNullOrEmpty( errorMess ) ) {
      //     //   return Result.Cancelled ;
      //     // }
      //     //
      //     // var fromConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x =>
      //     //   x.CeedSetCode == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.CeedSetCode &&
      //     //   x.GeneralDisplayDeviceSymbol == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.DeviceSymbol &&
      //     //   x.ModelNumber == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.ModelNumber ) ;
      //     // var toConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x =>
      //     //   x.CeedSetCode == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.CeedSetCode &&
      //     //   x.GeneralDisplayDeviceSymbol == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.DeviceSymbol &&
      //     //   x.ModelNumber == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.ModelNumber ) ;
      //     // if ( fromConnectorCeedModel == null && toConnectorCeedModel == null ) continue ;
      //     // var detailTableModelsByRouteName = detailTableModelData.Where( d => d.RouteName == routeName ).ToList() ;
      //     // if ( detailTableModelsByRouteName.Any() ) {
      //     //   // InsertDataFromDetailTableModelIntoElectricalSymbolModel( electricalSymbolModels,
      //     //   //   detailTableModelsByRouteName, fromConnectorCeedModel, toConnectorCeedModel,
      //     //   //   fromConnectorInfoAndToConnectorInfo.fromConnectorUniqueId,
      //     //   //   fromConnectorInfoAndToConnectorInfo.toConnectorUniqueId ) ;
      //     // }
      //     // else {
      //     //   // InsertDataFromRegularDatabaseIntoElectricalSymbolModel( hiroiSetMasterEcoModelData,
      //     //   //   hiroiSetMasterNormalModelData, hiroiMasterModelData, allConnectors, electricalSymbolModels,
      //     //   //   fromConnectorCeedModel, toConnectorCeedModel, fromConnectorInfoAndToConnectorInfo.fromConnectorUniqueId,
      //     //   //   fromConnectorInfoAndToConnectorInfo.toConnectorUniqueId ) ;
      //     // }
      //   }
      // }
      // catch {
      //   return Result.Cancelled ;
      // }
      //
      // return doc.Transaction(
      //   "TransactionName.Commands.Initialization.ShowElectricSymbolsCommand".GetAppStringByKeyOrDefault(
      //     "Create electrical schedule" ), _ =>
      //   {
      //     // CreateElectricalSchedule( doc, electricalSymbolModels ) ;
      //     return Result.Succeeded ;
      //   } ) ;
      return Result.Succeeded ;
    }
    
    private bool IsMixConstructionItems( List<DetailTableModel> detailTableModelsData,
      HashSet<string> detailSymbolIds )
    {
      var detailTableModelRowGroupMixConstructionItems = detailTableModelsData.FirstOrDefault( d =>
        detailSymbolIds.Contains( d.DetailSymbolId ) && ! string.IsNullOrEmpty( d.GroupId ) &&
        bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      var detailTableModelRowGroupNoMixConstructionItems = detailTableModelsData.FirstOrDefault( d =>
        detailSymbolIds.Contains( d.DetailSymbolId ) && ! string.IsNullOrEmpty( d.GroupId ) &&
        ! bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      return detailTableModelRowGroupNoMixConstructionItems == null &&
             detailTableModelRowGroupMixConstructionItems != null ;
    }

    private string GetParentRouteName( Document document, List<string> routeNames )
    {
      foreach ( var routeName in routeNames ) {
        var route = document.CollectRoutes( AddInType.Electrical ).FirstOrDefault( x => x.RouteName == routeName ) ;
        if ( route == null ) continue ;
        var parentRouteName = route.GetParentBranches().ToList().LastOrDefault()?.RouteName ;
        if ( string.IsNullOrEmpty( parentRouteName ) || parentRouteName == routeName ) {
          return routeName ;
        }
      }

      return string.Empty ;
    }

    private void SaveDetailTableData( IReadOnlyCollection<DetailTableModel> detailTableModels, Document doc )
    {
      try {
        DetailTableStorable detailTableStorable = doc.GetDetailTableStorable() ;
        {
          if ( ! detailTableModels.Any() ) return ;
          var existedDetailSymbolIds = detailTableStorable.DetailTableModelData.Select( d => d.DetailSymbolId )
            .Distinct().ToList() ;
          var itemNotInDb = detailTableStorable.DetailTableModelData
            .Where( d => ! existedDetailSymbolIds.Contains( d.DetailSymbolId ) ).ToList() ;
          if ( itemNotInDb.Any() ) detailTableStorable.DetailTableModelData.AddRange( itemNotInDb ) ;
        }
        detailTableStorable.Save() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    private void AddDetailTableModelRow( Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<DetailTableModel> detailTableModelsData, ICollection<DetailTableModel> detailTableModels, List<Element> pickedObjects, DetailSymbolModel detailSymbolModel, bool isParentRoute, bool mixConstructionItems )
    {
      var ceedCode = string.Empty ;
      var constructionClassification = string.Empty ;
      var signalType = string.Empty ;
      var wireType = string.Empty ;
      var wireSize = string.Empty ;
      var wireStrip = string.Empty ;
      var remark = string.Empty ;
      var groupId = string.Empty ;
      double wireCrossSectionalArea = 0 ;
      var element = pickedObjects.FirstOrDefault( p => p.UniqueId == detailSymbolModel.ConduitId ) ;
      string floor = doc.GetElementById<Level>( element!.GetLevelId() )?.Name ?? string.Empty ;
      string constructionItem = element!.LookupParameter( "Construction Item" ).AsString() ?? DefaultConstructionItems ;
      var plumbingItems = constructionItem ;
      string isEcoMode = element.LookupParameter( "IsEcoMode" ).AsString() ;
      string plumbingType = detailSymbolModel.PlumbingType ;

      var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == detailSymbolModel.Code && x.GeneralDisplayDeviceSymbol == detailSymbolModel.DeviceSymbol ) ;
      if ( ceedModel != null && ! string.IsNullOrEmpty( ceedModel.CeedSetCode ) && ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ) {
        ceedCode = ceedModel.CeedSetCode ;
        remark = ceedModel.GeneralDisplayDeviceSymbol ;
        var hiroiCdModel = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetCdMasterEcoModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) : hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) ;
        var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) ;
        constructionClassification = hiroiCdModel?.ConstructionClassification ;
        foreach ( var item in hiroiSetModels ) {
          List<string> listMaterialCode = new List<string>() ;
          if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
            listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
          }

          if ( ! listMaterialCode.Any() ) continue ;
          var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
          foreach ( var master in masterModels ) {
            wireType = master.Type ;
            wireSize = master.Size1 ;
            wireStrip = master.Size2 ;
            var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && wireStrip == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && wireStrip == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel == null ) continue ;
            signalType = wiresAndCablesModel.Classification ;
            wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
          }
        }
      }

      if ( detailTableModelsData.Any() ) {
        var oldDetailTableRow = detailTableModelsData.FirstOrDefault( d => d.DetailSymbolId == detailSymbolModel.DetailSymbolId && d.RouteName == detailSymbolModel.RouteName ) ;
        if ( oldDetailTableRow != null ) {
          groupId = oldDetailTableRow.GroupId ;
          plumbingItems = mixConstructionItems ? oldDetailTableRow.PlumbingItems : constructionItem ;
        }
      }

      var detailTableRow = new DetailTableModel( false, floor, ceedCode, detailSymbolModel.DetailSymbol, detailSymbolModel.DetailSymbolId, wireType, wireSize, wireStrip, "1", string.Empty, string.Empty, string.Empty, plumbingType, string.Empty, string.Empty, constructionClassification, signalType, constructionItem, plumbingItems, remark, wireCrossSectionalArea, detailSymbolModel.CountCableSamePosition, detailSymbolModel.RouteName, isEcoMode, isParentRoute, ! isParentRoute, string.Empty, groupId, true, mixConstructionItems, string.Empty ) ;
      detailTableModels.Add( detailTableRow ) ;
    }
  }
}