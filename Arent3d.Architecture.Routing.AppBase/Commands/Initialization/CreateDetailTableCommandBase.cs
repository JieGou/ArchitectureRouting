using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class CreateDetailTableCommandBase : IExternalCommand
  {
    private const string DefaultConstructionItems = "未設定" ;
    private const string DefaultChildPlumbingSymbol = "↑" ;
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      const string defaultParentPlumbingType = "E" ;
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var csvStorable = doc.GetCsvStorable() ;
      var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
      var hiroiSetCdMasterEcoModelData = csvStorable.HiroiSetCdMasterEcoModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      var detailTableModelsData = doc.GetDetailTableStorable().DetailTableModelData ;
      var detailTableModels = new ObservableCollection<DetailTableModel>() ;
      var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      var cnsStorable = doc.GetCnsSettingStorable() ;
      try {
        var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ).ToList() ;
        var pickedObjectIds = pickedObjects.Select( p => p.UniqueId ).ToList() ;
        var allDetailSymbolIdsOnDetailTableModels = detailTableModelsData.Select( d => d.DetailSymbolId ).Distinct().ToHashSet() ;
        var detailSymbolIdsOnDetailTableModels = detailSymbolStorable.DetailSymbolModelData.Where( x => pickedObjectIds.Contains( x.ConduitId ) && allDetailSymbolIdsOnDetailTableModels.Contains( x.DetailSymbolId ) ).Select( d => d.DetailSymbolId ).Distinct().ToList() ;
        var isMixConstructionItems = detailSymbolIdsOnDetailTableModels.Any() && CheckMixConstructionItems( detailTableModelsData, detailSymbolIdsOnDetailTableModels ) ;
        var detailSymbolModelsByDetailSymbolId = 
          detailSymbolStorable.DetailSymbolModelData
            .Where( x => pickedObjectIds.Contains( x.ConduitId ) && ! allDetailSymbolIdsOnDetailTableModels.Contains( x.DetailSymbolId ) )
            .OrderBy( x => x.DetailSymbol )
            .ThenByDescending( x => x.DetailSymbolId )
            .ThenByDescending( x => x.IsParentSymbol )
            .GroupBy( x => x.DetailSymbolId, ( key, p ) => new { DetailSymbolId = key, DetailSymbolModels = p.ToList() } ) ;
        foreach ( var detailSymbolModelByDetailSymbolId in detailSymbolModelsByDetailSymbolId ) {
          var firstDetailSymbolModelByDetailSymbolId = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault() ;
          var routeNames = detailSymbolModelByDetailSymbolId.DetailSymbolModels.Select( d => d.RouteName ).Distinct().ToList() ;
          var parentRouteName = firstDetailSymbolModelByDetailSymbolId!.CountCableSamePosition == 1 ? firstDetailSymbolModelByDetailSymbolId.RouteName : GetParentRouteName( doc, routeNames ) ;
          if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
            var parentDetailSymbolModel = detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == parentRouteName ) ;
            AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, pickedObjects, parentDetailSymbolModel!, true, isMixConstructionItems ) ;
            routeNames = routeNames.Where( n => n != parentRouteName ).OrderByDescending( n => n ).ToList() ;
          }

          foreach ( var childDetailSymbolModel in from routeName in routeNames select detailSymbolModelByDetailSymbolId.DetailSymbolModels.FirstOrDefault( d => d.RouteName == routeName ) ) {
            AddDetailTableModelRow( doc, ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiSetCdMasterEcoModelData, hiroiSetMasterEcoModelData, hiroiMasterModelData, wiresAndCablesModelData, detailTableModelsData, detailTableModels, pickedObjects, childDetailSymbolModel, false, isMixConstructionItems ) ;
          }
        }

        if ( detailTableModels.Any() ) {
          SetPlumbingDataForEachWiring( detailTableModelsData, conduitsModelData, ref detailTableModels, defaultParentPlumbingType ) ;
        }

        if ( detailSymbolIdsOnDetailTableModels.Any() ) {
          var detailTableModelRowsOnDetailTableStorable = detailTableModelsData.Where( d => detailSymbolIdsOnDetailTableModels.Contains( d.DetailSymbolId ) ).ToList() ;
          foreach ( var detailTableRow in detailTableModelRowsOnDetailTableStorable ) {
            detailTableModels.Add( detailTableRow ) ;
          }
        }

        SortDetailTableModel( ref detailTableModels, isMixConstructionItems ) ;

        var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
        var conduitTypes = ( from conduitTypeName in conduitTypeNames select new DetailTableModel.ComboboxItemType( conduitTypeName, conduitTypeName ) ).ToList() ;
        conduitTypes.Add( new DetailTableModel.ComboboxItemType( NoPlumping, NoPlumping ) ) ;

        var constructionItemNames = cnsStorable.CnsSettingData.Select( d => d.CategoryName ).ToList() ;
        var constructionItems = constructionItemNames.Any() ? ( from constructionItemName in constructionItemNames select new DetailTableModel.ComboboxItemType( constructionItemName, constructionItemName ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() { new( DefaultConstructionItems, DefaultConstructionItems ) } ;

        var levelNames = doc.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).OrderBy( l => l.Elevation ).Select( l => l.Name ).ToList() ;
        var levels = ( from levelName in levelNames select new DetailTableModel.ComboboxItemType( levelName, levelName ) ).ToList() ;

        var wireTypeNames = wiresAndCablesModelData.Select( w => w.WireType ).Distinct().ToList() ;
        var wireTypes = ( from wireType in wireTypeNames select new DetailTableModel.ComboboxItemType( wireType, wireType ) ).ToList() ;

        var earthTypes = new List<DetailTableModel.ComboboxItemType>() { new( "IV", "IV" ), new( "EM-IE", "EM-IE" ) } ;

        var numbers = new List<DetailTableModel.ComboboxItemType>() ;
        for ( var i = 1 ; i <= 10 ; i++ ) {
          numbers.Add( new DetailTableModel.ComboboxItemType( i.ToString(), i.ToString() ) ) ;
        }

        var constructionClassificationTypeNames = hiroiSetCdMasterNormalModelData.Select( h => h.ConstructionClassification ).Distinct().ToList() ;
        var constructionClassificationTypes = ( from constructionClassification in constructionClassificationTypeNames select new DetailTableModel.ComboboxItemType( constructionClassification, constructionClassification ) ).ToList() ;

        var signalTypes = ( from signalType in (SignalType[]) Enum.GetValues( typeof( SignalType ) ) select new DetailTableModel.ComboboxItemType( signalType.GetFieldName(), signalType.GetFieldName() ) ).ToList() ;

        var viewModel = new DetailTableViewModel( detailTableModels, conduitTypes, constructionItems, levels, wireTypes, earthTypes, numbers, constructionClassificationTypes, signalTypes ) ;
        var dialog = new DetailTableDialog( doc, viewModel, conduitsModelData, wiresAndCablesModelData, isMixConstructionItems ) ;
        dialog.ShowDialog() ;

        if ( dialog.RoutesWithConstructionItemHasChanged.Any() ) {
          var connectorGroups = UpdateConnectorAndConduitConstructionItem( doc, dialog.RoutesWithConstructionItemHasChanged ) ;
          if ( connectorGroups.Any() ) {
            using Transaction transaction = new( doc, "Group connector" ) ;
            transaction.Start() ;
            foreach ( var (connectorId, textNoteIds) in connectorGroups ) {
              // create group for updated connector (with new property) and related text note if any
              List<ElementId> groupIds = new() { connectorId } ;
              groupIds.AddRange( textNoteIds ) ;
              doc.Create.NewGroup( groupIds ) ;
            }

            transaction.Commit() ;
          }
        }

        if ( dialog.DetailSymbolIdsWithPlumbingTypeHasChanged.Any() ) {
          UpdateDetailSymbolPlumbingType( doc, detailSymbolStorable, dialog.DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
        }
        
        if ( dialog.DialogResult ?? false ) {
          return doc.Transaction( "TransactionName.Commands.Routing.CreateDetailTable".GetAppStringByKeyOrDefault( "Set detail table" ), _ =>
          {
            if ( ! viewModel.IsCreateDetailTableOnFloorPlanView && ! dialog.DetailTableViewModelSummary.IsCreateDetailTableOnFloorPlanView ) return Result.Succeeded ;
            var level = uiDoc.ActiveView.GenLevel ;
            var detailTableData = viewModel.IsCreateDetailTableOnFloorPlanView ? viewModel.DetailTableModels : dialog.DetailTableViewModelSummary.DetailTableModels ;
            CreateDetailTableSchedule( doc, detailTableData, level.Name ) ;
            viewModel.IsCreateDetailTableOnFloorPlanView = false ;
            dialog.DetailTableViewModelSummary.IsCreateDetailTableOnFloorPlanView = false ;

            return Result.Succeeded ;
          } ) ;
        }

        return Result.Succeeded ;
      }
      catch {
        return Result.Cancelled ;
      }
    }

    public enum SignalType
    {
      伝送幹線,
      低電圧,
      小勢力,
      動力
    }

    private enum ConstructionClassificationType
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

    private static void CreateDetailTableSchedule( Document document, IReadOnlyCollection<DetailTableModel> detailTableModels, string level )
    {
      string scheduleName = "Revit.Detail.Table.Name".GetDocumentStringByKeyOrDefault( document, "Detail Table" ) + DateTime.Now.ToString( " yyyy-MM-dd HH-mm-ss" ) ;
      var detailTable = document.GetAllElements<ViewSchedule>().SingleOrDefault( v => v.Name.Contains( scheduleName ) ) ;
      if ( detailTable == null ) {
        detailTable = ViewSchedule.CreateSchedule( document, new ElementId( BuiltInCategory.OST_Conduit ) ) ;
        detailTable.Name = scheduleName ;
        detailTable.TrySetProperty( ElectricalRoutingElementParameter.ScheduleBaseName, scheduleName ) ;
      }

      InsertDetailTableDataIntoSchedule( detailTable, detailTableModels, level ) ;
      MessageBox.Show( "集計表 \"" + scheduleName + "\" を作成しました", "Message" ) ;
    }

    private static void InsertDetailTableDataIntoSchedule( ViewSchedule viewSchedule, IReadOnlyCollection<DetailTableModel> detailTableModels, string level )
    {
      const int columnCount = 5 ;
      const int maxCharOfCell = 4 ;
      const double minColumnWidth = 0.05 ;
      var rowData = 1 ;
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

      var detailTableModelsGroupByDetailSymbol = detailTableModels.GroupBy( d => d.DetailSymbol ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      var rowCount = detailTableModels.Count + detailTableModelsGroupByDetailSymbol.Count ;
      for ( var i = 0 ; i < rowCount ; i++ ) {
        tsdHeader.InsertRow( tsdHeader.FirstRowNumber ) ;
      }

      tsdHeader.MergeCells( new TableMergedCell( 0, 0, 0, columnCount ) ) ;
      tsdHeader.SetCellText( 0, 0, level + "階平面図" ) ;
      tsdHeader.SetCellStyle( 0, 0, cellStyle ) ;

      var isSetPipeForCoWindingWiring = false ;
      foreach ( var (detailSymbol, detailTableModelsSameWithDetailSymbol) in detailTableModelsGroupByDetailSymbol ) {
        tsdHeader.MergeCells( new TableMergedCell( rowData, 0, rowData, columnCount ) ) ;
        tsdHeader.SetCellText( rowData, 0, detailSymbol ) ;
        tsdHeader.SetCellStyle( rowData, 0, cellStyle ) ;
        rowData++ ;
        foreach ( var rowDetailTableModel in detailTableModelsSameWithDetailSymbol ) {
          var wireType = rowDetailTableModel.WireType + rowDetailTableModel.WireSize ;
          var wireStrip = string.IsNullOrEmpty( rowDetailTableModel.WireStrip ) ? string.Empty : "－" + rowDetailTableModel.WireStrip ;
          var (plumbingType, numberOfPlumbing) = GetPlumbingType( rowDetailTableModel.ConstructionClassification, rowDetailTableModel.PlumbingType, rowDetailTableModel.PlumbingSize, rowDetailTableModel.NumberOfPlumbing, ref isSetPipeForCoWindingWiring ) ;
          tsdHeader.SetCellText( rowData, 0, wireType ) ;
          tsdHeader.SetCellText( rowData, 1, wireStrip ) ;
          tsdHeader.SetCellText( rowData, 2, "x" + rowDetailTableModel.WireBook ) ;
          tsdHeader.SetCellText( rowData, 3, plumbingType ) ;
          tsdHeader.SetCellText( rowData, 4, numberOfPlumbing ) ;
          tsdHeader.SetCellText( rowData, 5, rowDetailTableModel.Remark ) ;

          if ( plumbingType.Length > maxCharOfPlumbingTypeCell ) maxCharOfPlumbingTypeCell = plumbingType.Length ;
          if ( rowDetailTableModel.Remark.Length > maxCharOfRemarkCell ) maxCharOfRemarkCell = rowDetailTableModel.Remark.Length ;
          rowData++ ;
        }
      }

      for ( var i = 0 ; i <= columnCount ; i++ ) {
        var columnWidth = i switch
        {
          0 => minColumnWidth * 2,
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

    private static void SortDetailTableModel( ref ObservableCollection<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      List<DetailTableModel> sortedDetailTableModelsList = detailTableModels.ToList() ;
      DetailTableViewModel.SortDetailTableModel( ref sortedDetailTableModelsList, isMixConstructionItems ) ;
      detailTableModels = new ObservableCollection<DetailTableModel>( sortedDetailTableModelsList ) ;
    }

    protected internal static void SetNoPlumbingDataForOneSymbol( List<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      var parentDetailRow = detailTableModels.First() ;
      var plumbingIdentityInfo = string.Empty ;
      foreach ( var detailTableRow in detailTableModels ) {
        if ( detailTableRow == parentDetailRow ) {
          detailTableRow.PlumbingType = NoPlumping ;
          detailTableRow.PlumbingSize = NoPlumbingSize ;
          detailTableRow.NumberOfPlumbing = string.Empty ;
          detailTableRow.IsParentRoute = true ;
          detailTableRow.IsReadOnly = false ;
          detailTableRow.IsMixConstructionItems = isMixConstructionItems ;
          detailTableRow.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
          detailTableRow.IsReadOnlyPlumbingSize = true ;
          plumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( detailTableRow, isMixConstructionItems ) ;
          detailTableRow.PlumbingIdentityInfo = plumbingIdentityInfo ;
        }
        else {
          detailTableRow.PlumbingType = DefaultChildPlumbingSymbol ;
          detailTableRow.PlumbingSize = DefaultChildPlumbingSymbol ;
          detailTableRow.NumberOfPlumbing = DefaultChildPlumbingSymbol ;
          detailTableRow.IsParentRoute = false ;
          detailTableRow.IsReadOnly = true ;
          detailTableRow.IsMixConstructionItems = isMixConstructionItems ;
          detailTableRow.IsReadOnlyPlumbingItems = true ;
          detailTableRow.IsReadOnlyPlumbingSize = true ;
          detailTableRow.PlumbingIdentityInfo = plumbingIdentityInfo ;
        }
      }
    }

    protected internal static void SetPlumbingData( List<ConduitsModel> conduitsModelData, ref List<DetailTableModel> detailTableModels, string plumbingType, bool isMixConstructionItems = false )
    {
      var detailTableRowsGroupByPlumbingIdentityInfo = detailTableModels.GroupBy( d => d.PlumbingIdentityInfo ).Select( g => g.ToList() ) ;
      var detailTableRowsSinglePlumbing = new ObservableCollection<DetailTableModel>() ;
      foreach ( var detailTableRowsWithSamePlumbing in detailTableRowsGroupByPlumbingIdentityInfo ) {
        if ( detailTableRowsWithSamePlumbing.Count == 1 ) {
          detailTableRowsSinglePlumbing.Add( detailTableRowsWithSamePlumbing.First() ) ;
        }
        else {
          SetPlumbingDataForOneSymbol( conduitsModelData, detailTableRowsWithSamePlumbing, plumbingType, true, isMixConstructionItems ) ;
        }
      }

      SetPlumbingDataForEachWiring( new List<DetailTableModel>(), conduitsModelData, ref detailTableRowsSinglePlumbing, plumbingType ) ;
    }

    private static void SetPlumbingDataForEachWiring( List<DetailTableModel> detailTableModelData, List<ConduitsModel> conduitsModelData, ref ObservableCollection<DetailTableModel> detailTableModels, string plumbingType )
    {
      const double percentage = 0.32 ;
      var newDetailTableRows = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in detailTableModels ) {
        const int plumbingCount = 1 ;
        var oldDetailTableRows = detailTableModelData.Where( d => d.DetailSymbolId == detailTableRow.DetailSymbolId && d.RouteName == detailTableRow.RouteName ).ToList() ;
        if ( ! oldDetailTableRows.Any() ) {
          var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
          var currentPlumbingCrossSectionalArea = detailTableRow.WireCrossSectionalArea / percentage ;
          if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
            var plumbing = conduitsModels.Last() ;
            detailTableRow.PlumbingType = plumbingType ;
            detailTableRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
          }
          else {
            var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
            detailTableRow.PlumbingType = plumbingType ;
            detailTableRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
          }

          detailTableRow.NumberOfPlumbing = plumbingCount.ToString() ;
          detailTableRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( detailTableRow, false ) ;
          detailTableRow.IsParentRoute = true ;
          detailTableRow.IsReadOnly = false ;
          detailTableRow.IsReadOnlyPlumbingItems = true ;
          var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableRow.PlumbingSizes = ( from plumbingSizeType in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
        }
        else {
          var oldDetailTableRow = oldDetailTableRows.First() ;
          detailTableRow.PlumbingType = oldDetailTableRow.PlumbingType ;
          detailTableRow.PlumbingSize = oldDetailTableRow.PlumbingSize ;
          detailTableRow.NumberOfPlumbing = oldDetailTableRow.NumberOfPlumbing ;
          detailTableRow.PlumbingIdentityInfo = oldDetailTableRow.PlumbingIdentityInfo ;
          detailTableRow.IsParentRoute = oldDetailTableRow.IsParentRoute ;
          detailTableRow.IsReadOnly = oldDetailTableRow.IsReadOnly ;
          detailTableRow.IsReadOnlyPlumbingItems = oldDetailTableRow.IsReadOnlyPlumbingItems ;
          detailTableRow.IsReadOnlyPlumbingSize = oldDetailTableRow.IsReadOnlyPlumbingSize ;
          var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == detailTableRow.PlumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableRow.PlumbingSizes = ( from plumbingSizeType in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSizeType, plumbingSizeType ) ).ToList() ;
          if ( oldDetailTableRows.Count <= 1 ) continue ;
          oldDetailTableRows.Remove( oldDetailTableRow ) ;
          newDetailTableRows.AddRange( oldDetailTableRows ) ;
        }
      }

      foreach ( var newDetailTableRow in newDetailTableRows ) {
        detailTableModels.Add( newDetailTableRow ) ;
      }
    }
    
    protected internal static void SetPlumbingDataForOneSymbol( List<ConduitsModel> conduitsModelData, List<DetailTableModel> detailTableModelsByDetailSymbolId, string plumbingType, bool isPlumbingTypeHasBeenChanged, bool isMixConstructionItems)
    {
      const string noPlumpingConstructionClassification = "冷媒管共巻配線" ;
      const double percentage = 0.32 ;
      var isParentDetailRowHasTypeNoPlumbing = false ;

      var parentDetailRow = detailTableModelsByDetailSymbolId.First() ;
      if ( parentDetailRow?.ConstructionClassification == noPlumpingConstructionClassification ) {
        isParentDetailRowHasTypeNoPlumbing = true ;
      }
      
      if ( ! isPlumbingTypeHasBeenChanged ) {
        parentDetailRow = isParentDetailRowHasTypeNoPlumbing ? detailTableModelsByDetailSymbolId.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) : parentDetailRow ;
        if ( parentDetailRow != null ) plumbingType = string.IsNullOrEmpty( parentDetailRow.PlumbingType ) ? plumbingType : parentDetailRow.PlumbingType.Replace( DefaultChildPlumbingSymbol, string.Empty ) ;
      }

      if ( plumbingType == NoPlumping ) return ;
      var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
      var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
      var detailTableModelsBySignalType = isMixConstructionItems ? detailTableModelsByDetailSymbolId.GroupBy( d => d.SignalType ).Select( g =>  g.ToList()).ToList() : detailTableModelsByDetailSymbolId.GroupBy( d => new {d.SignalType, d.ConstructionItems} ).Select( g =>  g.ToList()).ToList();

      foreach ( var detailTableRows in detailTableModelsBySignalType ) {
        var plumbingCount = 0 ;
        Dictionary<string, List<DetailTableModel>> detailTableRowsGroupByPlumbingType = new() ;
        List<DetailTableModel> childDetailRows = new() ;
        parentDetailRow = detailTableRows.FirstOrDefault( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ;
        if ( parentDetailRow == null ) continue ;
        parentDetailRow.IsParentRoute = true ;
        parentDetailRow.IsReadOnly = false ;
        var currentPlumbingCrossSectionalArea = 0.0 ;
        foreach ( var currentDetailTableRow in detailTableRows ) {
          if ( currentDetailTableRow.ConstructionClassification != noPlumpingConstructionClassification ) {
            currentPlumbingCrossSectionalArea += ( currentDetailTableRow.WireCrossSectionalArea / percentage * int.Parse( currentDetailTableRow.WireBook ) ) ;

            if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
              var plumbing = conduitsModels.Last() ;
              if ( parentDetailRow != detailTableRows.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                parentDetailRow.IsParentRoute = false ;
                parentDetailRow.IsReadOnly = true ;
              }
              if ( isParentDetailRowHasTypeNoPlumbing && parentDetailRow == detailTableRows.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                parentDetailRow.PlumbingType = plumbingType ;
              }
              else {
                parentDetailRow.PlumbingType = parentDetailRow.IsParentRoute ? plumbingType : plumbingType + DefaultChildPlumbingSymbol ;
              }
              parentDetailRow.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
              parentDetailRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( parentDetailRow, isMixConstructionItems ) ;
              parentDetailRow.Remark = DetailTableViewModel.GetRemark( parentDetailRow.Remark, int.Parse( parentDetailRow.WireBook ) ) ;
              parentDetailRow.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
              parentDetailRow.IsMixConstructionItems = isMixConstructionItems ;
              parentDetailRow.IsReadOnlyPlumbingSize = false ;
              if ( ! detailTableRowsGroupByPlumbingType.ContainsKey( parentDetailRow.PlumbingIdentityInfo ) )
                detailTableRowsGroupByPlumbingType.Add( parentDetailRow.PlumbingIdentityInfo, childDetailRows ) ;
              else {
                detailTableRowsGroupByPlumbingType[ parentDetailRow.PlumbingIdentityInfo ].AddRange( childDetailRows ) ;
              }

              childDetailRows = new List<DetailTableModel>() ;
              plumbingCount++ ;
              parentDetailRow = currentDetailTableRow ;
              currentPlumbingCrossSectionalArea = currentDetailTableRow.WireCrossSectionalArea / percentage ;
              if ( currentDetailTableRow != detailTableRows.Last(  d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) continue ;
              plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea - currentDetailTableRow.WireCrossSectionalArea ) ;
              if ( currentDetailTableRow != detailTableRows.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                currentDetailTableRow.IsParentRoute = false ;
                currentDetailTableRow.IsReadOnly = true ;
              }
              if ( isParentDetailRowHasTypeNoPlumbing && currentDetailTableRow == detailTableRows.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                currentDetailTableRow.PlumbingType = plumbingType ;
              }
              else {
                currentDetailTableRow.PlumbingType = currentDetailTableRow == detailTableRows.First() ? plumbingType : plumbingType + DefaultChildPlumbingSymbol ;
              }
              currentDetailTableRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
              currentDetailTableRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( currentDetailTableRow, isMixConstructionItems ) ;
              currentDetailTableRow.Remark = DetailTableViewModel.GetRemark( currentDetailTableRow.Remark, int.Parse( currentDetailTableRow.WireBook ) ) ;
              currentDetailTableRow.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
              currentDetailTableRow.IsMixConstructionItems = isMixConstructionItems ;
              currentDetailTableRow.IsReadOnlyPlumbingSize = false ;
              plumbingCount++ ;
            }
            else {
              if ( currentDetailTableRow == detailTableRows.Last( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
                if ( parentDetailRow != detailTableRows.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                  parentDetailRow.IsParentRoute = false ;
                  parentDetailRow.IsReadOnly = true ;
                }
                if ( isParentDetailRowHasTypeNoPlumbing && parentDetailRow == detailTableRows.First( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) {
                  parentDetailRow.PlumbingType = plumbingType ;
                }
                else {
                  parentDetailRow.PlumbingType = parentDetailRow.IsParentRoute ? plumbingType : plumbingType + DefaultChildPlumbingSymbol ;
                }
                parentDetailRow.PlumbingSize = plumbing!.Size.Replace( "mm", "" ) ;
                parentDetailRow.PlumbingIdentityInfo = GetDetailTableRowPlumbingIdentityInfo( parentDetailRow, isMixConstructionItems ) ;
                parentDetailRow.Remark = DetailTableViewModel.GetRemark( parentDetailRow.Remark, int.Parse( parentDetailRow.WireBook ) ) ;
                parentDetailRow.IsReadOnlyPlumbingItems = ! isMixConstructionItems ;
                parentDetailRow.IsMixConstructionItems = isMixConstructionItems ;
                parentDetailRow.IsReadOnlyPlumbingSize = false ;
                if ( ! detailTableRowsGroupByPlumbingType.ContainsKey( parentDetailRow.PlumbingIdentityInfo ) ) {
                  detailTableRowsGroupByPlumbingType.Add( parentDetailRow.PlumbingIdentityInfo, childDetailRows ) ;
                }
                else {
                  detailTableRowsGroupByPlumbingType[ parentDetailRow.PlumbingIdentityInfo ].AddRange( childDetailRows ) ;
                  detailTableRowsGroupByPlumbingType[ parentDetailRow.PlumbingIdentityInfo ].Add( currentDetailTableRow ) ;
                }

                plumbingCount++ ;
              }

              if ( currentDetailTableRow == detailTableRows.FirstOrDefault( d => d.ConstructionClassification != noPlumpingConstructionClassification ) ) continue ;
              currentDetailTableRow.PlumbingType = DefaultChildPlumbingSymbol ;
              currentDetailTableRow.PlumbingSize = DefaultChildPlumbingSymbol ;
              currentDetailTableRow.NumberOfPlumbing = DefaultChildPlumbingSymbol ;
              currentDetailTableRow.Remark = DetailTableViewModel.GetRemark( currentDetailTableRow.Remark, int.Parse( currentDetailTableRow.WireBook ) ) ;
              currentDetailTableRow.IsReadOnlyPlumbingItems = true ;
              currentDetailTableRow.IsParentRoute = false ;
              currentDetailTableRow.IsReadOnly = true ;
              currentDetailTableRow.IsMixConstructionItems = isMixConstructionItems ;
              currentDetailTableRow.IsReadOnlyPlumbingSize = true ;
              childDetailRows.Add( currentDetailTableRow ) ;
            }
          }
          else {
            currentDetailTableRow.PlumbingType = NoPlumping ;
            currentDetailTableRow.PlumbingSize = NoPlumbingSize ;
            currentDetailTableRow.NumberOfPlumbing = string.Empty ;
            currentDetailTableRow.Remark = DetailTableViewModel.GetRemark( currentDetailTableRow.Remark, int.Parse( currentDetailTableRow.WireBook ) ) ;
            currentDetailTableRow.IsReadOnly = true ;
            currentDetailTableRow.IsReadOnlyPlumbingItems = true ;
            currentDetailTableRow.IsMixConstructionItems = false ;
            currentDetailTableRow.IsReadOnlyPlumbingSize = true ;
          }
        }

        foreach ( var (plumbingIdentityInfo, detailTableRowsWithSamePlumbing) in detailTableRowsGroupByPlumbingType ) {
          foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
            detailTableRow.PlumbingIdentityInfo = plumbingIdentityInfo ;
          }
        }

        foreach ( var detailTableRow in detailTableRows.Where( d => d.PlumbingSize != DefaultChildPlumbingSymbol ).ToList() ) {
          detailTableRow.NumberOfPlumbing = plumbingCount.ToString() ;
        }
      }
    }

    public static string GetDetailTableRowPlumbingIdentityInfo( DetailTableModel detailTableRow, bool mixConstructionItems )
    {
      return mixConstructionItems ? 
        string.Join( "-", detailTableRow.PlumbingType + detailTableRow.PlumbingSize, detailTableRow.SignalType, detailTableRow.RouteName, detailTableRow.DetailSymbolId, detailTableRow.CopyIndex ) : 
        string.Join( "-", detailTableRow.PlumbingType + detailTableRow.PlumbingSize, detailTableRow.SignalType, detailTableRow.RouteName, detailTableRow.DetailSymbolId, detailTableRow.CopyIndex, detailTableRow.ConstructionItems ) ;
    }

    private Dictionary<ElementId, List<ElementId>> UpdateConnectorAndConduitConstructionItem( Document document, Dictionary<string, string> routesChangedConstructionItem )
    {
      Dictionary<ElementId, List<ElementId>> connectorGroups = new() ;
      List<Element> allConnector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).ToList() ;
      using Transaction transaction = new( document, "Group connector" ) ;
      transaction.Start() ;
      foreach ( var (routeName, constructionItem) in routesChangedConstructionItem ) {
        var elements = GetToConnectorAndConduitOfRoute( document, allConnector, routeName ) ;
        foreach ( var element in elements ) {
          if ( document.GetElement( element.GroupId ) is Group parentGroup ) {
            // ungroup before set property
            var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
            List<ElementId> listTextNoteIds = new() ;
            // ungroup textNote before ungroup connector
            foreach ( var group in attachedGroup ) {
              var ids = group.GetMemberIds() ;
              listTextNoteIds.AddRange( ids ) ;
              group.UngroupMembers() ;
            }

            connectorGroups.Add( element.Id, listTextNoteIds ) ;
            parentGroup.UngroupMembers() ;
          }

          element.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, constructionItem ) ;
        }
      }

      transaction.Commit() ;

      return connectorGroups ;
    }

    private void UpdateDetailSymbolPlumbingType( Document document, DetailSymbolStorable detailSymbolStorable, Dictionary<string, string> detailSymbolsChangedPlumbingType )
    {
      using Transaction transaction = new( document, "Update Detail Symbol Data" ) ;
      transaction.Start() ;
      foreach ( var (detailSymbolId, plumbingType) in detailSymbolsChangedPlumbingType ) {
        var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( d => d.DetailSymbolId == detailSymbolId ).ToList() ;
        foreach ( var detailSymbolModel in detailSymbolModels ) {
          detailSymbolModel.PlumbingType = plumbingType ;
        }
      }

      detailSymbolStorable.Save() ;
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

    private void AddDetailTableModelRow( Document doc, CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiSetCdMasterModel> hiroiSetCdMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<DetailTableModel> detailTableModelsData, ICollection<DetailTableModel> detailTableModels, List<Element> pickedObjects, DetailSymbolModel detailSymbolModel, bool isParentRoute, bool mixConstructionItems )
    {
      var element = pickedObjects.FirstOrDefault( p => p.UniqueId == detailSymbolModel.ConduitId ) ;
      string floor = doc.GetElementById<Level>( element!.GetLevelId() )?.Name ?? string.Empty ;
      string constructionItem = element!.LookupParameter( "Construction Item" ).AsString() ?? DefaultConstructionItems ;
      string isEcoMode = element.LookupParameter( "IsEcoMode" ).AsString() ;
      string plumbingType = detailSymbolModel.PlumbingType ;

      var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == detailSymbolModel.Code && x.GeneralDisplayDeviceSymbol == detailSymbolModel.DeviceSymbol ) ;
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
            var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == wireType && w.DiameterOrNominal == wireSize && ( ( w.NumberOfHeartsOrLogarithm == "0" && master.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && master.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
            if ( wiresAndCablesModel == null ) continue ;
            var signalType = wiresAndCablesModel.Classification ;
            var wireCrossSectionalArea = double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
            
            var wireSizesOfWireType = wiresAndCablesModelData.Where( w => w.WireType == wireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
            var wireSizes = wireSizesOfWireType.Any() ? ( from wireSizeType in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
            
            var wireStripsOfWireType = wiresAndCablesModelData.Where( w => w.WireType == wireType && w.DiameterOrNominal == wireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
            var wireStrips = wireStripsOfWireType.Any() ? ( from wireStripType in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStripType, wireStripType ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;

            var plumbingItemTypes = new List<DetailTableModel.ComboboxItemType> { new( constructionItem, constructionItem ) } ;
            
            var detailTableRow = new DetailTableModel( false, floor, ceedCode, detailSymbolModel.DetailSymbol, detailSymbolModel.DetailSymbolId, wireType, wireSize, wireStrip, "1", 
              string.Empty, string.Empty, string.Empty, plumbingType, string.Empty, string.Empty, constructionClassification, signalType, 
              constructionItem, constructionItem, remark, wireCrossSectionalArea, detailSymbolModel.CountCableSamePosition, detailSymbolModel.RouteName, isEcoMode, isParentRoute, ! isParentRoute, 
              string.Empty, string.Empty, true, mixConstructionItems, string.Empty, false, false, false, wireSizes, wireStrips, new List<DetailTableModel.ComboboxItemType>(), new List<DetailTableModel.ComboboxItemType>(), plumbingItemTypes ) ;
            detailTableModels.Add( detailTableRow ) ;
          }
        }
      }
    }

    private bool CheckMixConstructionItems( List<DetailTableModel> detailTableModelsData, List<string> detailSymbolIds )
    {
      var detailTableModelRowGroupMixConstructionItems = detailTableModelsData.FirstOrDefault( d => detailSymbolIds.Contains( d.DetailSymbolId ) && ! string.IsNullOrEmpty( d.GroupId ) && bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      var detailTableModelRowGroupNoMixConstructionItems = detailTableModelsData.FirstOrDefault( d => detailSymbolIds.Contains( d.DetailSymbolId ) && ! string.IsNullOrEmpty( d.GroupId ) && ! bool.Parse( d.GroupId.Split( '-' ).First() ) ) ;
      return detailTableModelRowGroupNoMixConstructionItems == null && detailTableModelRowGroupMixConstructionItems != null ;
    }
  }
}