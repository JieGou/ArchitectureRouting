using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DetailTableViewModel : ViewModelBase
  {
    private const string DefaultParentPlumbingType = "E" ;
    private const string NoPlumping = "配管なし" ;

    public ObservableCollection<DetailTableModel> DetailTableModels { get ; set ; }
    
    public ObservableCollection<DetailTableModel> ReferenceDetailTableModels { get ; set ; }

    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }
    
    public  bool IsCancelCreateDetailTable { get; set; }
    
    public bool IsAddReference { get ; set ; }

    public ICommand SaveDetailTableCommand { get; set; }

    public ICommand SaveAndCreateDetailTableCommand { get ; set ; }

    public List<CreateDetailTableCommandBase.ComboboxItemType> ConduitTypes { get ; set ; }

    public List<CreateDetailTableCommandBase.ComboboxItemType> ConstructionItems { get ; set ; }

    public DetailTableViewModel( ObservableCollection<DetailTableModel> detailTableModels, ObservableCollection<DetailTableModel> referenceDetailTableModels, List<CreateDetailTableCommandBase.ComboboxItemType> conduitTypes, List<CreateDetailTableCommandBase.ComboboxItemType> constructionItems )
    {
      DetailTableModels = detailTableModels ;
      ReferenceDetailTableModels = referenceDetailTableModels ;
      IsCreateDetailTableOnFloorPlanView = false ;
      IsAddReference = false ;

      SaveDetailTableCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { SaveDetailTable() ; } // Execute()
      ) ;

      SaveAndCreateDetailTableCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { SaveAndCreateDetailTable() ; } // Execute()
      ) ;

      ConduitTypes = conduitTypes ;
      ConstructionItems = constructionItems ;
    }

    private void SaveDetailTable()
    {
      const string defaultConstructionItems = "未設定" ;
      // Configure open file dialog box
      SaveFileDialog dlg = new() { FileName = string.Empty, DefaultExt = ".ctl", Filter = "CTL files|*.ctl" } ;

      // Show open file dialog box
      var result = dlg.ShowDialog() ;

      // Process open file dialog box results
      if ( result != DialogResult.OK ) return ;
      string createText = string.Empty ;
      foreach ( var item in DetailTableModels ) {
        string line = string.Join( ";", item.Floor, item.CeedCode, item.DetailSymbol, item.DetailSymbolId, item.WireType, item.WireSize, item.WireStrip, item.WireBook, item.EarthType, item.EarthSize, item.NumberOfGrounds, 
          item.PlumbingType, item.PlumbingSize, item.NumberOfPlumbing, item.ConstructionClassification, item.SignalType, item.ConstructionItems, item.PlumbingItems, item.Remark, item.WireCrossSectionalArea, item.CountCableSamePosition,
          item.RouteName, item.IsEcoMode, item.IsParentRoute, item.IsReadOnly, item.PlumbingIdentityInfo, item.GroupId, item.IsReadOnlyPlumbingItems, item.IsMixConstructionItems, item.CopyIndex ) ;
        createText += line.Trim() + Environment.NewLine ;
      }

      if ( ! string.IsNullOrWhiteSpace( createText.Trim() ) && createText.Trim() != defaultConstructionItems ) {
        File.WriteAllText( dlg.FileName, createText.Trim() ) ;
      }
    }

    private void SaveAndCreateDetailTable()
    {
      if ( IsCancelCreateDetailTable ) return ;
      SaveDetailTable() ;
      IsCreateDetailTableOnFloorPlanView = true ;
    }

    public static void UnGroupDetailTableRowsAfterChangeConstructionItems( ObservableCollection<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
    {
      var groupIdOfDetailTableRowsWithConstructionItemHasChanged = detailTableModels.Where( d => routeNames.Contains( d.RouteName ) && ! string.IsNullOrEmpty( d.GroupId ) ).Select( d => d.GroupId ).Distinct() ;
      foreach ( var groupId in groupIdOfDetailTableRowsWithConstructionItemHasChanged ) {
        var detailTableRowsWithSameGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems != constructionItems ).ToList() ;
        var detailTableRowsWithConstructionItemHasChanged = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems == constructionItems ).ToList() ;
        if ( detailTableRowsWithSameGroupId.Any() ) {
          if ( detailTableRowsWithSameGroupId.Count == 1 ) {
            detailTableRowsWithSameGroupId.First().GroupId = string.Empty ;
          }
          if ( detailTableRowsWithConstructionItemHasChanged.Count == 1 ) {
            detailTableRowsWithConstructionItemHasChanged.First().GroupId = string.Empty ;
          }
        }

        if ( detailTableRowsWithConstructionItemHasChanged.Count <= 1 ) continue ;
        foreach ( var detailTableRow in detailTableRowsWithConstructionItemHasChanged ) {
          var newGroupId = string.Join( "-",detailTableRow.DetailSymbolId, detailTableRow.PlumbingIdentityInfo, detailTableRow.ConstructionItems, detailTableRow.WireType + detailTableRow.WireSize + detailTableRow.WireStrip ) ;
          detailTableRow.GroupId = newGroupId ;
        }
      }
    }

    public static void UpdatePlumbingItemsAfterChangeConstructionItems( ObservableCollection<DetailTableModel> detailTableModels, string routeName, string constructionItems )
    {
      var plumbingIdentityInfos = detailTableModels.Where( d => d.RouteName == routeName && d.IsParentRoute ).Select( d => d.PlumbingIdentityInfo ).Distinct() ;
      foreach ( var plumbingIdentityInfo in plumbingIdentityInfos ) {
        var detailTableRowsWithSamePlumbing = detailTableModels.Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo ) ;
        foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
          detailTableRow.PlumbingItems = constructionItems ;
        }
      }
    }

    public static void SetGroupIdForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      const bool isMixConstructionItems = false ;
      var detailTableRowsGroupByPlumbingIdentityInfo = 
        detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var detailTableRowsGroupByWiringType = 
          detailTableRowsWithSamePlumbingIdentityInfo
            .GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableRowsWithSameWiringType in detailTableRowsGroupByWiringType ) {
          var detailTableRowsGroupByConstructionItem = 
            detailTableRowsWithSameWiringType
              .GroupBy( d => d.ConstructionItems )
              .Select( g => g.ToList() ) ;
          foreach ( var detailTableRowsWithSameConstructionItem in detailTableRowsGroupByConstructionItem ) {
            var oldDetailTableRow = detailTableRowsWithSameConstructionItem.First() ;
            if ( detailTableRowsWithSameConstructionItem.Count == 1 ) {
              oldDetailTableRow.GroupId = string.Empty ;
              oldDetailTableRow.PlumbingItems = oldDetailTableRow.ConstructionItems ;
            }
            else {
              var groupId = string.Join( "-", isMixConstructionItems, oldDetailTableRow.PlumbingIdentityInfo,
                oldDetailTableRow.WireType + oldDetailTableRow.WireSize + oldDetailTableRow.WireStrip ) ;
              foreach ( var detailTableRow in detailTableRowsWithSameConstructionItem ) {
                detailTableRow.GroupId = groupId ;
                detailTableRow.PlumbingItems = detailTableRow.ConstructionItems ;
              }
            }
          }
        }
      }
    }

    public static void SetGroupIdForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      const bool isMixConstructionItems = true ;
      var detailTableRowsGroupByPlumbingIdentityInfo = 
        detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var parentConstructionItems = detailTableRowsWithSamePlumbingIdentityInfo.First().ConstructionItems ;
        var detailTableRowsGroupByWiringType = 
          detailTableRowsWithSamePlumbingIdentityInfo
            .GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableRowsWithSameWiringType in detailTableRowsGroupByWiringType ) {
          var oldDetailTableRow = detailTableRowsWithSameWiringType.First() ;
          if ( detailTableRowsWithSameWiringType.Count == 1 ) {
            oldDetailTableRow.GroupId = string.Empty ;
            oldDetailTableRow.PlumbingItems = parentConstructionItems ;
          }
          else {
            var groupId = string.Join( "-", isMixConstructionItems, oldDetailTableRow.PlumbingIdentityInfo,
              oldDetailTableRow.WireType + oldDetailTableRow.WireSize + oldDetailTableRow.WireStrip ) ;
            foreach ( var detailTableRowWithSameWiringType in detailTableRowsWithSameWiringType ) {
              detailTableRowWithSameWiringType.GroupId = groupId ;
              detailTableRowWithSameWiringType.PlumbingItems = parentConstructionItems ;
            }
          }
        }
      }
    }

    public static void SetPlumbingItemsForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      foreach ( var detailTableRow in detailTableRowsWithSameDetailSymbolId ) {
        detailTableRow.PlumbingItems = detailTableRow.ConstructionItems ;
      }
    }

    public static void SetPlumbingItemsForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      var detailTableRowsGroupByPlumbingIdentityInfo = 
        detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRows in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var parentDetailRow = detailTableRows.First().ConstructionItems ;
        foreach ( var detailTableRow in detailTableRows ) {
          detailTableRow.PlumbingItems = parentDetailRow ;
        }
      }
    }

    public static void SortDetailTableModel( ref List<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      if ( isMixConstructionItems ) {
        detailTableModels = 
          detailTableModels
            .OrderBy( x => x.DetailSymbol )
            .ThenByDescending( x => x.DetailSymbolId )
            .ThenByDescending( x => x.SignalType )
            .ThenByDescending( x => x.PlumbingIdentityInfo )
            .ThenByDescending( x => x.IsParentRoute )
            .ThenByDescending( x => x.GroupId )
            .GroupBy( x => x.DetailSymbolId )
            .SelectMany( x => x ).ToList() ;
      }
      else
      {
        detailTableModels = 
          detailTableModels
            .OrderBy( x => x.DetailSymbol )
            .ThenByDescending( x => x.DetailSymbolId )
            .ThenByDescending( x => x.SignalType )
            .ThenByDescending( x => x.ConstructionItems )
            .ThenByDescending( x => x.PlumbingIdentityInfo )
            .ThenByDescending( x => x.IsParentRoute )
            .ThenByDescending( x => x.GroupId )
            .GroupBy( x => x.DetailSymbolId )
            .SelectMany( x => x ).ToList() ;
      }
    }
    
    public static void SaveData( Document document, IReadOnlyCollection<DetailTableModel> detailTableRowsBySelectedDetailSymbols )
    {
      try {
        DetailTableStorable detailTableStorable = document.GetDetailTableStorable() ;
        {
          if ( ! detailTableRowsBySelectedDetailSymbols.Any() ) return ;
          var selectedDetailSymbolIds = detailTableRowsBySelectedDetailSymbols.Select( d => d.DetailSymbolId ).Distinct().ToHashSet() ;
          var detailTableRowsByOtherDetailSymbols = detailTableStorable.DetailTableModelData.Where( d => ! selectedDetailSymbolIds.Contains( d.DetailSymbolId ) ).ToList() ;
          detailTableStorable.DetailTableModelData = detailTableRowsBySelectedDetailSymbols.ToList() ;
          if ( detailTableRowsByOtherDetailSymbols.Any() ) detailTableStorable.DetailTableModelData.AddRange( detailTableRowsByOtherDetailSymbols ) ;
        }
        using Transaction t = new( document, "Save data" ) ;
        t.Start() ;
        detailTableStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    public static void SaveDetailSymbolData( Document document, DetailSymbolStorable detailSymbolStorable )
    {
      try {
        using Transaction t = new( document, "Save data" ) ;
        t.Start() ;
        detailSymbolStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    public static void DeleteDetailTableRows( DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableModels, DetailSymbolStorable detailSymbolStorable )
    {
      List<DetailTableModel> deletedDetailTableRows = new() ;
      foreach ( var selectedItem in selectedDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
          var selectedItems = detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId ).ToList() ;
          deletedDetailTableRows.AddRange( selectedItems ) ;
          foreach ( var item in selectedItems ) {
            var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = detailTableViewModel.DetailTableModels.Count( d => d.DetailSymbolId == item.DetailSymbolId && d.RouteName == item.RouteName && d != item ) ;
            if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
              var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == item.DetailSymbolId && s.RouteName == item.RouteName ).ToList() ;
              foreach ( var detailSymbolModel in detailSymbolModels ) {
                detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
              }
            }
          }
        }
        else {
          var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = detailTableViewModel.DetailTableModels.Count( d => d.DetailSymbolId == selectedItem.DetailSymbolId && d.RouteName == selectedItem.RouteName && d != selectedItem ) ;
          if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
            var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == selectedItem.DetailSymbolId && s.RouteName == selectedItem.RouteName ).ToList() ;
            foreach ( var detailSymbolModel in detailSymbolModels ) {
              detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
            }
          }

          deletedDetailTableRows.Add( selectedItem ) ;
        }
      }
      
      var detailTableRows = detailTableViewModel.DetailTableModels.Where( d => ! deletedDetailTableRows.Contains( d ) ) ;
      detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRows ) ;
    }

    public static List<DetailTableModel> PasteDetailTableRow( DetailTableViewModel detailTableViewModel, DetailTableModel copyDetailTableRow, DetailTableModel pasteDetailTableRow )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      var newDetailTableRow = new DetailTableModel( false, copyDetailTableRow.Floor, copyDetailTableRow.CeedCode, copyDetailTableRow.DetailSymbol, 
        copyDetailTableRow.DetailSymbolId, copyDetailTableRow.WireType, copyDetailTableRow.WireSize, copyDetailTableRow.WireStrip, copyDetailTableRow.WireBook, copyDetailTableRow.EarthType, 
        copyDetailTableRow.EarthSize, copyDetailTableRow.NumberOfGrounds, copyDetailTableRow.PlumbingType, copyDetailTableRow.PlumbingSize, copyDetailTableRow.NumberOfPlumbing, 
        copyDetailTableRow.ConstructionClassification, copyDetailTableRow.SignalType, copyDetailTableRow.ConstructionItems, copyDetailTableRow.PlumbingItems, copyDetailTableRow.Remark, 
        copyDetailTableRow.WireCrossSectionalArea, copyDetailTableRow.CountCableSamePosition, copyDetailTableRow.RouteName, copyDetailTableRow.IsEcoMode, copyDetailTableRow.IsParentRoute, 
        copyDetailTableRow.IsReadOnly, copyDetailTableRow.PlumbingIdentityInfo + index, string.Empty, copyDetailTableRow.IsReadOnlyPlumbingItems, copyDetailTableRow.IsMixConstructionItems, index ) ;
      foreach ( var detailTableRow in detailTableViewModel.DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == pasteDetailTableRow ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      return newDetailTableModels ;
    }

    public static void PlumbingSummary( List<ConduitsModel> conduitsModelData, DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableRows, bool isMixConstructionItems )
    {
      Dictionary<DetailTableModel, List<DetailTableModel>> sortDetailTableModel = new() ;
      var detailTableModelsGroupByDetailSymbolId = 
        detailTableViewModel.DetailTableModels
          .Where( selectedDetailTableRows.Contains )
          .GroupBy( d => d.DetailSymbolId )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var plumbingIdentityInfos = detailTableRowsWithSameDetailSymbolId.Select( d => d.PlumbingIdentityInfo ).Distinct().ToHashSet() ;
        var otherDetailTableRowsWithSamePlumbingIdentityInfo = detailTableViewModel.DetailTableModels
          .Where( d => plumbingIdentityInfos.Contains( d.PlumbingIdentityInfo ) && ! detailTableRowsWithSameDetailSymbolId.Contains( d ) )
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
        var parentDetailTableRow = detailTableViewModel.DetailTableModels.FirstOrDefault( d => d.IsParentRoute && d.DetailSymbolId == detailTableRowsWithSameDetailSymbolId.First().DetailSymbolId ) ;
        var plumbingType = parentDetailTableRow == null ? DefaultParentPlumbingType : parentDetailTableRow.PlumbingType ;
        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, detailTableRowsWithSameDetailSymbolId, plumbingType, true, isMixConstructionItems ) ;

        if ( isMixConstructionItems ) {
          SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsWithSameDetailSymbolId ) ;
        }
        else {
          SetGroupIdForDetailTableRows( detailTableRowsWithSameDetailSymbolId ) ;
        }
        
        var detailTableModelsGroupByPlumbingIdentityInfos = 
          detailTableRowsWithSameDetailSymbolId
            .GroupBy( d => d.PlumbingIdentityInfo )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableModelsGroupByPlumbingIdentityInfo in detailTableModelsGroupByPlumbingIdentityInfos ) {
          sortDetailTableModel.Add( detailTableModelsGroupByPlumbingIdentityInfo.First(), detailTableModelsGroupByPlumbingIdentityInfo ) ;
        }

        foreach ( var otherDetailTableRows in otherDetailTableRowsWithSamePlumbingIdentityInfo ) {
          CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, otherDetailTableRows, plumbingType, true, otherDetailTableRows.First().IsMixConstructionItems ) ;
          var isGroup = otherDetailTableRows.FirstOrDefault( d => ! string.IsNullOrEmpty( d.GroupId ) ) != null ;
          if ( isGroup ) {
            if ( isMixConstructionItems ) {
              SetGroupIdForDetailTableRowsMixConstructionItems( otherDetailTableRows ) ;
            }
            else {
              SetGroupIdForDetailTableRows( otherDetailTableRows ) ;
            }
          }

          var otherDetailTableModelsGroupByPlumbingIdentityInfos = 
            otherDetailTableRows
              .GroupBy( d => d.PlumbingIdentityInfo )
              .Select( g => g.ToList() ) ;
          foreach ( var detailTableModelsGroupByPlumbingIdentityInfo in otherDetailTableModelsGroupByPlumbingIdentityInfos ) {
            sortDetailTableModel.Add( detailTableModelsGroupByPlumbingIdentityInfo.First(), detailTableModelsGroupByPlumbingIdentityInfo ) ;
          }
        }
      }

      foreach ( var (parentDetailTableRow, detailTableRows) in sortDetailTableModel ) {
        List<DetailTableModel> newDetailTableModels = new() ;
        foreach ( var detailTableRow in detailTableViewModel.DetailTableModels ) {
          if ( detailTableRow == parentDetailTableRow ) {
            newDetailTableModels.AddRange( detailTableRows ) ;
          }
          else if ( ! detailTableRows.Contains( detailTableRow ) ) {
            newDetailTableModels.Add( detailTableRow ) ;
          }
        }

        detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      }
    }

    public static void DeleteReferenceDetailTableRows( DetailTableViewModel detailTableViewModel, DetailTableViewModel detailTableViewModelSummary, List<DetailTableModel> selectedDetailTableModels )
    {
      var deletedDetailTableRows = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRowsOfGroup = detailTableViewModel.ReferenceDetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          deletedDetailTableRows.AddRange( detailTableRowsOfGroup ) ;
        }
        else {
          deletedDetailTableRows.Add( detailTableRow ) ;
        }
      }

      var detailTableRows = detailTableViewModel.ReferenceDetailTableModels.Where( d => ! deletedDetailTableRows.Contains( d ) ) ;
      detailTableViewModel.ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRows ) ;

      var detailTableRowsSummary = detailTableViewModelSummary.ReferenceDetailTableModels.Where( d => ! selectedDetailTableModels.Contains( d ) ) ;
      detailTableViewModelSummary.ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRowsSummary ) ;
    }
    
    public static List<DetailTableModel> SelectDetailTableRowsWithSameDetailSymbolId( DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableModels )
    {
      List<DetailTableModel> detailTableRowsWithSameDetailSymbolId = new() ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        var detailTableRows = detailTableViewModel.ReferenceDetailTableModels.Where( d => d.DetailSymbolId == detailTableRow.DetailSymbolId ) ;
        detailTableRowsWithSameDetailSymbolId.AddRange( detailTableRows ) ;
      }

      return detailTableRowsWithSameDetailSymbolId ;
    }

    public static void ReadCtlFile( DetailTableViewModel detailTableViewModel, DetailTableViewModel detailTableViewModelSummary )
    {
      MessageBox.Show( "Please select ctl file.", "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = "Ctl files (*.ctl)|*.ctl", Multiselect = false } ;
      string filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }
      
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var referenceDetailTableModels = ExcelToModelConverter.GetReferenceDetailTableModels( filePath ) ;
      detailTableViewModelSummary.ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>( referenceDetailTableModels ) ;
      List<DetailTableModel> unGroupDetailTableModels = new() ;
      foreach ( var detailTableRow in referenceDetailTableModels ) {
        if ( detailTableRow.Remark.Contains( ',' ) || detailTableRow.Remark.Contains( 'x' ) ) {
          AddUnGroupDetailTableRows( unGroupDetailTableModels, detailTableRow ) ;
        }
        else {
          unGroupDetailTableModels.Add( detailTableRow ) ;
        }
      }

      detailTableViewModel.ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>( unGroupDetailTableModels ) ;
    }

    private static void AddUnGroupDetailTableRows( List<DetailTableModel> unGroupDetailTableModels, DetailTableModel detailTableRow )
    {
      var remarks = detailTableRow.Remark.Split( ',' ) ;
      var isParentDetailRow = ! detailTableRow.IsParentRoute ;
      foreach ( var remark in remarks ) {
        if ( remark.Contains( 'x' ) ) {
          var remarkArr = remark.Split( 'x' ) ;
          var countRows = int.Parse( remarkArr.Last() ) ;
          var remarkRow = remarkArr.Length == 2 ? remarkArr.First().Trim() : remarkArr.First().Trim() + 'x' + remarkArr.ElementAt( 1 ) ;
          var wireBook = remarkArr.Length == 2 ? "1" : remarkArr.ElementAt( 1 ) ;
          if ( ! isParentDetailRow ) {
            var newDetailTableRow = CreateParentDetailTableModel( detailTableRow, wireBook, remarkRow ) ;
            unGroupDetailTableModels.Add( newDetailTableRow ) ;
            for ( var i = 1 ; i < countRows ; i++ ) {
              newDetailTableRow = CreateChildDetailTableModel( detailTableRow, wireBook, remarkRow ) ;
              unGroupDetailTableModels.Add( newDetailTableRow );
            }
            isParentDetailRow = true ;
          }
          else {
            for ( var i = 0 ; i < countRows ; i++ ) {
              var newDetailTableRow = CreateChildDetailTableModel( detailTableRow, wireBook, remarkRow ) ;
              unGroupDetailTableModels.Add( newDetailTableRow );
            }
          }
        }
        else {
          if ( ! isParentDetailRow ) {
            var newDetailTableRow = CreateParentDetailTableModel( detailTableRow, "1", remark.Trim() ) ;
            unGroupDetailTableModels.Add( newDetailTableRow ) ;
            isParentDetailRow = true ;
          }
          else {
            var newDetailTableRow = CreateChildDetailTableModel( detailTableRow, "1", remark.Trim() ) ;
            unGroupDetailTableModels.Add( newDetailTableRow ) ;
          }
        }
      }
    }
    
    private static DetailTableModel CreateParentDetailTableModel( DetailTableModel detailTableRow, string wireBook, string remarkRow )
    {
      var newDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeedCode, detailTableRow.DetailSymbol, 
        detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, wireBook, detailTableRow.EarthType, 
        detailTableRow.EarthSize, detailTableRow.NumberOfGrounds, detailTableRow.PlumbingType, detailTableRow.PlumbingSize, detailTableRow.NumberOfPlumbing, 
        detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, remarkRow, 
        detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, true, 
        false, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, ! detailTableRow.IsMixConstructionItems, detailTableRow.IsMixConstructionItems, string.Empty ) ;
      return newDetailTableRow ;
    }

    private static DetailTableModel CreateChildDetailTableModel( DetailTableModel detailTableRow, string wireBook, string remarkRow )
    {
      const string defaultChildPlumbingSymbol = "↑" ;
      var newDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeedCode, detailTableRow.DetailSymbol, 
        detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, wireBook, detailTableRow.EarthType, 
        detailTableRow.EarthSize, detailTableRow.NumberOfGrounds, defaultChildPlumbingSymbol, defaultChildPlumbingSymbol, defaultChildPlumbingSymbol, 
        detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, remarkRow, 
        detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, false, 
        true, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, true, detailTableRow.IsMixConstructionItems, string.Empty ) ;
      return newDetailTableRow ;
    }
    
    public static void AddReferenceDetailTableRows( DetailTableViewModel detailTableViewModel, DetailTableViewModel detailTableViewModelSummary, List<DetailTableModel> selectedDetailTableModels )
    {
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        var referenceDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeedCode, detailTableRow.DetailSymbol, 
          detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, detailTableRow.WireBook, detailTableRow.EarthType, 
          detailTableRow.EarthSize, detailTableRow.NumberOfGrounds, detailTableRow.PlumbingType, detailTableRow.PlumbingSize, detailTableRow.NumberOfPlumbing, 
          detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, detailTableRow.Remark, 
          detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, detailTableRow.IsParentRoute, 
          detailTableRow.IsReadOnly, detailTableRow.PlumbingIdentityInfo + "-" + index, detailTableRow.GroupId,
          detailTableRow.IsReadOnlyPlumbingItems, detailTableRow.IsMixConstructionItems, detailTableRow.CopyIndex + index ) ;
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRowsOfGroup = detailTableViewModel.ReferenceDetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          foreach ( var detailTableRowOfGroup in detailTableRowsOfGroup ) {
            var newReferenceDetailTableRow = new DetailTableModel( detailTableRowOfGroup.CalculationExclusion, detailTableRowOfGroup.Floor, detailTableRowOfGroup.CeedCode, detailTableRowOfGroup.DetailSymbol, 
              detailTableRowOfGroup.DetailSymbolId, detailTableRowOfGroup.WireType, detailTableRowOfGroup.WireSize, detailTableRowOfGroup.WireStrip, detailTableRowOfGroup.WireBook, detailTableRowOfGroup.EarthType, 
              detailTableRowOfGroup.EarthSize, detailTableRowOfGroup.NumberOfGrounds, detailTableRowOfGroup.PlumbingType, detailTableRowOfGroup.PlumbingSize, detailTableRowOfGroup.NumberOfPlumbing, 
              detailTableRowOfGroup.ConstructionClassification, detailTableRowOfGroup.SignalType, detailTableRowOfGroup.ConstructionItems, detailTableRowOfGroup.PlumbingItems, detailTableRowOfGroup.Remark, 
              detailTableRowOfGroup.WireCrossSectionalArea, detailTableRowOfGroup.CountCableSamePosition, detailTableRowOfGroup.RouteName, detailTableRowOfGroup.IsEcoMode, detailTableRowOfGroup.IsParentRoute, 
              detailTableRowOfGroup.IsReadOnly, detailTableRowOfGroup.PlumbingIdentityInfo + "-" + index, detailTableRowOfGroup.GroupId + "-" + index, detailTableRowOfGroup.IsReadOnlyPlumbingItems,
              detailTableRowOfGroup.IsMixConstructionItems, detailTableRowOfGroup.CopyIndex + index ) ;
            detailTableViewModel.DetailTableModels.Add( newReferenceDetailTableRow ) ;
          }
        }
        else {
          detailTableViewModel.DetailTableModels.Add( referenceDetailTableRow ) ;
        }

        detailTableViewModelSummary.DetailTableModels.Add( referenceDetailTableRow ) ;
      }
    }

    public static List<DetailTableModel> GroupDetailTableModels( ObservableCollection<DetailTableModel> oldDetailTableModels )
    {
      List<DetailTableModel> newDetailTableModels = new() ;
      List<string> existedGroupIds = new() ;
      foreach ( var detailTableRow in oldDetailTableModels ) {
        if ( string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          newDetailTableModels.Add( detailTableRow ) ;
        }
        else {
          if ( existedGroupIds.Contains( detailTableRow.GroupId ) ) continue ;
          var detailTableRowWithSameWiringType = oldDetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          var detailTableRowsGroupByRemark = 
            detailTableRowWithSameWiringType
              .GroupBy( d => d.Remark )
              .ToDictionary( g => g.Key, g => g.ToList() ) ;
          List<string> newRemark = new() ;
          var numberOfGrounds = 0 ;
          foreach ( var (remark, detailTableRowsWithSameRemark) in detailTableRowsGroupByRemark ) {
            newRemark.Add( remark + ( detailTableRowsWithSameRemark.Count == 1 ? string.Empty : "x" + detailTableRowsWithSameRemark.Count ) ) ;
            numberOfGrounds += detailTableRowsWithSameRemark.Count ;
          }

          var newDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeedCode, detailTableRow.DetailSymbol, 
            detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, numberOfGrounds.ToString(), detailTableRow.EarthType, 
            detailTableRow.EarthSize, detailTableRow.NumberOfGrounds, detailTableRow.PlumbingType, detailTableRow.PlumbingSize, detailTableRow.NumberOfPlumbing, 
            detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, string.Join( ", ", newRemark ), 
            detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, detailTableRow.IsParentRoute, detailTableRow.IsReadOnly,
            detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, detailTableRow.IsReadOnlyPlumbingItems, detailTableRow.IsMixConstructionItems, detailTableRow.CopyIndex ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          existedGroupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      return newDetailTableModels ;
    }
  }
}