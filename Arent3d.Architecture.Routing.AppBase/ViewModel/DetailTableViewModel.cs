using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.IO ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public sealed class DetailTableViewModel : ViewModelBase, INotifyPropertyChanged
  {
    private const string DefaultParentPlumbingType = "E" ;
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;

    private ObservableCollection<DetailTableModel> _detailTableModels ;
    public ObservableCollection<DetailTableModel> DetailTableModels { 
      get => _detailTableModels ;
      set
      {
        _detailTableModels = value ;
        OnPropertyChanged( nameof(DetailTableModels) );
      } 
    }

    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }
    
    public  bool IsCancelCreateDetailTable { get; set; }

    public ICommand SaveDetailTableCommand { get; }

    public ICommand CreateDetailTableCommand { get ; }

    public List<DetailTableModel.ComboboxItemType> ConduitTypes { get ;}

    public List<DetailTableModel.ComboboxItemType> ConstructionItems { get ; }
    
    public List<DetailTableModel.ComboboxItemType> Levels { get ; }

    public List<DetailTableModel.ComboboxItemType> WireTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> EarthTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> Numbers { get ; }
    
    public List<DetailTableModel.ComboboxItemType> ConstructionClassificationTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> SignalTypes { get ; }

    public DetailTableViewModel( ObservableCollection<DetailTableModel> detailTableModels, List<DetailTableModel.ComboboxItemType> conduitTypes, List<DetailTableModel.ComboboxItemType> constructionItems,
      List<DetailTableModel.ComboboxItemType> levels, List<DetailTableModel.ComboboxItemType> wireTypes, List<DetailTableModel.ComboboxItemType> earthTypes, 
      List<DetailTableModel.ComboboxItemType> numbers, List<DetailTableModel.ComboboxItemType> constructionClassificationTypes, List<DetailTableModel.ComboboxItemType> signalTypes )
    {
      _detailTableModels = detailTableModels ;
      IsCreateDetailTableOnFloorPlanView = false ;

      SaveDetailTableCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { SaveDetailTable() ; } // Execute()
      ) ;

      CreateDetailTableCommand = new RelayCommand<object>( ( p ) => true, // CanExecute()
        ( p ) => { CreateDetailTable() ; } // Execute()
      ) ;

      ConduitTypes = conduitTypes ;
      ConstructionItems = constructionItems ;
      Levels = levels ;
      WireTypes = wireTypes ;
      EarthTypes = earthTypes ;
      Numbers = numbers ;
      ConstructionClassificationTypes = constructionClassificationTypes ;
      SignalTypes = signalTypes ;
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

    private void CreateDetailTable()
    {
      if ( IsCancelCreateDetailTable ) return ;
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
      var plumbingIdentityInfos = detailTableModels.Where( d => d.RouteName == routeName ).Select( d => d.PlumbingIdentityInfo ).Distinct() ;
      foreach ( var plumbingIdentityInfo in plumbingIdentityInfos ) {
        var detailTableRowsWithSamePlumbing = detailTableModels.Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo ).ToList() ;
        if ( ! detailTableRowsWithSamePlumbing.Any() ) continue ;
        {
          var isParentDetailTableRow = detailTableRowsWithSamePlumbing.FirstOrDefault( d => d.RouteName == routeName && d.IsParentRoute ) != null ;
          var plumbingItems = detailTableRowsWithSamePlumbing.Select( d => d.ConstructionItems ).Distinct() ;
          var plumbingItemTypes = ( from plumbingItem in plumbingItems select new DetailTableModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() ;
          foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
            if ( detailTableRow.IsMixConstructionItems && isParentDetailTableRow ) {
              detailTableRow.PlumbingItems = constructionItems ;
            }
            else if ( ! detailTableRow.IsMixConstructionItems ) {
              detailTableRow.PlumbingItems = detailTableRow.ConstructionItems ;
            }

            detailTableRow.PlumbingItemTypes = plumbingItemTypes ;
          }
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
        detailTableRow.PlumbingItemTypes = new List<DetailTableModel.ComboboxItemType> { new( detailTableRow.ConstructionItems, detailTableRow.ConstructionItems ) } ;
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
        var plumbingItems = detailTableRows.Select( d => d.ConstructionItems ).Distinct() ;
        var plumbingItemTypes = ( from plumbingItem in plumbingItems select new DetailTableModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() ;
        foreach ( var detailTableRow in detailTableRows ) {
          detailTableRow.PlumbingItems = parentDetailRow ;
          detailTableRow.PlumbingItemTypes = plumbingItemTypes ;
        }
      }
    }
    
    public static void SetPlumbingSizesForDetailTableRows( List<ConduitsModel> conduitsModelData, IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId, string plumbingType )
    {
      var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
      var plumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
      foreach ( var detailTableRow in detailTableRowsWithSameDetailSymbolId ) {
        detailTableRow.PlumbingSizes = plumbingSizes ;
      }
    }

    public static void SortDetailTableModel( ref List<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      List<DetailTableModel> sortedDetailTableModelsList = new() ;
      var detailTableModelsGroupByDetailSymbolId = detailTableModels.OrderBy( d => d.DetailSymbol ).GroupBy( d => d.DetailSymbolId ).Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsGroupByDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var signalTypes = (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType )) ;
        foreach ( var signalType in signalTypes ) {
          var detailTableRowsWithSameSignalType = detailTableRowsGroupByDetailSymbolId.Where( d => d.SignalType == signalType.GetFieldName() ) ;
          SortDetailTableRows( sortedDetailTableModelsList, detailTableRowsWithSameSignalType, isMixConstructionItems ) ;
        }

        var signalTypeNames = signalTypes.Select( s => s.GetFieldName() ) ;
        var detailTableRowsNotHaveSignalType = detailTableRowsGroupByDetailSymbolId.Where( d => ! signalTypeNames.Contains( d.SignalType ) ) ;
        SortDetailTableRows( sortedDetailTableModelsList, detailTableRowsNotHaveSignalType, isMixConstructionItems ) ;
      }

      detailTableModels = sortedDetailTableModelsList ;
    }
    
    private static void SortDetailTableRows( List<DetailTableModel> sortedDetailTableModelsList, IEnumerable<DetailTableModel> detailTableRowsWithSameSignalType, bool isMixConstructionItems )
    {
      IEnumerable<DetailTableModel> sortedDetailTableModels ;
      if ( isMixConstructionItems ) {
        sortedDetailTableModels = 
          detailTableRowsWithSameSignalType
            .OrderBy( x => x.PlumbingIdentityInfo )
            .ThenByDescending( x => x.IsParentRoute )
            .ThenByDescending( x => x.GroupId )
            .GroupBy( x => x.DetailSymbolId )
            .SelectMany( x => x ) ;
      }
      else {
        sortedDetailTableModels = 
          detailTableRowsWithSameSignalType
            .OrderBy( x => x.ConstructionItems )
            .ThenByDescending( x => x.PlumbingIdentityInfo )
            .ThenByDescending( x => x.IsParentRoute )
            .ThenByDescending( x => x.GroupId )
            .GroupBy( x => x.DetailSymbolId )
            .SelectMany( x => x ) ;
      }

      sortedDetailTableModelsList.AddRange( sortedDetailTableModels ) ;
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

    public static void DeleteDetailTableRows( DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableModels, DetailTableViewModel detailTableViewModelSummary, List<DetailTableModel> selectedDetailTableModelsSummary, DetailSymbolStorable detailSymbolStorable )
    {
      foreach ( var selectedItem in selectedDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
          var selectedItems = detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId ).ToList() ;
          foreach ( var item in selectedItems ) {
            var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = detailTableViewModel.DetailTableModels.Count( d => d.DetailSymbolId == item.DetailSymbolId && d.RouteName == item.RouteName && d != item ) ;
            if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
              var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == item.DetailSymbolId && s.RouteName == item.RouteName ).ToList() ;
              foreach ( var detailSymbolModel in detailSymbolModels ) {
                detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
              }
            }

            detailTableViewModel.DetailTableModels.Remove( item ) ;
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

          var detailTableRow = detailTableViewModel.DetailTableModels.FirstOrDefault( d => d == selectedItem ) ;
          detailTableViewModel.DetailTableModels.Remove( detailTableRow ) ;
        }
      }
      
      foreach ( var selectedItem in selectedDetailTableModelsSummary ) {
        detailTableViewModelSummary.DetailTableModels.Remove( selectedItem ) ;
      }
    }

    public static void PasteDetailTableRow( DetailTableViewModel detailTableViewModel, DetailTableModel copyDetailTableRow, DetailTableModel pasteDetailTableRow, DetailTableViewModel detailTableViewModelSummary, DetailTableModel pasteDetailTableRowSummary )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      var newDetailTableRow = new DetailTableModel( false, copyDetailTableRow.Floor, copyDetailTableRow.CeedCode, copyDetailTableRow.DetailSymbol, 
        copyDetailTableRow.DetailSymbolId, copyDetailTableRow.WireType, copyDetailTableRow.WireSize, copyDetailTableRow.WireStrip, copyDetailTableRow.WireBook, copyDetailTableRow.EarthType, 
        copyDetailTableRow.EarthSize, copyDetailTableRow.NumberOfGrounds, copyDetailTableRow.PlumbingType, copyDetailTableRow.PlumbingSize, copyDetailTableRow.NumberOfPlumbing, 
        copyDetailTableRow.ConstructionClassification, copyDetailTableRow.SignalType, copyDetailTableRow.ConstructionItems, copyDetailTableRow.PlumbingItems, copyDetailTableRow.Remark, 
        copyDetailTableRow.WireCrossSectionalArea, copyDetailTableRow.CountCableSamePosition, copyDetailTableRow.RouteName, copyDetailTableRow.IsEcoMode, copyDetailTableRow.IsParentRoute, 
        copyDetailTableRow.IsReadOnly, copyDetailTableRow.PlumbingIdentityInfo + index, string.Empty, copyDetailTableRow.IsReadOnlyPlumbingItems,
        copyDetailTableRow.IsMixConstructionItems, index, copyDetailTableRow.IsReadOnlyParameters, copyDetailTableRow.IsReadOnlyWireSizeAndWireStrip, copyDetailTableRow.IsReadOnlyPlumbingSize,
        copyDetailTableRow.WireSizes, copyDetailTableRow.WireStrips, copyDetailTableRow.EarthSizes, copyDetailTableRow.PlumbingSizes, copyDetailTableRow.PlumbingItemTypes ) ;
      foreach ( var detailTableRow in detailTableViewModel.DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == pasteDetailTableRow ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      
      newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in detailTableViewModelSummary.DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == pasteDetailTableRowSummary ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      detailTableViewModelSummary.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
    }

    public static void PlumbingSummary( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableRows, bool isMixConstructionItems, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      Dictionary<DetailTableModel, List<DetailTableModel>> sortDetailTableModel = new() ;
      var detailTableModelsGroupByDetailSymbolId = 
        detailTableViewModel.DetailTableModels
          .Where( selectedDetailTableRows.Contains )
          .Where( d => ! string.IsNullOrEmpty( d.WireType ) && ! string.IsNullOrEmpty( d.WireSize ) && ! string.IsNullOrEmpty( d.WireStrip ) && ! string.IsNullOrEmpty( d.WireBook ) && ! string.IsNullOrEmpty( d.SignalType ) && ! string.IsNullOrEmpty( d.ConstructionItems ) && ! string.IsNullOrEmpty( d.Remark ) )
          .GroupBy( d => d.DetailSymbolId )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var plumbingIdentityInfos = detailTableRowsWithSameDetailSymbolId.Select( d => d.PlumbingIdentityInfo ).Distinct().ToHashSet() ;
        var otherDetailTableRowsWithSamePlumbingIdentityInfo = detailTableViewModel.DetailTableModels
          .Where( d => plumbingIdentityInfos.Contains( d.PlumbingIdentityInfo ) && ! detailTableRowsWithSameDetailSymbolId.Contains( d ) )
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
        var detailSymbolId = detailTableRowsWithSameDetailSymbolId.First().DetailSymbolId ;
        var plumbingType = detailSymbolIdsWithPlumbingTypeHasChanged.SingleOrDefault( d => d.Key == detailSymbolId ).Value ;
        if ( string.IsNullOrEmpty( plumbingType ) ) {
          plumbingType = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( s => s.DetailSymbolId == detailSymbolId )?.PlumbingType ?? DefaultParentPlumbingType ;
        }

        if ( plumbingType == NoPlumping ) {
          CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableRowsWithSameDetailSymbolId, isMixConstructionItems );
        }
        else {
          CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, detailTableRowsWithSameDetailSymbolId, plumbingType, true, isMixConstructionItems ) ;
          SetPlumbingSizesForDetailTableRows( conduitsModelData, detailTableRowsWithSameDetailSymbolId, plumbingType ) ;
        }

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
          if ( plumbingType == NoPlumping ) {
            CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableRowsWithSameDetailSymbolId, isMixConstructionItems );
          }
          else {
            CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, otherDetailTableRows, plumbingType, true, otherDetailTableRows.First().IsMixConstructionItems ) ;
            SetPlumbingSizesForDetailTableRows( conduitsModelData, otherDetailTableRows, plumbingType ) ;
          }

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

    public static void AddDetailTableRow( DetailTableViewModel detailTableViewModel, DetailTableModel selectDetailTableRow, DetailTableViewModel detailTableViewModelSummary, DetailTableModel selectDetailTableRowSummary )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var newDetailTableRow = new DetailTableModel( selectDetailTableRow.DetailSymbol, selectDetailTableRow.DetailSymbolId ) ;
      foreach ( var detailTableRow in detailTableViewModel.DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == selectDetailTableRow ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      
      newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in detailTableViewModelSummary.DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == selectDetailTableRowSummary ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      detailTableViewModelSummary.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
    }
    
    public static bool MoveDetailTableRow( DetailTableViewModel detailTableViewModel, DetailTableModel selectDetailTableRow, DetailTableViewModel detailTableViewModelSummary, DetailTableModel selectDetailTableRowSummary, bool isMoveUp )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var selectDetailTableRowSummaryIndex = detailTableViewModelSummary.DetailTableModels.FindIndex( d => d == selectDetailTableRowSummary ) ;
      if ( ( isMoveUp && selectDetailTableRowSummaryIndex == 0 ) || ( ! isMoveUp && selectDetailTableRowSummaryIndex == detailTableViewModelSummary.DetailTableModels.Count - 1 ) ) return false ;
      var tempDetailTableRowSummary = detailTableViewModelSummary.DetailTableModels.ElementAt( isMoveUp ? selectDetailTableRowSummaryIndex - 1 : selectDetailTableRowSummaryIndex + 1 ) ;
      foreach ( var detailTableRow in detailTableViewModelSummary.DetailTableModels ) {
        if ( detailTableRow == tempDetailTableRowSummary ) {
          newDetailTableModels.Add( selectDetailTableRowSummary ) ;
        }
        else if ( detailTableRow == selectDetailTableRowSummary ) {
          newDetailTableModels.Add( tempDetailTableRowSummary ) ;
        }
        else {
          newDetailTableModels.Add( detailTableRow ) ;
        }
      }

      detailTableViewModelSummary.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      
      newDetailTableModels = new List<DetailTableModel>() ;
      var selectDetailTableRowIndex = detailTableViewModel.DetailTableModels.FindIndex( d => d == selectDetailTableRow ) ;
      var tempDetailTableRow = detailTableViewModel.DetailTableModels.ElementAt( isMoveUp ? selectDetailTableRowIndex - 1 : selectDetailTableRowIndex + 1 ) ;
      foreach ( var detailTableRow in detailTableViewModel.DetailTableModels ) {
        if ( detailTableRow == tempDetailTableRow ) {
          newDetailTableModels.Add( selectDetailTableRow ) ;
        }
        else if ( detailTableRow == selectDetailTableRow ) {
          newDetailTableModels.Add( tempDetailTableRow ) ;
        }
        else {
          newDetailTableModels.Add( detailTableRow ) ;
        }
      }

      detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      return true ;
    }

    public static void SplitPlumbing( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, DetailTableViewModel detailTableViewModel, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      var detailTableModels = detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.WireType ) && ! string.IsNullOrEmpty( d.WireSize ) && ! string.IsNullOrEmpty( d.WireStrip ) 
                                                                                 && ! string.IsNullOrEmpty( d.WireBook ) && ! string.IsNullOrEmpty( d.SignalType ) && ! string.IsNullOrEmpty( d.ConstructionItems ) && ! string.IsNullOrEmpty( d.Remark ) ) ;
      foreach ( var detailTableRow in detailTableModels ) {
        SetPlumbingDataForEachWiring( conduitsModelData, detailSymbolStorable, detailTableRow, detailSymbolIdsWithPlumbingTypeHasChanged ) ;
      }
    }

    private static void SetPlumbingDataForEachWiring( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, DetailTableModel detailTableRow, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      const double percentage = 0.32 ;
      const int plumbingCount = 1 ;
      var plumbingType = detailSymbolIdsWithPlumbingTypeHasChanged.SingleOrDefault( d => d.Key == detailTableRow.DetailSymbolId ).Value ;
      if ( string.IsNullOrEmpty( plumbingType ) ) {
        plumbingType = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( s => s.DetailSymbolId == detailTableRow.DetailSymbolId )?.PlumbingType ?? DefaultParentPlumbingType ;
      }
      var wireBook = string.IsNullOrEmpty( detailTableRow.WireBook ) ? 1 : int.Parse( detailTableRow.WireBook ) ;
      if ( plumbingType == NoPlumping ) {
        detailTableRow.PlumbingType = NoPlumping ;
        detailTableRow.PlumbingSize = NoPlumbingSize ;
        detailTableRow.NumberOfPlumbing = string.Empty ;
        detailTableRow.PlumbingIdentityInfo = CreateDetailTableCommandBase.GetDetailTableRowPlumbingIdentityInfo( detailTableRow, false ) ;
        detailTableRow.GroupId = string.Empty ;
        detailTableRow.Remark = GetRemark( detailTableRow.Remark, wireBook ) ;
        detailTableRow.IsParentRoute = true ;
        detailTableRow.IsReadOnly = false ;
        detailTableRow.IsReadOnlyPlumbingItems = true ;
        detailTableRow.IsMixConstructionItems = false ;
        detailTableRow.IsReadOnlyPlumbingSize = true ;
      }
      else {
        var conduitsModels = conduitsModelData.Where( c => c.PipingType == plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
        var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
        var currentPlumbingCrossSectionalArea = detailTableRow.WireCrossSectionalArea / percentage * wireBook ;
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

        detailTableRow.Remark = GetRemark( detailTableRow.Remark, wireBook ) ;
        detailTableRow.NumberOfPlumbing = plumbingCount.ToString() ;
        detailTableRow.PlumbingIdentityInfo = CreateDetailTableCommandBase.GetDetailTableRowPlumbingIdentityInfo( detailTableRow, false ) ;
        detailTableRow.GroupId = string.Empty ;
        detailTableRow.IsParentRoute = true ;
        detailTableRow.IsReadOnly = false ;
        detailTableRow.IsReadOnlyPlumbingItems = true ;
        detailTableRow.IsReadOnlyPlumbingSize = false ;
        detailTableRow.IsMixConstructionItems = false ;
        if ( detailTableRow.PlumbingSizes.Any() ) return ;
        {
          var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableRow.PlumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
        }
      }
    }
    
    public enum EditedColumn
    {
      Floor,
      WireType,
      WireSize,
      WireStrip,
      WireBook,
      EarthType,
      EarthSize,
      NumberOfGrounds,
      PlumbingSize,
      ConstructionClassification,
      SignalType,
      Remark
    }

    public static void ComboboxSelectionChanged( DetailTableViewModel detailTableViewModel, DetailTableViewModel detailTableViewModelSummary, DetailTableModel editedDetailTableRow, EditedColumn editedColumn, string changedValue, List<DetailTableModel.ComboboxItemType> itemSourceCombobox, double crossSectionalArea = 0 )
    {
      if ( ! string.IsNullOrEmpty( editedDetailTableRow.GroupId ) ) {
        var detailTableRows = detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == editedDetailTableRow.GroupId ).ToList() ;
        foreach ( var detailTableRow in detailTableRows ) {
          UpdateDetailTableModelRow( detailTableRow, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
        }
      }
      else {
        var detailTableRow = detailTableViewModel.DetailTableModels.FirstOrDefault( d => d == editedDetailTableRow ) ;
        if ( detailTableRow != null ) UpdateDetailTableModelRow( detailTableRow, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
      }

      var selectedDetailTableRowSummary = detailTableViewModelSummary.DetailTableModels.FirstOrDefault( d => d == editedDetailTableRow ) ;
      if ( selectedDetailTableRowSummary != null ) UpdateDetailTableModelRow( selectedDetailTableRowSummary, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
    }

    private static void UpdateDetailTableModelRow( DetailTableModel detailTableModelRow, EditedColumn editedColumn, string changedValue, double crossSectionalArea, List<DetailTableModel.ComboboxItemType> itemSourceCombobox )
    {
      switch ( editedColumn ) {
        case EditedColumn.Floor:
          detailTableModelRow.Floor = changedValue ;
          break;
        case EditedColumn.WireType:
          detailTableModelRow.WireType = changedValue ;
          detailTableModelRow.WireSizes = itemSourceCombobox ;
          break;
        case EditedColumn.WireSize:
          detailTableModelRow.WireSize = changedValue ;
          detailTableModelRow.WireStrips = itemSourceCombobox ;
          break;
        case EditedColumn.WireStrip:
          detailTableModelRow.WireStrip = changedValue ;
          detailTableModelRow.WireCrossSectionalArea = crossSectionalArea ;
          break;
        case EditedColumn.WireBook:
          detailTableModelRow.WireBook = changedValue ;
          break;
        case EditedColumn.EarthType:
          detailTableModelRow.EarthType = changedValue ;
          detailTableModelRow.EarthSizes = itemSourceCombobox ;
          break;
        case EditedColumn.EarthSize:
          detailTableModelRow.EarthSize = changedValue ;
          break;
        case EditedColumn.NumberOfGrounds:
          detailTableModelRow.NumberOfGrounds = changedValue ;
          break;
        case EditedColumn.PlumbingSize:
          detailTableModelRow.PlumbingSize = changedValue ;
          break;
        case EditedColumn.ConstructionClassification:
          detailTableModelRow.ConstructionClassification = changedValue ;
          break;
        case EditedColumn.SignalType:
          detailTableModelRow.SignalType = changedValue ;
          break;
        case EditedColumn.Remark :
          detailTableModelRow.Remark = changedValue ;
          break ;
      }
    }

    public static string GetRemark( string oldRemark, int wireBook )
    {
      const char multiplicationSymbol = 'x' ;
      var remarks = oldRemark.Split( multiplicationSymbol ) ;
      if ( ! remarks.Any() ) return string.Empty ;
      var newRemarks = wireBook > 1 ? remarks.First() + multiplicationSymbol + wireBook : remarks.First() ;
      return newRemarks ;
    }

    public event PropertyChangedEventHandler? PropertyChanged ;

    private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }
  }
}