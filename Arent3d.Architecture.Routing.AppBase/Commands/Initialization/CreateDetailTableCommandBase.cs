using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using static Arent3d.Architecture.Routing.AppBase.Commands.Initialization.ShowElectricSymbolsCommandBase ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class CreateDetailTableCommandBase : IExternalCommand
  {
    private const string DefaultPlumbingType = "E" ;
    private const string DefaultConstructionItems = "未設定" ;
    private const string DefaultChildPlumbingSymbol = "↑" ;
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;
    public const double Percentage = 0.32 ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var csvStorable = doc.GetCsvStorable() ;
      var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
      var cnsStorable = doc.GetCnsSettingStorable() ;
      var storageService = new StorageService<Level, DetailSymbolModel>( ( (ViewPlan) doc.ActiveView ).GenLevel ) ;
      try {
        var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
        var pickedObjectIds = pickedObjects.Select( p => p.UniqueId ) ;
        var (detailTableModels, isMixConstructionItems, isExistDetailTableItemModel) =
          CreateDetailTableItem( doc, csvStorable, storageService, pickedObjects, pickedObjectIds, false, true ) ;

        var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct() ;
        var conduitTypes = ( from conduitTypeName in conduitTypeNames select new DetailTableItemModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
        conduitTypes.Add( new DetailTableItemModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;

        var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
        var constructionItems = constructionItemNames.Any()
          ? ( from constructionItemName in constructionItemNames select new DetailTableItemModel.ComboboxItemType( constructionItemName, constructionItemName ) )
          : new List<DetailTableItemModel.ComboboxItemType>() { new( DefaultConstructionItems, DefaultConstructionItems ) } ;

        var levelNames = doc.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).Select( l => l.Name ) ;
        var levels = ( from levelName in levelNames select new DetailTableItemModel.ComboboxItemType( levelName, levelName ) ) ;

        var wireTypeNames = wiresAndCablesModelData.Select( w => w.WireType ).Distinct() ;
        var wireTypes = ( from wireType in wireTypeNames select new DetailTableItemModel.ComboboxItemType( wireType, wireType ) ) ;

        var earthTypes = new List<DetailTableItemModel.ComboboxItemType>() { new( "IV", "IV" ), new( "EM-IE", "EM-IE" ) } ;

        var numbers = new List<DetailTableItemModel.ComboboxItemType>() ;
        for ( var i = 1 ; i <= 10 ; i++ ) {
          numbers.Add( new DetailTableItemModel.ComboboxItemType( i.ToString(), i.ToString() ) ) ;
        }

        var constructionClassificationTypeNames = hiroiSetCdMasterNormalModelData.Select( h => h.ConstructionClassification ).Distinct() ;
        var constructionClassificationTypes = ( from constructionClassification in constructionClassificationTypeNames
          select new DetailTableItemModel.ComboboxItemType( constructionClassification, constructionClassification ) ) ;

        var signalTypes = ( from signalType in (SignalType[]) Enum.GetValues( typeof( SignalType ) )
          select new DetailTableItemModel.ComboboxItemType( signalType.GetFieldName(), signalType.GetFieldName() ) ) ;

        var viewModel =
          new DetailTableViewModel(
            doc, 
            detailTableModels,
            new ObservableCollection<DetailTableItemModel>(),
            conduitTypes,
            constructionItems,
            levels,
            wireTypes,
            earthTypes,
            numbers,
            constructionClassificationTypes,
            signalTypes,
            conduitsModelData,
            wiresAndCablesModelData,
            isMixConstructionItems ) ;
        var dialog = new DetailTableDialog( viewModel ) ;
        dialog.ShowDialog() ;

        while ( dialog.DialogResult is false && viewModel.IsAddReference ) {
          TextNotePickFilter textNotePickFilter = new() ;
          List<string> detailSymbolIds = new() ;
          
          try {
            var pickedDetailSymbols = uiDoc.Selection.PickObjects( ObjectType.Element, textNotePickFilter ) ;
            foreach ( var pickedDetailSymbol in pickedDetailSymbols ) {
              if ( uiDoc.Document.GetElement(pickedDetailSymbol) is TextNote detailSymbol && ! detailSymbolIds.Contains( detailSymbol.UniqueId ) ) {
                detailSymbolIds.Add( detailSymbol.UniqueId ) ;
              }
            }

            var ( referenceDetailTableItemModels, _, _) = CreateDetailTableItem( doc, csvStorable, storageService, new List<Element>(), detailSymbolIds, true, true ) ;
            foreach ( var referenceDetailTableModelRow in referenceDetailTableItemModels ) {
              viewModel.ReferenceDetailTableItemModelsOrigin.Add( referenceDetailTableModelRow ) ;
            }
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            // Ignore
          }
          
          uiDoc.Selection.SetElementIds(new List<ElementId>());
          
          dialog = new DetailTableDialog( viewModel ) ;
          dialog.ShowDialog() ;
        }

        if ( isExistDetailTableItemModel || dialog.DialogResult == true ) {
          if ( viewModel.RoutesWithConstructionItemHasChanged.Any() ) {
            UpdateConnectorWithConstructionItem( doc, viewModel.RoutesWithConstructionItemHasChanged ) ;
          }
          
          if ( viewModel.DetailSymbolIdsWithPlumbingTypeHasChanged.Any() ) {
            UpdateDetailSymbolPlumbingType( doc, storageService, viewModel.DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
          }
        }

        if ( dialog.DialogResult ?? false ) {
          return doc.Transaction( "TransactionName.Commands.Routing.CreateDetailTable".GetAppStringByKeyOrDefault( "Set detail table" ), _ =>
          {
            if ( ! viewModel.IsCreateDetailTableItemOnFloorPlanView ) return Result.Succeeded ;
            var level = uiDoc.ActiveView.GenLevel ;
            var detailTableData = viewModel.DetailTableItemModels ;
            var scheduleName = CreateDetailTableSchedule( doc, detailTableData, level.Name ) ;
            MessageBox.Show( string.Format( "Revit.Electrical.CreateSchedule.Message".GetAppStringByKeyOrDefault( CreateScheduleSuccessfullyMessage ), scheduleName ), "Message" ) ;
            viewModel.IsCreateDetailTableItemOnFloorPlanView = false ;

            return Result.Succeeded ;
          } ) ;
        }

        return Result.Succeeded ;
      }
      catch {
        return Result.Cancelled ;
      }
    }

    public static string GetKeyRouting( string detailSymbolUniqueId, string fromConnectorUniqueId, string toConnectorUniqueId )
    {
      return $"{detailSymbolUniqueId},{fromConnectorUniqueId},{toConnectorUniqueId}" ;
    }

    public static string GetKeyRouting( DetailSymbolItemModel? detailSymbolItemModel )
    {
      return null == detailSymbolItemModel ? string.Empty : GetKeyRouting( detailSymbolItemModel.DetailSymbolUniqueId, detailSymbolItemModel.FromConnectorUniqueId, detailSymbolItemModel.ToConnectorUniqueId ) ;
    }

    public static string GetKeyRouting( DetailTableItemModel? detailTableItemModel )
    {
      return null == detailTableItemModel ? string.Empty : GetKeyRouting( detailTableItemModel.DetailSymbolUniqueId, detailTableItemModel.FromConnectorUniqueId, detailTableItemModel.ToConnectorUniqueId ) ;
    }

    public static ( ObservableCollection<DetailTableItemModel>, bool, bool ) CreateDetailTableItem( Document doc, CsvStorable csvStorable, StorageService<Level, DetailSymbolModel> storageServiceForDetailSymbol, List<Element> conduits, IEnumerable<string> elementUniqueIds, bool isReferenceDetailTableModels, bool isFromCreateDetailTable = false )
    {
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
      var hiroiSetCdMasterEcoModelData = csvStorable.HiroiSetCdMasterEcoModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      var storageServiceForDetailTable = new StorageService<Level, DetailTableModel>( storageServiceForDetailSymbol.Owner ) ;
      var detailTableModelsData = storageServiceForDetailTable.Data.DetailTableData ;
      var detailTableItemModels = new ObservableCollection<DetailTableItemModel>() ;

      var detailSymbolItemModels = new List<DetailSymbolItemModel>() ;
      foreach ( var detailSymbolItemModel in storageServiceForDetailSymbol.Data.DetailSymbolData ) {
        if ( isFromCreateDetailTable) {
          if ( detailSymbolItemModel.DetailSymbol != AddWiringInformationCommandBase.SpecialSymbol ) {
            detailSymbolItemModels.Add(detailSymbolItemModel);
          }
        }
        else {
          detailSymbolItemModels.Add(detailSymbolItemModel);
        }
      }
      
      var keyRoutingOnDetailTableModels = detailTableModelsData.Select( GetKeyRouting )
        .Distinct().ToList() ;
 
      var keyRoutingOnDetailSymbolModels = detailSymbolItemModels
        .Where( x => elementUniqueIds.Contains( isReferenceDetailTableModels ? x.DetailSymbolUniqueId : x.ConduitUniqueId ) 
                     && keyRoutingOnDetailTableModels.Contains( GetKeyRouting( x.DetailSymbolUniqueId, x.FromConnectorUniqueId, x.ToConnectorUniqueId ) ) )
        .Select( d => GetKeyRouting( d.DetailSymbolUniqueId, d.FromConnectorUniqueId, d.ToConnectorUniqueId ) ).Distinct().ToList() ;
      
      var isMixConstructionItems = keyRoutingOnDetailSymbolModels.Any() && CheckMixConstructionItems( detailTableModelsData, keyRoutingOnDetailSymbolModels ) ;
      
      var detailSymbolModelsByKeyRouting = detailSymbolItemModels
            .Where( x => elementUniqueIds.Contains( isReferenceDetailTableModels ? x.DetailSymbolUniqueId : x.ConduitUniqueId ) 
                         && ! keyRoutingOnDetailTableModels.Contains( GetKeyRouting(x.DetailSymbolUniqueId, x.FromConnectorUniqueId, x.ToConnectorUniqueId) ) )
            .OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.IsParentSymbol )
            .GroupBy( x => GetKeyRouting(x.DetailSymbolUniqueId, x.FromConnectorUniqueId, x.ToConnectorUniqueId), ( key, p ) => new { KeyRouting = key, DetailSymbolModels = p.ToList() } ) ;
      
      foreach ( var detailSymbolModelByKeyRouting in detailSymbolModelsByKeyRouting ) {
        var firstDetailSymbolModel = detailSymbolModelByKeyRouting.DetailSymbolModels.FirstOrDefault() ;
        var routeNames = detailSymbolModelByKeyRouting.DetailSymbolModels.Select( d => d.RouteName ).Distinct().ToList() ;
        var parentRouteName = firstDetailSymbolModel!.CountCableSamePosition == 1 ? firstDetailSymbolModel.RouteName : GetParentRouteName( doc, routeNames ) ;
        if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
          var parentDetailSymbolModel = detailSymbolModelByKeyRouting.DetailSymbolModels.FirstOrDefault( d => d.RouteName == parentRouteName ) ;
          if ( isReferenceDetailTableModels ) {
            var conduitOfFirstRoute = doc.GetElement( parentDetailSymbolModel!.ConduitUniqueId ) ;
            conduits = new List<Element> { conduitOfFirstRoute } ;
          }
          
          AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, csvStorable.WiresAndCablesModelData, detailTableItemModels, conduits, parentDetailSymbolModel!, true, isMixConstructionItems ) ;
          var routeNameArray = parentRouteName.Split( '_' ) ;
          parentRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
          routeNames = routeNames.Where( n =>
          {
            var nameArray = n.Split( '_' ) ;
            var strRouteName = string.Join( "_", nameArray.First(), nameArray.ElementAt( 1 ) ) ;
            return strRouteName != parentRouteName ;
          } ).OrderByDescending( n => n ).ToList() ;
          
        }

        var childDetailSymbolItemModels = from routeName in routeNames select detailSymbolModelByKeyRouting.DetailSymbolModels.FirstOrDefault( d => d.RouteName == routeName ) ;
        foreach ( var childDetailSymbolItemModel in childDetailSymbolItemModels ) {
          if ( isReferenceDetailTableModels ) {
            var conduitOfFirstRoute = doc.GetElement( childDetailSymbolItemModel.ConduitUniqueId ) ;
            conduits = new List<Element> { conduitOfFirstRoute } ;
          }

          AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, csvStorable.WiresAndCablesModelData, detailTableItemModels, conduits, childDetailSymbolItemModel, false, isMixConstructionItems ) ;
        }
      }

      var detailSymbolModel = conduits.Any() ? detailSymbolItemModels.FirstOrDefault( x => x.ConduitUniqueId.Equals( conduits.First().UniqueId ) ) : null;
      var plumbingType = null != detailSymbolModel ? detailSymbolModel.PlumbingType : DefaultPlumbingType ;
      if ( detailTableItemModels.Any() ) {
        SetPlumbingDataForEachWiring( detailTableModelsData, csvStorable.ConduitsModelData, ref detailTableItemModels, plumbingType, isFromCreateDetailTable && ! isReferenceDetailTableModels ) ;
      }

      if ( keyRoutingOnDetailSymbolModels.Any() ) {
        var index = "-" + DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
        var detailTableModelRowsOnDetailTableStorable = detailTableModelsData.Where( d => keyRoutingOnDetailSymbolModels.Contains( GetKeyRouting( d ) ) ).ToList() ;
        foreach ( var detailTableRow in detailTableModelRowsOnDetailTableStorable ) {
          if ( isReferenceDetailTableModels ) {
            if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) detailTableRow.GroupId += index ;
            detailTableRow.PlumbingIdentityInfo += index ;
          }
          detailTableItemModels.Add( detailTableRow ) ;
        }
      }

      var resultDetailTableItemModels = SortDetailTableModel( detailTableItemModels, isMixConstructionItems ) ;
      return ( resultDetailTableItemModels, isMixConstructionItems, keyRoutingOnDetailSymbolModels.Any() ) ;
    }

    public enum SignalType
    {
      伝送幹線,
      低電圧,
      小勢力,
      動力
    }

    public enum ConstructionClassificationType
    {
      天井隠蔽,
      天井コロガシ,
      打ち込み,
      フリーアクセス,
      露出,
      地中埋設,
      ケーブルラック配線,
      冷媒管共巻配線,
      漏水帯コロガシ,
      漏水帯配管巻,
      導圧管類
    }

    public static string CreateDetailTableSchedule( Document document, IReadOnlyCollection<DetailTableItemModel> detailTableItemModels, string levelName )
    {
      string scheduleName = "Revit.Detail.Table.Name".GetDocumentStringByKeyOrDefault( document, "Detail Table" ) + " - " + levelName + DateTime.Now.ToString( " yyyy-MM-dd HH-mm-ss" ) ;
      var detailTable = document.GetAllElements<ViewSchedule>().SingleOrDefault( v => v.Name.Contains( scheduleName ) ) ;
      if ( detailTable == null ) {
        detailTable = ViewSchedule.CreateSchedule( document, new ElementId( BuiltInCategory.OST_Conduit ) ) ;
        detailTable.Name = scheduleName ;
        detailTable.TrySetProperty( ElectricalRoutingElementParameter.ScheduleBaseName, scheduleName ) ;
      }

      InsertDetailTableDataIntoSchedule( detailTable, detailTableItemModels ) ;
      return scheduleName ;
    }

    private static void InsertDetailTableDataIntoSchedule( ViewSchedule viewSchedule, IReadOnlyCollection<DetailTableItemModel> detailTableItemModels )
    {
      const int columnCount = 5 ;
      const int maxCharOfCell = 4 ;
      const double minColumnWidth = 0.05 ;
      var rowData = 0 ;
      var maxCharOfWireTypeCell = 0 ;
      var maxCharOfPlumbingTypeCell = 0 ;
      var maxCharOfRemarkCell = 0 ;

      TableCellStyleOverrideOptions tableStyleOverride = new()
      {
        HorizontalAlignment = true,
        BorderLineStyle = true,
        BorderLeftLineStyle = true,
        BorderRightLineStyle = true,
        BorderTopLineStyle = false,
        BorderBottomLineStyle = false
      } ;
      TableCellStyle cellStyle = new() ;
      cellStyle.SetCellStyleOverrideOptions( tableStyleOverride ) ;
      cellStyle.FontHorizontalAlignment = HorizontalAlignmentStyle.Left ;

      TableData tableData = viewSchedule.GetTableData() ;
      TableSectionData tsdHeader = tableData.GetSectionData( SectionType.Header ) ;

      for ( var i = 0 ; i <= columnCount ; i++ ) {
        if ( i != 2 ) tsdHeader.InsertColumn( i ) ;
      }

      detailTableItemModels = detailTableItemModels.Where( d => ! string.IsNullOrEmpty( d.WireType ) && ! string.IsNullOrEmpty( d.WireSize ) ).ToList() ;
      var detailTableItemModelByFloors = detailTableItemModels.OrderBy( d => d.Floor ).GroupBy( d => d.Floor ).Select( g => g.ToList() ).ToList() ;
      var rowCount = detailTableItemModels.Count + detailTableItemModelByFloors.Count - 1 ;
      foreach ( var detailTableModelsByFloor in detailTableItemModelByFloors ) {
        var detailTableModelsGroupByDetailSymbol = detailTableModelsByFloor.GroupBy( d => d.DetailSymbol ).ToDictionary( g => g.Key, g => g.ToList() ) ;
        rowCount += detailTableModelsGroupByDetailSymbol.Count ;
      }
      
      for ( var i = 0 ; i < rowCount ; i++ ) {
        tsdHeader.InsertRow( tsdHeader.FirstRowNumber ) ;
      }

      var isSetPipeForCoWindingWiring = false ;
      foreach ( var detailTableItemModelByFloor in detailTableItemModelByFloors ) {
        var detailTableItemModelsGroupByDetailSymbolItem = detailTableItemModelByFloor.GroupBy( d => d.DetailSymbol ).ToDictionary( g => g.Key, g => g.ToList() ) ;
        var level = detailTableItemModelByFloor.FirstOrDefault( d => ! string.IsNullOrEmpty( d.Floor ) )?.Floor ?? string.Empty ;
        tsdHeader.MergeCells( new TableMergedCell( 0, 0, 0, columnCount ) ) ;
        tsdHeader.SetCellText( rowData, 0, level + "階平面図" ) ;
        tsdHeader.SetCellStyle( rowData, 0, cellStyle ) ;
        rowData++ ;
        
        foreach ( var (detailSymbol, detailTableItemModelSameWithDetailSymbol) in detailTableItemModelsGroupByDetailSymbolItem ) {
          tsdHeader.MergeCells( new TableMergedCell( rowData, 0, rowData, columnCount ) ) ;
          tsdHeader.SetCellText( rowData, 0, detailSymbol ) ;
          tsdHeader.SetCellStyle( rowData, 0, cellStyle ) ;
          rowData++ ;
          foreach ( var rowDetailTableModelItem in detailTableItemModelSameWithDetailSymbol ) {
            var wireType = rowDetailTableModelItem.WireType + rowDetailTableModelItem.WireSize ;
            var wireStrip = string.IsNullOrEmpty( rowDetailTableModelItem.WireStrip ) || rowDetailTableModelItem.WireStrip == "-" ? "－" : "－" + rowDetailTableModelItem.WireStrip ;
            var wireBook = string.IsNullOrEmpty( rowDetailTableModelItem.WireBook ) ? "x1" : "x" + rowDetailTableModelItem.WireBook ;
            var (plumbingType, numberOfPlumbing) = GetPlumbingType( rowDetailTableModelItem.ConstructionClassification, rowDetailTableModelItem.PlumbingType, rowDetailTableModelItem.PlumbingSize, rowDetailTableModelItem.NumberOfPlumbing, ref isSetPipeForCoWindingWiring ) ;
            tsdHeader.SetCellText( rowData, 0, wireType ) ;
            tsdHeader.SetCellText( rowData, 1, wireStrip ) ;
            tsdHeader.SetCellText( rowData, 2, wireBook ) ;
            tsdHeader.SetCellText( rowData, 3, plumbingType ) ;
            tsdHeader.SetCellText( rowData, 4, numberOfPlumbing ) ;
            tsdHeader.SetCellText( rowData, 5, rowDetailTableModelItem.Remark ) ;

            if ( wireType.Length > maxCharOfWireTypeCell ) maxCharOfWireTypeCell = wireType.Length ;
            if ( plumbingType.Length > maxCharOfPlumbingTypeCell ) maxCharOfPlumbingTypeCell = plumbingType.Length ;
            if ( rowDetailTableModelItem.Remark.Length > maxCharOfRemarkCell ) maxCharOfRemarkCell = rowDetailTableModelItem.Remark.Length ;
            rowData++ ;
          }
        }
      }

      for ( var i = 0 ; i <= columnCount ; i++ ) {
        var columnWidth = i switch
        {
          0 when maxCharOfWireTypeCell > maxCharOfCell => minColumnWidth * Math.Ceiling( (double) maxCharOfWireTypeCell / maxCharOfCell ),
          3 when maxCharOfPlumbingTypeCell > maxCharOfCell => minColumnWidth * Math.Ceiling( (double) maxCharOfPlumbingTypeCell / maxCharOfCell ),
          5 when maxCharOfRemarkCell > maxCharOfCell => minColumnWidth * Math.Ceiling( (double) maxCharOfRemarkCell / maxCharOfCell ),
          _ => minColumnWidth
        } ;
        tsdHeader.SetColumnWidth( i, columnWidth ) ;
        tsdHeader.SetCellStyle( i, cellStyle ) ;
      }
      
      if ( isSetPipeForCoWindingWiring )
        MessageBox.Show( "施工区分「冷媒管共巻配線」の電線は配管が設定されているので、再度ご確認ください。", "Error" ) ;
    }

    private static ( string, string ) GetPlumbingType( string constructionClassification, string plumbingType, string plumbingSize, string numberOfPlumbing, ref bool isSetPipeForCoWindingWiring )
    {
      const string korogashi = "コロガシ" ;
      const string rack = "ラック" ;
      const string coil = "共巻" ;
      if ( plumbingType == DefaultChildPlumbingSymbol ) {
        plumbingSize = string.Empty ;
        numberOfPlumbing = string.Empty ;
      }
      else {
        plumbingType = plumbingType.Replace( DefaultChildPlumbingSymbol, "" ) ;
      }

      if ( constructionClassification == ConstructionClassificationType.天井隠蔽.GetFieldName() || constructionClassification == ConstructionClassificationType.打ち込み.GetFieldName() || constructionClassification == ConstructionClassificationType.露出.GetFieldName() || constructionClassification == ConstructionClassificationType.地中埋設.GetFieldName() ) {
        plumbingType = "(" + plumbingType + plumbingSize + ")" ;
        numberOfPlumbing = string.IsNullOrEmpty( numberOfPlumbing ) || numberOfPlumbing == "1" ? string.Empty : "x" + numberOfPlumbing ;
      }
      else if ( constructionClassification == ConstructionClassificationType.天井コロガシ.GetFieldName() || constructionClassification == ConstructionClassificationType.フリーアクセス.GetFieldName() ) {
        plumbingType = "(" + korogashi + ")" ;
        numberOfPlumbing = string.Empty ;
      }
      else if ( constructionClassification == ConstructionClassificationType.ケーブルラック配線.GetFieldName() ) {
        plumbingType = "(" + rack + ")" ;
        numberOfPlumbing = string.Empty ;
      }
      else if ( constructionClassification == ConstructionClassificationType.冷媒管共巻配線.GetFieldName() ) {
        if ( plumbingType != NoPlumping ) {
          isSetPipeForCoWindingWiring = true ;
        }
        plumbingType = "(" + coil + ")" ;
        numberOfPlumbing = string.Empty ;
      }
      else if ( constructionClassification == ConstructionClassificationType.導圧管類.GetFieldName() ) {
        plumbingType = string.IsNullOrEmpty( plumbingType ) ? string.Empty : "(" + plumbingType + plumbingSize + ")" ;
        numberOfPlumbing = string.IsNullOrEmpty( numberOfPlumbing ) || numberOfPlumbing == "1" ? string.Empty : "x" + numberOfPlumbing ;
      }
      else {
        plumbingType = string.Empty ;
        numberOfPlumbing = string.Empty ;
      }

      return ( plumbingType, numberOfPlumbing ) ;
    }

    private static ObservableCollection<DetailTableItemModel> SortDetailTableModel( ObservableCollection<DetailTableItemModel> detailTableModelItems, bool isMixConstructionItems )
    {
      var sortedDetailTableModelItemList = detailTableModelItems.ToList() ;
      var resultSortDetailModel = SortDetailModel( sortedDetailTableModelItemList, isMixConstructionItems ) ;
      return new ObservableCollection<DetailTableItemModel>( resultSortDetailModel ) ;
    }
    
     private static List<DetailTableItemModel> SortDetailModel( IEnumerable<DetailTableItemModel> detailTableModelItems, bool isMixConstructionItems )
     {
        List<DetailTableItemModel> sortedDetailTableItemModelList = new() ;
        var detailTableModelItemGroupByKey = detailTableModelItems.OrderBy( d => d.DetailSymbol ).GroupBy( GetKeyRouting ).Select( g => g.ToList() ) ;
        foreach ( var detailTableItemGroupByKey in detailTableModelItemGroupByKey ) {
          var signalTypes = (SignalType[]) Enum.GetValues( typeof( SignalType )) ;
          foreach ( var signalType in signalTypes ) {
            var detailTableItemWithSameSignalType = detailTableItemGroupByKey.Where( d => d.SignalType == signalType.GetFieldName() ).ToList() ;
            SortDetailTableRows( sortedDetailTableItemModelList, detailTableItemWithSameSignalType, isMixConstructionItems ) ;
          }

          var signalTypeNames = signalTypes.Select( s => s.GetFieldName() ) ;
          var detailTableRowsNotHaveSignalType = detailTableItemGroupByKey.Where( d => ! signalTypeNames.Contains( d.SignalType ) ).ToList() ;
          SortDetailTableRows( sortedDetailTableItemModelList, detailTableRowsNotHaveSignalType, isMixConstructionItems ) ;
        }

        return sortedDetailTableItemModelList ;
     }
    
    private static void SortDetailTableRows( List<DetailTableItemModel> sortedDetailTableItemModels, List<DetailTableItemModel> detailTableItemWithSameSignalTypes, bool isMixConstructionItems )
    {
      if ( ! isMixConstructionItems ) detailTableItemWithSameSignalTypes = detailTableItemWithSameSignalTypes.OrderBy( d => d.ConstructionItems ).ToList() ;
      var detailTableItemGroupByPlumbingIdentityInfos = detailTableItemWithSameSignalTypes.GroupBy( d => d.PlumbingIdentityInfo ).Select( g => g.ToList() ) ;
      foreach ( var detailTableItemGroupByPlumbingIdentityInfo in detailTableItemGroupByPlumbingIdentityInfos ) {
        var detailTableModelItems = 
            detailTableItemGroupByPlumbingIdentityInfo
              .OrderByDescending( x => x.IsParentRoute )
              .ThenBy( x => x.GroupId ) ;

        sortedDetailTableItemModels.AddRange( detailTableModelItems ) ;
      }
    }

    protected internal static void SetNoPlumbingDataForOneSymbol( List<DetailTableItemModel> detailTableItemModels, bool isMixConstructionItems )
    {
      var parentDetailRow = detailTableItemModels.First() ;
      var plumbingIdentityInfo = string.Empty ;
      foreach ( var detailTableItemModel in detailTableItemModels ) {
        if ( detailTableItemModel == parentDetailRow ) {
          detailTableItemModel.PlumbingType = NoPlumping ;
          detailTableItemModel.PlumbingSize = NoPlumbingSize ;
          detailTableItemModel.NumberOfPlumbing = string.Empty ;
          detailTableItemModel.IsParentRoute = true ;
          detailTableItemModel.IsReadOnly = false ;
          detailTableItemModel.IsMixConstructionItems = isMixConstructionItems ;
          detailTableItemModel.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
          detailTableItemModel.IsReadOnlyPlumbingSize = true ;
          plumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( detailTableItemModel, isMixConstructionItems ) ;
          detailTableItemModel.PlumbingIdentityInfo = plumbingIdentityInfo ;
        }
        else {
          detailTableItemModel.PlumbingType = DefaultChildPlumbingSymbol ;
          detailTableItemModel.PlumbingSize = DefaultChildPlumbingSymbol ;
          detailTableItemModel.NumberOfPlumbing = DefaultChildPlumbingSymbol ;
          detailTableItemModel.IsParentRoute = false ;
          detailTableItemModel.IsReadOnly = true ;
          detailTableItemModel.IsMixConstructionItems = isMixConstructionItems ;
          detailTableItemModel.IsReadOnlyPlumbingItems = true ;
          detailTableItemModel.IsReadOnlyPlumbingSize = true ;
          detailTableItemModel.PlumbingIdentityInfo = plumbingIdentityInfo ;
        }
      }
    }

    protected internal static void SetPlumbingData( List<ConduitsModel> conduitsModelData, ref List<DetailTableItemModel> detailTableItemModels, string plumbingType, bool isMixConstructionItems = false )
    {
      var detailTableItemGroupByPlumbingIdentityInfo = detailTableItemModels.GroupBy( d => d.PlumbingIdentityInfo ).Select( g => g.ToList() ) ;
      var detailTableRowsSinglePlumbing = new ObservableCollection<DetailTableItemModel>() ;
      foreach ( var detailTableItemWithSamePlumbing in detailTableItemGroupByPlumbingIdentityInfo ) {
        if ( detailTableItemWithSamePlumbing.Count == 1 ) {
          detailTableRowsSinglePlumbing.Add( detailTableItemWithSamePlumbing.First() ) ;
        }
        else {
          SetPlumbingDataForOneSymbol( conduitsModelData, detailTableItemWithSamePlumbing, plumbingType, true, isMixConstructionItems ) ;
        }
      }

      SetPlumbingDataForEachWiring( new List<DetailTableItemModel>(), conduitsModelData, ref detailTableRowsSinglePlumbing, plumbingType ) ;
    }

    private static void SetPlumbingDataForEachWiring( List<DetailTableItemModel> detailTableData, List<ConduitsModel> conduitsModelData, ref ObservableCollection<DetailTableItemModel> detailTableItemModels, string plumbingType, bool isFromCreateDetailTable = false )
    {
      var newDetailTableItems = new List<DetailTableItemModel>() ;
      foreach ( var detailTableItemModel in detailTableItemModels ) {
        const int plumbingCount = 1 ;
        var oldDetailTableItems = detailTableData.Where( d => GetKeyRouting(d) == GetKeyRouting( detailTableItemModel ) && d.RouteName == detailTableItemModel.RouteName ).ToList() ;
        if ( ! oldDetailTableItems.Any() ) {
          if ( isFromCreateDetailTable && ! string.IsNullOrEmpty( detailTableItemModel.PlumbingType ) ) plumbingType = detailTableItemModel.PlumbingType ;
          var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          if(!conduitsModels.Any())
            continue;

          var wireBook = int.TryParse( detailTableItemModel.WireBook, out var value ) ? value : 1 ;
          var currentPlumbingCrossSectionalArea = detailTableItemModel.WireCrossSectionalArea / Percentage * wireBook;
          var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ?? conduitsModels.Last() ;

          detailTableItemModel.PlumbingType = plumbing.PipingType ;
          detailTableItemModel.NumberOfPlumbing = plumbingCount.ToString() ;
          detailTableItemModel.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( detailTableItemModel, false ) ;
          detailTableItemModel.IsParentRoute = true ;
          detailTableItemModel.IsReadOnly = false ;
          detailTableItemModel.IsReadOnlyPlumbingItems = true ;
          var plumbingSizesOfPlumbingType = conduitsModels.Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableItemModel.PlumbingSizes = ( from plumbingSizeType in plumbingSizesOfPlumbingType select new DetailTableItemModel.ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
          detailTableItemModel.PlumbingSize = plumbing.Size.Replace("mm", "") ;
        }
        else {
          var oldDetailTableRow = oldDetailTableItems.First() ;
          detailTableItemModel.PlumbingType = oldDetailTableRow.PlumbingType ;
          detailTableItemModel.NumberOfPlumbing = oldDetailTableRow.NumberOfPlumbing ;
          detailTableItemModel.PlumbingIdentityInfo = oldDetailTableRow.PlumbingIdentityInfo ;
          detailTableItemModel.IsParentRoute = oldDetailTableRow.IsParentRoute ;
          detailTableItemModel.IsReadOnly = oldDetailTableRow.IsReadOnly ;
          detailTableItemModel.IsReadOnlyPlumbingItems = oldDetailTableRow.IsReadOnlyPlumbingItems ;
          detailTableItemModel.IsReadOnlyPlumbingSize = oldDetailTableRow.IsReadOnlyPlumbingSize ;
          var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == detailTableItemModel.PlumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableItemModel.PlumbingSizes = ( from plumbingSizeType in plumbingSizesOfPlumbingType select new DetailTableItemModel.ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
          detailTableItemModel.PlumbingSize = oldDetailTableRow.PlumbingSize ;
          if ( oldDetailTableItems.Count <= 1 ) continue ;
          oldDetailTableItems.Remove( oldDetailTableRow ) ;
          newDetailTableItems.AddRange( oldDetailTableItems ) ;
        }
      }

      foreach ( var newDetailTableRow in newDetailTableItems ) {
        detailTableItemModels.Add( newDetailTableRow ) ;
      }
    }
    
    protected internal static void SetPlumbingDataForOneSymbol( List<ConduitsModel> conduitsModelData, List<DetailTableItemModel> detailTableItemModelsByDetailSymbolId, string plumbingType, bool isPlumbingTypeHasBeenChanged, bool isMixConstructionItems)
    {
      const string noPlumpingConstructionClassification = "冷媒管共巻配線" ;
      var isParentDetailRowHasTypeNoPlumbing = false ;

      var parentDetailTable = detailTableItemModelsByDetailSymbolId.First() ;
      if ( parentDetailTable?.ConstructionClassification == noPlumpingConstructionClassification ) {
        isParentDetailRowHasTypeNoPlumbing = true ;
      }
      
      if ( ! isPlumbingTypeHasBeenChanged ) {
        parentDetailTable = isParentDetailRowHasTypeNoPlumbing ? detailTableItemModelsByDetailSymbolId.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) : parentDetailTable ;
        if ( parentDetailTable != null ) plumbingType = string.IsNullOrEmpty( parentDetailTable.PlumbingType ) ? plumbingType : parentDetailTable.PlumbingType.Replace( DefaultChildPlumbingSymbol, string.Empty ) ;
      }

      if ( plumbingType == NoPlumping ) return ;
      var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
      if(!conduitsModels.Any())
        return;
      
      var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
      var plumbingSizesOfPlumbingType = conduitsModels.Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
      var plumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableItemModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
      var detailTableItemModelBySignalTypes = isMixConstructionItems ? detailTableItemModelsByDetailSymbolId.GroupBy( d => d.SignalType ).Select( g =>  g.ToList()).ToList() : detailTableItemModelsByDetailSymbolId.GroupBy( d => new {d.SignalType, d.ConstructionItems} ).Select( g =>  g.ToList()).ToList();

      foreach ( var detailTableItemModels in detailTableItemModelBySignalTypes ) {
        var plumbingCount = 0 ;
        Dictionary<string, List<DetailTableItemModel>> detailTableItemGroupByPlumbingTypes = new() ;
        List<DetailTableItemModel> childDetailTableItemRows = new() ;
        parentDetailTable = detailTableItemModels.FirstOrDefault( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ;
        if ( parentDetailTable == null ) continue ;
        parentDetailTable.IsParentRoute = true ;
        parentDetailTable.IsReadOnly = false ;
        var currentPlumbingCrossSectionalArea = 0.0 ;
        foreach ( var currentDetailTableItem in detailTableItemModels ) {
          var wireBook = string.IsNullOrEmpty( currentDetailTableItem.WireBook ) ? 1 : int.Parse( currentDetailTableItem.WireBook ) ;
          if ( currentDetailTableItem.ConstructionClassification != noPlumpingConstructionClassification ) {
            currentPlumbingCrossSectionalArea += ( currentDetailTableItem.WireCrossSectionalArea / Percentage * wireBook ) ;

            if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
              var plumbing = conduitsModels.Last() ;
              if ( parentDetailTable != detailTableItemModels.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                parentDetailTable.IsParentRoute = false ;
                parentDetailTable.IsReadOnly = true ;
              }
              if ( isParentDetailRowHasTypeNoPlumbing && parentDetailTable == detailTableItemModels.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                parentDetailTable.PlumbingType = plumbingType ;
              }
              else {
                parentDetailTable.PlumbingType = parentDetailTable.IsParentRoute ? plumbingType : plumbingType + DefaultChildPlumbingSymbol ;
              }

              parentDetailTable.PlumbingSizes = plumbingSizes ;
              parentDetailTable.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
              parentDetailTable.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( parentDetailTable, isMixConstructionItems ) ;
              parentDetailTable.Remark = DetailTableViewModel.GetRemark( parentDetailTable.Remark, int.TryParse(parentDetailTable.WireBook, out var value) ? value : 1 ) ;
              parentDetailTable.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
              parentDetailTable.IsMixConstructionItems = isMixConstructionItems ;
              parentDetailTable.IsReadOnlyPlumbingSize = false ;
              if ( ! detailTableItemGroupByPlumbingTypes.ContainsKey( parentDetailTable.PlumbingIdentityInfo ) )
                detailTableItemGroupByPlumbingTypes.Add( parentDetailTable.PlumbingIdentityInfo, childDetailTableItemRows ) ;
              else {
                detailTableItemGroupByPlumbingTypes[ parentDetailTable.PlumbingIdentityInfo ].AddRange( childDetailTableItemRows ) ;
              }

              childDetailTableItemRows = new List<DetailTableItemModel>() ;
              var parentWireBook = string.IsNullOrEmpty( parentDetailTable.WireBook ) ? 1 : int.Parse( parentDetailTable.WireBook ) ;
              if ( parentWireBook > 1 && parentDetailTable.WireCrossSectionalArea / Percentage * wireBook > maxInnerCrossSectionalArea ) {
                var wireCountInPlumbing = (int) ( maxInnerCrossSectionalArea / ( currentDetailTableItem.WireCrossSectionalArea / Percentage ) ) ;
                plumbingCount += (int) Math.Ceiling( (double) wireBook / wireCountInPlumbing ) ;
              }
              else {
                plumbingCount++ ;
              }
              parentDetailTable = currentDetailTableItem ;
              currentPlumbingCrossSectionalArea = currentDetailTableItem.WireCrossSectionalArea / Percentage * wireBook ;
              if ( currentDetailTableItem != detailTableItemModels.Last( d => d.ConstructionClassification != noPlumpingConstructionClassification ) )
                continue ;
              if ( wireBook > 1 && currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
                plumbing = conduitsModels.LastOrDefault() ;
                var wireCountInPlumbing = (int) ( maxInnerCrossSectionalArea / ( currentDetailTableItem.WireCrossSectionalArea / Percentage ) ) ;
                plumbingCount += (int) Math.Ceiling( (double) wireBook / wireCountInPlumbing ) ;
              }
              else {
                plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
                plumbingCount++ ;
              }

              if ( currentDetailTableItem != detailTableItemModels.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                currentDetailTableItem.IsParentRoute = false ;
                currentDetailTableItem.IsReadOnly = true ;
              }
              if ( isParentDetailRowHasTypeNoPlumbing && currentDetailTableItem == detailTableItemModels.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                currentDetailTableItem.PlumbingType = plumbingType ;
              }
              else {
                currentDetailTableItem.PlumbingType = currentDetailTableItem == detailTableItemModels.First() ? plumbingType : plumbingType + DefaultChildPlumbingSymbol ;
              }
              
              currentDetailTableItem.PlumbingSizes = plumbingSizes ;
              if ( null != plumbing ) {
                currentDetailTableItem.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
              }
              currentDetailTableItem.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( currentDetailTableItem, isMixConstructionItems ) ;
              currentDetailTableItem.Remark = DetailTableViewModel.GetRemark( currentDetailTableItem.Remark, wireBook ) ;
              currentDetailTableItem.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
              currentDetailTableItem.IsMixConstructionItems = isMixConstructionItems ;
              currentDetailTableItem.IsReadOnlyPlumbingSize = false ;
            }
            else {
              if ( currentDetailTableItem == detailTableItemModels.Last( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
                if ( parentDetailTable != detailTableItemModels.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                  parentDetailTable.IsParentRoute = false ;
                  parentDetailTable.IsReadOnly = true ;
                }
                if ( isParentDetailRowHasTypeNoPlumbing && parentDetailTable == detailTableItemModels.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                  parentDetailTable.PlumbingType = plumbingType ;
                }
                else {
                  parentDetailTable.PlumbingType = parentDetailTable.IsParentRoute ? plumbingType : plumbingType + DefaultChildPlumbingSymbol ;
                }
                
                parentDetailTable.PlumbingSizes = plumbingSizes ;
                if ( null != plumbing ) {
                  parentDetailTable.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
                }
                parentDetailTable.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( parentDetailTable, isMixConstructionItems ) ;
                parentDetailTable.Remark = DetailTableViewModel.GetRemark( parentDetailTable.Remark, int.TryParse(parentDetailTable.WireBook, out var value) ? value : 1 ) ;
                parentDetailTable.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
                parentDetailTable.IsMixConstructionItems = isMixConstructionItems ;
                parentDetailTable.IsReadOnlyPlumbingSize = false ;
                if ( ! detailTableItemGroupByPlumbingTypes.ContainsKey( parentDetailTable.PlumbingIdentityInfo ) ) {
                  detailTableItemGroupByPlumbingTypes.Add( parentDetailTable.PlumbingIdentityInfo, childDetailTableItemRows ) ;
                }
                else {
                  detailTableItemGroupByPlumbingTypes[ parentDetailTable.PlumbingIdentityInfo ].AddRange( childDetailTableItemRows ) ;
                  detailTableItemGroupByPlumbingTypes[ parentDetailTable.PlumbingIdentityInfo ].Add( currentDetailTableItem ) ;
                }

                plumbingCount++ ;
              }

              if ( currentDetailTableItem == detailTableItemModels.FirstOrDefault( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) continue ;
              currentDetailTableItem.PlumbingType = DefaultChildPlumbingSymbol ;
              currentDetailTableItem.PlumbingSizes = plumbingSizes ;
              currentDetailTableItem.PlumbingSize = DefaultChildPlumbingSymbol ;
              currentDetailTableItem.NumberOfPlumbing = DefaultChildPlumbingSymbol ;
              currentDetailTableItem.Remark = DetailTableViewModel.GetRemark( currentDetailTableItem.Remark, wireBook ) ;
              currentDetailTableItem.IsReadOnlyPlumbingItems = true ;
              currentDetailTableItem.IsParentRoute = false ;
              currentDetailTableItem.IsReadOnly = true ;
              currentDetailTableItem.IsMixConstructionItems = isMixConstructionItems ;
              currentDetailTableItem.IsReadOnlyPlumbingSize = true ;
              childDetailTableItemRows.Add( currentDetailTableItem ) ;
            }
          }
          else {
            currentDetailTableItem.PlumbingType = NoPlumping ;
            currentDetailTableItem.PlumbingSizes = plumbingSizes ;
            currentDetailTableItem.PlumbingSize = NoPlumbingSize ;
            currentDetailTableItem.NumberOfPlumbing = string.Empty ;
            currentDetailTableItem.Remark = DetailTableViewModel.GetRemark( currentDetailTableItem.Remark, wireBook ) ;
            currentDetailTableItem.IsReadOnly = true ;
            currentDetailTableItem.IsReadOnlyPlumbingItems = true ;
            currentDetailTableItem.IsMixConstructionItems = false ;
            currentDetailTableItem.IsReadOnlyPlumbingSize = true ;
          }
        }

        foreach ( var (plumbingIdentityInfo, detailTableItemWithSamePlumbing) in detailTableItemGroupByPlumbingTypes ) {
          foreach ( var detailTableRow in detailTableItemWithSamePlumbing ) {
            detailTableRow.PlumbingIdentityInfo = plumbingIdentityInfo ;
          }
        }

        foreach ( var detailTableItem in detailTableItemModels.Where( d => d.PlumbingSize != DefaultChildPlumbingSymbol ).ToList() ) {
          detailTableItem.NumberOfPlumbing = plumbingCount.ToString() ;
        }
      }
    }

    public static string GetDetailTableRowPlumbingIdentityInfo( DetailTableItemModel detailTableItemModel, bool mixConstructionItems )
    {
      return mixConstructionItems ? 
        string.Join( "-", detailTableItemModel.PlumbingType + detailTableItemModel.PlumbingSize, detailTableItemModel.SignalType, detailTableItemModel.RouteName, GetKeyRouting(detailTableItemModel), detailTableItemModel.CopyIndex ) : 
        string.Join( "-", detailTableItemModel.PlumbingType + detailTableItemModel.PlumbingSize, detailTableItemModel.SignalType, detailTableItemModel.RouteName, GetKeyRouting(detailTableItemModel), detailTableItemModel.CopyIndex, detailTableItemModel.ConstructionItems ) ;
    }

    private void UpdateConnectorWithConstructionItem( Document document, Dictionary<string, string> routesChangedConstructionItem )
    {
      var allConnector = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).ToList() ;
      
      using Transaction transaction = new( document, "Change Parameter Connector" ) ;
      transaction.Start() ;
      
      foreach ( var (routeName, constructionItem) in routesChangedConstructionItem ) {
        var elements = GetToConnectorAndConduitOfRoute( document, allConnector, routeName ) ;
        foreach ( var element in elements ) {
          if(element.HasParameter(ElectricalRoutingElementParameter.ConstructionItem))
            element.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem ) ;
        }
      }

      transaction.Commit() ;
    }

    private void UpdateDetailSymbolPlumbingType( Document document, StorageService<Level, DetailSymbolModel> storageService, Dictionary<string, string> detailSymbolsChangedPlumbingType )
    {
      using Transaction transaction = new( document, "Update Detail Symbol Data" ) ;
      transaction.Start() ;
      foreach ( var (detailSymbolId, plumbingType) in detailSymbolsChangedPlumbingType ) {
        var detailSymbolItemModels = storageService.Data.DetailSymbolData.Where( d => d.DetailSymbolUniqueId == detailSymbolId ).ToList() ;
        foreach ( var detailSymbolItemModel in detailSymbolItemModels ) {
          detailSymbolItemModel.PlumbingType = plumbingType ;
        }
      }

      storageService.SaveChange() ;
      transaction.Commit() ;
    }

    private List<Element> GetToConnectorAndConduitOfRoute( Document document, IReadOnlyCollection<Element> allConnectors, string routeName )
    {
      var conduitsAndConnectorOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      foreach ( var conduit in conduitsAndConnectorOfRoute ) {
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toElementUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toElementUniqueId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toElementUniqueId ) ;
        if ( toConnector == null || toConnector.IsTerminatePoint() || toConnector.IsPassPoint() ) continue ;
        conduitsAndConnectorOfRoute.Add( toConnector ) ;
        return conduitsAndConnectorOfRoute ;
      }

      return conduitsAndConnectorOfRoute ;
    }

    private static string GetParentRouteName( Document document, List<string> routeNames )
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

    private static void AddDetailTableModelRow( Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData, ICollection<DetailTableItemModel> detailTableItemModels, List<Element> pickedObjects, DetailSymbolItemModel detailSymbolItemModel, bool isParentRoute, bool mixConstructionItems )
    {
      var element = pickedObjects.FirstOrDefault( p => p.UniqueId == detailSymbolItemModel.ConduitUniqueId ) ;
      string floor = doc.GetElementById<Level>( element!.GetLevelId() )?.Name ?? string.Empty ;
      string constructionItem = element!.LookupParameter( "Construction Item" ).AsString() ?? DefaultConstructionItems ;
      string isEcoMode = element.LookupParameter( "IsEcoMode" ).AsString() ;
      string plumbingType = detailSymbolItemModel.PlumbingType ;

      var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == detailSymbolItemModel.Code && x.GeneralDisplayDeviceSymbol == detailSymbolItemModel.DeviceSymbol ) ;
      if ( ceedModel != null && ! string.IsNullOrEmpty( ceedModel.CeedSetCode ) && ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ) {
        var ceedCode = ceedModel.CeedSetCode ;
        var remark = ceedModel.GeneralDisplayDeviceSymbol ;
        var hiroiCdModel = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetCdMasterEcoModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) : hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeedSetCode ) ;
        var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ).Skip( 1 ) ;
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
            var wireType = master.Type ;
            var wireSize = master.Size1 ;
            var wireStrip = string.IsNullOrEmpty( master.Size2 ) || master.Size2 == "0" ? "-" : master.Size2 ;
            var wiresAndCablesModel = wiresAndCablesModelData
              .FirstOrDefault( w => 
                w.WireType == wireType 
                && w.DiameterOrNominal == wireSize
                && ( ( w.NumberOfHeartsOrLogarithm == "0" && master.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && master.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel == null ) continue ;
            var signalType = wiresAndCablesModel.Classification ;
            var wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
            
            var wireSizesOfWireType = wiresAndCablesModelData
              .Where( w => w.WireType == wireType )
              .Select( w => w.DiameterOrNominal )
              .Distinct()
              .ToList() ;
            var wireSizes = wireSizesOfWireType.Any() ? 
              ( from wireSizeType in wireSizesOfWireType select new DetailTableItemModel.ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() 
              : new List<DetailTableItemModel.ComboboxItemType>() ;
            
            var wireStripsOfWireType = wiresAndCablesModelData
              .Where( w => w.WireType == wireType && w.DiameterOrNominal == wireSize )
              .Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP )
              .Distinct()
              .ToList() ;
            var wireStrips = wireStripsOfWireType.Any() ? 
              ( from wireStripType in wireStripsOfWireType select new DetailTableItemModel.ComboboxItemType( wireStripType, wireStripType ) ).ToList() 
              : new List<DetailTableItemModel.ComboboxItemType>() ;

            var plumbingItemTypes = new List<DetailTableItemModel.ComboboxItemType> { new( constructionItem, constructionItem ) } ;
            
            var detailTableItemModel = new DetailTableItemModel( 
              false, 
              floor, 
              ceedCode,
              detailSymbolItemModel.DetailSymbol,
              detailSymbolItemModel.DetailSymbolUniqueId, 
              detailSymbolItemModel.FromConnectorUniqueId, 
              detailSymbolItemModel.ToConnectorUniqueId, 
              wireType, 
              wireSize,
              wireStrip, 
              "1", 
              string.Empty, 
              string.Empty, 
              string.Empty,
              plumbingType, 
              string.Empty,
              string.Empty, 
              constructionClassification, 
              signalType, 
              constructionItem, 
              constructionItem,
              remark, 
              wireCrossSectionalArea, 
              detailSymbolItemModel.CountCableSamePosition,
              detailSymbolItemModel.RouteName,
              isEcoMode, isParentRoute, 
              ! isParentRoute, 
              string.Empty, 
              string.Empty, 
              true,
              mixConstructionItems, 
              string.Empty, 
              false, 
              false, 
              false,
              wireSizes,
              wireStrips, 
              new List<DetailTableItemModel.ComboboxItemType>(), 
              new List<DetailTableItemModel.ComboboxItemType>(), 
              plumbingItemTypes ) ;
            detailTableItemModels.Add( detailTableItemModel ) ;
          }
        }
      }
    }

    private static bool CheckMixConstructionItems( List<DetailTableItemModel> detailTableModelsData, List<string> keyRoutings )
    {
      var detailTableItemModelRowGroupMixConstructionItems = detailTableModelsData.FirstOrDefault( d => keyRoutings.Contains(GetKeyRouting(d)) && ! string.IsNullOrEmpty( d.GroupId ) && bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      var detailTableItemModelRowGroupNoMixConstructionItems = detailTableModelsData.FirstOrDefault( d => keyRoutings.Contains(GetKeyRouting(d)) && ! string.IsNullOrEmpty( d.GroupId ) && ! bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      return detailTableItemModelRowGroupNoMixConstructionItems == null && detailTableItemModelRowGroupMixConstructionItems != null ;
    }

    private class TextNotePickFilter : ISelectionFilter
    {
      private const string DetailSymbolType = "DetailSymbol-TNT" ;
      public bool AllowElement( Element element )
      {
        return element.GetBuiltInCategory() == BuiltInCategory.OST_TextNotes && element.Name.StartsWith( DetailSymbolType ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
    
    #region CreateDetailTableInCaseAddWiringInfo
    public static ( ObservableCollection<DetailTableItemModel>, bool, bool ) CreateDetailTableItemAddWiringInfo( Document doc, CsvStorable csvStorable, StorageService<Level, DetailSymbolModel> storageService, List<Element> conduits, List<string> elementIds, bool isReferenceDetailTableModels )
    {
      var detailTable = CreateDetailTableItem( doc, csvStorable, storageService, conduits, elementIds, isReferenceDetailTableModels ) ;
      return detailTable ;
    }

    #endregion
  }
}