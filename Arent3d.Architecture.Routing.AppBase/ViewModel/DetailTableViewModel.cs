using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
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
    public ObservableCollection<DetailTableModel> DetailTableModels { get ; set ; }

    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }
    
    public  bool IsCancelCreateDetailTable { get; set; }

    public ICommand SaveDetailTableCommand { get; set; }

    public ICommand SaveAndCreateDetailTableCommand { get ; set ; }

    public List<CreateDetailTableCommandBase.ComboboxItemType> ConduitTypes { get ; set ; }

    public List<CreateDetailTableCommandBase.ComboboxItemType> ConstructionItems { get ; set ; }

    public DetailTableViewModel( ObservableCollection<DetailTableModel> detailTableModels, List<CreateDetailTableCommandBase.ComboboxItemType> conduitTypes, List<CreateDetailTableCommandBase.ComboboxItemType> constructionItems )
    {
      DetailTableModels = detailTableModels ;
      IsCreateDetailTableOnFloorPlanView = false ;

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
        string line = string.Join( ",", item.Floor, item.Remark, item.ConstructionClassification, item.ConstructionItems, item.DetailSymbol, item.EarthSize, item.EarthType, 
          item.PlumbingSize, item.PlumbingType, item.PlumbingItems, item.WireBook, item.WireSize, item.WireStrip, item.WireType, item.NumberOfGrounds, item.NumberOfPlumbing ) ;
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

    public static void UnGroupDetailTableRowsAfterChangeConstructionItems( ref List<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
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

    public static void UpdatePlumbingItemsAfterChangeConstructionItems( ref List<DetailTableModel> detailTableModels, string routeName, string constructionItems )
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
              var groupId = string.Join( "-", isMixConstructionItems, oldDetailTableRow.DetailSymbolId, oldDetailTableRow.PlumbingIdentityInfo,
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
            var groupId = string.Join( "-", isMixConstructionItems, oldDetailTableRow.DetailSymbolId, oldDetailTableRow.PlumbingIdentityInfo,
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

    public static void SortDetailTableModel( ref List<DetailTableModel> detailTableModels )
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
    
    public static void SaveDetailSymbolData( Document document, StorableBase detailSymbolStorable )
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

    public static void DeleteDetailTableRows( List<ConduitsModel> conduitsModelData, DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableModels, DetailSymbolStorable detailSymbolStorable)
    {
      var detailTableRowDeleted = new List<DetailTableModel>() ;
      var parentDetailRowDeletedInfo = new Dictionary<string, string>() ;
      var childDetailRowDeletedPlumbingIdentityInfo = new List<string>() ;
      foreach ( var selectedItem in selectedDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
          var selectedItems = detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId ).ToList() ;
          foreach ( var item in selectedItems ) {
            var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = detailTableViewModel.DetailTableModels.Count( d => d.DetailSymbolId == item.DetailSymbolId && d.RouteName == item.RouteName && d != item ) ;
            if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName > 0 ) {
              var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == item.DetailSymbolId && s.RouteName == item.RouteName ).ToList() ;
              foreach ( var detailSymbolModel in detailSymbolModels ) {
                detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
              }
            }

            detailTableRowDeleted.AddRange( selectedDetailTableModels.Where( d => d.DetailSymbolId == item.DetailSymbolId && d.RouteName == item.RouteName ) ) ;
            if ( item.IsParentRoute && ! parentDetailRowDeletedInfo.ContainsKey( item.PlumbingIdentityInfo ) ) {
              parentDetailRowDeletedInfo.Add( item.PlumbingIdentityInfo, item.PlumbingType ) ;
            }

            if ( ! item.IsParentRoute && ! childDetailRowDeletedPlumbingIdentityInfo.Contains( item.PlumbingIdentityInfo ) && ! parentDetailRowDeletedInfo.ContainsKey( item.PlumbingIdentityInfo ) ) {
              childDetailRowDeletedPlumbingIdentityInfo.Add( item.PlumbingIdentityInfo ) ;
            }

            detailTableViewModel.DetailTableModels.Remove( item ) ;
          }
        }
        else {
          var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = detailTableViewModel.DetailTableModels.Count( d => d.DetailSymbolId == selectedItem.DetailSymbolId && d.RouteName == selectedItem.RouteName && d != selectedItem ) ;
          if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName > 0 ) {
            var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == selectedItem.DetailSymbolId && s.RouteName == selectedItem.RouteName ).ToList() ;
            foreach ( var detailSymbolModel in detailSymbolModels ) {
              detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
            }
          }

          detailTableRowDeleted.AddRange( selectedDetailTableModels.Where( d => d.DetailSymbolId == selectedItem.DetailSymbolId && d.RouteName == selectedItem.RouteName ) ) ;
          if ( selectedItem.IsParentRoute && ! parentDetailRowDeletedInfo.ContainsKey( selectedItem.PlumbingIdentityInfo ) ) {
            parentDetailRowDeletedInfo.Add( selectedItem.PlumbingIdentityInfo, selectedItem.PlumbingType ) ;
          }

          if ( ! selectedItem.IsParentRoute && ! childDetailRowDeletedPlumbingIdentityInfo.Contains( selectedItem.PlumbingIdentityInfo ) && ! parentDetailRowDeletedInfo.ContainsKey( selectedItem.PlumbingIdentityInfo ) ) {
            childDetailRowDeletedPlumbingIdentityInfo.Add( selectedItem.PlumbingIdentityInfo ) ;
          }

          var detailTableRow = detailTableViewModel.DetailTableModels.FirstOrDefault( d => d == selectedItem ) ;
          detailTableViewModel.DetailTableModels.Remove( detailTableRow ) ;
        }
      }

      foreach ( var detailTableRow in detailTableRowDeleted ) {
        selectedDetailTableModels.Remove( detailTableRow ) ;
      }

      foreach ( var (plumbingIdentityInfo, plumbingType) in parentDetailRowDeletedInfo ) {
        var detailTableRowsWithSamePlumbingIdentityInfo = detailTableViewModel.DetailTableModels.Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo ).ToList() ;
        if ( ! detailTableRowsWithSamePlumbingIdentityInfo.Any() ) continue ;
        var isMixConstructionItems = detailTableRowsWithSamePlumbingIdentityInfo.First().IsMixConstructionItems ;
        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, detailTableRowsWithSamePlumbingIdentityInfo, plumbingType, true, isMixConstructionItems ) ;
      }

      foreach ( var plumbingIdentityInfo in childDetailRowDeletedPlumbingIdentityInfo ) {
        var detailTableRowsWithSamePlumbingIdentityInfo = detailTableViewModel.DetailTableModels.Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo ).ToList() ;
        if ( ! detailTableRowsWithSamePlumbingIdentityInfo.Any() ) continue ;
        var parentDetailRow = detailTableRowsWithSamePlumbingIdentityInfo.FirstOrDefault( d => d.IsParentRoute ) ;
        var plumbingType = parentDetailRow == null ? DefaultParentPlumbingType : parentDetailRow.PlumbingType ;
        var isMixConstructionItems = detailTableRowsWithSamePlumbingIdentityInfo.First().IsMixConstructionItems ;
        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, detailTableRowsWithSamePlumbingIdentityInfo, plumbingType, true, isMixConstructionItems ) ;
      }
    }

    public static List<DetailTableModel> PasteDetailTableRow( DetailTableViewModel detailTableViewModel, DetailTableModel copyDetailTableRow, bool isMixConstructionItems )
    {
      var newDetailTableRow = new DetailTableModel( false, copyDetailTableRow.Floor, copyDetailTableRow.CeedCode, copyDetailTableRow.DetailSymbol, 
        copyDetailTableRow.DetailSymbolId, copyDetailTableRow.WireType, copyDetailTableRow.WireSize, copyDetailTableRow.WireStrip, copyDetailTableRow.WireBook, copyDetailTableRow.EarthType, 
        copyDetailTableRow.EarthSize, copyDetailTableRow.NumberOfGrounds, copyDetailTableRow.PlumbingType, copyDetailTableRow.PlumbingSize, copyDetailTableRow.NumberOfPlumbing, 
        copyDetailTableRow.ConstructionClassification, copyDetailTableRow.SignalType, copyDetailTableRow.ConstructionItems, copyDetailTableRow.PlumbingItems, copyDetailTableRow.Remark, 
        copyDetailTableRow.WireCrossSectionalArea, copyDetailTableRow.CountCableSamePosition, copyDetailTableRow.RouteName, copyDetailTableRow.IsEcoMode, copyDetailTableRow.IsParentRoute, 
        copyDetailTableRow.IsReadOnly, copyDetailTableRow.PlumbingIdentityInfo, copyDetailTableRow.GroupId, copyDetailTableRow.IsReadOnlyPlumbingItems, copyDetailTableRow.IsMixConstructionItems ) ;
      detailTableViewModel.DetailTableModels.Add( newDetailTableRow ) ;
      var newDetailTableModels = detailTableViewModel.DetailTableModels.ToList() ;
      SortDetailTableModel( ref newDetailTableModels ) ;
      return newDetailTableModels ;
    }

    public static List<DetailTableModel> GetSelectedDetailTableRows( DetailTableViewModel detailTableViewModel )
    {
      return detailTableViewModel.DetailTableModels.Where( d => d.CalculationExclusion ).ToList() ;
    }

    public static void PlumbingSummary( List<ConduitsModel> conduitsModelData, DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableRows, bool isMixConstructionItems )
    {
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
        var parentDetailRow = detailTableViewModel.DetailTableModels.FirstOrDefault( d => d.IsParentRoute && d.DetailSymbolId == detailTableRowsWithSameDetailSymbolId.First().DetailSymbolId ) ;
        var plumbingType = parentDetailRow == null ? DefaultParentPlumbingType : parentDetailRow.PlumbingType ;
        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, detailTableRowsWithSameDetailSymbolId, plumbingType, true, isMixConstructionItems ) ;
        if ( isMixConstructionItems ) {
          SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsWithSameDetailSymbolId ) ;
        }
        else {
          SetGroupIdForDetailTableRows( detailTableRowsWithSameDetailSymbolId ) ;
        }

        foreach ( var otherDetailTableRows in otherDetailTableRowsWithSamePlumbingIdentityInfo ) {
          CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, otherDetailTableRows, plumbingType, true, otherDetailTableRows.First().IsMixConstructionItems ) ;
          if ( isMixConstructionItems ) {
            SetGroupIdForDetailTableRowsMixConstructionItems( otherDetailTableRows ) ;
          }
          else {
            SetGroupIdForDetailTableRows( otherDetailTableRows ) ;
          }
        }
      }

      var newDetailTableModels = detailTableViewModel.DetailTableModels.ToList() ;
      SortDetailTableModel( ref newDetailTableModels ) ;
      detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
    }
  }
}