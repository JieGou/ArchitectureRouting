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
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
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
    
    public ObservableCollection<DetailTableModel> ReferenceDetailTableModels { get ; set ; }
    
    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }
    
    public  bool IsCancelCreateDetailTable { get; set; }
    
    public bool IsAddReference { get ; set ; }

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

    public DetailTableViewModel( ObservableCollection<DetailTableModel> detailTableModels,  ObservableCollection<DetailTableModel> referenceDetailTableModels, List<DetailTableModel.ComboboxItemType> conduitTypes, List<DetailTableModel.ComboboxItemType> constructionItems,
      List<DetailTableModel.ComboboxItemType> levels, List<DetailTableModel.ComboboxItemType> wireTypes, List<DetailTableModel.ComboboxItemType> earthTypes, 
      List<DetailTableModel.ComboboxItemType> numbers, List<DetailTableModel.ComboboxItemType> constructionClassificationTypes, List<DetailTableModel.ComboboxItemType> signalTypes )
    {
      _detailTableModels = detailTableModels ;
      ReferenceDetailTableModels = referenceDetailTableModels ;
      IsCreateDetailTableOnFloorPlanView = false ;
      IsAddReference = false ;

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
        string line = string.Join( ";", item.Floor, item.CeedCode, item.DetailSymbol, item.DetailSymbolId, item.WireType, item.WireSize, item.WireStrip, item.WireBook, item.EarthType, item.EarthSize, item.NumberOfGrounds, 
          item.PlumbingType, item.PlumbingSize, item.NumberOfPlumbing, item.ConstructionClassification, item.SignalType, item.ConstructionItems, item.PlumbingItems, item.Remark, item.WireCrossSectionalArea, item.CountCableSamePosition,
          item.RouteName, item.IsEcoMode, item.IsParentRoute, item.IsReadOnly, item.PlumbingIdentityInfo, item.GroupId, item.IsReadOnlyPlumbingItems, item.IsMixConstructionItems, item.CopyIndex ) ;
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

    public static void SortDetailTableModel( ref List<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      List<DetailTableModel> sortedDetailTableModelsList = new() ;
      var detailTableModelsGroupByDetailSymbolId = detailTableModels.OrderBy( d => d.DetailSymbol ).GroupBy( d => d.DetailSymbolId ).Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsGroupByDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var signalTypes = (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType )) ;
        foreach ( var signalType in signalTypes ) {
          var detailTableRowsWithSameSignalType = detailTableRowsGroupByDetailSymbolId.Where( d => d.SignalType == signalType.GetFieldName() ).ToList() ;
          SortDetailTableRows( sortedDetailTableModelsList, detailTableRowsWithSameSignalType, isMixConstructionItems ) ;
        }

        var signalTypeNames = signalTypes.Select( s => s.GetFieldName() ) ;
        var detailTableRowsNotHaveSignalType = detailTableRowsGroupByDetailSymbolId.Where( d => ! signalTypeNames.Contains( d.SignalType ) ).ToList() ;
        SortDetailTableRows( sortedDetailTableModelsList, detailTableRowsNotHaveSignalType, isMixConstructionItems ) ;
      }

      detailTableModels = sortedDetailTableModelsList ;
    }
    
    private static void SortDetailTableRows( List<DetailTableModel> sortedDetailTableModelsList, List<DetailTableModel> detailTableRowsWithSameSignalType, bool isMixConstructionItems )
    {
      if ( ! isMixConstructionItems ) detailTableRowsWithSameSignalType = detailTableRowsWithSameSignalType.OrderBy( d => d.ConstructionItems ).ToList() ;
      var detailTableRowsGroupByPlumbingIdentityInfo = detailTableRowsWithSameSignalType.GroupBy( d => d.PlumbingIdentityInfo ).Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var sortedDetailTableModels = 
            detailTableRowsWithSamePlumbingIdentityInfo
              .OrderByDescending( x => x.IsParentRoute )
              .ThenBy( x => x.GroupId ) ;

        sortedDetailTableModelsList.AddRange( sortedDetailTableModels ) ;
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

    public static void DeleteDetailTableRows( DetailTableViewModel detailTableViewModel, List<DetailTableModel> selectedDetailTableModels, DetailTableViewModel detailTableViewModelSummary, List<DetailTableModel> selectedDetailTableModelsSummary, DetailSymbolStorable detailSymbolStorable )
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
      
      var detailTableRowsSummary = detailTableViewModelSummary.DetailTableModels.Where( d => ! selectedDetailTableModelsSummary.Contains( d ) ) ;
      detailTableViewModelSummary.DetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRowsSummary ) ;
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

    public static void ReadCtlFile( List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData, DetailTableViewModel detailTableViewModel, DetailTableViewModel detailTableViewModelSummary )
    {
      MessageBox.Show( "Please select ctl file.", "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = "Ctl files (*.ctl)|*.ctl", Multiselect = false } ;
      string filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }
      
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      var referenceDetailTableModels = ExcelToModelConverter.GetReferenceDetailTableModels( filePath ) ;
      GetValuesForParametersOfDetailTableModels( referenceDetailTableModels, conduitsModelData, wiresAndCablesModelData ) ;
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
        false, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, ! detailTableRow.IsMixConstructionItems, detailTableRow.IsMixConstructionItems, string.Empty,
        detailTableRow.IsReadOnlyParameters, detailTableRow.IsReadOnlyWireSizeAndWireStrip, detailTableRow.IsReadOnlyPlumbingSize, detailTableRow.WireSizes, detailTableRow.WireStrips, 
        detailTableRow.EarthSizes, detailTableRow.PlumbingSizes, detailTableRow.PlumbingItemTypes) ;
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
        true, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, true, detailTableRow.IsMixConstructionItems, string.Empty,
        detailTableRow.IsReadOnlyParameters, detailTableRow.IsReadOnlyWireSizeAndWireStrip, true, detailTableRow.WireSizes, detailTableRow.WireStrips, 
        detailTableRow.EarthSizes, detailTableRow.PlumbingSizes, detailTableRow.PlumbingItemTypes) ;
      return newDetailTableRow ;
    }

    private static void GetValuesForParametersOfDetailTableModels( List<DetailTableModel> detailTableModels, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
    {
      var detailTableRowsGroupByDetailSymbolId = detailTableModels.GroupBy( d => d.DetailSymbolId ).Select( d => d.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableRowsGroupByDetailSymbolId ) {
        var parentDetailTableRow = detailTableRowsWithSameDetailSymbolId.FirstOrDefault( d => d.IsParentRoute ) ;
        var plumbingType = parentDetailTableRow == null ? DefaultParentPlumbingType : parentDetailTableRow.PlumbingType ;
        var plumbingSizesOfPlumbingType = plumbingType == NoPlumping ? new List<string>() { NoPlumbingSize } 
            : conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
        var plumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
        var detailTableRowsGroupByPlumbingIdentityInfo = detailTableRowsWithSameDetailSymbolId.GroupBy( d => d.PlumbingIdentityInfo ).Select( d => d.ToList() ) ;
        foreach ( var detailTableRowsWithSamePlumbing in detailTableRowsGroupByPlumbingIdentityInfo ) {
          var constructionItems = detailTableRowsWithSamePlumbing.Select( d => d.ConstructionItems ).Distinct().ToList() ;
          var plumbingItemTypes = constructionItems.Any() ? ( from plumbingItem in constructionItems select new DetailTableModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
          foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
            var wireSizesOfWireType = wiresAndCablesModelData.Where( w => w.WireType == detailTableRow.WireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
            var wireSizes = wireSizesOfWireType.Any() ? ( from wireSizeType in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
              
            var wireStripsOfWireType = wiresAndCablesModelData.Where( w => w.WireType == detailTableRow.WireType && w.DiameterOrNominal == detailTableRow.WireSize ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
            var wireStrips = wireStripsOfWireType.Any() ? ( from wireStripType in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStripType, wireStripType ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
            
            detailTableRow.WireSizes = wireSizes ;
            detailTableRow.WireStrips = wireStrips ;
            detailTableRow.PlumbingSizes = plumbingSizes ;
            detailTableRow.PlumbingItemTypes = detailTableRow.IsMixConstructionItems ? plumbingItemTypes : new List<DetailTableModel.ComboboxItemType> { new( detailTableRow.ConstructionItems, detailTableRow.ConstructionItems ) } ;
            if ( string.IsNullOrEmpty( detailTableRow.EarthType ) ) continue ;
            var earthSizes = wiresAndCablesModelData.Where( c => c.WireType == detailTableRow.EarthType ).Select( c => c.DiameterOrNominal ).ToList() ;
            detailTableRow.EarthSizes = earthSizes.Any() ? ( from earthSize in earthSizes select new DetailTableModel.ComboboxItemType( earthSize, earthSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
          }
        }
      }
    }

    public static void AddReferenceDetailTableRows( DetailTableViewModel detailTableViewModel, DetailTableViewModel detailTableViewModelSummary, List<DetailTableModel> selectedDetailTableModels )
    {
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        var groupId = string.IsNullOrEmpty( detailTableRow.GroupId ) ? string.Empty : detailTableRow.GroupId + "-" + index ;
        var referenceDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeedCode, detailTableRow.DetailSymbol, 
          detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, detailTableRow.WireBook, detailTableRow.EarthType, 
          detailTableRow.EarthSize, detailTableRow.NumberOfGrounds, detailTableRow.PlumbingType, detailTableRow.PlumbingSize, detailTableRow.NumberOfPlumbing, 
          detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, detailTableRow.Remark, 
          detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, detailTableRow.IsParentRoute, 
          detailTableRow.IsReadOnly, detailTableRow.PlumbingIdentityInfo + "-" + index, groupId, detailTableRow.IsReadOnlyPlumbingItems,
          detailTableRow.IsMixConstructionItems, detailTableRow.CopyIndex + index, detailTableRow.IsReadOnlyParameters, detailTableRow.IsReadOnlyWireSizeAndWireStrip,
          detailTableRow.IsReadOnlyPlumbingSize, detailTableRow.WireSizes, detailTableRow.WireStrips, detailTableRow.EarthSizes, detailTableRow.PlumbingSizes, detailTableRow.PlumbingItemTypes ) ;
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRowsOfGroup = detailTableViewModel.ReferenceDetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          foreach ( var detailTableRowOfGroup in detailTableRowsOfGroup ) {
            var newReferenceDetailTableRow = new DetailTableModel( detailTableRowOfGroup.CalculationExclusion, detailTableRowOfGroup.Floor, detailTableRowOfGroup.CeedCode, detailTableRowOfGroup.DetailSymbol, 
              detailTableRowOfGroup.DetailSymbolId, detailTableRowOfGroup.WireType, detailTableRowOfGroup.WireSize, detailTableRowOfGroup.WireStrip, detailTableRowOfGroup.WireBook, detailTableRowOfGroup.EarthType, 
              detailTableRowOfGroup.EarthSize, detailTableRowOfGroup.NumberOfGrounds, detailTableRowOfGroup.PlumbingType, detailTableRowOfGroup.PlumbingSize, detailTableRowOfGroup.NumberOfPlumbing, 
              detailTableRowOfGroup.ConstructionClassification, detailTableRowOfGroup.SignalType, detailTableRowOfGroup.ConstructionItems, detailTableRowOfGroup.PlumbingItems, detailTableRowOfGroup.Remark, 
              detailTableRowOfGroup.WireCrossSectionalArea, detailTableRowOfGroup.CountCableSamePosition, detailTableRowOfGroup.RouteName, detailTableRowOfGroup.IsEcoMode, detailTableRowOfGroup.IsParentRoute, 
              detailTableRowOfGroup.IsReadOnly, detailTableRowOfGroup.PlumbingIdentityInfo + "-" + index, groupId, detailTableRowOfGroup.IsReadOnlyPlumbingItems,
              detailTableRowOfGroup.IsMixConstructionItems, detailTableRowOfGroup.CopyIndex + index, detailTableRowOfGroup.IsReadOnlyParameters, detailTableRowOfGroup.IsReadOnlyWireSizeAndWireStrip,
              detailTableRowOfGroup.IsReadOnlyPlumbingSize, detailTableRowOfGroup.WireSizes, detailTableRowOfGroup.WireStrips, detailTableRowOfGroup.EarthSizes, detailTableRowOfGroup.PlumbingSizes, detailTableRowOfGroup.PlumbingItemTypes ) ;
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
      const char multiplicationSymbol = 'x' ;
      List<DetailTableModel> newDetailTableModels = new() ;
      List<string> existedGroupIds = new() ;
      foreach ( var detailTableRow in oldDetailTableModels ) {
        if ( string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          newDetailTableModels.Add( detailTableRow ) ;
        }
        else {
          if ( existedGroupIds.Contains( detailTableRow.GroupId ) ) continue ;
          var detailTableRowWithSameWiringType = oldDetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          var detailTableRowsGroupByRemark = detailTableRowWithSameWiringType.GroupBy( d => d.Remark ).ToDictionary( g => g.Key, g => g.ToList() ) ;
          List<string> newRemark = new() ;
          var wireBook = 0 ;
          var numberOfGrounds = 0 ;
          foreach ( var (remark, detailTableRowsWithSameRemark) in detailTableRowsGroupByRemark ) {
            newRemark.Add( detailTableRowsWithSameRemark.Count == 1 ? remark : remark + multiplicationSymbol + detailTableRowsWithSameRemark.Count ) ;
            foreach ( var detailTableRowWithSameRemark in detailTableRowsWithSameRemark ) {
              if ( ! string.IsNullOrEmpty( detailTableRowWithSameRemark.WireBook ) ) {
                wireBook += int.Parse( detailTableRowWithSameRemark.WireBook ) ;
              }
              if ( ! string.IsNullOrEmpty( detailTableRowWithSameRemark.NumberOfGrounds ) ) {
                numberOfGrounds += int.Parse( detailTableRowWithSameRemark.NumberOfGrounds ) ;
              }
            }
          }

          var newDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeedCode, detailTableRow.DetailSymbol, 
            detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, wireBook > 0 ? wireBook.ToString() : string.Empty, detailTableRow.EarthType, 
            detailTableRow.EarthSize, numberOfGrounds > 0 ? numberOfGrounds.ToString() : string.Empty, detailTableRow.PlumbingType, detailTableRow.PlumbingSize, detailTableRow.NumberOfPlumbing, 
            detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, string.Join( ", ", newRemark ), 
            detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, detailTableRow.IsParentRoute, 
            detailTableRow.IsReadOnly, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, detailTableRow.IsReadOnlyPlumbingItems, detailTableRow.IsMixConstructionItems,
            detailTableRow.CopyIndex, detailTableRow.IsReadOnlyParameters, detailTableRow.IsReadOnlyWireSizeAndWireStrip, detailTableRow.IsReadOnlyPlumbingSize, detailTableRow.WireSizes, detailTableRow.WireStrips,
            detailTableRow.EarthSizes, detailTableRow.PlumbingSizes, detailTableRow.PlumbingItemTypes ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          existedGroupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      return newDetailTableModels ;
    }
  }
}