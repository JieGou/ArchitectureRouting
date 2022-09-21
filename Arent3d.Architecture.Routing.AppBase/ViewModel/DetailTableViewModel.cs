using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using ComboBox = System.Windows.Controls.ComboBox ;
using DataGrid = System.Windows.Controls.DataGrid ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using MoreLinq ;
using MessageBox = System.Windows.Forms.MessageBox ;
using TextBox = System.Windows.Controls.TextBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class DetailTableViewModel  : NotifyPropertyChanged
  {
    private const string DefaultParentPlumbingType = "E" ;
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;
    private const string MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage = "Construction categories are mixed in the detail symbol {0}. Would you like to proceed to create the detail table?" ;
    public const string DefaultChildPlumbingSymbol = "↑" ;
    private const string IncorrectDataErrorMessage = "Incorrect data." ;
    private const string CaptionErrorMessage = "Error" ;
    private const char MultiplicationSymbol = 'x' ;
    
    
    private readonly Document _document ;
    
    public DataGrid DtGrid ;
    public DataGrid DtReferenceGrid ;
    
    private readonly List<ConduitsModel> _conduitsModelData ;
    private readonly List<WiresAndCablesModel> _wiresAndCablesModelData ;
    private readonly StorageService<Level, DetailSymbolModel> _storageService ;
    private List<DetailTableItemModel> _selectedDetailTableItemRows ;
    private List<DetailTableItemModel> _selectedDetailTableItemRowsSummary ;
    private List<DetailTableItemModel> _selectedReferenceDetailTableItemRows ;
    private DetailTableItemModel? _copyDetailTableItemRow ;
    private DetailTableItemModel? _copyDetailTableItemRowSummary ;
    private readonly bool _isCallFromAddWiringInformationCommand ;
    
    public Dictionary<string, string> RoutesWithConstructionItemHasChanged { get ; }
    public Dictionary<string, string> DetailSymbolIdsWithPlumbingTypeHasChanged { get ; }
    private bool _isMixConstructionItems ;
    
    private ObservableCollection<DetailTableItemModel> _detailTableModelItemOrigins ;
    public ObservableCollection<DetailTableItemModel> DetailTableModelItemOrigins => _detailTableModelItemOrigins ??= new ObservableCollection<DetailTableItemModel>();

    private ObservableCollection<DetailTableItemModel> _detailTableItemItemModels ;
    public ObservableCollection<DetailTableItemModel> DetailTableItemModels { 
      get => _detailTableItemItemModels ;
      set
      {
        _detailTableItemItemModels = value ;
        OnPropertyChanged( nameof(DetailTableItemModels) );
      } 
    }

    public ObservableCollection<DetailTableItemModel> ReferenceDetailTableItemModelsOrigin { get ; set ; }
    
    private ObservableCollection<DetailTableItemModel> _referenceDetailTableItemItemModels ;
    public ObservableCollection<DetailTableItemModel> ReferenceDetailTableItemModels {  
      get => _referenceDetailTableItemItemModels ;
      set
      {
        _referenceDetailTableItemItemModels = value ;
        OnPropertyChanged( nameof(ReferenceDetailTableItemModels) );
      }  }
    
    public bool IsCreateDetailTableItemOnFloorPlanView { get ; set ; }

    public bool IsAddReference { get ; set ; }

    private bool _isShowSymbol ;

    public bool IsShowSymbol
    {
      get => _isShowSymbol ;
      set
      {
        _isShowSymbol = value ;
        OnPropertyChanged();
      }
    }

    private PointOnRoutePicker.PickInfo? _pickInfo ;

    public PointOnRoutePicker.PickInfo? PickInfo
    {
      get => _pickInfo ;
      set
      {
        _pickInfo = value ;
        var conduit = _pickInfo?.Element ;
        var storageService = new StorageService<Level, DetailSymbolModel>( ( (ViewPlan) _document.ActiveView ).GenLevel ) ;
        var detailSymbolModels = storageService.Data.DetailSymbolData.Where( x => !string.IsNullOrEmpty(x.DetailSymbolUniqueId) && x.ConduitUniqueId == conduit?.UniqueId && x.DetailSymbol == AddWiringInformationCommandBase.SpecialSymbol).EnumerateAll() ;
        IsShowSymbol = detailSymbolModels.Any() ;
      }
    }
    
    public List<DetailTableItemModel.ComboboxItemType> ConduitTypes { get ;}

    public List<DetailTableItemModel.ComboboxItemType> ConstructionItems { get ; }
    
    public List<DetailTableItemModel.ComboboxItemType> Levels { get ; }

    public List<DetailTableItemModel.ComboboxItemType> WireTypes { get ; }

    public List<DetailTableItemModel.ComboboxItemType> EarthTypes { get ; }

    public List<DetailTableItemModel.ComboboxItemType> Numbers { get ; }
    
    public List<DetailTableItemModel.ComboboxItemType> ConstructionClassificationTypes { get ; }

    public List<DetailTableItemModel.ComboboxItemType> SignalTypes { get ; }

    public ICommand SaveDetailTableCommand => new RelayCommand<Window>( SaveDetailTable ) ;
    
    public ICommand CreateDetailTableCommand => new RelayCommand<Window>( CreateDetailTable ) ;
    
    public ICommand CompletedCommand => new RelayCommand<Window>( Completed ) ;

    public ICommand AddCommand => new RelayCommand( Add ) ;
    
    public ICommand DeleteLineCommand => new RelayCommand( DeleteLine ) ;
    
    public ICommand CopyLineCommand => new RelayCommand( CopyLine ) ;
    
    public ICommand PasteLineCommand => new RelayCommand( PasteLine ) ;
    
    public ICommand DoubleClickCommand => new RelayCommand( RowDoubleClick ) ;
    
    public ICommand SelectAllCommand => new RelayCommand( SelectAll ) ;
    
    public ICommand MoveUpCommand => new RelayCommand( MoveUp ) ;
    
    public ICommand MoveDownCommand => new RelayCommand( MoveDown ) ;
    
    public ICommand SplitPlumbingCommand => new RelayCommand( SplitPlumbing ) ;
    
    public ICommand PlumbingSummaryCommand => new RelayCommand( PlumbingSummary ) ;
    
    public ICommand PlumbingSummaryMixConstructionItemsCommand => new RelayCommand( PlumbingSummaryMixConstructionItems ) ;

    public ICommand LoadedCommand => new RelayCommand( Loaded ) ;
    
    public ICommand ReferenceSelectAllCommand => new RelayCommand( ReferenceSelectAll ) ;
    
    public ICommand DeleteReferenceLineCommand => new RelayCommand( DeleteReferenceLine ) ;
    
    public ICommand ReadCtlFileCommand => new RelayCommand( ReadCtlFile ) ;
    
    public ICommand SelectDetailTableRowWithSameDetailSymbolIdCommand => new RelayCommand( SelectDetailTableRowWithSameDetailSymbolId ) ;
    
    public ICommand AddReferenceCommand => new RelayCommand<Window>( AddReference ) ;
    
    public ICommand AddReferenceRowsCommand => new RelayCommand( AddReferenceRows ) ;

    private void Loaded()
    {
      if ( DetailTableData.Instance.FirstLoaded )
        return ;

      if ( DetailTableItemModels.Any( x => x.IsMultipleConnector ) )
        return ;

      DtGrid.SelectAll() ;
      PlumbingSummaryMixConstructionItems() ;
      DetailTableData.Instance.FirstLoaded = true ;
    }

    private void SelectionChangedReference()
    {
      var selectedItems = DtReferenceGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      _selectedReferenceDetailTableItemRows.Clear() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableItemModel detailTableItemModel ) continue ;
        _selectedReferenceDetailTableItemRows.Add( detailTableItemModel ) ;
      }
    }
    
    private void AddReferenceRows()
    {
      SelectionChangedReference() ;
      if ( ! _selectedReferenceDetailTableItemRows.Any() ) {
        MessageBox.Show( "Arent3d.Architecture.Routing.AppBase.ViewModel.Select.ReferenceTable".GetAppStringByKeyOrDefault( "Please select the row on the reference detail table." ), "Arent Inc" ) ;
        return ;
      }
      AddReferenceDetailTableRows(_selectedReferenceDetailTableItemRows ) ;
      DtReferenceGrid.SelectedItems.Clear();
    }
    private void AddReference( Window window )
    {
      window.DialogResult = false ;
      IsAddReference = true ;
      window.Close() ;
    }
    
    private void SelectDetailTableRowWithSameDetailSymbolId ( )
    {
      SelectionChangedReference() ;
      if ( ! _selectedReferenceDetailTableItemRows.Any() ) return ;
      var detailTableRowsWithSameDetailSymbolId = SelectDetailTableRowsWithSameDetailSymbolId(_selectedReferenceDetailTableItemRows ) ;
      _selectedReferenceDetailTableItemRows.Clear() ;
      DtReferenceGrid.SelectedItems.Clear() ;
      foreach ( var detailTableModelRow in detailTableRowsWithSameDetailSymbolId ) {
        DtReferenceGrid.SelectedItems.Add( detailTableModelRow ) ;
      }
    }

    private void ReadCtlFile()
    {
      ReadCtlFile( _conduitsModelData, _wiresAndCablesModelData ) ;
      UpdateReferenceDetailTableModels() ;
    }

    private void DeleteReferenceLine()
    {
      SelectionChangedReference() ;
      if ( ! _selectedReferenceDetailTableItemRows.Any() ) return ;
      DeleteReferenceDetailTableRows(_selectedReferenceDetailTableItemRows) ;
      UpdateReferenceDetailTableModels() ;
    }
    
    private void UpdateReferenceDetailTableModels()
    {
      SelectionChangedReference() ;
      _selectedReferenceDetailTableItemRows.Clear() ;
      DtReferenceGrid.SelectedItems.Clear() ;
    }
    
    private void ReferenceSelectAll()
    {
      _selectedReferenceDetailTableItemRows = ReferenceDetailTableItemModels.ToList() ;
      DtReferenceGrid.SelectAll() ;
    }
    
    private void PlumbingSummary()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableItemRows.Any() ) return;
      _isMixConstructionItems = false ;
      PlumbingSum() ;
    }
    
    private void PlumbingSummaryMixConstructionItems()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableItemRows.Any() ) return ;
      _isMixConstructionItems = true ;
      PlumbingSum() ;
    }
    
    private void PlumbingSum()
    {
      SelectionChanged() ;
      SetGroup( _selectedDetailTableItemRows, true ) ;
      PlumbingSummary( _conduitsModelData, _storageService, _selectedDetailTableItemRows, _isMixConstructionItems, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
    }
    
    private void SplitPlumbing()
    {
      SelectionChanged() ;
      SetGroup(_detailTableModelItemOrigins, false);
      SplitPlumbing( _conduitsModelData, _storageService, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
    }

    private static void SetGroup(IEnumerable<DetailTableItemModel> detailTableItemModels, bool isGrouped)
    {
      foreach ( var detailTableItemModel in detailTableItemModels ) {
        detailTableItemModel.IsGrouped = isGrouped ;
      }
    }

    private void MoveUp()
    {
      SelectionChanged() ;
      MoveDetailTableRow( true ) ;
    }
    
    private void MoveDown()
    {
      SelectionChanged() ;
      MoveDetailTableRow( false ) ;
    }
    
    private void MoveDetailTableRow( bool isMoveUp )
    {
      if ( ! _selectedDetailTableItemRows.Any() || ! _selectedDetailTableItemRowsSummary.Any() ) return ;
      var selectedDetailTableRow = _selectedDetailTableItemRows.First() ;
      var selectedDetailTableRowSummary = _selectedDetailTableItemRowsSummary.First() ;
      var isMove = MoveDetailTableRow(  selectedDetailTableRow, selectedDetailTableRowSummary, isMoveUp ) ;
      if ( isMove ) UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void Add()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableItemRows.Any() || ! _selectedDetailTableItemRowsSummary.Any() ) return ;
      var selectedDetailTableRow = _selectedDetailTableItemRows.Last() ;
      var selectedDetailTableRowSummary = _selectedDetailTableItemRowsSummary.Last() ;
      AddDetailTableRow( _document,  selectedDetailTableRow, selectedDetailTableRowSummary ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void DeleteLine()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableItemRows.Any() || ! _selectedDetailTableItemRowsSummary.Any() ) return ;
      DeleteDetailTableRows() ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    private void CopyLine()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableItemRows.Any() || ! _selectedDetailTableItemRowsSummary.Any() ) return ;
      _copyDetailTableItemRow = _selectedDetailTableItemRows.First() ;
      _copyDetailTableItemRowSummary = _selectedDetailTableItemRowsSummary.First() ;
      ResetSelectedItems() ;
    }
    
    private void PasteLine()
    {
      SelectionChanged() ;
      if ( _copyDetailTableItemRow == null || _copyDetailTableItemRowSummary == null ) {
        MessageBox.Show( @"Please choose a row to copy", @"Message" ) ;
        return ;
      }
      
      var pasteDetailTableRow = !  _selectedDetailTableItemRows.Any() ? _copyDetailTableItemRow : _selectedDetailTableItemRows.First() ;
      var pasteDetailTableRowSummary = ! _selectedDetailTableItemRowsSummary.Any() ? _copyDetailTableItemRowSummary : _selectedDetailTableItemRowsSummary.First() ;
      PasteDetailTableRow(pasteDetailTableRow, pasteDetailTableRowSummary) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void SelectionChanged()
    {
      var selectedItems = DtGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      ResetSelectedItems() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableItemModel detailTableItemRow ) continue ;
        if ( ! string.IsNullOrEmpty( detailTableItemRow.GroupId ) ) {
          var detailTableRows = _detailTableModelItemOrigins
            .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == detailTableItemRow.GroupId )
            .ToList() ;
          _selectedDetailTableItemRows.AddRange( detailTableRows ) ;
        }
        else {
          _selectedDetailTableItemRows.Add( detailTableItemRow ) ;
        }
        _selectedDetailTableItemRowsSummary.Add( detailTableItemRow ) ;
      }
    }
    
    private void RowDoubleClick()
    {
      if ( DtGrid.SelectedValue == null ) return ;
      var selectedItem = (DetailTableItemModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedItem.GroupId ) ) return ;
      UnGroupDetailTableRows( selectedItem.GroupId ) ;
      CreateDetailTableViewModelByGroupId() ;
    }
    
    private void UnGroupDetailTableRows( string groupId )
    {
      var detailTableModels = _detailTableModelItemOrigins
        .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId )
        .ToList() ;
      foreach ( var detailTableRow in detailTableModels ) {
        detailTableRow.GroupId = string.Empty ;
      }
    }

    private void SelectAll()
    {
      _selectedDetailTableItemRows = _detailTableModelItemOrigins.ToList() ;
      _selectedDetailTableItemRowsSummary = _detailTableItemItemModels.ToList() ;
      DtGrid.SelectAll() ;
    }

    public DetailTableViewModel
    ( 
      Document document, 
      ObservableCollection<DetailTableItemModel> detailTableItemModels, 
      ObservableCollection<DetailTableItemModel> referenceDetailTableItemModels,
      IEnumerable<DetailTableItemModel.ComboboxItemType> conduitTypes, 
      IEnumerable<DetailTableItemModel.ComboboxItemType> constructionItems, 
      IEnumerable<DetailTableItemModel.ComboboxItemType> levels,
      IEnumerable<DetailTableItemModel.ComboboxItemType> wireTypes, 
      IEnumerable<DetailTableItemModel.ComboboxItemType> earthTypes, 
      IEnumerable<DetailTableItemModel.ComboboxItemType> numbers,
      IEnumerable<DetailTableItemModel.ComboboxItemType> constructionClassificationTypes, 
      IEnumerable<DetailTableItemModel.ComboboxItemType> signalTypes, 
      IEnumerable<ConduitsModel> conduitsModelData,
      IEnumerable<WiresAndCablesModel> wiresAndCablesModelData, 
      bool mixConstructionItems,
      bool isCallFromAddWiringInformationCommand = false
    )
    {
      _detailTableModelItemOrigins =  new ObservableCollection<DetailTableItemModel>(detailTableItemModels)  ;
      _detailTableItemItemModels = new ObservableCollection<DetailTableItemModel>(detailTableItemModels) ;
      _document = document ;
      DtGrid = new DataGrid() ;
      DtReferenceGrid = new DataGrid() ;
      ReferenceDetailTableItemModelsOrigin = new ObservableCollection<DetailTableItemModel>(referenceDetailTableItemModels) ;
      _referenceDetailTableItemItemModels = new ObservableCollection<DetailTableItemModel>(referenceDetailTableItemModels) ;
      IsCreateDetailTableItemOnFloorPlanView = false ;
      IsAddReference = false ;
      ConduitTypes = conduitTypes.ToList() ;
      ConstructionItems = constructionItems.ToList() ;
      Levels = levels.ToList() ;
      WireTypes = wireTypes.ToList() ;
      EarthTypes = earthTypes.ToList() ;
      Numbers = numbers.ToList() ;
      ConstructionClassificationTypes = constructionClassificationTypes.ToList() ;
      SignalTypes = signalTypes.ToList() ;
      _conduitsModelData = conduitsModelData.ToList() ;
      _wiresAndCablesModelData = wiresAndCablesModelData.ToList() ;
      _isMixConstructionItems = mixConstructionItems ;
      RoutesWithConstructionItemHasChanged = new Dictionary<string, string>() ;
      DetailSymbolIdsWithPlumbingTypeHasChanged = new Dictionary<string, string>() ;
      _storageService = new StorageService<Level, DetailSymbolModel>(((ViewPlan)_document.ActiveView).GenLevel) ;
      _selectedDetailTableItemRows = new List<DetailTableItemModel>() ;
      _selectedDetailTableItemRowsSummary = new List<DetailTableItemModel>() ;
      _copyDetailTableItemRow = null ;
      _copyDetailTableItemRowSummary = null ;
      _selectedReferenceDetailTableItemRows = new List<DetailTableItemModel>() ;
      _isCallFromAddWiringInformationCommand = isCallFromAddWiringInformationCommand ;
    }
    
    public void CreateDetailTableViewModelByGroupId( bool isGroupReferenceDetailTableModels = false )
    {
      List<DetailTableItemModel> newDetailTableModels = GroupDetailTableModels(_detailTableModelItemOrigins) ;
      List<DetailTableItemModel> newReferenceDetailTableModels = GroupDetailTableModels(ReferenceDetailTableItemModelsOrigin) ;
      DetailTableItemModels = new ObservableCollection<DetailTableItemModel>(newDetailTableModels)  ;
      
      if ( newReferenceDetailTableModels.Any() && isGroupReferenceDetailTableModels ) {
        ReferenceDetailTableItemModels = new ObservableCollection<DetailTableItemModel>(newReferenceDetailTableModels);
      }
    }
    
    private void SaveDetailTable(Window window)
    {
      var mixtureOfMultipleConstructionClassificationsInDetailSymbol = string.Empty ;
      if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableModelItemOrigins, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ) {
        MyMessageBox.Show(string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), mixtureOfMultipleConstructionClassificationsInDetailSymbol), "Warning") ;
      }
      else {
        SaveData( _document, _detailTableModelItemOrigins ) ;
        SaveDetailSymbolData( _document, _storageService ) ;
        window.DialogResult = true ;
        window.Close();
      
        const string defaultConstructionItems = "未設定" ;
        // Configure open file dialog box
        SaveFileDialog dlg = new() { FileName = string.Empty, DefaultExt = ".ctl", Filter = @"CTL files|*.ctl" } ;

        // Show open file dialog box
        var result = dlg.ShowDialog() ;

        // Process open file dialog box results
        if ( result != DialogResult.OK ) return ;
        string createText = string.Empty ;
        foreach ( var item in DetailTableItemModels ) {
          string line = string.Join( ";", 
            item.Floor, 
            item.CeedCode, 
            item.DetailSymbol, 
            item.DetailSymbolUniqueId,
            item.FromConnectorUniqueId,
            item.ToConnectorUniqueId,
            item.WireType, 
            item.WireSize, 
            item.WireStrip, 
            item.WireBook, 
            item.EarthType, 
            item.EarthSize, 
            item.NumberOfGround, 
            item.PlumbingType, 
            item.PlumbingSize, 
            item.NumberOfPlumbing, 
            item.ConstructionClassification, 
            item.SignalType, 
            item.ConstructionItems, 
            item.PlumbingItems, 
            item.Remark, 
            item.WireCrossSectionalArea, 
            item.CountCableSamePosition,
            item.RouteName, 
            item.IsEcoMode, 
            item.IsParentRoute, 
            item.IsReadOnly, 
            item.PlumbingIdentityInfo, 
            item.GroupId, 
            item.IsReadOnlyPlumbingItems, 
            item.IsMixConstructionItems, 
            item.CopyIndex ) ;
          createText += line.Trim() + Environment.NewLine ;
        }

        if ( ! string.IsNullOrWhiteSpace( createText.Trim() ) && createText.Trim() != defaultConstructionItems ) {
          File.WriteAllText( dlg.FileName, createText.Trim() ) ;
        }
      }
    }
    
    private void CreateDetailTable(Window window)
    {
      var mixtureOfMultipleConstructionClassificationsInDetailSymbol = string.Empty ;
      if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableModelItemOrigins, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ) {
        MyMessageBox.Show(string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), mixtureOfMultipleConstructionClassificationsInDetailSymbol), "Warning") ;
        IsCreateDetailTableItemOnFloorPlanView = false ;
      }
      else {
        SaveData( _document, _detailTableModelItemOrigins ) ;
        SaveDetailSymbolData( _document, _storageService ) ;
        window.DialogResult = true ;
        window.Close() ;
        IsCreateDetailTableItemOnFloorPlanView = true ;
      }
    }

    private bool IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol(ObservableCollection<DetailTableItemModel> detailTableItemModels, ref string mixtureOfMultipleConstructionClassificationsInDetailSymbol )
    {
      var detailTableItemModelsGroupByDetailSymbolId = detailTableItemModels.GroupBy( d => d.DetailSymbol ) ;
      var mixSymbolGroup = detailTableItemModelsGroupByDetailSymbolId
        .Where( x => x.GroupBy( y => y.ConstructionClassification ).Count() > 1 )
        .ToList() ;
      mixtureOfMultipleConstructionClassificationsInDetailSymbol = mixSymbolGroup.Any()
        ? string.Join( ", ", mixSymbolGroup.Select( y => y.Key ).Distinct() )
        : string.Empty ;
      return !string.IsNullOrEmpty( mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ;
    }

    private void UnGroupDetailTableRowsAfterChangeConstructionItems( ObservableCollection<DetailTableItemModel> detailTableItemModels, List<string> routeNames, string constructionItems )
    {
      var groupIdOfDetailTableItemRowsWithConstructionItemHasChanged = detailTableItemModels
        .Where( d => routeNames.Contains( d.RouteName ) && ! string.IsNullOrEmpty( d.GroupId ) )
        .Select( d => d.GroupId )
        .Distinct() ;
      foreach ( var groupId in groupIdOfDetailTableItemRowsWithConstructionItemHasChanged ) {
        var detailTableRowsWithSameGroupId = detailTableItemModels
          .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems != constructionItems )
          .ToList() ;
        var detailTableRowsWithConstructionItemHasChanged = detailTableItemModels
          .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems == constructionItems )
          .ToList() ;
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
          var newGroupId = string.Join( 
            "-",
            detailTableRow.DetailSymbolUniqueId, 
            detailTableRow.FromConnectorUniqueId, 
            detailTableRow.ToConnectorUniqueId, 
            detailTableRow.PlumbingIdentityInfo, 
            detailTableRow.ConstructionItems, 
            detailTableRow.WireType + detailTableRow.WireSize + detailTableRow.WireStrip ) ;
          detailTableRow.GroupId = newGroupId ;
        }
      }
    }
    
    private void Completed(Window window)
    {
      SaveData( _document, _detailTableModelItemOrigins ) ;
      SaveDetailSymbolData( _document, _storageService ) ;
      ShowDetailSymbol() ;
      window.DialogResult = true ;
      window.Close() ;
    }

    private void ShowDetailSymbol()
    {
      if ( ! _isCallFromAddWiringInformationCommand && null == PickInfo )
        return ;

      var storageServiceForDetailSymbol = new StorageService<Level, DetailSymbolModel>(((ViewPlan)_document.ActiveView).GenLevel) ;
      var conduit = PickInfo!.Element ;
      if ( IsShowSymbol ) {
        var removeDetailSymbols = storageServiceForDetailSymbol.Data.DetailSymbolData.Where( x => x.ConduitUniqueId == conduit.UniqueId && ! string.IsNullOrEmpty( x.DetailSymbolUniqueId ) ).EnumerateAll() ;
        if ( removeDetailSymbols.Any() )
          return ;

        using var transaction = new Transaction( _document ) ;
        transaction.Start( "Create Detail Symbol" ) ;

        var (symbols, angle, defaultSymbol) = CreateDetailSymbolCommandBase.CreateValueForCombobox( storageServiceForDetailSymbol.Data.DetailSymbolData, conduit ) ;
        var detailSymbolSettingDialog = new DetailSymbolSettingDialog( symbols, angle, defaultSymbol ) ;
        detailSymbolSettingDialog.GetValues() ;
        detailSymbolSettingDialog.DetailSymbol = AddWiringInformationCommandBase.SpecialSymbol ;

        var isParentSymbol = CreateDetailSymbolCommandBase.CheckDetailSymbolOfConduitDifferentCode( _document, conduit, storageServiceForDetailSymbol.Data.DetailSymbolData, detailSymbolSettingDialog.DetailSymbol ) ;
        var firstPoint = PickInfo.Position ;
        var (textNote, lineIds) = CreateDetailSymbolCommandBase.CreateDetailSymbol( _document, detailSymbolSettingDialog, firstPoint, detailSymbolSettingDialog.Angle, isParentSymbol ) ;

        var detailSymbolItemModelModel = CreateDetailSymbolCommandBase.SaveDetailSymbol( _document, storageServiceForDetailSymbol, conduit, textNote, detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol ) ;

        if ( null != detailSymbolItemModelModel ) {
          var storageServiceForDetailTable = new StorageService<Level, DetailTableModel>( ( (ViewPlan) _document.ActiveView ).GenLevel ) ;
          foreach ( var detailTableItemModel in storageServiceForDetailTable.Data.DetailTableData ) {
            if ( detailSymbolItemModelModel.FromConnectorUniqueId == detailTableItemModel.FromConnectorUniqueId
                && detailSymbolItemModelModel.ToConnectorUniqueId == detailTableItemModel.ToConnectorUniqueId
                && detailSymbolItemModelModel.RouteName == detailTableItemModel.RouteName) {
              detailTableItemModel.DetailSymbolUniqueId = detailSymbolItemModelModel.DetailSymbolUniqueId ;
            }
          }
          storageServiceForDetailTable.SaveChange();
        }
        
        transaction.Commit() ;
      }
      else {
        var detailSymbolModel = storageServiceForDetailSymbol.Data.DetailSymbolData.FirstOrDefault( x => x.ConduitUniqueId == conduit.UniqueId && x.DetailSymbol == AddWiringInformationCommandBase.SpecialSymbol ) ;
        if ( null == detailSymbolModel )
          return ;
        
        using var transaction = new Transaction( _document ) ;
        transaction.Start( "Remove Detail Symbol" ) ;

        var removeDetailSymbols = storageServiceForDetailSymbol.Data.DetailSymbolData
          .Where( x => CreateDetailTableCommandBase.GetKeyRouting(x) == CreateDetailTableCommandBase.GetKeyRouting( detailSymbolModel ) )
          .EnumerateAll() ;

        foreach ( var removeDetailSymbol in removeDetailSymbols ) {
          storageServiceForDetailSymbol.Data.DetailSymbolData.Remove( removeDetailSymbol ) ;
        }

        removeDetailSymbols = removeDetailSymbols.DistinctBy( x => x.DetailSymbolUniqueId ).EnumerateAll() ;
        foreach ( var removeDetailSymbol in removeDetailSymbols ) {
          CreateDetailSymbolCommandBase.DeleteDetailSymbol( _document, removeDetailSymbol.DetailSymbolUniqueId, removeDetailSymbol.LineUniqueIds ) ;
        }

        if ( removeDetailSymbols.Any() )
          storageServiceForDetailSymbol.SaveChange() ;

        var storageServiceForDetailTable = new StorageService<Level, DetailTableModel>( ( (ViewPlan) _document.ActiveView ).GenLevel ) ;
        foreach ( var detailTableModel in storageServiceForDetailTable.Data.DetailTableData ) {
          if ( CreateDetailTableCommandBase.GetKeyRouting( detailSymbolModel) == CreateDetailTableCommandBase.GetKeyRouting(detailTableModel) ) {
            detailTableModel.DetailSymbolUniqueId = "" ;
          }
        }
        storageServiceForDetailTable.SaveChange();

        transaction.Commit() ;
      }
    }

    private void UpdatePlumbingItemsAfterChangeConstructionItems( ObservableCollection<DetailTableItemModel> detailTableItemModels, string routeName, string constructionItems )
    {
      var plumbingIdentityInfos = detailTableItemModels
        .Where( d => d.RouteName == routeName )
        .Select( d => d.PlumbingIdentityInfo )
        .Distinct() ;
      foreach ( var plumbingIdentityInfo in plumbingIdentityInfos ) {
        var detailTableRowsWithSamePlumbing = detailTableItemModels
          .Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo )
          .ToList() ;
        if ( ! detailTableRowsWithSamePlumbing.Any() ) continue ;
        {
          var isParentDetailTableRow = detailTableRowsWithSamePlumbing.FirstOrDefault( d => d.RouteName == routeName && d.IsParentRoute ) != null ;
          var plumbingItems = detailTableRowsWithSamePlumbing
            .Select( d => d.ConstructionItems )
            .Distinct() ;
          var plumbingItemTypes = ( from plumbingItem in plumbingItems select new DetailTableItemModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() ;
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

    private static void SetGroupIdForDetailTableRows( IEnumerable<DetailTableItemModel> detailTableItemRowsWithSameDetailSymbolId )
    {
      const bool isMixConstructionItems = false ;
      var detailTableItemRowsGroupByPlumbingIdentityInfo = 
        detailTableItemRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableItemRowsWithSamePlumbingIdentityInfo in detailTableItemRowsGroupByPlumbingIdentityInfo ) {
        var detailTableRowsGroupByWiringType = 
          detailTableItemRowsWithSamePlumbingIdentityInfo
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

    private static void SetGroupIdForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableItemModel> detailTableItemRowsWithSameDetailSymbolId )
    {
      const bool isMixConstructionItems = true ;
      var detailTableItemRowsGroupByPlumbingIdentityInfo = 
        detailTableItemRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableItemRowsGroupByPlumbingIdentityInfo ) {
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

    public static void SetPlumbingItemsForDetailTableItemRows( IEnumerable<DetailTableItemModel> detailTableItemsWithSameDetailSymbolId )
    {
      foreach ( var detailTableItemModel in detailTableItemsWithSameDetailSymbolId ) {
        detailTableItemModel.PlumbingItemTypes = new List<DetailTableItemModel.ComboboxItemType> { new( detailTableItemModel.ConstructionItems, detailTableItemModel.ConstructionItems ) } ;
        detailTableItemModel.PlumbingItems = detailTableItemModel.ConstructionItems ;
      }
    }

    public static void SetPlumbingItemsForDetailTableItemRowsMixConstructionItems( IEnumerable<DetailTableItemModel> detailTableItemsWithSameDetailSymbolId )
    {
      var detailTableItemGroupByPlumbingIdentityInfo = 
        detailTableItemsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableItemModels in detailTableItemGroupByPlumbingIdentityInfo ) {
        var parentDetailRow = detailTableItemModels.First().ConstructionItems ;
        var plumbingItems = detailTableItemModels.Select( d => d.ConstructionItems ).Distinct() ;
        var plumbingItemTypes = ( from plumbingItem in plumbingItems select new DetailTableItemModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() ;
        foreach ( var detailTableRow in detailTableItemModels ) {
          detailTableRow.PlumbingItemTypes = plumbingItemTypes ;
          detailTableRow.PlumbingItems = parentDetailRow ;
        }
      }
    }

    private List<DetailTableItemModel> SortDetailTableModel( IEnumerable<DetailTableItemModel> detailTableItemModels, bool isMixConstructionItems )
    {
      List<DetailTableItemModel> sortedDetailTableItemModelsList = new() ;
      var detailTableModelsGroupByDetailSymbolId = detailTableItemModels
        .OrderBy( d => d.DetailSymbol )
        .GroupBy( CreateDetailTableCommandBase.GetKeyRouting )
        .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsGroupByDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var signalTypes = (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType )) ;
        foreach ( var signalType in signalTypes ) {
          var detailTableRowsWithSameSignalType = detailTableRowsGroupByDetailSymbolId
            .Where( d => d.SignalType == signalType.GetFieldName() )
            .ToList() ;
          SortDetailTableRows( sortedDetailTableItemModelsList, detailTableRowsWithSameSignalType, isMixConstructionItems ) ;
        }
        
        var signalTypeNames = signalTypes.Select( s => s.GetFieldName() ) ;
        var detailTableRowsNotHaveSignalType = detailTableRowsGroupByDetailSymbolId
          .Where( d => ! signalTypeNames.Contains( d.SignalType ) )
          .ToList() ;
        SortDetailTableRows( sortedDetailTableItemModelsList, detailTableRowsNotHaveSignalType, isMixConstructionItems ) ;
      }
      
      return sortedDetailTableItemModelsList ;
    }
    
    private void SortDetailTableRows( List<DetailTableItemModel> sortedDetailTableItemModelsList, List<DetailTableItemModel> detailTableItemRowsWithSameSignalType, bool isMixConstructionItems )
    {
      if ( ! isMixConstructionItems ) detailTableItemRowsWithSameSignalType = detailTableItemRowsWithSameSignalType
        .OrderBy( d => d.ConstructionItems )
        .ToList() ;
      var detailTableItemRowsGroupByPlumbingIdentityInfo = detailTableItemRowsWithSameSignalType
        .GroupBy( d => d.PlumbingIdentityInfo )
        .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableItemRowsGroupByPlumbingIdentityInfo ) {
        var sortedDetailTableModels = 
            detailTableRowsWithSamePlumbingIdentityInfo
              .OrderByDescending( x => x.IsParentRoute )
              .ThenBy( x => x.GroupId ) ;

        sortedDetailTableItemModelsList.AddRange( sortedDetailTableModels ) ;
      }
    }
    
    private void SaveData( Document document, IReadOnlyCollection<DetailTableItemModel> detailTableItemRowsBySelectedDetailSymbols )
    {
      try {
        var storageService = new StorageService<Level, DetailTableModel>( ( (ViewPlan) document.ActiveView ).GenLevel ) ;
        if ( ! detailTableItemRowsBySelectedDetailSymbols.Any() )
          return ;

        var selectedDetailSymbolIds = Enumerable.ToHashSet( detailTableItemRowsBySelectedDetailSymbols.Select( CreateDetailTableCommandBase.GetKeyRouting ).Distinct() ) ;

        var detailTableRowsByOtherDetailSymbols = storageService.Data.DetailTableData.Where( d => ! selectedDetailSymbolIds.Contains( CreateDetailTableCommandBase.GetKeyRouting( d ) ) ).ToList() ;

        storageService.Data.DetailTableData = detailTableItemRowsBySelectedDetailSymbols.ToList() ;
        if ( detailTableRowsByOtherDetailSymbols.Any() )
          storageService.Data.DetailTableData.AddRange( detailTableRowsByOtherDetailSymbols ) ;

        using Transaction t = new(document, "Save data") ;
        t.Start() ;
        storageService.SaveChange() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    private void SaveDetailSymbolData( Document document, StorageService<Level, DetailSymbolModel> symbolStorable )
    {
      try {
        using Transaction t = new( document, "Save data" ) ;
        t.Start() ;
        symbolStorable.SaveChange() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    private void DeleteDetailTableRows()
    {
      List<DetailTableItemModel> deletedDetailTableItemRows = new() ;
      foreach ( var selectedItem in _selectedDetailTableItemRows ) {
        if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
          var selectedItems = _detailTableModelItemOrigins
            .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId )
            .ToList() ;
          deletedDetailTableItemRows.AddRange( selectedItems ) ;
          foreach ( var item in selectedItems ) {
            var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = _detailTableModelItemOrigins
              .Count( d => CreateDetailTableCommandBase.GetKeyRouting( d ) == CreateDetailTableCommandBase.GetKeyRouting( item ) && d.RouteName == item.RouteName && d != item ) ;
            if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
              _storageService.Data.DetailSymbolData.RemoveAll(  s => CreateDetailTableCommandBase.GetKeyRouting(s) == CreateDetailTableCommandBase.GetKeyRouting( item ) && s.RouteName == item.RouteName ) ;
            }
          }
        }
        else {
          var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = _detailTableModelItemOrigins
            .Count( d => CreateDetailTableCommandBase.GetKeyRouting(d) == CreateDetailTableCommandBase.GetKeyRouting(selectedItem) && d.RouteName == selectedItem.RouteName && d != selectedItem ) ;
          if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
            var detailSymbolModels = _storageService.Data.DetailSymbolData
              .Where( s => CreateDetailTableCommandBase.GetKeyRouting(s) == CreateDetailTableCommandBase.GetKeyRouting(selectedItem) && s.RouteName == selectedItem.RouteName ).ToList() ;
            foreach ( var detailSymbolModel in detailSymbolModels ) {
              _storageService.Data.DetailSymbolData.Remove( detailSymbolModel ) ;
            }
          }

          deletedDetailTableItemRows.Add( selectedItem ) ;
        }
      }

      using var transaction = new Transaction( _document ) ;
      transaction.Start( "Delete Detail Table" ) ;
      var storageService = new StorageService<Level, DetailTableModel>( ( (ViewPlan) _document.ActiveView ).GenLevel ) ;
      storageService.Data.DetailTableData.RemoveAll( x => deletedDetailTableItemRows.Any( y => CreateDetailTableCommandBase.GetKeyRouting( y ) == CreateDetailTableCommandBase.GetKeyRouting( x ) ) ) ;
      storageService.SaveChange();
      transaction.Commit() ;
      
      var detailTableRows = _detailTableModelItemOrigins.Where( d => ! deletedDetailTableItemRows.Contains( d ) ) ;
      _detailTableModelItemOrigins = new ObservableCollection<DetailTableItemModel>( detailTableRows ) ;
      
      var detailTableItemRowsSummary = DetailTableItemModels.Where( d => ! _selectedDetailTableItemRowsSummary.Contains( d ) ) ;
      DetailTableItemModels = new ObservableCollection<DetailTableItemModel>( detailTableItemRowsSummary ) ;
    }
    
    private void UpdateDataGridAndRemoveSelectedRow()
    {
      ResetSelectedItems() ;
    }
    
    private void ResetSelectedItems()
    {
      _selectedDetailTableItemRows.Clear() ;
      _selectedDetailTableItemRowsSummary.Clear() ;
    }

    private void PasteDetailTableRow(DetailTableItemModel pasteDetailTableItemRow, DetailTableItemModel pasteDetailTableItemRowSummary)
    {
      var newDetailTableItemModels = new List<DetailTableItemModel>() ;
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      
      var newDetailTableItemRow = new DetailTableItemModel( 
        false, 
        _copyDetailTableItemRow?.Floor,
        _copyDetailTableItemRow?.CeedCode, 
        _copyDetailTableItemRow?.DetailSymbol, 
        _copyDetailTableItemRow?.DetailSymbolUniqueId, 
        _copyDetailTableItemRow?.FromConnectorUniqueId, 
        _copyDetailTableItemRow?.ToConnectorUniqueId, 
        _copyDetailTableItemRow?.WireType, 
        _copyDetailTableItemRow?.WireSize,
        _copyDetailTableItemRow?.WireStrip,
        _copyDetailTableItemRow?.WireBook, 
        _copyDetailTableItemRow?.EarthType, 
        _copyDetailTableItemRow?.EarthSize, 
        _copyDetailTableItemRow?.NumberOfGround, 
        _copyDetailTableItemRow?.PlumbingType,
        _copyDetailTableItemRow?.PlumbingSize, 
        _copyDetailTableItemRow?.NumberOfPlumbing, 
        _copyDetailTableItemRow?.ConstructionClassification, 
        _copyDetailTableItemRow?.SignalType, 
        _copyDetailTableItemRow?.ConstructionItems, 
        _copyDetailTableItemRow?.PlumbingItems, 
        _copyDetailTableItemRow?.Remark, 
        _copyDetailTableItemRow?.WireCrossSectionalArea,
        _copyDetailTableItemRow?.CountCableSamePosition, 
        _copyDetailTableItemRow?.RouteName,
        _copyDetailTableItemRow?.IsEcoMode, 
        _copyDetailTableItemRow?.IsParentRoute, 
        _copyDetailTableItemRow?.IsReadOnly, 
        _copyDetailTableItemRow?.PlumbingIdentityInfo + index, string.Empty, 
        _copyDetailTableItemRow?.IsReadOnlyPlumbingItems,
        _copyDetailTableItemRow?.IsMixConstructionItems, 
        index, _copyDetailTableItemRow?.IsReadOnlyParameters, 
        _copyDetailTableItemRow?.IsReadOnlyWireSizeAndWireStrip, 
        _copyDetailTableItemRow?.IsReadOnlyPlumbingSize,
        _copyDetailTableItemRow?.WireSizes, 
        _copyDetailTableItemRow?.WireStrips, 
        _copyDetailTableItemRow?.EarthSizes, 
        _copyDetailTableItemRow?.PlumbingSizes, 
        _copyDetailTableItemRow?.PlumbingItemTypes ) ;
      foreach ( var detailTableRow in _detailTableModelItemOrigins ) {
        newDetailTableItemModels.Add( detailTableRow ) ;
        if ( detailTableRow == pasteDetailTableItemRow ) {
          newDetailTableItemModels.Add( newDetailTableItemRow ) ;
        }
      }

      _detailTableModelItemOrigins = new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
      
      newDetailTableItemModels = new List<DetailTableItemModel>() ;
      foreach ( var detailTableItemRow in DetailTableItemModels ) {
        newDetailTableItemModels.Add( detailTableItemRow ) ;
        if ( detailTableItemRow == pasteDetailTableItemRowSummary ) {
          newDetailTableItemModels.Add( newDetailTableItemRow ) ;
        }
      }

      DetailTableItemModels = new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
    }

    private void PlumbingSummary( List<ConduitsModel> conduitsModelData, StorageService<Level, DetailSymbolModel> symbolStorable, List<DetailTableItemModel> selectedDetailTableItemRows, bool isMixConstructionItems, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      _detailTableModelItemOrigins = SummarizePlumbing( _detailTableModelItemOrigins, conduitsModelData, symbolStorable, selectedDetailTableItemRows,
        isMixConstructionItems, detailSymbolIdsWithPlumbingTypeHasChanged ) ;
    }
    
    public static ObservableCollection<DetailTableItemModel> SummarizePlumbing(ObservableCollection<DetailTableItemModel> detailTableItemModels, List<ConduitsModel> conduitsModelData, 
      StorageService<Level, DetailSymbolModel> storageService, List<DetailTableItemModel> selectedDetailTableItemModels, bool isMixConstructionItems, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      Dictionary<DetailTableItemModel, List<DetailTableItemModel>> sortDetailTableItemModel = new() ;
      var detailTableItemModelsGroupByDetailSymbolId = 
        detailTableItemModels
          .Where(d => !selectedDetailTableItemModels.Any() || selectedDetailTableItemModels.Contains(d) )
          .Where( d => ! string.IsNullOrEmpty( d.WireType ) 
                       && ! string.IsNullOrEmpty( d.WireSize ) 
                       && ! string.IsNullOrEmpty( d.WireStrip )
                       && ! string.IsNullOrEmpty( d.WireBook ) 
                       && ! string.IsNullOrEmpty( d.SignalType ) 
                       && ! string.IsNullOrEmpty( d.ConstructionItems ) 
                       && ! string.IsNullOrEmpty( d.Remark ) )
          .GroupBy( CreateDetailTableCommandBase.GetKeyRouting )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableItemRowsWithSameDetailSymbolId in detailTableItemModelsGroupByDetailSymbolId ) {
        var plumbingIdentityInfos = detailTableItemRowsWithSameDetailSymbolId.Select( d => d.PlumbingIdentityInfo ).Distinct();
        var otherDetailTableItemRowsWithSamePlumbingIdentityInfo = detailTableItemModels
          .Where( d => plumbingIdentityInfos.Contains( d.PlumbingIdentityInfo ) && ! detailTableItemRowsWithSameDetailSymbolId.Contains( d ) )
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
        var keyRouting = CreateDetailTableCommandBase.GetKeyRouting(detailTableItemRowsWithSameDetailSymbolId.First()) ;
        var plumbingType = detailSymbolIdsWithPlumbingTypeHasChanged.SingleOrDefault( d => d.Key == keyRouting ).Value ;
        if ( string.IsNullOrEmpty( plumbingType ) ) {
          if ( selectedDetailTableItemModels.Any() ) {
            plumbingType = selectedDetailTableItemModels.FirstOrDefault( s => CreateDetailTableCommandBase.GetKeyRouting(s) == keyRouting && s.PlumbingType != DefaultChildPlumbingSymbol )?.PlumbingType ?? DefaultParentPlumbingType ;
          }
          else {
            plumbingType = storageService.Data.DetailSymbolData.FirstOrDefault( s => CreateDetailTableCommandBase.GetKeyRouting(s) == keyRouting )?.PlumbingType ?? DefaultParentPlumbingType ;
          }
        }

        if ( plumbingType == NoPlumping ) {
          CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableItemRowsWithSameDetailSymbolId, isMixConstructionItems );
        }
        else {
          CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( conduitsModelData, detailTableItemRowsWithSameDetailSymbolId, plumbingType, true, isMixConstructionItems ) ;
        }

        if ( isMixConstructionItems ) {
          SetGroupIdForDetailTableRowsMixConstructionItems( detailTableItemRowsWithSameDetailSymbolId ) ;
        }
        else {
          SetGroupIdForDetailTableRows( detailTableItemRowsWithSameDetailSymbolId ) ;
        }
        
        var detailTableModelsGroupByPlumbingIdentityInfos = 
          detailTableItemRowsWithSameDetailSymbolId
            .GroupBy( d => d.PlumbingIdentityInfo )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableModelsGroupByPlumbingIdentityInfo in detailTableModelsGroupByPlumbingIdentityInfos ) {
          sortDetailTableItemModel.Add( detailTableModelsGroupByPlumbingIdentityInfo.First(), detailTableModelsGroupByPlumbingIdentityInfo ) ;
        }

        foreach ( var otherDetailTableRows in otherDetailTableItemRowsWithSamePlumbingIdentityInfo ) {
          if ( plumbingType == NoPlumping ) {
            CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableItemRowsWithSameDetailSymbolId, isMixConstructionItems );
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
            sortDetailTableItemModel.Add( detailTableModelsGroupByPlumbingIdentityInfo.First(), detailTableModelsGroupByPlumbingIdentityInfo ) ;
          }
        }
      }

      foreach ( var (parentDetailTableItemRow, detailTableItemRows) in sortDetailTableItemModel ) {
        List<DetailTableItemModel> newDetailTableItemModels = new() ;
        foreach ( var detailTableRow in detailTableItemModels ) {
          if ( detailTableRow == parentDetailTableItemRow ) {
            newDetailTableItemModels.AddRange( detailTableItemRows ) ;
          }
          else if ( ! detailTableItemRows.Contains( detailTableRow ) ) {
            newDetailTableItemModels.Add( detailTableRow ) ;
          }
        }

        return new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
      }

      return new ObservableCollection<DetailTableItemModel>() ;
    }

    private void AddDetailTableRow(Document document, DetailTableItemModel selectDetailTableItemRow, DetailTableItemModel selectDetailTableItemRowSummary )
    {
      var newDetailTableItemModels = new List<DetailTableItemModel>() ;
      
      var routeName = RouteUtil.GetMainRouteName(selectDetailTableItemRow.RouteName);
      var toConnector = ConduitUtil.GetConnectorOfRoute( document, routeName, false ) ;
      var quantity = toConnector?.GetPropertyInt(ElectricalRoutingElementParameter.Quantity).ToString() ?? selectDetailTableItemRow.NumberOfPlumbing;
      
      var newDetailTableItemRow = new DetailTableItemModel( selectDetailTableItemRow.DetailSymbol, selectDetailTableItemRow.DetailSymbolUniqueId, 
        selectDetailTableItemRow.FromConnectorUniqueId, selectDetailTableItemRow.ToConnectorUniqueId, selectDetailTableItemRow.RouteName )
      {
        Floor = selectDetailTableItemRow.Floor,
        PlumbingType = selectDetailTableItemRow.PlumbingType,
        PlumbingItemTypes = selectDetailTableItemRow.PlumbingItemTypes,
        PlumbingSize = selectDetailTableItemRow.PlumbingSize, 
        PlumbingSizes = selectDetailTableItemRow.PlumbingSizes,
        NumberOfPlumbing = quantity,
        ConstructionClassification = selectDetailTableItemRow.ConstructionClassification,
        SignalType = selectDetailTableItemRow.SignalType,
        ConstructionItems = selectDetailTableItemRow.ConstructionItems,
        PlumbingItems = selectDetailTableItemRow.PlumbingItems,
        WireBook = quantity,
        Remark = selectDetailTableItemRow.Remark
      } ;

      foreach ( var detailTableRow in _detailTableModelItemOrigins ) {
        newDetailTableItemModels.Add( detailTableRow ) ;
        if ( detailTableRow == selectDetailTableItemRow ) {
          newDetailTableItemModels.Add( newDetailTableItemRow ) ;
        }
      }

      _detailTableModelItemOrigins = new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
      
      newDetailTableItemModels = new List<DetailTableItemModel>() ;
      foreach ( var detailTableItemRow in DetailTableItemModels ) {
        newDetailTableItemModels.Add( detailTableItemRow ) ;
        if ( detailTableItemRow == selectDetailTableItemRowSummary ) {
          newDetailTableItemModels.Add( newDetailTableItemRow ) ;
        }
      }

      DetailTableItemModels = new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
    }
    
    private bool MoveDetailTableRow(DetailTableItemModel selectDetailTableItemRow, DetailTableItemModel selectDetailTableItemRowSummary, bool isMoveUp )
    {
      var newDetailTableItemModels = new List<DetailTableItemModel>() ;
      var selectDetailTableItemRowSummaryIndex = DetailTableItemModels.FindIndex( d => d == selectDetailTableItemRowSummary ) ;
      if ( ( isMoveUp && selectDetailTableItemRowSummaryIndex == 0 ) || ( ! isMoveUp && selectDetailTableItemRowSummaryIndex == DetailTableItemModels.Count - 1 ) ) return false ;
      var tempDetailTableRowSummary = DetailTableItemModels.ElementAt( isMoveUp ? selectDetailTableItemRowSummaryIndex - 1 : selectDetailTableItemRowSummaryIndex + 1 ) ;
      foreach ( var detailTableRow in DetailTableItemModels ) {
        if ( detailTableRow == tempDetailTableRowSummary ) {
          newDetailTableItemModels.Add( selectDetailTableItemRowSummary ) ;
        }
        else if ( detailTableRow == selectDetailTableItemRowSummary ) {
          newDetailTableItemModels.Add( tempDetailTableRowSummary ) ;
        }
        else {
          newDetailTableItemModels.Add( detailTableRow ) ;
        }
      }

      DetailTableItemModels = new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
      
      newDetailTableItemModels = new List<DetailTableItemModel>() ;
      var selectDetailTableRowIndex = _detailTableModelItemOrigins.FindIndex( d => d == selectDetailTableItemRow ) ;
      var tempDetailTableRow = _detailTableModelItemOrigins.ElementAt( isMoveUp ? selectDetailTableRowIndex - 1 : selectDetailTableRowIndex + 1 ) ;
      foreach ( var detailTableRow in _detailTableModelItemOrigins ) {
        if ( detailTableRow == tempDetailTableRow ) {
          newDetailTableItemModels.Add( selectDetailTableItemRow ) ;
        }
        else if ( detailTableRow == selectDetailTableItemRow ) {
          newDetailTableItemModels.Add( tempDetailTableRow ) ;
        }
        else {
          newDetailTableItemModels.Add( detailTableRow ) ;
        }
      }

      _detailTableModelItemOrigins = new ObservableCollection<DetailTableItemModel>( newDetailTableItemModels ) ;
      return true ;
    }

    private void SplitPlumbing( List<ConduitsModel> conduitsModelData, StorageService<Level, DetailSymbolModel> symbolStorable, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      var detailTableModels = _detailTableModelItemOrigins
        .Where( d => 
             ! string.IsNullOrEmpty( d.WireType ) 
          && ! string.IsNullOrEmpty( d.WireSize ) 
          && ! string.IsNullOrEmpty( d.WireStrip ) 
          && ! string.IsNullOrEmpty( d.WireBook ) 
          && ! string.IsNullOrEmpty( d.SignalType ) 
          && ! string.IsNullOrEmpty( d.ConstructionItems ) 
          && ! string.IsNullOrEmpty( d.Remark ) ).EnumerateAll() ;
      
      if(!detailTableModels.Any())
        return;

      if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.Any() ) {
        if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.ContainsKey( CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First()) ) ) {
          DetailSymbolIdsWithPlumbingTypeHasChanged.Add( CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First()), detailTableModels.First().PlumbingType ) ;
        }
        else {
          DetailSymbolIdsWithPlumbingTypeHasChanged[ CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First()) ] = detailTableModels.First().PlumbingType ;
        }
      }

      foreach ( var detailTableRow in detailTableModels ) {
        SetPlumbingDataForEachWiring( conduitsModelData, symbolStorable, detailTableRow, detailSymbolIdsWithPlumbingTypeHasChanged ) ;
      }
    }

    private void SetPlumbingDataForEachWiring( List<ConduitsModel> conduitsModelData, StorageService<Level, DetailSymbolModel> symbolStorable, DetailTableItemModel detailTableItemRow, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      const double percentage = 0.32 ;
      const int plumbingCount = 1 ;
      var plumbingType = detailSymbolIdsWithPlumbingTypeHasChanged.SingleOrDefault( d => d.Key ==  CreateDetailTableCommandBase.GetKeyRouting(detailTableItemRow) ).Value ;
      if ( string.IsNullOrEmpty( plumbingType ) ) {
        plumbingType = symbolStorable.Data.DetailSymbolData.FirstOrDefault( s => CreateDetailTableCommandBase.GetKeyRouting(s) == CreateDetailTableCommandBase.GetKeyRouting( detailTableItemRow ) )?.PlumbingType ?? DefaultParentPlumbingType ;
      }
      var wireBook = string.IsNullOrEmpty( detailTableItemRow.WireBook ) ? 1 : int.Parse( detailTableItemRow.WireBook ) ;
      if ( plumbingType == NoPlumping ) {
        detailTableItemRow.PlumbingType = NoPlumping ;
        detailTableItemRow.PlumbingSize = NoPlumbingSize ;
        detailTableItemRow.NumberOfPlumbing = string.Empty ;
        detailTableItemRow.PlumbingIdentityInfo = CreateDetailTableCommandBase.GetDetailTableRowPlumbingIdentityInfo( detailTableItemRow, false ) ;
        detailTableItemRow.GroupId = string.Empty ;
        detailTableItemRow.Remark = GetRemark( detailTableItemRow.Remark, wireBook ) ;
        detailTableItemRow.IsParentRoute = true ;
        detailTableItemRow.IsReadOnly = false ;
        detailTableItemRow.IsReadOnlyPlumbingItems = true ;
        detailTableItemRow.IsMixConstructionItems = false ;
        detailTableItemRow.IsReadOnlyPlumbingSize = true ;
      }
      else {
        var conduitsModels = conduitsModelData
          .Where( c => c.PipingType == plumbingType )
          .OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) )
          .ToList() ;
        var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
        var currentPlumbingCrossSectionalArea = detailTableItemRow.WireCrossSectionalArea / percentage * wireBook ;
        if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
          var plumbing = conduitsModels.LastOrDefault() ;
          detailTableItemRow.PlumbingType = plumbingType ;
          if ( null != plumbing ) {
            detailTableItemRow.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          }
        }
        else {
          var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
          if ( null != plumbing ) {
            detailTableItemRow.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          }
          detailTableItemRow.PlumbingType = plumbingType ;
        }

        detailTableItemRow.Remark = GetRemark( detailTableItemRow.Remark, wireBook ) ;
        detailTableItemRow.NumberOfPlumbing = plumbingCount.ToString() ;
        detailTableItemRow.PlumbingIdentityInfo = CreateDetailTableCommandBase.GetDetailTableRowPlumbingIdentityInfo( detailTableItemRow, false ) ;
        detailTableItemRow.GroupId = string.Empty ;
        detailTableItemRow.IsParentRoute = true ;
        detailTableItemRow.IsReadOnly = false ;
        detailTableItemRow.IsReadOnlyPlumbingItems = true ;
        detailTableItemRow.IsReadOnlyPlumbingSize = false ;
        detailTableItemRow.IsMixConstructionItems = false ;
        if ( detailTableItemRow.PlumbingSizes.Any() ) return ;
        {
          var plumbingSizesOfPlumbingType = conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
          detailTableItemRow.PlumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableItemModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
        }
      }
    }

    private enum EditedColumn
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
    
    public void PlumbingItemsSelection( ComboBox comboBox )
    {
      var plumbingItem = comboBox.SelectedValue ;
      if ( plumbingItem == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableItemModel detailTableItemRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableItemRow.PlumbingItems == plumbingItem.ToString() ) return ;
        var detailTableItemRowsWithSamePlumbing = _detailTableModelItemOrigins.Where( c => c.PlumbingIdentityInfo == detailTableItemRow.PlumbingIdentityInfo ).ToList() ;
        foreach ( var detailTableItemRowWithSamePlumbing in detailTableItemRowsWithSamePlumbing ) {
          detailTableItemRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
        }
      
        var detailTableRowsSummaryWithSamePlumbing = DetailTableItemModels.Where( c => c.PlumbingIdentityInfo == detailTableItemRow.PlumbingIdentityInfo ).ToList() ;
        foreach ( var detailTableRowWithSamePlumbing in detailTableRowsSummaryWithSamePlumbing ) {
          detailTableRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
        }
        
        UpdateDataGridAndRemoveSelectedRow() ;
      }
    }
    
    public void ConstructionItemSelection( ComboBox comboBox )
    {
      var constructionItem = comboBox.SelectedValue ;
      if ( constructionItem == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableItemModel detailTableItemRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableItemRow.ConstructionItems == constructionItem.ToString() ) return ;
        var detailTableItemRowsChangeConstructionItems = _detailTableModelItemOrigins.Where( c => c.RouteName == detailTableItemRow.RouteName ).ToList() ;
        var detailTableItemRowsWithSameGroupId = _detailTableModelItemOrigins
          .Where( c => 
               ! string.IsNullOrEmpty( c.GroupId ) 
            && c.GroupId == detailTableItemRow.GroupId 
            && c.RouteName != detailTableItemRow.RouteName ).ToList() ;
        if ( detailTableItemRowsWithSameGroupId.Any() ) {
          var routeWithSameGroupId = Enumerable.ToHashSet( detailTableItemRowsWithSameGroupId.Select( d => d.RouteName ).Distinct() ) ;
          detailTableItemRowsChangeConstructionItems.AddRange( _detailTableModelItemOrigins.Where( c => routeWithSameGroupId.Contains( c.RouteName ) ).ToList() ) ;
        }
      
        foreach ( var detailTableRowChangeConstructionItems in detailTableItemRowsChangeConstructionItems ) {
          detailTableRowChangeConstructionItems.ConstructionItems = constructionItem.ToString() ;
        }
      
        var routesWithConstructionItemHasChanged = detailTableItemRowsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
        UpdatePlumbingItemsAfterChangeConstructionItems( _detailTableModelItemOrigins, detailTableItemRow.RouteName, constructionItem.ToString() ) ;
        if ( ! detailTableItemRow.IsMixConstructionItems ) {
          UnGroupDetailTableRowsAfterChangeConstructionItems( _detailTableModelItemOrigins, routesWithConstructionItemHasChanged, constructionItem.ToString() ) ;
        }
        foreach ( var routeName in routesWithConstructionItemHasChanged ) {
          if ( ! RoutesWithConstructionItemHasChanged.ContainsKey( routeName ) ) {
            RoutesWithConstructionItemHasChanged.Add( routeName, constructionItem.ToString() ) ;
          }
          else {
            RoutesWithConstructionItemHasChanged[ routeName ] = constructionItem.ToString() ;
          }
        }
      
        CreateDetailTableViewModelByGroupId() ;
      }
    }
    
    public void Remark( TextBox textBox )
    {
      var remark = textBox.Text ;
      
      if ( textBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.Remark, remark, new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void SignalTypeSelection( ComboBox comboBox)
    {
      var selectedSignalType = comboBox.SelectedValue ;
      if ( selectedSignalType == null ) return ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.SignalType, selectedSignalType.ToString(), new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void ConstructionClassificationSelectionChanged( ComboBox comboBox )
    {
      var selectedConstructionClassification = comboBox.SelectedValue ;
      if ( selectedConstructionClassification == null ) return ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.ConstructionClassification, selectedConstructionClassification.ToString(), new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireBookSelection( ComboBox comboBox )
    {
      var selectedWireBook = comboBox.SelectedValue ;
      if( selectedWireBook == null ) 
        return ;

      if ( comboBox.DataContext is not DetailTableItemModel editedDetailTableItemRow )
        return ;
      
      if ( editedDetailTableItemRow.IsGrouped ) {
        if ( $"{selectedWireBook}" != editedDetailTableItemRow.WireBook ) {
          MessageBox.Show( "Arent3d.Architecture.Routing.AppBase.ViewModel.Select.AfterGrouping".GetAppStringByKeyOrDefault( "Not allowed to change the number of wires after the grouping." ), "Arent Inc" ) ;
        }
        comboBox.SelectedItem = Numbers.SingleOrDefault(x => x.Name == editedDetailTableItemRow.WireBook ) ;
        return;
      }
        
      var wireBook = int.TryParse($"{selectedWireBook}", out var value) ? value : int.Parse( editedDetailTableItemRow.WireBook ) ;
      var plumbingModel = FindPlumbingModel(editedDetailTableItemRow.PlumbingType, editedDetailTableItemRow.PlumbingSize) ;
      if( null == plumbingModel )
        return;

      var wireAreas = editedDetailTableItemRow.WireCrossSectionalArea / CreateDetailTableCommandBase.Percentage * wireBook ;
      if ( wireAreas > double.Parse( plumbingModel.InnerCrossSectionalArea ) ) {
        var maxPlumbingSize = GetMaxPlumbingSize( wireAreas, editedDetailTableItemRow ) ;
        if ( null == maxPlumbingSize ) {
          MessageBox.Show( "The number of wires exceeds the size of the plumbing.", "Arent Inc" ) ;
          comboBox.SelectedItem = Numbers.SingleOrDefault(x => x.Name == editedDetailTableItemRow.WireBook ) ;
        }
        else {
          ChangeRow( editedDetailTableItemRow, $"{selectedWireBook}", maxPlumbingSize ) ;
        }
      }
      else {
        var maxPlumbingSize = GetMaxPlumbingSize( wireAreas, editedDetailTableItemRow ) ;
        if ( null == maxPlumbingSize ) {
          MessageBox.Show( "Not found the plumbing size.", "Arent Inc" ) ;
        }
        else {
          ChangeRow( editedDetailTableItemRow, $"{selectedWireBook}", maxPlumbingSize ) ;
        }
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void ChangeRow(DetailTableItemModel editedDetailTableItemRow, string selectedWireBook, string maxPlumbingSize )
    {
      ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.WireBook, selectedWireBook, new List<DetailTableItemModel.ComboboxItemType>() ) ;
      editedDetailTableItemRow.PlumbingSize = maxPlumbingSize ;
      DetailTableItemModels = new ObservableCollection<DetailTableItemModel>( DetailTableItemModels ) ;
    }

    private ConduitsModel? FindPlumbingModel( string plumbingType, string plumbingSize )
    {
      var plumbingTypeModels = _conduitsModelData.Where( x => x.PipingType == plumbingType.Replace( DefaultChildPlumbingSymbol, "" ) ).ToList() ;
      return plumbingTypeModels.Any() ? plumbingTypeModels.FirstOrDefault( x => x.Size.Replace( "mm", "" ) == plumbingSize ) : null;
    }

    private string? GetMaxPlumbingSize( double wireAreas, DetailTableItemModel editedDetailTableItemRow )
    {
      foreach ( var plumbingSize in editedDetailTableItemRow.PlumbingSizes ) {
        var plumbingModel = FindPlumbingModel( editedDetailTableItemRow.PlumbingType, plumbingSize.Name ) ;
        if(null == plumbingModel)
          continue;

        if ( double.Parse( plumbingModel.InnerCrossSectionalArea ) >= wireAreas )
          return plumbingSize.Name ;
      }

      return null ;
    }
    
    public void PlumbingSizeSelectionChanged( ComboBox comboBox )
    {
      var selectedPlumbingSize = comboBox.SelectedValue ;
      if ( selectedPlumbingSize == null ) return ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.PlumbingSize, selectedPlumbingSize.ToString(), new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void NumberOfGroundsLostFocus( ComboBox comboBox )
    {
      var numberOfGrounds = comboBox.Text ;
      if( string.IsNullOrEmpty( numberOfGrounds ) ) return ;
      var isNumberValue = int.TryParse( numberOfGrounds, out var numberOfGroundsInt ) ;
      if ( ! isNumberValue || ( isNumberValue && numberOfGroundsInt < 1 ) ) {
        comboBox.Text = string.Empty ;
        return ;
      }
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.NumberOfGrounds, numberOfGrounds!, new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void NumberOfGroundsSelection( ComboBox comboBox  )
    {
      var selectedNumberOfGrounds = comboBox.SelectedValue ;
      if ( selectedNumberOfGrounds == null ) return ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.NumberOfGrounds, selectedNumberOfGrounds.ToString(), new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    
    public void EarthSizeSelection( ComboBox comboBox  )
    {
      var selectedEarthSize = comboBox.SelectedValue ;
      if ( selectedEarthSize == null ) return ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.EarthSize, selectedEarthSize.ToString(), new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void EarthTypeSelection(ComboBox comboBox)
    {
      var selectedEarthType = comboBox.SelectedValue ;
      if ( selectedEarthType == null ) return ;
      
      var earthSizes = _wiresAndCablesModelData
        .Where( c => c.WireType == selectedEarthType.ToString() )
        .Select( c => c.DiameterOrNominal )
        .ToList() ;
      var earthSizeTypes = earthSizes.Any() ? 
        ( from earthSize in earthSizes select new DetailTableItemModel.ComboboxItemType( earthSize, earthSize ) ).ToList() 
        : new List<DetailTableItemModel.ComboboxItemType>() ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.EarthType, selectedEarthType.ToString(), earthSizeTypes ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    public void WireStripSelection( ComboBox comboBox )
    {
      var selectedWireStrip = comboBox.SelectedValue ;
      var selectedDetailTableItemRow = (DetailTableItemModel) DtGrid.SelectedValue ;
      if (    string.IsNullOrEmpty( selectedDetailTableItemRow.WireType ) 
           || string.IsNullOrEmpty( selectedDetailTableItemRow.WireSize )
           || selectedWireStrip == null
           || string.IsNullOrEmpty( selectedWireStrip.ToString() ) ) return ;
      
      var crossSectionalArea = Convert.ToDouble( 
        _wiresAndCablesModelData
          .FirstOrDefault( w => 
            w.WireType == selectedDetailTableItemRow.WireType 
            && w.DiameterOrNominal == selectedDetailTableItemRow.WireSize
            && ( w.NumberOfHeartsOrLogarithm + w.COrP == selectedWireStrip.ToString() || ( selectedWireStrip.ToString() == "-" && w.NumberOfHeartsOrLogarithm == "0" ) ) )?.CrossSectionalArea ) ;
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged( editedDetailTableItemRow, EditedColumn.WireStrip, selectedWireStrip.ToString(), new List<DetailTableItemModel.ComboboxItemType>(), crossSectionalArea ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireSizeSelection( ComboBox comboBox )
    {
      var selectedWireSize = comboBox.SelectedValue ;
      var selectedDetailTableItemRow = (DetailTableItemModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedDetailTableItemRow.WireType ) || selectedWireSize == null || string.IsNullOrEmpty( selectedWireSize.ToString() ) ) return ;
      
      var wireStripsOfWireType = _wiresAndCablesModelData
        .Where( w => w.WireType == selectedDetailTableItemRow.WireType && w.DiameterOrNominal == selectedWireSize.ToString() )
        .Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      var wireStrips = wireStripsOfWireType.Any() ? 
        ( from wireStrip in wireStripsOfWireType select new DetailTableItemModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() 
        : new List<DetailTableItemModel.ComboboxItemType>() ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged(editedDetailTableItemRow, EditedColumn.WireSize, selectedWireSize.ToString(), wireStrips ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireTypeSelection(ComboBox comboBox)
    {
      var wireType = comboBox.SelectedValue == null ? string.Empty : comboBox.SelectedValue.ToString() ;
      if ( string.IsNullOrEmpty( wireType ) ) return ;
      
      var wireSizesOfWireType = _wiresAndCablesModelData
        .Where( w => w.WireType == wireType )
        .Select( w => w.DiameterOrNominal )
        .Distinct()
        .ToList() ;
      var wireSizes = wireSizesOfWireType.Any() 
        ? ( from wireSize in wireSizesOfWireType select new DetailTableItemModel.ComboboxItemType( wireSize, wireSize ) ).ToList() 
        : new List<DetailTableItemModel.ComboboxItemType>() ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged(editedDetailTableItemRow, EditedColumn.WireType, wireType, wireSizes ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    public void PlumingTypeSelection(ComboBox comboBox)
    {
      var plumbingType = comboBox.SelectedValue ;
      if ( plumbingType == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableItemModel detailTableItemRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableItemRow.PlumbingType == plumbingType.ToString() ) return ;
        if ( plumbingType.ToString() == DefaultChildPlumbingSymbol ) {
          comboBox.SelectedValue = detailTableItemRow.PlumbingType ;
        }
        else {
          var detailTableModels = _detailTableModelItemOrigins.Where( c => CreateDetailTableCommandBase.GetKeyRouting(c) == CreateDetailTableCommandBase.GetKeyRouting(detailTableItemRow) ).ToList() ;
      
          if ( plumbingType.ToString() == NoPlumping ) {
            CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableModels, _isMixConstructionItems ) ;
          }
          else {
            CreateDetailTableCommandBase.SetPlumbingData( _conduitsModelData, ref detailTableModels, plumbingType.ToString(), _isMixConstructionItems ) ;
          }
      
          var detailTableRowsHaveGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) ).ToList() ;
          if ( detailTableRowsHaveGroupId.Any() ) {
            if ( _isMixConstructionItems ) {
              SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsHaveGroupId ) ;
            }
            else {
              SetGroupIdForDetailTableRows( detailTableRowsHaveGroupId ) ;
            }
          }
      
          if ( _isMixConstructionItems ) {
            SetPlumbingItemsForDetailTableItemRowsMixConstructionItems( detailTableModels ) ;
          }
          else {
            SetPlumbingItemsForDetailTableItemRows( detailTableModels ) ;
          }
      
          if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.ContainsKey( CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First() ) ) ) {
            DetailSymbolIdsWithPlumbingTypeHasChanged.Add( CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First() ), plumbingType.ToString() ) ;
          }
          else {
            DetailSymbolIdsWithPlumbingTypeHasChanged[ CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First() ) ] = plumbingType.ToString() ;
          }

          var sortDetailTableModels =  SortDetailTableModel( _detailTableModelItemOrigins, _isMixConstructionItems ) ;
          
          _detailTableModelItemOrigins = new ObservableCollection<DetailTableItemModel>( sortDetailTableModels ) ;
          
          CreateDetailTableViewModelByGroupId() ;
        }
      }
    }
    
    public void FloorSelection( ComboBox comboBox )
    {
      var selectedFloor = comboBox.SelectedValue ;
      if ( selectedFloor == null ) return ;
      
      if ( comboBox.DataContext is DetailTableItemModel editedDetailTableItemRow ) {
        ComboboxSelectionChanged(  editedDetailTableItemRow, EditedColumn.Floor, selectedFloor.ToString(), new List<DetailTableItemModel.ComboboxItemType>() ) ;
      }
    }

    private void ComboboxSelectionChanged(DetailTableItemModel editedDetailTableItemRow, EditedColumn editedColumn, string changedValue, List<DetailTableItemModel.ComboboxItemType> itemSourceCombobox, double crossSectionalArea = 0 )
    {
      if ( ! string.IsNullOrEmpty( editedDetailTableItemRow.GroupId ) ) {
        var detailTableItemRows = _detailTableModelItemOrigins.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == editedDetailTableItemRow.GroupId ).ToList() ;
        foreach ( var detailTableRow in detailTableItemRows ) {
          UpdateDetailTableModelRow( detailTableRow, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
        }
      }
      else {
        var detailTableRow = _detailTableModelItemOrigins.FirstOrDefault( d => d == editedDetailTableItemRow ) ;
        if ( detailTableRow != null ) 
          UpdateDetailTableModelRow( detailTableRow, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
      }

      var selectedDetailTableRowSummary = DetailTableItemModels.FirstOrDefault( d => d == editedDetailTableItemRow ) ;
      if ( selectedDetailTableRowSummary != null )
        UpdateDetailTableModelRow( selectedDetailTableRowSummary, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void UpdateDetailTableModelRow( DetailTableItemModel detailTableItemModelRow, EditedColumn editedColumn, string changedValue, double crossSectionalArea, List<DetailTableItemModel.ComboboxItemType> itemSourceCombobox )
    {
      switch ( editedColumn ) {
        case EditedColumn.Floor:
          detailTableItemModelRow.Floor = changedValue ;
          break;
        case EditedColumn.WireType:
          detailTableItemModelRow.WireType = changedValue ;
          detailTableItemModelRow.WireSizes = itemSourceCombobox ;
          break;
        case EditedColumn.WireSize:
          detailTableItemModelRow.WireSize = changedValue ;
          detailTableItemModelRow.WireStrips = itemSourceCombobox ;
          break;
        case EditedColumn.WireStrip:
          detailTableItemModelRow.WireStrip = changedValue ;
          detailTableItemModelRow.WireCrossSectionalArea = crossSectionalArea ;
          break;
        case EditedColumn.WireBook:
          detailTableItemModelRow.WireBook = changedValue ;
          var mark = detailTableItemModelRow.Remark.Contains( MultiplicationSymbol ) ? detailTableItemModelRow.Remark.Split( MultiplicationSymbol )[ 0 ] : detailTableItemModelRow.Remark ;
          var newRemark = int.TryParse( changedValue, out var value ) && value > 1 ? $"{mark}{MultiplicationSymbol}{value}" : mark ;
          if ( detailTableItemModelRow.Remark != newRemark ) {
            detailTableItemModelRow.Remark = newRemark ;
            DetailTableItemModels = new ObservableCollection<DetailTableItemModel>( DetailTableItemModels ) ;
          }
          break;
        case EditedColumn.EarthType:
          detailTableItemModelRow.EarthType = changedValue ;
          detailTableItemModelRow.EarthSizes = itemSourceCombobox ;
          break;
        case EditedColumn.EarthSize:
          detailTableItemModelRow.EarthSize = changedValue ;
          break;
        case EditedColumn.NumberOfGrounds:
          detailTableItemModelRow.NumberOfGround = changedValue ;
          break;
        case EditedColumn.PlumbingSize:
          detailTableItemModelRow.PlumbingSize = changedValue ;
          break;
        case EditedColumn.ConstructionClassification:
          detailTableItemModelRow.ConstructionClassification = changedValue ;
          break;
        case EditedColumn.SignalType:
          detailTableItemModelRow.SignalType = changedValue ;
          break;
        case EditedColumn.Remark :
          detailTableItemModelRow.Remark = changedValue ;
          break ;
      }
    }

    public static string GetRemark( string oldRemark, int wireBook )
    {
      var remarks = oldRemark.Split( MultiplicationSymbol ) ;
      if ( ! remarks.Any() ) 
        return string.Empty ;
      var newRemarks = wireBook > 1 ? remarks.First() + MultiplicationSymbol + wireBook : remarks.First() ;
      return newRemarks ;
    }

    private void DeleteReferenceDetailTableRows(List<DetailTableItemModel> selectedDetailTableItemModels )
    {
      var deletedDetailTableItemRows = new List<DetailTableItemModel>() ;
      foreach ( var selectedDetailTableItemModel in selectedDetailTableItemModels ) {
        if ( ! string.IsNullOrEmpty( selectedDetailTableItemModel.GroupId ) ) {
          var detailTableRowsOfGroup = ReferenceDetailTableItemModelsOrigin.Where( d => d.GroupId == selectedDetailTableItemModel.GroupId ) ;
          deletedDetailTableItemRows.AddRange( detailTableRowsOfGroup ) ;
        }
        else {
          deletedDetailTableItemRows.Add( selectedDetailTableItemModel ) ;
        }
      }
    
      var detailTableItemRows = ReferenceDetailTableItemModelsOrigin.Where( d => ! deletedDetailTableItemRows.Contains( d ) ) ;
      ReferenceDetailTableItemModelsOrigin = new ObservableCollection<DetailTableItemModel>( detailTableItemRows ) ;
    
      var detailTableItemRowsSummary = ReferenceDetailTableItemModels.Where( d => ! selectedDetailTableItemModels.Contains( d ) ) ;
      ReferenceDetailTableItemModels = new ObservableCollection<DetailTableItemModel>( detailTableItemRowsSummary ) ;
    }
    
    private List<DetailTableItemModel> SelectDetailTableRowsWithSameDetailSymbolId(List<DetailTableItemModel> selectedDetailTableItemModels )
    {
      List<DetailTableItemModel> detailTableItemRowsWithSameDetailSymbolId = new() ;
      foreach ( var selectedDetailTableItemModel in selectedDetailTableItemModels ) {
        var detailTableRows = ReferenceDetailTableItemModels.Where( d => CreateDetailTableCommandBase.GetKeyRouting(d) == CreateDetailTableCommandBase.GetKeyRouting( selectedDetailTableItemModel ) ) ;
        detailTableItemRowsWithSameDetailSymbolId.AddRange( detailTableRows ) ;
      }

      return detailTableItemRowsWithSameDetailSymbolId ;
    }

    private void ReadCtlFile( List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
    {
      MessageBox.Show( "Arent3d.Architecture.Routing.AppBase.ViewModel.Select.CTLFile".GetAppStringByKeyOrDefault( "Please select ctl file." ), "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = @"Ctl files (*.ctl)|*.ctl", Multiselect = false } ;
      var filePath = string.Empty ;
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
        if ( detailTableRow.Remark.Contains( ',' ) || detailTableRow.Remark.Contains( MultiplicationSymbol ) ) {
          AddUnGroupDetailTableRows( ReferenceDetailTableItemModelsOrigin, detailTableRow ) ; 
        }
        else {
          ReferenceDetailTableItemModelsOrigin.Add( detailTableRow ) ;
        }
        ReferenceDetailTableItemModels.Add( detailTableRow ) ;
      }
    }

    private void AddUnGroupDetailTableRows( ObservableCollection<DetailTableItemModel> unGroupDetailTableItemModels, DetailTableItemModel detailTableItemRow )
    {
      var remarks = detailTableItemRow.Remark.Split( ',' ) ;
      var isParentDetailRow = ! detailTableItemRow.IsParentRoute ;
      foreach ( var remark in remarks ) {
        if ( remark.Contains( MultiplicationSymbol ) ) {
          var remarkArr = remark.Split( MultiplicationSymbol ) ;
          var countRows = int.Parse( remarkArr.Last() ) ;
          var remarkRow = remarkArr.Length == 2 ? remarkArr.First().Trim() : remarkArr.First().Trim() + MultiplicationSymbol + remarkArr.ElementAt( 1 ) ;
          var wireBook = remarkArr.Length == 2 ? "1" : remarkArr.ElementAt( 1 ) ;
          if ( ! isParentDetailRow ) {
            var newDetailTableRow = CreateParentDetailTableItemModel( detailTableItemRow, wireBook, remarkRow ) ;
            unGroupDetailTableItemModels.Add( newDetailTableRow ) ;
            for ( var i = 1 ; i < countRows ; i++ ) {
              newDetailTableRow = CreateChildDetailTableModel( detailTableItemRow, wireBook, remarkRow ) ;
              unGroupDetailTableItemModels.Add( newDetailTableRow );
            }
            isParentDetailRow = true ;
          }
          else {
            for ( var i = 0 ; i < countRows ; i++ ) {
              var newDetailTableRow = CreateChildDetailTableModel( detailTableItemRow, wireBook, remarkRow ) ;
              unGroupDetailTableItemModels.Add( newDetailTableRow );
            }
          }
        }
        else {
          if ( ! isParentDetailRow ) {
            var newDetailTableRow = CreateParentDetailTableItemModel( detailTableItemRow, "1", remark.Trim() ) ;
            unGroupDetailTableItemModels.Add( newDetailTableRow ) ;
            isParentDetailRow = true ;
          }
          else {
            var newDetailTableRow = CreateChildDetailTableModel( detailTableItemRow, "1", remark.Trim() ) ;
            unGroupDetailTableItemModels.Add( newDetailTableRow ) ;
          }
        }
      }
    }
    
    private DetailTableItemModel CreateParentDetailTableItemModel( DetailTableItemModel detailTableItemRow, string wireBook, string remarkRow )
    {
      var newDetailTableItemRow = new DetailTableItemModel
      ( 
        detailTableItemRow.CalculationExclusion, 
        detailTableItemRow.Floor, 
        detailTableItemRow.CeedCode, 
        detailTableItemRow.DetailSymbol, 
        detailTableItemRow.DetailSymbolUniqueId,
        detailTableItemRow.FromConnectorUniqueId,
        detailTableItemRow.ToConnectorUniqueId,
        detailTableItemRow.WireType, 
        detailTableItemRow.WireSize, 
        detailTableItemRow.WireStrip, 
        wireBook, 
        detailTableItemRow.EarthType, 
        detailTableItemRow.EarthSize, 
        detailTableItemRow.NumberOfGround, 
        detailTableItemRow.PlumbingType, 
        detailTableItemRow.PlumbingSize, 
        detailTableItemRow.NumberOfPlumbing, 
        detailTableItemRow.ConstructionClassification, 
        detailTableItemRow.SignalType, 
        detailTableItemRow.ConstructionItems, 
        detailTableItemRow.PlumbingItems, 
        remarkRow, 
        detailTableItemRow.WireCrossSectionalArea, 
        detailTableItemRow.CountCableSamePosition, 
        detailTableItemRow.RouteName, 
        detailTableItemRow.IsEcoMode, true, 
        false, 
        detailTableItemRow.PlumbingIdentityInfo, 
        detailTableItemRow.GroupId, 
        ! detailTableItemRow.IsMixConstructionItems, 
        detailTableItemRow.IsMixConstructionItems, 
        string.Empty,
        detailTableItemRow.IsReadOnlyParameters, 
        detailTableItemRow.IsReadOnlyWireSizeAndWireStrip, 
        detailTableItemRow.IsReadOnlyPlumbingSize, 
        detailTableItemRow.WireSizes, 
        detailTableItemRow.WireStrips, 
        detailTableItemRow.EarthSizes, 
        detailTableItemRow.PlumbingSizes, 
        detailTableItemRow.PlumbingItemTypes 
      ) ;
      return newDetailTableItemRow ;
    }

    private DetailTableItemModel CreateChildDetailTableModel( DetailTableItemModel detailTableItemRow, string wireBook, string remarkRow )
    {
      const string defaultChildPlumbingSymbol = "↑" ;
      var newDetailTableItemRow = new DetailTableItemModel
      ( detailTableItemRow.CalculationExclusion, 
        detailTableItemRow.Floor, 
        detailTableItemRow.CeedCode, 
        detailTableItemRow.DetailSymbol, 
        detailTableItemRow.DetailSymbolUniqueId, 
        detailTableItemRow.FromConnectorUniqueId, 
        detailTableItemRow.ToConnectorUniqueId, 
        detailTableItemRow.WireType, 
        detailTableItemRow.WireSize, 
        detailTableItemRow.WireStrip, 
        wireBook, 
        detailTableItemRow.EarthType, 
        detailTableItemRow.EarthSize, 
        detailTableItemRow.NumberOfGround, 
        defaultChildPlumbingSymbol, 
        defaultChildPlumbingSymbol, 
        defaultChildPlumbingSymbol, 
        detailTableItemRow.ConstructionClassification, 
        detailTableItemRow.SignalType, 
        detailTableItemRow.ConstructionItems, 
        detailTableItemRow.PlumbingItems, 
        remarkRow, 
        detailTableItemRow.WireCrossSectionalArea, 
        detailTableItemRow.CountCableSamePosition, 
        detailTableItemRow.RouteName, 
        detailTableItemRow.IsEcoMode, 
        false, 
        true, 
        detailTableItemRow.PlumbingIdentityInfo, 
        detailTableItemRow.GroupId, 
        true, 
        detailTableItemRow.IsMixConstructionItems, 
        string.Empty,
        detailTableItemRow.IsReadOnlyParameters, 
        detailTableItemRow.IsReadOnlyWireSizeAndWireStrip, 
        true, 
        detailTableItemRow.WireSizes, 
        detailTableItemRow.WireStrips, 
        detailTableItemRow.EarthSizes, 
        detailTableItemRow.PlumbingSizes, 
        detailTableItemRow.PlumbingItemTypes
      ) ;
      return newDetailTableItemRow ;
    }

    private void GetValuesForParametersOfDetailTableModels( List<DetailTableItemModel> detailTableItemModels, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
    {
      var detailTableRowsGroupByDetailSymbolId = detailTableItemModels.GroupBy( CreateDetailTableCommandBase.GetKeyRouting ).Select( d => d.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableRowsGroupByDetailSymbolId ) {
        var parentDetailTableRow = detailTableRowsWithSameDetailSymbolId.FirstOrDefault( d => d.IsParentRoute ) ;
        var plumbingType = parentDetailTableRow == null ? DefaultParentPlumbingType : parentDetailTableRow.PlumbingType ;
        var plumbingSizesOfPlumbingType = plumbingType == NoPlumping ? new List<string>() { NoPlumbingSize } 
            : conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
        var plumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableItemModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
        var detailTableRowsGroupByPlumbingIdentityInfo = detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( d => d.ToList() ) ;
        foreach ( var detailTableRowsWithSamePlumbing in detailTableRowsGroupByPlumbingIdentityInfo ) {
          var constructionItems = detailTableRowsWithSamePlumbing.Select( d => d.ConstructionItems ).Distinct().ToList() ;
          var plumbingItemTypes = constructionItems.Any() ? 
            ( from plumbingItem in constructionItems select new DetailTableItemModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() 
            : new List<DetailTableItemModel.ComboboxItemType>() ;
          foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
            var wireSizesOfWireType = wiresAndCablesModelData
              .Where( w => w.WireType == detailTableRow.WireType )
              .Select( w => w.DiameterOrNominal )
              .Distinct()
              .ToList() ;
            var wireSizes = wireSizesOfWireType.Any() ? 
              ( from wireSizeType in wireSizesOfWireType select new DetailTableItemModel.ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() 
              : new List<DetailTableItemModel.ComboboxItemType>() ;
              
            var wireStripsOfWireType = wiresAndCablesModelData
              .Where( w => w.WireType == detailTableRow.WireType && w.DiameterOrNominal == detailTableRow.WireSize )
              .Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP )
              .Distinct()
              .ToList() ;
            var wireStrips = wireStripsOfWireType.Any() ? 
              ( from wireStripType in wireStripsOfWireType select new DetailTableItemModel.ComboboxItemType( wireStripType, wireStripType ) ).ToList() 
              : new List<DetailTableItemModel.ComboboxItemType>() ;
            
            detailTableRow.WireSizes = wireSizes ;
            detailTableRow.WireStrips = wireStrips ;
            detailTableRow.PlumbingSizes = plumbingSizes ;
            detailTableRow.PlumbingItemTypes = detailTableRow.IsMixConstructionItems ? 
              plumbingItemTypes 
              : new List<DetailTableItemModel.ComboboxItemType> { new( detailTableRow.ConstructionItems, detailTableRow.ConstructionItems ) } ;
            if ( string.IsNullOrEmpty( detailTableRow.EarthType ) ) continue ;
            var earthSizes = wiresAndCablesModelData
              .Where( c => c.WireType == detailTableRow.EarthType )
              .Select( c => c.DiameterOrNominal )
              .ToList() ;
            detailTableRow.EarthSizes = earthSizes.Any() ? 
              ( from earthSize in earthSizes select new DetailTableItemModel.ComboboxItemType( earthSize, earthSize ) ).ToList() 
              : new List<DetailTableItemModel.ComboboxItemType>() ;
          }
        }
      }
    }

    private void AddReferenceDetailTableRows(List<DetailTableItemModel> selectedDetailTableItemModels )
    {
      DetailTableItemModel? detailTableItemModel = null ;
      if ( DtGrid.SelectedItems.Count == 1 && selectedDetailTableItemModels.Count > 0)
        detailTableItemModel = (DetailTableItemModel) DtGrid.SelectedItems[ 0 ] ;

      SelectionChanged() ;
      if ( ! _selectedDetailTableItemRows.Any() || ! _selectedDetailTableItemRowsSummary.Any() ) {
        MessageBox.Show( "Arent3d.Architecture.Routing.AppBase.ViewModel.Select.Table".GetAppStringByKeyOrDefault( "Please select a row on the detail table." ), "Arent Inc" ) ;
        return ;
      }
      
      if ( null == detailTableItemModel )
        return;

      var indexForSelectedDetailTableRow = _detailTableModelItemOrigins.IndexOf( _selectedDetailTableItemRows.Last() );
      var indexForSelectedDetailTableRowSummary = DetailTableItemModels.IndexOf( _selectedDetailTableItemRowsSummary.Last() ) ;

      foreach ( var selectedDetailTableItemModel in selectedDetailTableItemModels ) {
        var extendValue = $"{Guid.NewGuid()}" ;
        indexForSelectedDetailTableRow++ ;
        indexForSelectedDetailTableRowSummary++ ;
        var groupId = string.IsNullOrEmpty( selectedDetailTableItemModel.GroupId ) ? string.Empty : selectedDetailTableItemModel.GroupId + "-" + extendValue ;
        var referenceDetailTableRow = new DetailTableItemModel( 
          selectedDetailTableItemModel.CalculationExclusion, 
          selectedDetailTableItemModel.Floor, 
          selectedDetailTableItemModel.CeedCode, 
          _isCallFromAddWiringInformationCommand ? AddWiringInformationCommandBase.SpecialSymbol : detailTableItemModel.DetailSymbol, 
          detailTableItemModel.DetailSymbolUniqueId,
          detailTableItemModel.FromConnectorUniqueId, 
          detailTableItemModel.ToConnectorUniqueId, 
          selectedDetailTableItemModel.WireType, 
          selectedDetailTableItemModel.WireSize,
          selectedDetailTableItemModel.WireStrip, 
          selectedDetailTableItemModel.WireBook, 
          selectedDetailTableItemModel.EarthType, 
          selectedDetailTableItemModel.EarthSize,
          selectedDetailTableItemModel.NumberOfGround,
          selectedDetailTableItemModel.PlumbingType, 
          selectedDetailTableItemModel.PlumbingSize, 
          selectedDetailTableItemModel.NumberOfPlumbing, 
          selectedDetailTableItemModel.ConstructionClassification, 
          selectedDetailTableItemModel.SignalType, 
          selectedDetailTableItemModel.ConstructionItems,
          selectedDetailTableItemModel.PlumbingItems,
          selectedDetailTableItemModel.Remark, 
          selectedDetailTableItemModel.WireCrossSectionalArea, 
          selectedDetailTableItemModel.CountCableSamePosition, 
          detailTableItemModel.RouteName, 
          selectedDetailTableItemModel.IsEcoMode, 
          selectedDetailTableItemModel.IsParentRoute, 
          selectedDetailTableItemModel.IsReadOnly, 
          selectedDetailTableItemModel.PlumbingIdentityInfo + "-" + extendValue, 
          groupId, 
          selectedDetailTableItemModel.IsReadOnlyPlumbingItems,
          selectedDetailTableItemModel.IsMixConstructionItems, 
          selectedDetailTableItemModel.CopyIndex + extendValue, 
          selectedDetailTableItemModel.IsReadOnlyParameters, 
          selectedDetailTableItemModel.IsReadOnlyWireSizeAndWireStrip,
          selectedDetailTableItemModel.IsReadOnlyPlumbingSize,
          selectedDetailTableItemModel.WireSizes, 
          selectedDetailTableItemModel.WireStrips, 
          selectedDetailTableItemModel.EarthSizes, 
          selectedDetailTableItemModel.PlumbingSizes, 
          selectedDetailTableItemModel.PlumbingItemTypes ) ;
        if ( ! string.IsNullOrEmpty( selectedDetailTableItemModel.GroupId ) ) {
          var detailTableRowsOfGroup = ReferenceDetailTableItemModelsOrigin.Where( d => d.GroupId == selectedDetailTableItemModel.GroupId ) ;
          foreach ( var detailTableRowOfGroup in detailTableRowsOfGroup ) {
            var newReferenceDetailTableItemRow = new DetailTableItemModel( 
              detailTableRowOfGroup.CalculationExclusion, 
              detailTableRowOfGroup.Floor, 
              detailTableRowOfGroup.CeedCode, 
              _isCallFromAddWiringInformationCommand ? AddWiringInformationCommandBase.SpecialSymbol : detailTableItemModel.DetailSymbol, 
              detailTableItemModel.DetailSymbolUniqueId,
              detailTableItemModel.FromConnectorUniqueId, 
              detailTableItemModel.ToConnectorUniqueId, 
              detailTableRowOfGroup.WireType, 
              detailTableRowOfGroup.WireSize, 
              detailTableRowOfGroup.WireStrip,
              detailTableRowOfGroup.WireBook,
              detailTableRowOfGroup.EarthType, 
              detailTableRowOfGroup.EarthSize, 
              detailTableRowOfGroup.NumberOfGround,
              detailTableRowOfGroup.PlumbingType,
              detailTableRowOfGroup.PlumbingSize, 
              detailTableRowOfGroup.NumberOfPlumbing, 
              detailTableRowOfGroup.ConstructionClassification, 
              detailTableRowOfGroup.SignalType,
              detailTableRowOfGroup.ConstructionItems, 
              detailTableRowOfGroup.PlumbingItems, 
              detailTableRowOfGroup.Remark, 
              detailTableRowOfGroup.WireCrossSectionalArea, 
              detailTableRowOfGroup.CountCableSamePosition, 
              detailTableItemModel.RouteName, 
              detailTableRowOfGroup.IsEcoMode, 
              detailTableRowOfGroup.IsParentRoute, 
              detailTableRowOfGroup.IsReadOnly, 
              detailTableRowOfGroup.PlumbingIdentityInfo + "-" + extendValue, 
              groupId, 
              detailTableRowOfGroup.IsReadOnlyPlumbingItems,
              detailTableRowOfGroup.IsMixConstructionItems, 
              detailTableRowOfGroup.CopyIndex + extendValue, 
              detailTableRowOfGroup.IsReadOnlyParameters,
              detailTableRowOfGroup.IsReadOnlyWireSizeAndWireStrip,
              detailTableRowOfGroup.IsReadOnlyPlumbingSize, 
              detailTableRowOfGroup.WireSizes, 
              detailTableRowOfGroup.WireStrips, 
              detailTableRowOfGroup.EarthSizes, 
              detailTableRowOfGroup.PlumbingSizes, 
              detailTableRowOfGroup.PlumbingItemTypes ) ;
            _detailTableModelItemOrigins.Insert( indexForSelectedDetailTableRow, newReferenceDetailTableItemRow ) ;
          }
        }
        else {
          _detailTableModelItemOrigins.Insert( indexForSelectedDetailTableRow, referenceDetailTableRow ) ;
        }

        DetailTableItemModels.Insert( indexForSelectedDetailTableRowSummary, referenceDetailTableRow ) ;
      }
    }

    private List<DetailTableItemModel> GroupDetailTableModels( ObservableCollection<DetailTableItemModel> oldDetailTableItemModels )
    {
      List<DetailTableItemModel> newDetailTableItemModels = new() ;
      List<string> existedGroupIds = new() ;
      foreach ( var oldDetailTableItemModel in oldDetailTableItemModels ) {
        if ( string.IsNullOrEmpty( oldDetailTableItemModel.GroupId ) ) {
          newDetailTableItemModels.Add( oldDetailTableItemModel ) ;
        }
        else {
          if ( existedGroupIds.Contains( oldDetailTableItemModel.GroupId ) ) 
            continue ;
          
          var detailTableRowWithSameWiringType = oldDetailTableItemModels.Where( d => d.GroupId == oldDetailTableItemModel.GroupId ) ;
          var detailTableRowsGroupByRemark = detailTableRowWithSameWiringType
            .GroupBy( d => d.Remark )
            .ToDictionary( g => g.Key, g => g.ToList() ) ;
          
          List<string> newRemark = new() ;
          var wireBook = 0 ;
          var numberOfGrounds = 0 ;
          foreach ( var (remark, detailTableRowsWithSameRemark) in detailTableRowsGroupByRemark ) {
            newRemark.Add( detailTableRowsWithSameRemark.Count == 1 ? remark : remark + MultiplicationSymbol + detailTableRowsWithSameRemark.Count ) ;
            foreach ( var detailTableRowWithSameRemark in detailTableRowsWithSameRemark ) {
              if ( ! string.IsNullOrEmpty( detailTableRowWithSameRemark.WireBook ) ) {
                wireBook += int.Parse( detailTableRowWithSameRemark.WireBook ) ;
              }
              if ( ! string.IsNullOrEmpty( detailTableRowWithSameRemark.NumberOfGround ) ) {
                numberOfGrounds += int.Parse( detailTableRowWithSameRemark.NumberOfGround ) ;
              }
            }
          }

          var newDetailTableItemRow = new DetailTableItemModel( 
            oldDetailTableItemModel.CalculationExclusion, 
            oldDetailTableItemModel.Floor, 
            oldDetailTableItemModel.CeedCode, 
            oldDetailTableItemModel.DetailSymbol, 
            oldDetailTableItemModel.DetailSymbolUniqueId,
            oldDetailTableItemModel.FromConnectorUniqueId,
            oldDetailTableItemModel.ToConnectorUniqueId,
            oldDetailTableItemModel.WireType, 
            oldDetailTableItemModel.WireSize, 
            oldDetailTableItemModel.WireStrip, 
            wireBook > 0 ? wireBook.ToString() : string.Empty, 
            oldDetailTableItemModel.EarthType, 
            oldDetailTableItemModel.EarthSize, 
            numberOfGrounds > 0 ? numberOfGrounds.ToString() : string.Empty, 
            oldDetailTableItemModel.PlumbingType, 
            oldDetailTableItemModel.PlumbingSize, 
            oldDetailTableItemModel.NumberOfPlumbing, 
            oldDetailTableItemModel.ConstructionClassification, 
            oldDetailTableItemModel.SignalType, 
            oldDetailTableItemModel.ConstructionItems, 
            oldDetailTableItemModel.PlumbingItems, 
            string.Join( ", ", newRemark ), 
            oldDetailTableItemModel.WireCrossSectionalArea, 
            oldDetailTableItemModel.CountCableSamePosition, 
            oldDetailTableItemModel.RouteName, 
            oldDetailTableItemModel.IsEcoMode, 
            oldDetailTableItemModel.IsParentRoute, 
            oldDetailTableItemModel.IsReadOnly, 
            oldDetailTableItemModel.PlumbingIdentityInfo, 
            oldDetailTableItemModel.GroupId,
            oldDetailTableItemModel.IsReadOnlyPlumbingItems, 
            oldDetailTableItemModel.IsMixConstructionItems,
            oldDetailTableItemModel.CopyIndex, 
            oldDetailTableItemModel.IsReadOnlyParameters, 
            oldDetailTableItemModel.IsReadOnlyWireSizeAndWireStrip, 
            oldDetailTableItemModel.IsReadOnlyPlumbingSize, 
            oldDetailTableItemModel.WireSizes, 
            oldDetailTableItemModel.WireStrips,
            oldDetailTableItemModel.EarthSizes, 
            oldDetailTableItemModel.PlumbingSizes, 
            oldDetailTableItemModel.PlumbingItemTypes ) { IsGrouped = oldDetailTableItemModel.IsGrouped };
          newDetailTableItemModels.Add( newDetailTableItemRow ) ;
          existedGroupIds.Add( oldDetailTableItemModel.GroupId ) ;
        }
      }

      return newDetailTableItemModels ;
    }
  }

  public class DetailTableData
  {
    
    #region Fields
    private static readonly object _padlock = new();
    private static DetailTableData? _instance;
    #endregion

    private DetailTableData()
    {
      
    }
    
    public static DetailTableData Instance
    {
      get
      {
        if ( null != _instance ) 
          return _instance ;
        
        lock (_padlock) {
          _instance ??= new DetailTableData() ;
        }
        
        return _instance;
      }
    }

    public bool FirstLoaded { get ; set ; }

  }
}