using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.IO ;
using System.Linq ;
using System.Runtime.CompilerServices ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using DataGrid = System.Windows.Controls.DataGrid ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DetailTableViewModel2  : ViewModelBase, INotifyPropertyChanged
  {
    private const string DefaultParentPlumbingType = "E" ;
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;
    private static string MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage =
      "Construction categories are mixed in the detail symbol {0}. Would you like to proceed to create the detail table?" ;
    private readonly Document _document ;
    
    public DataGrid DtGrid ;
    public DataGrid DtReferenceGrid ;
    

    public List<ConduitsModel> ConduitsModelData ;
    public List<WiresAndCablesModel> WiresAndCablesModelData ;
   // public DetailTableViewModel DetailTableViewModel ;
    public DetailSymbolStorable DetailSymbolStorable ;
    public List<DetailTableModel> SelectedDetailTableRows ;
    public List<DetailTableModel> SelectedDetailTableRowsSummary ;
    public List<DetailTableModel> SelectedReferenceDetailTableRows ;
    public DetailTableModel? CopyDetailTableRow ;
    public DetailTableModel? CopyDetailTableRowSummary ;
    
    //public DetailTableViewModel2 DetailTableViewModelSummary { get ; set ; }
    public Dictionary<string, string> RoutesWithConstructionItemHasChanged { get ; }
    public Dictionary<string, string> DetailSymbolIdsWithPlumbingTypeHasChanged { get ; }
    public bool IsMixConstructionItems ;
    
    private ObservableCollection<DetailTableModel> _detailTableModelsOrigin ;
    
    public ObservableCollection<DetailTableModel> DetailTableModelsOrigin => _detailTableModelsOrigin ;

    private ObservableCollection<DetailTableModel> _detailTableModels ;
    public ObservableCollection<DetailTableModel> DetailTableModels { 
      get => _detailTableModels ;
      set
      {
        _detailTableModels = value ;
        OnPropertyChanged( nameof(DetailTableModels) );
      } 
    }
    
    private ObservableCollection<DetailTableModel> _referenceDetailTableModelsOrigin { get ; set ; }
    
    public ObservableCollection<DetailTableModel> ReferenceDetailTableModels { get ; set ; }
    
    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }
    
    public  bool IsCancelCreateDetailTable { get; set; }
    
    public bool IsAddReference { get ; set ; }

    //public ICommand SaveDetailTableCommand { get; }

    //public ICommand CreateDetailTableCommand { get ; }

    public List<DetailTableModel.ComboboxItemType> ConduitTypes { get ;}

    public List<DetailTableModel.ComboboxItemType> ConstructionItems { get ; }
    
    public List<DetailTableModel.ComboboxItemType> Levels { get ; }

    public List<DetailTableModel.ComboboxItemType> WireTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> EarthTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> Numbers { get ; }
    
    public List<DetailTableModel.ComboboxItemType> ConstructionClassificationTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> SignalTypes { get ; }
    
    public ICommand SaveDetailTableCommand => new RelayCommand<Window>( SaveDetailTable ) ;
    
    public ICommand CreateDetailTableCommand => new RelayCommand<Window>( CreateDetailTable ) ;
    
    public ICommand CompletedCommand => new RelayCommand<Window>( Completed ) ;

    public ICommand AddCommand => new RelayCommand( Add ) ;
    
    public ICommand DeleteLineCommand => new RelayCommand( DeleteLine ) ;
    
    public ICommand CopyLineCommand => new RelayCommand( CopyLine ) ;
    
    public ICommand PasteLineCommand => new RelayCommand( PasteLine ) ;
    
    public ICommand SelectionChangedCommand => new RelayCommand( SelectionChanged ) ;
    
    public ICommand DoubleClickCommand => new RelayCommand( RowDoubleClick ) ;
    
    public ICommand SelectAllCommand => new RelayCommand( SelectAll ) ;
    
    public ICommand MoveUpCommand => new RelayCommand( MoveUp ) ;
    
    public ICommand MoveDownCommand => new RelayCommand( MoveDown ) ;
    
    public ICommand SplitPlumbingCommand => new RelayCommand( SplitPlumbing ) ;
    
    public ICommand PlumbingSummaryCommand => new RelayCommand( PlumbingSummary ) ;
    
    public ICommand PlumbingSummaryMixConstructionItemsCommand => new RelayCommand( PlumbingSummaryMixConstructionItems ) ;
    
    public ICommand ReferenceSelectAllCommand => new RelayCommand( ReferenceSelectAll ) ;
    
    public ICommand DeleteReferenceLineCommand => new RelayCommand( DeleteReferenceLine ) ;
    
    public ICommand ReadCtlFileCommand => new RelayCommand( ReadCtlFile ) ;
    
    public ICommand FloorSelectionChangedCommand => new RelayCommand( FloorSelectionChanged ) ; //
    
    public ICommand SelectDetailTableRowWithSameDetailSymbolIdCommand => new RelayCommand( SelectDetailTableRowWithSameDetailSymbolId ) ;
    
    public ICommand AddReferenceCommand => new RelayCommand<Window>( AddReference ) ;
    
    public ICommand AddReferenceRowsCommand => new RelayCommand( AddReferenceRows ) ;
    
    public ICommand SelectionChangedReferenceCommand => new RelayCommand( SelectionChangedReference ) ;

    private void SelectionChangedReference( )
    {
      var selectedItems = DtReferenceGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      SelectedReferenceDetailTableRows.Clear() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableModel detailTableRow ) continue ;
        SelectedReferenceDetailTableRows.Add( detailTableRow ) ;
      }
    }
    
    private void AddReferenceRows()
    {
      if ( ! SelectedReferenceDetailTableRows.Any() ) return ;
      AddReferenceDetailTableRows(SelectedReferenceDetailTableRows ) ;
    }
    private void AddReference( Window window )
    {
      window.DialogResult = false ;
      IsAddReference = true ;
      window.Close() ;
    }
    
    private void SelectDetailTableRowWithSameDetailSymbolId ( )
    {
      if ( ! SelectedReferenceDetailTableRows.Any() ) return ;
      var detailTableRowsWithSameDetailSymbolId = SelectDetailTableRowsWithSameDetailSymbolId(SelectedReferenceDetailTableRows ) ;
      SelectedReferenceDetailTableRows.Clear() ;
      DtReferenceGrid.SelectedItems.Clear() ;
      foreach ( var detailTableModelRow in detailTableRowsWithSameDetailSymbolId ) {
        DtReferenceGrid.SelectedItems.Add( detailTableModelRow ) ;
      }
    }
    
    private void FloorSelectionChanged( )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedFloor = comboBox.SelectedValue ;
      // if ( selectedFloor == null ) return ;
      
   
      //ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.Floor, selectedFloor.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void ReadCtlFile()
    {
      ReadCtlFile( ConduitsModelData, WiresAndCablesModelData ) ;
      UpdateReferenceDetailTableModels() ;
    }

    private void DeleteReferenceLine()
    {
      if ( ! SelectedReferenceDetailTableRows.Any() ) return ;
      DeleteReferenceDetailTableRows(SelectedReferenceDetailTableRows) ;
  //    UpdateReferenceDetailTableModels() ;
    }
    
    private void UpdateReferenceDetailTableModels()
    {
      SelectedReferenceDetailTableRows.Clear() ;
      DtReferenceGrid.SelectedItems.Clear() ;
    }
    
    private void ReferenceSelectAll()
    {
       SelectedReferenceDetailTableRows = ReferenceDetailTableModels.ToList() ;
       DtReferenceGrid.SelectAll() ;
    }
    
    private void PlumbingSummary()
    {
      if ( ! SelectedDetailTableRows.Any() ) return;
      IsMixConstructionItems = false ;
      PlumbingSum() ;
    }
    
    private void PlumbingSummaryMixConstructionItems()
    {
      if ( ! SelectedDetailTableRows.Any() ) return ;
      IsMixConstructionItems = true ;
      PlumbingSum() ;
    }
    
    private void PlumbingSum()
    {
      PlumbingSummary( ConduitsModelData, DetailSymbolStorable, SelectedDetailTableRows, IsMixConstructionItems, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
       DtGrid.SelectedItems.Clear() ;
    }
    
    private void SplitPlumbing()
    {
      SplitPlumbing( ConduitsModelData, DetailSymbolStorable, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
    }

    private void MoveUp()
    {
      MoveDetailTableRow( true ) ;
    }
    
    private void MoveDown()
    {
      MoveDetailTableRow( false ) ;
    }
    
    private void MoveDetailTableRow( bool isMoveUp )
    {
      if ( ! SelectedDetailTableRows.Any() || ! SelectedDetailTableRowsSummary.Any() ) return ;
      var selectedDetailTableRow = SelectedDetailTableRows.First() ;
      var selectedDetailTableRowSummary = SelectedDetailTableRowsSummary.First() ;
      var isMove = MoveDetailTableRow(  selectedDetailTableRow, selectedDetailTableRowSummary, isMoveUp ) ;
      if ( isMove ) UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void Add()
    {
      if ( ! SelectedDetailTableRows.Any() || ! SelectedDetailTableRowsSummary.Any() ) return ;
      var selectedDetailTableRow = SelectedDetailTableRows.Last() ;
      var selectedDetailTableRowSummary = SelectedDetailTableRowsSummary.Last() ;
      AddDetailTableRow(  selectedDetailTableRow, selectedDetailTableRowSummary ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void DeleteLine()
    {
      if ( ! SelectedDetailTableRows.Any() || ! SelectedDetailTableRowsSummary.Any() ) return ;
      DeleteDetailTableRows() ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    private void CopyLine()
    {
      if ( ! SelectedDetailTableRows.Any() || ! SelectedDetailTableRowsSummary.Any() ) return ;
      CopyDetailTableRow = SelectedDetailTableRows.First() ;
      CopyDetailTableRowSummary = SelectedDetailTableRowsSummary.First() ;
      ResetSelectedItems() ;
    }
    
    private void PasteLine()
    {
      if ( CopyDetailTableRow == null || CopyDetailTableRowSummary == null ) {
        MessageBox.Show( "Please choose a row to copy", "Message" ) ;
        return ;
      }
      
      var pasteDetailTableRow = !  SelectedDetailTableRows.Any() ? CopyDetailTableRow : SelectedDetailTableRows.First() ;
      var pasteDetailTableRowSummary = ! SelectedDetailTableRowsSummary.Any() ? CopyDetailTableRowSummary : SelectedDetailTableRowsSummary.First() ;
      PasteDetailTableRow(pasteDetailTableRow, pasteDetailTableRowSummary) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void SelectionChanged()
    {
      var selectedItems = DtGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      ResetSelectedItems() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableModel detailTableRow ) continue ;
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRows = _detailTableModelsOrigin.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == detailTableRow.GroupId ).ToList() ;
          SelectedDetailTableRows.AddRange( detailTableRows ) ;
        }
        else {
          SelectedDetailTableRows.Add( detailTableRow ) ;
        }
        SelectedDetailTableRowsSummary.Add( detailTableRow ) ;
      }
    }
    
    private void RowDoubleClick()
    {
      var selectedItem = (DetailTableModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedItem.GroupId ) ) return ;
      UnGroupDetailTableRows( selectedItem.GroupId ) ;
      CreateDetailTableViewModelByGroupId() ;
    }
    
    private void UnGroupDetailTableRows( string groupId )
    {
      var detailTableModels = _detailTableModelsOrigin.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId ).ToList() ;
      foreach ( var detailTableRow in detailTableModels ) {
        detailTableRow.GroupId = string.Empty ;
      }
    }

    private void SelectAll()
    {
      SelectedDetailTableRows = _detailTableModelsOrigin.ToList() ;
      SelectedDetailTableRowsSummary = _detailTableModels.ToList() ;
      DtGrid.SelectAll() ;
    }

    public DetailTableViewModel2(Document document , ObservableCollection<DetailTableModel> detailTableModels,  ObservableCollection<DetailTableModel> referenceDetailTableModels, List<DetailTableModel.ComboboxItemType> conduitTypes, List<DetailTableModel.ComboboxItemType> constructionItems,
      List<DetailTableModel.ComboboxItemType> levels, List<DetailTableModel.ComboboxItemType> wireTypes, List<DetailTableModel.ComboboxItemType> earthTypes, 
      List<DetailTableModel.ComboboxItemType> numbers, List<DetailTableModel.ComboboxItemType> constructionClassificationTypes, List<DetailTableModel.ComboboxItemType> signalTypes
      ,List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData, bool mixConstructionItems)
    {
      _detailTableModelsOrigin =  new ObservableCollection<DetailTableModel>(detailTableModels)  ;
      _detailTableModels = new ObservableCollection<DetailTableModel>(detailTableModels) ;
      _document = document ;
      DtGrid = new DataGrid() ;
      DtReferenceGrid = new DataGrid() ;

      _referenceDetailTableModelsOrigin = new ObservableCollection<DetailTableModel>(referenceDetailTableModels) ;
      ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>(referenceDetailTableModels) ;
      IsCreateDetailTableOnFloorPlanView = false ;
      IsAddReference = false ;

      ConduitTypes = conduitTypes ;
      ConstructionItems = constructionItems ;
      Levels = levels ;
      WireTypes = wireTypes ;
      EarthTypes = earthTypes ;
      Numbers = numbers ;
      ConstructionClassificationTypes = constructionClassificationTypes ;
      SignalTypes = signalTypes ;

      ConduitsModelData = conduitsModelData ;
      WiresAndCablesModelData = wiresAndCablesModelData ;
      IsMixConstructionItems = mixConstructionItems ;
      RoutesWithConstructionItemHasChanged = new Dictionary<string, string>() ;
      DetailSymbolIdsWithPlumbingTypeHasChanged = new Dictionary<string, string>() ;
      DetailSymbolStorable = document.GetDetailSymbolStorable() ;
      SelectedDetailTableRows = new List<DetailTableModel>() ;
      SelectedDetailTableRowsSummary = new List<DetailTableModel>() ;
      CopyDetailTableRow = null ;
      CopyDetailTableRowSummary = null ;
      SelectedReferenceDetailTableRows = new List<DetailTableModel>() ;

      CreateDetailTableViewModelByGroupId() ;
    }
    
    private void CreateDetailTableViewModelByGroupId()
    {
      List<DetailTableModel> newDetailTableModels = GroupDetailTableModels(_detailTableModelsOrigin) ;
      List<DetailTableModel> newReferenceDetailTableModels = GroupDetailTableModels(ReferenceDetailTableModels) ;
      DetailTableModels = new ObservableCollection<DetailTableModel>(newDetailTableModels)  ;
      
      if ( newReferenceDetailTableModels.Any() ) {
        ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>(newReferenceDetailTableModels);
      }
    }
    
    private void SaveDetailTable(Window window)
    {
      SaveData( _document, _detailTableModelsOrigin ) ;
      SaveDetailSymbolData( _document, DetailSymbolStorable ) ;
      window.DialogResult = true ;
      window.Close();
      
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
    
    private void CreateDetailTable(Window window)
    {
      var confirmResult = MessageBoxResult.OK ;
      var mixtureOfMultipleConstructionClassificationsInDetailSymbol = string.Empty ;
      if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableModelsOrigin, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) )
        confirmResult = MessageBox.Show( string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), mixtureOfMultipleConstructionClassificationsInDetailSymbol ), "Warning", MessageBoxButton.OKCancel ) ;
      if ( confirmResult == MessageBoxResult.OK ) {
        SaveData( _document, _detailTableModelsOrigin ) ;
        SaveDetailSymbolData( _document, DetailSymbolStorable ) ;
        window.DialogResult = true ;
        window.Close() ;
      }
      
      IsCancelCreateDetailTable = confirmResult == MessageBoxResult.Cancel ;
      
      if ( IsCancelCreateDetailTable ) return ;
      IsCreateDetailTableOnFloorPlanView = true ;
    }

    private bool IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol(ObservableCollection<DetailTableModel> detailTableModels, ref string mixtureOfMultipleConstructionClassificationsInDetailSymbol )
    {
      var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy( d => d.DetailSymbol ) ;
      var mixSymbolGroup = detailTableModelsGroupByDetailSymbolId.Where( x => x.GroupBy( y => y.ConstructionClassification ).Count() > 1 ).ToList() ;
      mixtureOfMultipleConstructionClassificationsInDetailSymbol = mixSymbolGroup.Any()
        ? string.Join( ", ", mixSymbolGroup.Select( y => y.Key ).Distinct() )
        : string.Empty ;
      return !string.IsNullOrEmpty( mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ;
    }

    public void UnGroupDetailTableRowsAfterChangeConstructionItems( ObservableCollection<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
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
    
    private void Completed(Window window)
    {
      SaveData( _document, _detailTableModelsOrigin ) ;
      SaveDetailSymbolData( _document, DetailSymbolStorable ) ;
      window.DialogResult = true ;
      window.Close() ;
    }

    public void UpdatePlumbingItemsAfterChangeConstructionItems( ObservableCollection<DetailTableModel> detailTableModels, string routeName, string constructionItems )
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

    public void SetGroupIdForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
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

    public void SetGroupIdForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
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

    public void SetPlumbingItemsForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
    {
      foreach ( var detailTableRow in detailTableRowsWithSameDetailSymbolId ) {
        detailTableRow.PlumbingItems = detailTableRow.ConstructionItems ;
        detailTableRow.PlumbingItemTypes = new List<DetailTableModel.ComboboxItemType> { new( detailTableRow.ConstructionItems, detailTableRow.ConstructionItems ) } ;
      }
    }

    public void SetPlumbingItemsForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
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

    public void SortDetailTableModel( ref List<DetailTableModel> detailTableModels, bool isMixConstructionItems )
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
    
    private void SortDetailTableRows( List<DetailTableModel> sortedDetailTableModelsList, List<DetailTableModel> detailTableRowsWithSameSignalType, bool isMixConstructionItems )
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
    
    public void SaveData( Document document, IReadOnlyCollection<DetailTableModel> detailTableRowsBySelectedDetailSymbols )
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
    
    public void SaveDetailSymbolData( Document document, DetailSymbolStorable detailSymbolStorable )
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

    private void DeleteDetailTableRows()
    {
      List<DetailTableModel> deletedDetailTableRows = new() ;
      foreach ( var selectedItem in SelectedDetailTableRows ) {
        if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
          var selectedItems = _detailTableModelsOrigin.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId ).ToList() ;
          deletedDetailTableRows.AddRange( selectedItems ) ;
          foreach ( var item in selectedItems ) {
            var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = _detailTableModelsOrigin.Count( d => d.DetailSymbolId == item.DetailSymbolId && d.RouteName == item.RouteName && d != item ) ;
            if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
              var detailSymbolModels = DetailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == item.DetailSymbolId && s.RouteName == item.RouteName ).ToList() ;
              foreach ( var detailSymbolModel in detailSymbolModels ) {
                DetailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
              }
            }
          }
        }
        else {
          var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = _detailTableModelsOrigin.Count( d => d.DetailSymbolId == selectedItem.DetailSymbolId && d.RouteName == selectedItem.RouteName && d != selectedItem ) ;
          if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
            var detailSymbolModels = DetailSymbolStorable.DetailSymbolModelData.Where( s => s.DetailSymbolId == selectedItem.DetailSymbolId && s.RouteName == selectedItem.RouteName ).ToList() ;
            foreach ( var detailSymbolModel in detailSymbolModels ) {
              DetailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
            }
          }

          deletedDetailTableRows.Add( selectedItem ) ;
        }
      }
      
      var detailTableRows = _detailTableModelsOrigin.Where( d => ! deletedDetailTableRows.Contains( d ) ) ;
      _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( detailTableRows ) ;
      
      var detailTableRowsSummary = _detailTableModelsOrigin.Where( d => ! SelectedDetailTableRowsSummary.Contains( d ) ) ;
      DetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRowsSummary ) ;
    }
    
    private void UpdateDataGridAndRemoveSelectedRow()
    {
      ResetSelectedItems() ;
    }
    
    private void ResetSelectedItems()
    {
      SelectedDetailTableRows.Clear() ;
      SelectedDetailTableRowsSummary.Clear() ;
    }

    public void PasteDetailTableRow(DetailTableModel pasteDetailTableRow, DetailTableModel pasteDetailTableRowSummary)
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      
      var newDetailTableRow = new DetailTableModel( false, CopyDetailTableRow?.Floor, CopyDetailTableRow?.CeedCode, CopyDetailTableRow?.DetailSymbol, 
        CopyDetailTableRow?.DetailSymbolId, CopyDetailTableRow?.WireType, CopyDetailTableRow?.WireSize, CopyDetailTableRow?.WireStrip, CopyDetailTableRow?.WireBook, CopyDetailTableRow?.EarthType, 
        CopyDetailTableRow?.EarthSize, CopyDetailTableRow?.NumberOfGrounds, CopyDetailTableRow?.PlumbingType, CopyDetailTableRow?.PlumbingSize, CopyDetailTableRow?.NumberOfPlumbing, 
        CopyDetailTableRow?.ConstructionClassification, CopyDetailTableRow?.SignalType, CopyDetailTableRow?.ConstructionItems, CopyDetailTableRow?.PlumbingItems, CopyDetailTableRow?.Remark, 
        CopyDetailTableRow?.WireCrossSectionalArea, CopyDetailTableRow?.CountCableSamePosition, CopyDetailTableRow?.RouteName, CopyDetailTableRow?.IsEcoMode, CopyDetailTableRow?.IsParentRoute, 
        CopyDetailTableRow?.IsReadOnly, CopyDetailTableRow?.PlumbingIdentityInfo + index, string.Empty, CopyDetailTableRow?.IsReadOnlyPlumbingItems,
        CopyDetailTableRow?.IsMixConstructionItems, index, CopyDetailTableRow?.IsReadOnlyParameters, CopyDetailTableRow?.IsReadOnlyWireSizeAndWireStrip, CopyDetailTableRow?.IsReadOnlyPlumbingSize,
        CopyDetailTableRow?.WireSizes, CopyDetailTableRow?.WireStrips, CopyDetailTableRow?.EarthSizes, CopyDetailTableRow?.PlumbingSizes, CopyDetailTableRow?.PlumbingItemTypes ) ;
      foreach ( var detailTableRow in _detailTableModelsOrigin ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == pasteDetailTableRow ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      
      newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == pasteDetailTableRowSummary ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
    }

    public void PlumbingSummary( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, List<DetailTableModel> selectedDetailTableRows, bool isMixConstructionItems, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      Dictionary<DetailTableModel, List<DetailTableModel>> sortDetailTableModel = new() ;
      var detailTableModelsGroupByDetailSymbolId = 
        _detailTableModelsOrigin
          .Where( selectedDetailTableRows.Contains )
          .Where( d => ! string.IsNullOrEmpty( d.WireType ) && ! string.IsNullOrEmpty( d.WireSize ) && ! string.IsNullOrEmpty( d.WireStrip ) && ! string.IsNullOrEmpty( d.WireBook ) && ! string.IsNullOrEmpty( d.SignalType ) && ! string.IsNullOrEmpty( d.ConstructionItems ) && ! string.IsNullOrEmpty( d.Remark ) )
          .GroupBy( d => d.DetailSymbolId )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var plumbingIdentityInfos = detailTableRowsWithSameDetailSymbolId.Select( d => d.PlumbingIdentityInfo ).Distinct().ToHashSet() ;
        var otherDetailTableRowsWithSamePlumbingIdentityInfo = _detailTableModelsOrigin
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
        foreach ( var detailTableRow in _detailTableModelsOrigin ) {
          if ( detailTableRow == parentDetailTableRow ) {
            newDetailTableModels.AddRange( detailTableRows ) ;
          }
          else if ( ! detailTableRows.Contains( detailTableRow ) ) {
            newDetailTableModels.Add( detailTableRow ) ;
          }
        }

        _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      }
    }

    private void AddDetailTableRow(DetailTableModel selectDetailTableRow, DetailTableModel selectDetailTableRowSummary )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var newDetailTableRow = new DetailTableModel( selectDetailTableRow.DetailSymbol, selectDetailTableRow.DetailSymbolId ) ;
      foreach ( var detailTableRow in _detailTableModelsOrigin ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == selectDetailTableRow ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      
      newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in DetailTableModels ) {
        newDetailTableModels.Add( detailTableRow ) ;
        if ( detailTableRow == selectDetailTableRowSummary ) {
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
      }

      DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
    }
    
    public bool MoveDetailTableRow(DetailTableModel selectDetailTableRow, DetailTableModel selectDetailTableRowSummary, bool isMoveUp )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var selectDetailTableRowSummaryIndex = DetailTableModels.FindIndex( d => d == selectDetailTableRowSummary ) ;
      if ( ( isMoveUp && selectDetailTableRowSummaryIndex == 0 ) || ( ! isMoveUp && selectDetailTableRowSummaryIndex == DetailTableModels.Count - 1 ) ) return false ;
      var tempDetailTableRowSummary = DetailTableModels.ElementAt( isMoveUp ? selectDetailTableRowSummaryIndex - 1 : selectDetailTableRowSummaryIndex + 1 ) ;
      foreach ( var detailTableRow in DetailTableModels ) {
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

      DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      
      newDetailTableModels = new List<DetailTableModel>() ;
      var selectDetailTableRowIndex = _detailTableModelsOrigin.FindIndex( d => d == selectDetailTableRow ) ;
      var tempDetailTableRow = _detailTableModelsOrigin.ElementAt( isMoveUp ? selectDetailTableRowIndex - 1 : selectDetailTableRowIndex + 1 ) ;
      foreach ( var detailTableRow in _detailTableModelsOrigin ) {
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

      _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      return true ;
    }

    private void SplitPlumbing( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      var detailTableModels = _detailTableModelsOrigin.Where( d => ! string.IsNullOrEmpty( d.WireType ) && ! string.IsNullOrEmpty( d.WireSize ) && ! string.IsNullOrEmpty( d.WireStrip ) 
                                                                                 && ! string.IsNullOrEmpty( d.WireBook ) && ! string.IsNullOrEmpty( d.SignalType ) && ! string.IsNullOrEmpty( d.ConstructionItems ) && ! string.IsNullOrEmpty( d.Remark ) ) ;
      foreach ( var detailTableRow in detailTableModels ) {
        SetPlumbingDataForEachWiring( conduitsModelData, detailSymbolStorable, detailTableRow, detailSymbolIdsWithPlumbingTypeHasChanged ) ;
      }
    }

    private void SetPlumbingDataForEachWiring( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, DetailTableModel detailTableRow, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
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

    public void ComboboxSelectionChanged( DetailTableViewModel2 detailTableViewModel, DetailTableViewModel2 detailTableViewModelSummary, DetailTableModel editedDetailTableRow, DetailTableViewModel.EditedColumn editedColumn, string changedValue, List<DetailTableModel.ComboboxItemType> itemSourceCombobox, double crossSectionalArea = 0 )
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

    private void UpdateDetailTableModelRow( DetailTableModel detailTableModelRow, DetailTableViewModel.EditedColumn editedColumn, string changedValue, double crossSectionalArea, List<DetailTableModel.ComboboxItemType> itemSourceCombobox )
    {
      switch ( editedColumn ) {
        case DetailTableViewModel.EditedColumn.Floor:
          detailTableModelRow.Floor = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.WireType:
          detailTableModelRow.WireType = changedValue ;
          detailTableModelRow.WireSizes = itemSourceCombobox ;
          break;
        case DetailTableViewModel.EditedColumn.WireSize:
          detailTableModelRow.WireSize = changedValue ;
          detailTableModelRow.WireStrips = itemSourceCombobox ;
          break;
        case DetailTableViewModel.EditedColumn.WireStrip:
          detailTableModelRow.WireStrip = changedValue ;
          detailTableModelRow.WireCrossSectionalArea = crossSectionalArea ;
          break;
        case DetailTableViewModel.EditedColumn.WireBook:
          detailTableModelRow.WireBook = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.EarthType:
          detailTableModelRow.EarthType = changedValue ;
          detailTableModelRow.EarthSizes = itemSourceCombobox ;
          break;
        case DetailTableViewModel.EditedColumn.EarthSize:
          detailTableModelRow.EarthSize = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.NumberOfGrounds:
          detailTableModelRow.NumberOfGrounds = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.PlumbingSize:
          detailTableModelRow.PlumbingSize = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.ConstructionClassification:
          detailTableModelRow.ConstructionClassification = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.SignalType:
          detailTableModelRow.SignalType = changedValue ;
          break;
        case DetailTableViewModel.EditedColumn.Remark :
          detailTableModelRow.Remark = changedValue ;
          break ;
      }
    }

    public string GetRemark( string oldRemark, int wireBook )
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

    public void DeleteReferenceDetailTableRows(List<DetailTableModel> selectedDetailTableModels )
    {
      var deletedDetailTableRows = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRowsOfGroup = _referenceDetailTableModelsOrigin.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          deletedDetailTableRows.AddRange( detailTableRowsOfGroup ) ;
        }
        else {
          deletedDetailTableRows.Add( detailTableRow ) ;
        }
      }
    
      var detailTableRows = _referenceDetailTableModelsOrigin.Where( d => ! deletedDetailTableRows.Contains( d ) ) ;
      _referenceDetailTableModelsOrigin = new ObservableCollection<DetailTableModel>( detailTableRows ) ;
    
      var detailTableRowsSummary = ReferenceDetailTableModels.Where( d => ! selectedDetailTableModels.Contains( d ) ) ;
      ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRowsSummary ) ;
    }
    
    public List<DetailTableModel> SelectDetailTableRowsWithSameDetailSymbolId(List<DetailTableModel> selectedDetailTableModels )
    {
      List<DetailTableModel> detailTableRowsWithSameDetailSymbolId = new() ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        var detailTableRows = ReferenceDetailTableModels.Where( d => d.DetailSymbolId == detailTableRow.DetailSymbolId ) ;
        detailTableRowsWithSameDetailSymbolId.AddRange( detailTableRows ) ;
      }

      return detailTableRowsWithSameDetailSymbolId ;
    }

    public void ReadCtlFile( List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
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
      var index = "-" + DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      foreach ( var detailTableRow in referenceDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) detailTableRow.GroupId += index ;
        detailTableRow.PlumbingIdentityInfo += index ;
        if ( detailTableRow.Remark.Contains( ',' ) || detailTableRow.Remark.Contains( 'x' ) ) {
          AddUnGroupDetailTableRows( _referenceDetailTableModelsOrigin, detailTableRow ) ; 
        }
        else {
          _referenceDetailTableModelsOrigin.Add( detailTableRow ) ;
        }
        ReferenceDetailTableModels.Add( detailTableRow ) ;
      }
    }

    private void AddUnGroupDetailTableRows( ObservableCollection<DetailTableModel> unGroupDetailTableModels, DetailTableModel detailTableRow )
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
    
    private DetailTableModel CreateParentDetailTableModel( DetailTableModel detailTableRow, string wireBook, string remarkRow )
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

    private DetailTableModel CreateChildDetailTableModel( DetailTableModel detailTableRow, string wireBook, string remarkRow )
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

    private void GetValuesForParametersOfDetailTableModels( List<DetailTableModel> detailTableModels, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
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

    public void AddReferenceDetailTableRows(List<DetailTableModel> selectedDetailTableModels )
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
          var detailTableRowsOfGroup = _referenceDetailTableModelsOrigin.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          foreach ( var detailTableRowOfGroup in detailTableRowsOfGroup ) {
            var newReferenceDetailTableRow = new DetailTableModel( detailTableRowOfGroup.CalculationExclusion, detailTableRowOfGroup.Floor, detailTableRowOfGroup.CeedCode, detailTableRowOfGroup.DetailSymbol, 
              detailTableRowOfGroup.DetailSymbolId, detailTableRowOfGroup.WireType, detailTableRowOfGroup.WireSize, detailTableRowOfGroup.WireStrip, detailTableRowOfGroup.WireBook, detailTableRowOfGroup.EarthType, 
              detailTableRowOfGroup.EarthSize, detailTableRowOfGroup.NumberOfGrounds, detailTableRowOfGroup.PlumbingType, detailTableRowOfGroup.PlumbingSize, detailTableRowOfGroup.NumberOfPlumbing, 
              detailTableRowOfGroup.ConstructionClassification, detailTableRowOfGroup.SignalType, detailTableRowOfGroup.ConstructionItems, detailTableRowOfGroup.PlumbingItems, detailTableRowOfGroup.Remark, 
              detailTableRowOfGroup.WireCrossSectionalArea, detailTableRowOfGroup.CountCableSamePosition, detailTableRowOfGroup.RouteName, detailTableRowOfGroup.IsEcoMode, detailTableRowOfGroup.IsParentRoute, 
              detailTableRowOfGroup.IsReadOnly, detailTableRowOfGroup.PlumbingIdentityInfo + "-" + index, groupId, detailTableRowOfGroup.IsReadOnlyPlumbingItems,
              detailTableRowOfGroup.IsMixConstructionItems, detailTableRowOfGroup.CopyIndex + index, detailTableRowOfGroup.IsReadOnlyParameters, detailTableRowOfGroup.IsReadOnlyWireSizeAndWireStrip,
              detailTableRowOfGroup.IsReadOnlyPlumbingSize, detailTableRowOfGroup.WireSizes, detailTableRowOfGroup.WireStrips, detailTableRowOfGroup.EarthSizes, detailTableRowOfGroup.PlumbingSizes, detailTableRowOfGroup.PlumbingItemTypes ) ;
            _detailTableModelsOrigin.Add( newReferenceDetailTableRow ) ;
          }
        }
        else {
          _detailTableModelsOrigin.Add( referenceDetailTableRow ) ;
        }

        DetailTableModels.Add( referenceDetailTableRow ) ;
      }
    }

    public List<DetailTableModel> GroupDetailTableModels( ObservableCollection<DetailTableModel> oldDetailTableModels )
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