using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
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
using ComboBox = System.Windows.Controls.ComboBox ;
using DataGrid = System.Windows.Controls.DataGrid ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.UI ;
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
    private readonly DetailSymbolStorable _detailSymbolStorable ;
    private List<DetailTableModel> _selectedDetailTableRows ;
    private List<DetailTableModel> _selectedDetailTableRowsSummary ;
    private List<DetailTableModel> _selectedReferenceDetailTableRows ;
    private DetailTableModel? _copyDetailTableRow ;
    private DetailTableModel? _copyDetailTableRowSummary ;
    private readonly bool _isCallFromAddWiringInformationCommand ;
    
    public Dictionary<string, string> RoutesWithConstructionItemHasChanged { get ; }
    public Dictionary<string, string> DetailSymbolIdsWithPlumbingTypeHasChanged { get ; }
    private bool _isMixConstructionItems ;
    
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

    public ObservableCollection<DetailTableModel> ReferenceDetailTableModelsOrigin { get ; set ; }
    
    private ObservableCollection<DetailTableModel> _referenceDetailTableModels ;
    public ObservableCollection<DetailTableModel> ReferenceDetailTableModels {  
      get => _referenceDetailTableModels ;
      set
      {
        _referenceDetailTableModels = value ;
        OnPropertyChanged( nameof(ReferenceDetailTableModels) );
      }  }
    
    public bool IsCreateDetailTableOnFloorPlanView { get ; set ; }

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
        var detailSymbolStorable = _document.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? _document.GetDetailSymbolStorable() ;
        var detailSymbolModels = detailSymbolStorable.DetailSymbolModelData.Where( x => !string.IsNullOrEmpty(x.DetailSymbolUniqueId) && x.ConduitId == conduit?.UniqueId && x.DetailSymbol == AddWiringInformationCommandBase.SpecialSymbol).EnumerateAll() ;
        IsShowSymbol = detailSymbolModels.Any() ;
      }
    }
    
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
      
      DtGrid.SelectAll();
      PlumbingSummaryMixConstructionItems() ;
      DetailTableData.Instance.FirstLoaded = true ;
    }

    private void SelectionChangedReference()
    {
      var selectedItems = DtReferenceGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      _selectedReferenceDetailTableRows.Clear() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableModel detailTableRow ) continue ;
        _selectedReferenceDetailTableRows.Add( detailTableRow ) ;
      }
    }
    
    private void AddReferenceRows()
    {
      SelectionChangedReference() ;
      if ( ! _selectedReferenceDetailTableRows.Any() ) {
        MessageBox.Show( "Please select the row on the reference detail table.", "Arent Inc" ) ;
        return ;
      }
      AddReferenceDetailTableRows(_selectedReferenceDetailTableRows ) ;
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
      if ( ! _selectedReferenceDetailTableRows.Any() ) return ;
      var detailTableRowsWithSameDetailSymbolId = SelectDetailTableRowsWithSameDetailSymbolId(_selectedReferenceDetailTableRows ) ;
      _selectedReferenceDetailTableRows.Clear() ;
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
      if ( ! _selectedReferenceDetailTableRows.Any() ) return ;
      DeleteReferenceDetailTableRows(_selectedReferenceDetailTableRows) ;
      UpdateReferenceDetailTableModels() ;
    }
    
    private void UpdateReferenceDetailTableModels()
    {
      SelectionChangedReference() ;
      _selectedReferenceDetailTableRows.Clear() ;
      DtReferenceGrid.SelectedItems.Clear() ;
    }
    
    private void ReferenceSelectAll()
    {
      _selectedReferenceDetailTableRows = ReferenceDetailTableModels.ToList() ;
      DtReferenceGrid.SelectAll() ;
    }
    
    private void PlumbingSummary()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableRows.Any() ) return;
      _isMixConstructionItems = false ;
      PlumbingSum() ;
    }
    
    private void PlumbingSummaryMixConstructionItems()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableRows.Any() ) return ;
      _isMixConstructionItems = true ;
      PlumbingSum() ;
    }
    
    private void PlumbingSum()
    {
      SelectionChanged() ;
      PlumbingSummary( _conduitsModelData, _detailSymbolStorable, _selectedDetailTableRows, _isMixConstructionItems, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
    }
    
    private void SplitPlumbing()
    {
      SelectionChanged() ;
      SplitPlumbing( _conduitsModelData, _detailSymbolStorable, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
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
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      var selectedDetailTableRow = _selectedDetailTableRows.First() ;
      var selectedDetailTableRowSummary = _selectedDetailTableRowsSummary.First() ;
      var isMove = MoveDetailTableRow(  selectedDetailTableRow, selectedDetailTableRowSummary, isMoveUp ) ;
      if ( isMove ) UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void Add()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      var selectedDetailTableRow = _selectedDetailTableRows.Last() ;
      var selectedDetailTableRowSummary = _selectedDetailTableRowsSummary.Last() ;
      AddDetailTableRow(  selectedDetailTableRow, selectedDetailTableRowSummary ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void DeleteLine()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      DeleteDetailTableRows() ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    private void CopyLine()
    {
      SelectionChanged() ;
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      _copyDetailTableRow = _selectedDetailTableRows.First() ;
      _copyDetailTableRowSummary = _selectedDetailTableRowsSummary.First() ;
      ResetSelectedItems() ;
    }
    
    private void PasteLine()
    {
      SelectionChanged() ;
      if ( _copyDetailTableRow == null || _copyDetailTableRowSummary == null ) {
        MessageBox.Show( @"Please choose a row to copy", @"Message" ) ;
        return ;
      }
      
      var pasteDetailTableRow = !  _selectedDetailTableRows.Any() ? _copyDetailTableRow : _selectedDetailTableRows.First() ;
      var pasteDetailTableRowSummary = ! _selectedDetailTableRowsSummary.Any() ? _copyDetailTableRowSummary : _selectedDetailTableRowsSummary.First() ;
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
          var detailTableRows = _detailTableModelsOrigin
            .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == detailTableRow.GroupId )
            .ToList() ;
          _selectedDetailTableRows.AddRange( detailTableRows ) ;
        }
        else {
          _selectedDetailTableRows.Add( detailTableRow ) ;
        }
        _selectedDetailTableRowsSummary.Add( detailTableRow ) ;
      }
    }
    
    private void RowDoubleClick()
    {
      if ( DtGrid.SelectedValue == null ) return ;
      var selectedItem = (DetailTableModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedItem.GroupId ) ) return ;
      UnGroupDetailTableRows( selectedItem.GroupId ) ;
      CreateDetailTableViewModelByGroupId() ;
    }
    
    private void UnGroupDetailTableRows( string groupId )
    {
      var detailTableModels = _detailTableModelsOrigin
        .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId )
        .ToList() ;
      foreach ( var detailTableRow in detailTableModels ) {
        detailTableRow.GroupId = string.Empty ;
      }
    }

    private void SelectAll()
    {
      _selectedDetailTableRows = _detailTableModelsOrigin.ToList() ;
      _selectedDetailTableRowsSummary = _detailTableModels.ToList() ;
      DtGrid.SelectAll() ;
    }

    public DetailTableViewModel
    ( 
      Document document, 
      ObservableCollection<DetailTableModel> detailTableModels, 
      ObservableCollection<DetailTableModel> referenceDetailTableModels,
      IEnumerable<DetailTableModel.ComboboxItemType> conduitTypes, 
      IEnumerable<DetailTableModel.ComboboxItemType> constructionItems, 
      IEnumerable<DetailTableModel.ComboboxItemType> levels,
      IEnumerable<DetailTableModel.ComboboxItemType> wireTypes, 
      IEnumerable<DetailTableModel.ComboboxItemType> earthTypes, 
      IEnumerable<DetailTableModel.ComboboxItemType> numbers,
      IEnumerable<DetailTableModel.ComboboxItemType> constructionClassificationTypes, 
      IEnumerable<DetailTableModel.ComboboxItemType> signalTypes, 
      IEnumerable<ConduitsModel> conduitsModelData,
      IEnumerable<WiresAndCablesModel> wiresAndCablesModelData, 
      bool mixConstructionItems,
      bool isCallFromAddWiringInformationCommand = false
    )
    {
      _detailTableModelsOrigin =  new ObservableCollection<DetailTableModel>(detailTableModels)  ;
      _detailTableModels = new ObservableCollection<DetailTableModel>(detailTableModels) ;
      _document = document ;
      DtGrid = new DataGrid() ;
      DtReferenceGrid = new DataGrid() ;
      ReferenceDetailTableModelsOrigin = new ObservableCollection<DetailTableModel>(referenceDetailTableModels) ;
      _referenceDetailTableModels = new ObservableCollection<DetailTableModel>(referenceDetailTableModels) ;
      IsCreateDetailTableOnFloorPlanView = false ;
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
      _detailSymbolStorable = document.GetDetailSymbolStorable() ;
      _selectedDetailTableRows = new List<DetailTableModel>() ;
      _selectedDetailTableRowsSummary = new List<DetailTableModel>() ;
      _copyDetailTableRow = null ;
      _copyDetailTableRowSummary = null ;
      _selectedReferenceDetailTableRows = new List<DetailTableModel>() ;
      _isCallFromAddWiringInformationCommand = isCallFromAddWiringInformationCommand ;
    }
    
    public void CreateDetailTableViewModelByGroupId( bool isGroupReferenceDetailTableModels = false )
    {
      List<DetailTableModel> newDetailTableModels = GroupDetailTableModels(_detailTableModelsOrigin) ;
      List<DetailTableModel> newReferenceDetailTableModels = GroupDetailTableModels(ReferenceDetailTableModelsOrigin) ;
      DetailTableModels = new ObservableCollection<DetailTableModel>(newDetailTableModels)  ;
      
      if ( newReferenceDetailTableModels.Any() && isGroupReferenceDetailTableModels ) {
        ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>(newReferenceDetailTableModels);
      }
    }
    
    private void SaveDetailTable(Window window)
    {
      var mixtureOfMultipleConstructionClassificationsInDetailSymbol = string.Empty ;
      if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableModelsOrigin, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ) {
        MyMessageBox.Show(string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), mixtureOfMultipleConstructionClassificationsInDetailSymbol), "Error") ;
      }
      else {
        SaveData( _document, _detailTableModelsOrigin ) ;
        SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
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
        foreach ( var item in DetailTableModels ) {
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
            item.NumberOfGrounds, 
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
      if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableModelsOrigin, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ) {
        MyMessageBox.Show(string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), mixtureOfMultipleConstructionClassificationsInDetailSymbol), "Error") ;
        IsCreateDetailTableOnFloorPlanView = false ;
      }
      else {
        SaveData( _document, _detailTableModelsOrigin ) ;
        SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
        window.DialogResult = true ;
        window.Close() ;
        IsCreateDetailTableOnFloorPlanView = true ;
      }
    }

    private bool IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol(ObservableCollection<DetailTableModel> detailTableModels, ref string mixtureOfMultipleConstructionClassificationsInDetailSymbol )
    {
      var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy( d => d.DetailSymbol ) ;
      var mixSymbolGroup = detailTableModelsGroupByDetailSymbolId
        .Where( x => x.GroupBy( y => y.ConstructionClassification ).Count() > 1 )
        .ToList() ;
      mixtureOfMultipleConstructionClassificationsInDetailSymbol = mixSymbolGroup.Any()
        ? string.Join( ", ", mixSymbolGroup.Select( y => y.Key ).Distinct() )
        : string.Empty ;
      return !string.IsNullOrEmpty( mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ;
    }

    private void UnGroupDetailTableRowsAfterChangeConstructionItems( ObservableCollection<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
    {
      var groupIdOfDetailTableRowsWithConstructionItemHasChanged = detailTableModels
        .Where( d => routeNames.Contains( d.RouteName ) && ! string.IsNullOrEmpty( d.GroupId ) )
        .Select( d => d.GroupId )
        .Distinct() ;
      foreach ( var groupId in groupIdOfDetailTableRowsWithConstructionItemHasChanged ) {
        var detailTableRowsWithSameGroupId = detailTableModels
          .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems != constructionItems )
          .ToList() ;
        var detailTableRowsWithConstructionItemHasChanged = detailTableModels
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
      SaveData( _document, _detailTableModelsOrigin ) ;
      SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
      ShowDetailSymbol() ;
      window.DialogResult = true ;
      window.Close() ;
    }

    private void ShowDetailSymbol()
    {
      if ( ! _isCallFromAddWiringInformationCommand && null == PickInfo )
        return ;

      var detailSymbolStorable = _document.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? _document.GetDetailSymbolStorable() ;
      var conduit = PickInfo!.Element ;
      if ( IsShowSymbol ) {
        var removeDetailSymbols = detailSymbolStorable.DetailSymbolModelData.Where( x => x.ConduitId == conduit.UniqueId && ! string.IsNullOrEmpty( x.DetailSymbolUniqueId ) ).EnumerateAll() ;
        if ( removeDetailSymbols.Any() )
          return ;

        using var transaction = new Transaction( _document ) ;
        transaction.Start( "Create Detail Symbol" ) ;

        var (symbols, angle, defaultSymbol) = CreateDetailSymbolCommandBase.CreateValueForCombobox( detailSymbolStorable.DetailSymbolModelData, conduit ) ;
        var detailSymbolSettingDialog = new DetailSymbolSettingDialog( symbols, angle, defaultSymbol ) ;
        detailSymbolSettingDialog.GetValues() ;
        detailSymbolSettingDialog.DetailSymbol = AddWiringInformationCommandBase.SpecialSymbol ;

        var isParentSymbol = CreateDetailSymbolCommandBase.CheckDetailSymbolOfConduitDifferentCode( _document, conduit, detailSymbolStorable.DetailSymbolModelData, detailSymbolSettingDialog.DetailSymbol ) ;
        var firstPoint = PickInfo.Position ;
        var (textNote, lineIds) = CreateDetailSymbolCommandBase.CreateDetailSymbol( _document, detailSymbolSettingDialog, firstPoint, detailSymbolSettingDialog.Angle, isParentSymbol ) ;

        CreateDetailSymbolCommandBase.SaveDetailSymbol( _document, detailSymbolStorable, conduit, textNote, detailSymbolSettingDialog.DetailSymbol, lineIds, isParentSymbol ) ;

        transaction.Commit() ;
      }
      else {
        var detailSymbolModel = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( x => x.ConduitId == conduit.UniqueId && x.DetailSymbol == AddWiringInformationCommandBase.SpecialSymbol ) ;
        if ( null == detailSymbolModel )
          return ;
        
        using var transaction = new Transaction( _document ) ;
        transaction.Start( "Remove Detail Symbol" ) ;

        var removeDetailSymbols = detailSymbolStorable.DetailSymbolModelData
          .Where( x => CreateDetailTableCommandBase.GetKeyRouting(x) == CreateDetailTableCommandBase.GetKeyRouting( detailSymbolModel ) )
          .EnumerateAll() ;

        foreach ( var removeDetailSymbol in removeDetailSymbols ) {
          detailSymbolStorable.DetailSymbolModelData.Remove( removeDetailSymbol ) ;
        }

        removeDetailSymbols = removeDetailSymbols.DistinctBy( x => x.DetailSymbolUniqueId ).EnumerateAll() ;
        foreach ( var removeDetailSymbol in removeDetailSymbols ) {
          CreateDetailSymbolCommandBase.DeleteDetailSymbol( _document, removeDetailSymbol.DetailSymbolUniqueId, removeDetailSymbol.LineIds ) ;
        }

        if ( removeDetailSymbols.Any() )
          detailSymbolStorable.Save() ;
        
        var detailTableStorable = _document.GetDetailTableStorable();
        foreach ( var detailTableModel in detailTableStorable.DetailTableModelData ) {
          if ( CreateDetailTableCommandBase.GetKeyRouting( detailSymbolModel) == CreateDetailTableCommandBase.GetKeyRouting(detailTableModel) ) {
            detailTableModel.DetailSymbolUniqueId = "" ;
          }
        }
        detailTableStorable.Save();

        transaction.Commit() ;
      }
    }

    private void UpdatePlumbingItemsAfterChangeConstructionItems( ObservableCollection<DetailTableModel> detailTableModels, string routeName, string constructionItems )
    {
      var plumbingIdentityInfos = detailTableModels
        .Where( d => d.RouteName == routeName )
        .Select( d => d.PlumbingIdentityInfo )
        .Distinct() ;
      foreach ( var plumbingIdentityInfo in plumbingIdentityInfos ) {
        var detailTableRowsWithSamePlumbing = detailTableModels
          .Where( d => d.PlumbingIdentityInfo == plumbingIdentityInfo )
          .ToList() ;
        if ( ! detailTableRowsWithSamePlumbing.Any() ) continue ;
        {
          var isParentDetailTableRow = detailTableRowsWithSamePlumbing.FirstOrDefault( d => d.RouteName == routeName && d.IsParentRoute ) != null ;
          var plumbingItems = detailTableRowsWithSamePlumbing
            .Select( d => d.ConstructionItems )
            .Distinct() ;
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

    private static void SetGroupIdForDetailTableRows( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
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

    private static void SetGroupIdForDetailTableRowsMixConstructionItems( IEnumerable<DetailTableModel> detailTableRowsWithSameDetailSymbolId )
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

    private List<DetailTableModel> SortDetailTableModel( IEnumerable<DetailTableModel> detailTableModels, bool isMixConstructionItems )
    {
      List<DetailTableModel> sortedDetailTableModelsList = new() ;
      var detailTableModelsGroupByDetailSymbolId = detailTableModels
        .OrderBy( d => d.DetailSymbol )
        .GroupBy( CreateDetailTableCommandBase.GetKeyRouting )
        .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsGroupByDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var signalTypes = (CreateDetailTableCommandBase.SignalType[]) Enum.GetValues( typeof( CreateDetailTableCommandBase.SignalType )) ;
        foreach ( var signalType in signalTypes ) {
          var detailTableRowsWithSameSignalType = detailTableRowsGroupByDetailSymbolId
            .Where( d => d.SignalType == signalType.GetFieldName() )
            .ToList() ;
          SortDetailTableRows( sortedDetailTableModelsList, detailTableRowsWithSameSignalType, isMixConstructionItems ) ;
        }
        
        var signalTypeNames = signalTypes.Select( s => s.GetFieldName() ) ;
        var detailTableRowsNotHaveSignalType = detailTableRowsGroupByDetailSymbolId
          .Where( d => ! signalTypeNames.Contains( d.SignalType ) )
          .ToList() ;
        SortDetailTableRows( sortedDetailTableModelsList, detailTableRowsNotHaveSignalType, isMixConstructionItems ) ;
      }
      
      return sortedDetailTableModelsList ;
    }
    
    private void SortDetailTableRows( List<DetailTableModel> sortedDetailTableModelsList, List<DetailTableModel> detailTableRowsWithSameSignalType, bool isMixConstructionItems )
    {
      if ( ! isMixConstructionItems ) detailTableRowsWithSameSignalType = detailTableRowsWithSameSignalType
        .OrderBy( d => d.ConstructionItems )
        .ToList() ;
      var detailTableRowsGroupByPlumbingIdentityInfo = detailTableRowsWithSameSignalType
        .GroupBy( d => d.PlumbingIdentityInfo )
        .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSamePlumbingIdentityInfo in detailTableRowsGroupByPlumbingIdentityInfo ) {
        var sortedDetailTableModels = 
            detailTableRowsWithSamePlumbingIdentityInfo
              .OrderByDescending( x => x.IsParentRoute )
              .ThenBy( x => x.GroupId ) ;

        sortedDetailTableModelsList.AddRange( sortedDetailTableModels ) ;
      }
    }
    
    private void SaveData( Document document, IReadOnlyCollection<DetailTableModel> detailTableRowsBySelectedDetailSymbols )
    {
      try {
        var detailTableStorable = document.GetDetailTableStorable() ;
        if ( ! detailTableRowsBySelectedDetailSymbols.Any() )
          return ;
          
        var selectedDetailSymbolIds = Enumerable.ToHashSet( detailTableRowsBySelectedDetailSymbols
            .Select( CreateDetailTableCommandBase.GetKeyRouting )
            .Distinct() ) ;
        
        var detailTableRowsByOtherDetailSymbols = detailTableStorable.DetailTableModelData
          .Where( d => ! selectedDetailSymbolIds.Contains( CreateDetailTableCommandBase.GetKeyRouting(d) ) )
          .ToList() ;
          
        detailTableStorable.DetailTableModelData = detailTableRowsBySelectedDetailSymbols.ToList() ;
        if ( detailTableRowsByOtherDetailSymbols.Any() ) 
          detailTableStorable.DetailTableModelData.AddRange( detailTableRowsByOtherDetailSymbols ) ;
        
        using Transaction t = new( document, "Save data" ) ;
        t.Start() ;
        detailTableStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
    
    private void SaveDetailSymbolData( Document document, DetailSymbolStorable detailSymbolStorable )
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
      foreach ( var selectedItem in _selectedDetailTableRows ) {
        if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
          var selectedItems = _detailTableModelsOrigin
            .Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId )
            .ToList() ;
          deletedDetailTableRows.AddRange( selectedItems ) ;
          foreach ( var item in selectedItems ) {
            var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = _detailTableModelsOrigin
              .Count( d => CreateDetailTableCommandBase.GetKeyRouting( d ) == CreateDetailTableCommandBase.GetKeyRouting( item ) && d.RouteName == item.RouteName && d != item ) ;
            if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
              _detailSymbolStorable.DetailSymbolModelData.RemoveAll(  s => CreateDetailTableCommandBase.GetKeyRouting(s) == CreateDetailTableCommandBase.GetKeyRouting( item ) && s.RouteName == item.RouteName ) ;
            }
          }
        }
        else {
          var countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName = _detailTableModelsOrigin
            .Count( d => CreateDetailTableCommandBase.GetKeyRouting(d) == CreateDetailTableCommandBase.GetKeyRouting(selectedItem) && d.RouteName == selectedItem.RouteName && d != selectedItem ) ;
          if ( countOfDetailTableRowsWithSameDetailSymbolIdAndRouteName == 0 ) {
            var detailSymbolModels = _detailSymbolStorable.DetailSymbolModelData
              .Where( s => CreateDetailTableCommandBase.GetKeyRouting(s) == CreateDetailTableCommandBase.GetKeyRouting(selectedItem) && s.RouteName == selectedItem.RouteName ).ToList() ;
            foreach ( var detailSymbolModel in detailSymbolModels ) {
              _detailSymbolStorable.DetailSymbolModelData.Remove( detailSymbolModel ) ;
            }
          }

          deletedDetailTableRows.Add( selectedItem ) ;
        }
      }
      
      var detailTableRows = _detailTableModelsOrigin.Where( d => ! deletedDetailTableRows.Contains( d ) ) ;
      _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( detailTableRows ) ;
      
      var detailTableRowsSummary = DetailTableModels.Where( d => ! _selectedDetailTableRowsSummary.Contains( d ) ) ;
      DetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRowsSummary ) ;
    }
    
    private void UpdateDataGridAndRemoveSelectedRow()
    {
      ResetSelectedItems() ;
    }
    
    private void ResetSelectedItems()
    {
      _selectedDetailTableRows.Clear() ;
      _selectedDetailTableRowsSummary.Clear() ;
    }

    private void PasteDetailTableRow(DetailTableModel pasteDetailTableRow, DetailTableModel pasteDetailTableRowSummary)
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      var index = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      
      var newDetailTableRow = new DetailTableModel( 
        false, 
        _copyDetailTableRow?.Floor,
        _copyDetailTableRow?.CeedCode, 
        _copyDetailTableRow?.DetailSymbol, 
        _copyDetailTableRow?.DetailSymbolUniqueId, 
        _copyDetailTableRow?.FromConnectorUniqueId, 
        _copyDetailTableRow?.ToConnectorUniqueId, 
        _copyDetailTableRow?.WireType, 
        _copyDetailTableRow?.WireSize,
        _copyDetailTableRow?.WireStrip,
        _copyDetailTableRow?.WireBook, 
        _copyDetailTableRow?.EarthType, 
        _copyDetailTableRow?.EarthSize, 
        _copyDetailTableRow?.NumberOfGrounds, 
        _copyDetailTableRow?.PlumbingType,
        _copyDetailTableRow?.PlumbingSize, 
        _copyDetailTableRow?.NumberOfPlumbing, 
        _copyDetailTableRow?.ConstructionClassification, 
        _copyDetailTableRow?.SignalType, 
        _copyDetailTableRow?.ConstructionItems, 
        _copyDetailTableRow?.PlumbingItems, 
        _copyDetailTableRow?.Remark, 
        _copyDetailTableRow?.WireCrossSectionalArea,
        _copyDetailTableRow?.CountCableSamePosition, 
        _copyDetailTableRow?.RouteName,
        _copyDetailTableRow?.IsEcoMode, 
        _copyDetailTableRow?.IsParentRoute, 
        _copyDetailTableRow?.IsReadOnly, 
        _copyDetailTableRow?.PlumbingIdentityInfo + index, string.Empty, 
        _copyDetailTableRow?.IsReadOnlyPlumbingItems,
        _copyDetailTableRow?.IsMixConstructionItems, 
        index, _copyDetailTableRow?.IsReadOnlyParameters, 
        _copyDetailTableRow?.IsReadOnlyWireSizeAndWireStrip, 
        _copyDetailTableRow?.IsReadOnlyPlumbingSize,
        _copyDetailTableRow?.WireSizes, 
        _copyDetailTableRow?.WireStrips, 
        _copyDetailTableRow?.EarthSizes, 
        _copyDetailTableRow?.PlumbingSizes, 
        _copyDetailTableRow?.PlumbingItemTypes ) ;
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

    private void PlumbingSummary( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, List<DetailTableModel> selectedDetailTableRows, bool isMixConstructionItems, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      _detailTableModelsOrigin = SummarizePlumbing( _detailTableModelsOrigin, conduitsModelData, detailSymbolStorable, selectedDetailTableRows,
        isMixConstructionItems, detailSymbolIdsWithPlumbingTypeHasChanged ) ;
    }
    
    public static ObservableCollection<DetailTableModel> SummarizePlumbing(ObservableCollection<DetailTableModel> detailTableModels, List<ConduitsModel> conduitsModelData, 
      DetailSymbolStorable detailSymbolStorable, List<DetailTableModel> selectedDetailTableRows, bool isMixConstructionItems, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      Dictionary<DetailTableModel, List<DetailTableModel>> sortDetailTableModel = new() ;
      var detailTableModelsGroupByDetailSymbolId = 
        detailTableModels
          .Where(d => !selectedDetailTableRows.Any() || selectedDetailTableRows.Contains(d) )
          .Where( d => ! string.IsNullOrEmpty( d.WireType ) 
                       && ! string.IsNullOrEmpty( d.WireSize ) 
                       && ! string.IsNullOrEmpty( d.WireStrip )
                       && ! string.IsNullOrEmpty( d.WireBook ) 
                       && ! string.IsNullOrEmpty( d.SignalType ) 
                       && ! string.IsNullOrEmpty( d.ConstructionItems ) 
                       && ! string.IsNullOrEmpty( d.Remark ) )
          .GroupBy( CreateDetailTableCommandBase.GetKeyRouting )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        var plumbingIdentityInfos = detailTableRowsWithSameDetailSymbolId.Select( d => d.PlumbingIdentityInfo ).Distinct();
        var otherDetailTableRowsWithSamePlumbingIdentityInfo = detailTableModels
          .Where( d => plumbingIdentityInfos.Contains( d.PlumbingIdentityInfo ) && ! detailTableRowsWithSameDetailSymbolId.Contains( d ) )
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( g => g.ToList() ) ;
        var keyRouting = CreateDetailTableCommandBase.GetKeyRouting(detailTableRowsWithSameDetailSymbolId.First()) ;
        var plumbingType = detailSymbolIdsWithPlumbingTypeHasChanged.SingleOrDefault( d => d.Key == keyRouting ).Value ;
        if ( string.IsNullOrEmpty( plumbingType ) ) {
          if ( selectedDetailTableRows.Any() ) {
            plumbingType = selectedDetailTableRows.FirstOrDefault( s => CreateDetailTableCommandBase.GetKeyRouting(s) == keyRouting && s.PlumbingType != DefaultChildPlumbingSymbol )?.PlumbingType ?? DefaultParentPlumbingType ;
          }
          else {
            plumbingType = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( s => CreateDetailTableCommandBase.GetKeyRouting(s) == keyRouting )?.PlumbingType ?? DefaultParentPlumbingType ;
          }
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
        foreach ( var detailTableRow in detailTableModels ) {
          if ( detailTableRow == parentDetailTableRow ) {
            newDetailTableModels.AddRange( detailTableRows ) ;
          }
          else if ( ! detailTableRows.Contains( detailTableRow ) ) {
            newDetailTableModels.Add( detailTableRow ) ;
          }
        }

        return new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      }

      return new ObservableCollection<DetailTableModel>() ;
    }

    private void AddDetailTableRow(DetailTableModel selectDetailTableRow, DetailTableModel selectDetailTableRowSummary )
    {
      var newDetailTableModels = new List<DetailTableModel>() ;
      
      var newDetailTableRow = new DetailTableModel( selectDetailTableRow.DetailSymbol, selectDetailTableRow.DetailSymbolUniqueId, 
        selectDetailTableRow.FromConnectorUniqueId, selectDetailTableRow.ToConnectorUniqueId, selectDetailTableRow.RouteName ) ;
      
      if ( _isCallFromAddWiringInformationCommand ) {
        newDetailTableRow.PlumbingType = DefaultParentPlumbingType ;
        newDetailTableRow.ConstructionClassification = selectDetailTableRow.ConstructionClassification ;
        newDetailTableRow.SignalType = selectDetailTableRow.SignalType ;
        newDetailTableRow.ConstructionItems = selectDetailTableRow.ConstructionItems ;
        newDetailTableRow.PlumbingItems = selectDetailTableRow.PlumbingItems ;
        newDetailTableRow.Remark = selectDetailTableRow.Remark ;
      }
      
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
    
    private bool MoveDetailTableRow(DetailTableModel selectDetailTableRow, DetailTableModel selectDetailTableRowSummary, bool isMoveUp )
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
      var detailTableModels = _detailTableModelsOrigin
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
        SetPlumbingDataForEachWiring( conduitsModelData, detailSymbolStorable, detailTableRow, detailSymbolIdsWithPlumbingTypeHasChanged ) ;
      }
    }

    private void SetPlumbingDataForEachWiring( List<ConduitsModel> conduitsModelData, DetailSymbolStorable detailSymbolStorable, DetailTableModel detailTableRow, Dictionary<string, string> detailSymbolIdsWithPlumbingTypeHasChanged )
    {
      const double percentage = 0.32 ;
      const int plumbingCount = 1 ;
      var plumbingType = detailSymbolIdsWithPlumbingTypeHasChanged.SingleOrDefault( d => d.Key ==  CreateDetailTableCommandBase.GetKeyRouting(detailTableRow) ).Value ;
      if ( string.IsNullOrEmpty( plumbingType ) ) {
        plumbingType = detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( s => CreateDetailTableCommandBase.GetKeyRouting(s) == CreateDetailTableCommandBase.GetKeyRouting( detailTableRow ) )?.PlumbingType ?? DefaultParentPlumbingType ;
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
        var conduitsModels = conduitsModelData
          .Where( c => c.PipingType == plumbingType )
          .OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) )
          .ToList() ;
        var maxInnerCrossSectionalArea = conduitsModels.Select( c => double.Parse( c.InnerCrossSectionalArea ) ).Max() ;
        var currentPlumbingCrossSectionalArea = detailTableRow.WireCrossSectionalArea / percentage * wireBook ;
        if ( currentPlumbingCrossSectionalArea > maxInnerCrossSectionalArea ) {
          var plumbing = conduitsModels.LastOrDefault() ;
          detailTableRow.PlumbingType = plumbingType ;
          if ( null != plumbing ) {
            detailTableRow.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          }
        }
        else {
          var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= currentPlumbingCrossSectionalArea ) ;
          if ( null != plumbing ) {
            detailTableRow.PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          }
          detailTableRow.PlumbingType = plumbingType ;
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
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableRow.PlumbingItems == plumbingItem.ToString() ) return ;
        var detailTableRowsWithSamePlumbing = _detailTableModelsOrigin.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
        foreach ( var detailTableRowWithSamePlumbing in detailTableRowsWithSamePlumbing ) {
          detailTableRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
        }
      
        var detailTableRowsSummaryWithSamePlumbing = DetailTableModels.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
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
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableRow.ConstructionItems == constructionItem.ToString() ) return ;
        var detailTableRowsChangeConstructionItems = _detailTableModelsOrigin.Where( c => c.RouteName == detailTableRow.RouteName ).ToList() ;
        var detailTableRowsWithSameGroupId = _detailTableModelsOrigin
          .Where( c => 
               ! string.IsNullOrEmpty( c.GroupId ) 
            && c.GroupId == detailTableRow.GroupId 
            && c.RouteName != detailTableRow.RouteName ).ToList() ;
        if ( detailTableRowsWithSameGroupId.Any() ) {
          var routeWithSameGroupId = Enumerable.ToHashSet( detailTableRowsWithSameGroupId.Select( d => d.RouteName ).Distinct() ) ;
          detailTableRowsChangeConstructionItems.AddRange( _detailTableModelsOrigin.Where( c => routeWithSameGroupId.Contains( c.RouteName ) ).ToList() ) ;
        }
      
        foreach ( var detailTableRowChangeConstructionItems in detailTableRowsChangeConstructionItems ) {
          detailTableRowChangeConstructionItems.ConstructionItems = constructionItem.ToString() ;
        }
      
        var routesWithConstructionItemHasChanged = detailTableRowsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
        UpdatePlumbingItemsAfterChangeConstructionItems( _detailTableModelsOrigin, detailTableRow.RouteName, constructionItem.ToString() ) ;
        if ( ! detailTableRow.IsMixConstructionItems ) {
          UnGroupDetailTableRowsAfterChangeConstructionItems( _detailTableModelsOrigin, routesWithConstructionItemHasChanged, constructionItem.ToString() ) ;
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
      
      if ( textBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.Remark, remark, new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void SignalTypeSelection( ComboBox comboBox)
    {
      var selectedSignalType = comboBox.SelectedValue ;
      if ( selectedSignalType == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.SignalType, selectedSignalType.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void ConstructionClassificationSelectionChanged( ComboBox comboBox )
    {
      var selectedConstructionClassification = comboBox.SelectedValue ;
      if ( selectedConstructionClassification == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.ConstructionClassification, selectedConstructionClassification.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void PlumbingSizeSelectionChanged( ComboBox comboBox )
    {
      var selectedPlumbingSize = comboBox.SelectedValue ;
      if ( selectedPlumbingSize == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.PlumbingSize, selectedPlumbingSize.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
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
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.NumberOfGrounds, numberOfGrounds!, new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void NumberOfGroundsSelection( ComboBox comboBox  )
    {
      var selectedNumberOfGrounds = comboBox.SelectedValue ;
      if ( selectedNumberOfGrounds == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.NumberOfGrounds, selectedNumberOfGrounds.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    
    public void EarthSizeSelection( ComboBox comboBox  )
    {
      var selectedEarthSize = comboBox.SelectedValue ;
      if ( selectedEarthSize == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.EarthSize, selectedEarthSize.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
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
        ( from earthSize in earthSizes select new DetailTableModel.ComboboxItemType( earthSize, earthSize ) ).ToList() 
        : new List<DetailTableModel.ComboboxItemType>() ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.EarthType, selectedEarthType.ToString(), earthSizeTypes ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireBookLostFocus( ComboBox comboBox )
    {
      var wireBook = comboBox.Text ;
      if( string.IsNullOrEmpty( wireBook ) ) return ;
      var isNumberValue = int.TryParse( wireBook, out var selectedWireBookInt ) ;
      if ( ! isNumberValue || ( isNumberValue && selectedWireBookInt < 1 ) ) {
        comboBox.Text = string.Empty ;
        return ;
      }
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.WireBook, wireBook!, new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireBookSelection( ComboBox comboBox )
    {
      var selectedWireBook = comboBox.SelectedValue ;
      if( selectedWireBook == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.WireBook, comboBox.SelectedValue.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireStripSelection( ComboBox comboBox )
    {
      var selectedWireStrip = comboBox.SelectedValue ;
      var selectedDetailTableRow = (DetailTableModel) DtGrid.SelectedValue ;
      if (    string.IsNullOrEmpty( selectedDetailTableRow.WireType ) 
           || string.IsNullOrEmpty( selectedDetailTableRow.WireSize )
           || selectedWireStrip == null
           || string.IsNullOrEmpty( selectedWireStrip.ToString() ) ) return ;
      
      var crossSectionalArea = Convert.ToDouble( 
        _wiresAndCablesModelData
          .FirstOrDefault( w => 
            w.WireType == selectedDetailTableRow.WireType 
            && w.DiameterOrNominal == selectedDetailTableRow.WireSize
            && ( w.NumberOfHeartsOrLogarithm + w.COrP == selectedWireStrip.ToString() || ( selectedWireStrip.ToString() == "-" && w.NumberOfHeartsOrLogarithm == "0" ) ) )?.CrossSectionalArea ) ;
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged( editedDetailTableRow, EditedColumn.WireStrip, selectedWireStrip.ToString(), new List<DetailTableModel.ComboboxItemType>(), crossSectionalArea ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    public void WireSizeSelection( ComboBox comboBox )
    {
      var selectedWireSize = comboBox.SelectedValue ;
      var selectedDetailTableRow = (DetailTableModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedDetailTableRow.WireType ) || selectedWireSize == null || string.IsNullOrEmpty( selectedWireSize.ToString() ) ) return ;
      
      var wireStripsOfWireType = _wiresAndCablesModelData
        .Where( w => w.WireType == selectedDetailTableRow.WireType && w.DiameterOrNominal == selectedWireSize.ToString() )
        .Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      var wireStrips = wireStripsOfWireType.Any() ? 
        ( from wireStrip in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() 
        : new List<DetailTableModel.ComboboxItemType>() ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged(editedDetailTableRow, EditedColumn.WireSize, selectedWireSize.ToString(), wireStrips ) ;
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
        ? ( from wireSize in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSize, wireSize ) ).ToList() 
        : new List<DetailTableModel.ComboboxItemType>() ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged(editedDetailTableRow, EditedColumn.WireType, wireType, wireSizes ) ;
      }
      
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    public void PlumingTypeSelection(ComboBox comboBox)
    {
      var plumbingType = comboBox.SelectedValue ;
      if ( plumbingType == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableRow.PlumbingType == plumbingType.ToString() ) return ;
        if ( plumbingType.ToString() == DefaultChildPlumbingSymbol ) {
          comboBox.SelectedValue = detailTableRow.PlumbingType ;
        }
        else {
          List<DetailTableModel> detailTableModels = _detailTableModelsOrigin.Where( c => CreateDetailTableCommandBase.GetKeyRouting(c) == CreateDetailTableCommandBase.GetKeyRouting(detailTableRow) ).ToList() ;
      
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
            SetPlumbingItemsForDetailTableRowsMixConstructionItems( detailTableModels ) ;
          }
          else {
            SetPlumbingItemsForDetailTableRows( detailTableModels ) ;
          }
      
          if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.ContainsKey( CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First() ) ) ) {
            DetailSymbolIdsWithPlumbingTypeHasChanged.Add( CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First() ), plumbingType.ToString() ) ;
          }
          else {
            DetailSymbolIdsWithPlumbingTypeHasChanged[ CreateDetailTableCommandBase.GetKeyRouting(detailTableModels.First() ) ] = plumbingType.ToString() ;
          }

          var sortDetailTableModels =  SortDetailTableModel( _detailTableModelsOrigin, _isMixConstructionItems ) ;
          
          _detailTableModelsOrigin = new ObservableCollection<DetailTableModel>( sortDetailTableModels ) ;
          
          CreateDetailTableViewModelByGroupId() ;
        }
      }
    }
    
    public void FloorSelection( ComboBox comboBox )
    {
      var selectedFloor = comboBox.SelectedValue ;
      if ( selectedFloor == null ) return ;
      
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        ComboboxSelectionChanged(  editedDetailTableRow, EditedColumn.Floor, selectedFloor.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }
    }

    private void ComboboxSelectionChanged(DetailTableModel editedDetailTableRow, EditedColumn editedColumn, string changedValue, List<DetailTableModel.ComboboxItemType> itemSourceCombobox, double crossSectionalArea = 0 )
    {
      if ( ! string.IsNullOrEmpty( editedDetailTableRow.GroupId ) ) {
        var detailTableRows = _detailTableModelsOrigin.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == editedDetailTableRow.GroupId ).ToList() ;
        foreach ( var detailTableRow in detailTableRows ) {
          UpdateDetailTableModelRow( detailTableRow, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
        }
      }
      else {
        var detailTableRow = _detailTableModelsOrigin.FirstOrDefault( d => d == editedDetailTableRow ) ;
        if ( detailTableRow != null ) 
          UpdateDetailTableModelRow( detailTableRow, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;
      }

      var selectedDetailTableRowSummary = DetailTableModels.FirstOrDefault( d => d == editedDetailTableRow ) ;
      if ( selectedDetailTableRowSummary != null )
        UpdateDetailTableModelRow( selectedDetailTableRowSummary, editedColumn, changedValue, crossSectionalArea, itemSourceCombobox ) ;

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void UpdateDetailTableModelRow( DetailTableModel detailTableModelRow, EditedColumn editedColumn, string changedValue, double crossSectionalArea, List<DetailTableModel.ComboboxItemType> itemSourceCombobox )
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
          var mark = detailTableModelRow.Remark.Contains( MultiplicationSymbol ) ? detailTableModelRow.Remark.Split( MultiplicationSymbol )[ 0 ] : detailTableModelRow.Remark ;
          var newRemark = int.TryParse( changedValue, out var value ) && value > 1 ? $"{mark}{MultiplicationSymbol}{value}" : mark ;
          if ( detailTableModelRow.Remark != newRemark ) {
            detailTableModelRow.Remark = newRemark ;
            DetailTableModels = new ObservableCollection<DetailTableModel>( DetailTableModels ) ;
          }
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
      var remarks = oldRemark.Split( MultiplicationSymbol ) ;
      if ( ! remarks.Any() ) 
        return string.Empty ;
      var newRemarks = wireBook > 1 ? remarks.First() + MultiplicationSymbol + wireBook : remarks.First() ;
      return newRemarks ;
    }

    private void DeleteReferenceDetailTableRows(List<DetailTableModel> selectedDetailTableModels )
    {
      var deletedDetailTableRows = new List<DetailTableModel>() ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRowsOfGroup = ReferenceDetailTableModelsOrigin.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          deletedDetailTableRows.AddRange( detailTableRowsOfGroup ) ;
        }
        else {
          deletedDetailTableRows.Add( detailTableRow ) ;
        }
      }
    
      var detailTableRows = ReferenceDetailTableModelsOrigin.Where( d => ! deletedDetailTableRows.Contains( d ) ) ;
      ReferenceDetailTableModelsOrigin = new ObservableCollection<DetailTableModel>( detailTableRows ) ;
    
      var detailTableRowsSummary = ReferenceDetailTableModels.Where( d => ! selectedDetailTableModels.Contains( d ) ) ;
      ReferenceDetailTableModels = new ObservableCollection<DetailTableModel>( detailTableRowsSummary ) ;
    }
    
    private List<DetailTableModel> SelectDetailTableRowsWithSameDetailSymbolId(List<DetailTableModel> selectedDetailTableModels )
    {
      List<DetailTableModel> detailTableRowsWithSameDetailSymbolId = new() ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        var detailTableRows = ReferenceDetailTableModels.Where( d => CreateDetailTableCommandBase.GetKeyRouting(d) == CreateDetailTableCommandBase.GetKeyRouting( detailTableRow ) ) ;
        detailTableRowsWithSameDetailSymbolId.AddRange( detailTableRows ) ;
      }

      return detailTableRowsWithSameDetailSymbolId ;
    }

    private void ReadCtlFile( List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
    {
      MessageBox.Show( @"Please select ctl file.", @"Message" ) ;
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
          AddUnGroupDetailTableRows( ReferenceDetailTableModelsOrigin, detailTableRow ) ; 
        }
        else {
          ReferenceDetailTableModelsOrigin.Add( detailTableRow ) ;
        }
        ReferenceDetailTableModels.Add( detailTableRow ) ;
      }
    }

    private void AddUnGroupDetailTableRows( ObservableCollection<DetailTableModel> unGroupDetailTableModels, DetailTableModel detailTableRow )
    {
      var remarks = detailTableRow.Remark.Split( ',' ) ;
      var isParentDetailRow = ! detailTableRow.IsParentRoute ;
      foreach ( var remark in remarks ) {
        if ( remark.Contains( MultiplicationSymbol ) ) {
          var remarkArr = remark.Split( MultiplicationSymbol ) ;
          var countRows = int.Parse( remarkArr.Last() ) ;
          var remarkRow = remarkArr.Length == 2 ? remarkArr.First().Trim() : remarkArr.First().Trim() + MultiplicationSymbol + remarkArr.ElementAt( 1 ) ;
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
      var newDetailTableRow = new DetailTableModel
      ( 
        detailTableRow.CalculationExclusion, 
        detailTableRow.Floor, 
        detailTableRow.CeedCode, 
        detailTableRow.DetailSymbol, 
        detailTableRow.DetailSymbolUniqueId,
        detailTableRow.FromConnectorUniqueId,
        detailTableRow.ToConnectorUniqueId,
        detailTableRow.WireType, 
        detailTableRow.WireSize, 
        detailTableRow.WireStrip, 
        wireBook, 
        detailTableRow.EarthType, 
        detailTableRow.EarthSize, 
        detailTableRow.NumberOfGrounds, 
        detailTableRow.PlumbingType, 
        detailTableRow.PlumbingSize, 
        detailTableRow.NumberOfPlumbing, 
        detailTableRow.ConstructionClassification, 
        detailTableRow.SignalType, 
        detailTableRow.ConstructionItems, 
        detailTableRow.PlumbingItems, 
        remarkRow, 
        detailTableRow.WireCrossSectionalArea, 
        detailTableRow.CountCableSamePosition, 
        detailTableRow.RouteName, 
        detailTableRow.IsEcoMode, true, 
        false, 
        detailTableRow.PlumbingIdentityInfo, 
        detailTableRow.GroupId, 
        ! detailTableRow.IsMixConstructionItems, 
        detailTableRow.IsMixConstructionItems, 
        string.Empty,
        detailTableRow.IsReadOnlyParameters, 
        detailTableRow.IsReadOnlyWireSizeAndWireStrip, 
        detailTableRow.IsReadOnlyPlumbingSize, 
        detailTableRow.WireSizes, 
        detailTableRow.WireStrips, 
        detailTableRow.EarthSizes, 
        detailTableRow.PlumbingSizes, 
        detailTableRow.PlumbingItemTypes 
      ) ;
      return newDetailTableRow ;
    }

    private DetailTableModel CreateChildDetailTableModel( DetailTableModel detailTableRow, string wireBook, string remarkRow )
    {
      const string defaultChildPlumbingSymbol = "↑" ;
      var newDetailTableRow = new DetailTableModel
      ( detailTableRow.CalculationExclusion, 
        detailTableRow.Floor, 
        detailTableRow.CeedCode, 
        detailTableRow.DetailSymbol, 
        detailTableRow.DetailSymbolUniqueId, 
        detailTableRow.FromConnectorUniqueId, 
        detailTableRow.ToConnectorUniqueId, 
        detailTableRow.WireType, 
        detailTableRow.WireSize, 
        detailTableRow.WireStrip, 
        wireBook, 
        detailTableRow.EarthType, 
        detailTableRow.EarthSize, 
        detailTableRow.NumberOfGrounds, 
        defaultChildPlumbingSymbol, 
        defaultChildPlumbingSymbol, 
        defaultChildPlumbingSymbol, 
        detailTableRow.ConstructionClassification, 
        detailTableRow.SignalType, 
        detailTableRow.ConstructionItems, 
        detailTableRow.PlumbingItems, 
        remarkRow, 
        detailTableRow.WireCrossSectionalArea, 
        detailTableRow.CountCableSamePosition, 
        detailTableRow.RouteName, 
        detailTableRow.IsEcoMode, 
        false, 
        true, 
        detailTableRow.PlumbingIdentityInfo, 
        detailTableRow.GroupId, 
        true, 
        detailTableRow.IsMixConstructionItems, 
        string.Empty,
        detailTableRow.IsReadOnlyParameters, 
        detailTableRow.IsReadOnlyWireSizeAndWireStrip, 
        true, 
        detailTableRow.WireSizes, 
        detailTableRow.WireStrips, 
        detailTableRow.EarthSizes, 
        detailTableRow.PlumbingSizes, 
        detailTableRow.PlumbingItemTypes
      ) ;
      return newDetailTableRow ;
    }

    private void GetValuesForParametersOfDetailTableModels( List<DetailTableModel> detailTableModels, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData )
    {
      var detailTableRowsGroupByDetailSymbolId = detailTableModels.GroupBy( CreateDetailTableCommandBase.GetKeyRouting ).Select( d => d.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableRowsGroupByDetailSymbolId ) {
        var parentDetailTableRow = detailTableRowsWithSameDetailSymbolId.FirstOrDefault( d => d.IsParentRoute ) ;
        var plumbingType = parentDetailTableRow == null ? DefaultParentPlumbingType : parentDetailTableRow.PlumbingType ;
        var plumbingSizesOfPlumbingType = plumbingType == NoPlumping ? new List<string>() { NoPlumbingSize } 
            : conduitsModelData.Where( c => c.PipingType == plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
        var plumbingSizes = ( from plumbingSize in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSize, plumbingSize ) ).ToList() ;
        var detailTableRowsGroupByPlumbingIdentityInfo = detailTableRowsWithSameDetailSymbolId
          .GroupBy( d => d.PlumbingIdentityInfo )
          .Select( d => d.ToList() ) ;
        foreach ( var detailTableRowsWithSamePlumbing in detailTableRowsGroupByPlumbingIdentityInfo ) {
          var constructionItems = detailTableRowsWithSamePlumbing.Select( d => d.ConstructionItems ).Distinct().ToList() ;
          var plumbingItemTypes = constructionItems.Any() ? 
            ( from plumbingItem in constructionItems select new DetailTableModel.ComboboxItemType( plumbingItem, plumbingItem ) ).ToList() 
            : new List<DetailTableModel.ComboboxItemType>() ;
          foreach ( var detailTableRow in detailTableRowsWithSamePlumbing ) {
            var wireSizesOfWireType = wiresAndCablesModelData
              .Where( w => w.WireType == detailTableRow.WireType )
              .Select( w => w.DiameterOrNominal )
              .Distinct()
              .ToList() ;
            var wireSizes = wireSizesOfWireType.Any() ? 
              ( from wireSizeType in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSizeType, wireSizeType ) ).ToList() 
              : new List<DetailTableModel.ComboboxItemType>() ;
              
            var wireStripsOfWireType = wiresAndCablesModelData
              .Where( w => w.WireType == detailTableRow.WireType && w.DiameterOrNominal == detailTableRow.WireSize )
              .Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP )
              .Distinct()
              .ToList() ;
            var wireStrips = wireStripsOfWireType.Any() ? 
              ( from wireStripType in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStripType, wireStripType ) ).ToList() 
              : new List<DetailTableModel.ComboboxItemType>() ;
            
            detailTableRow.WireSizes = wireSizes ;
            detailTableRow.WireStrips = wireStrips ;
            detailTableRow.PlumbingSizes = plumbingSizes ;
            detailTableRow.PlumbingItemTypes = detailTableRow.IsMixConstructionItems ? 
              plumbingItemTypes 
              : new List<DetailTableModel.ComboboxItemType> { new( detailTableRow.ConstructionItems, detailTableRow.ConstructionItems ) } ;
            if ( string.IsNullOrEmpty( detailTableRow.EarthType ) ) continue ;
            var earthSizes = wiresAndCablesModelData
              .Where( c => c.WireType == detailTableRow.EarthType )
              .Select( c => c.DiameterOrNominal )
              .ToList() ;
            detailTableRow.EarthSizes = earthSizes.Any() ? 
              ( from earthSize in earthSizes select new DetailTableModel.ComboboxItemType( earthSize, earthSize ) ).ToList() 
              : new List<DetailTableModel.ComboboxItemType>() ;
          }
        }
      }
    }

    private void AddReferenceDetailTableRows(List<DetailTableModel> selectedDetailTableModels )
    {
      DetailTableModel? detailTableModel = null ;
      if ( DtGrid.SelectedItems.Count == 1 && selectedDetailTableModels.Count > 0)
        detailTableModel = (DetailTableModel) DtGrid.SelectedItems[ 0 ] ;

      SelectionChanged() ;
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) {
        MessageBox.Show( "Please select a row on the detail table.", "Arent Inc" ) ;
        return ;
      }
      
      if ( null == detailTableModel )
        return;

      var indexForSelectedDetailTableRow = _detailTableModelsOrigin.IndexOf( _selectedDetailTableRows.Last() );
      var indexForSelectedDetailTableRowSummary = DetailTableModels.IndexOf( _selectedDetailTableRowsSummary.Last() ) ;
      
      var extendValue = DateTime.Now.ToString( "yyyyMMddHHmmss.fff" ) ;
      foreach ( var detailTableRow in selectedDetailTableModels ) {
        indexForSelectedDetailTableRow++ ;
        indexForSelectedDetailTableRowSummary++ ;
        var groupId = string.IsNullOrEmpty( detailTableRow.GroupId ) ? string.Empty : detailTableRow.GroupId + "-" + extendValue ;
        var referenceDetailTableRow = new DetailTableModel( 
          detailTableRow.CalculationExclusion, 
          detailTableRow.Floor, 
          detailTableRow.CeedCode, 
          _isCallFromAddWiringInformationCommand ? AddWiringInformationCommandBase.SpecialSymbol : detailTableModel.DetailSymbol, 
          detailTableModel.DetailSymbolUniqueId,
          detailTableModel.FromConnectorUniqueId, 
          detailTableModel.ToConnectorUniqueId, 
          detailTableRow.WireType, 
          detailTableRow.WireSize,
          detailTableRow.WireStrip, 
          detailTableRow.WireBook, 
          detailTableRow.EarthType, 
          detailTableRow.EarthSize,
          detailTableRow.NumberOfGrounds,
          detailTableRow.PlumbingType, 
          detailTableRow.PlumbingSize, 
          detailTableRow.NumberOfPlumbing, 
          detailTableRow.ConstructionClassification, 
          detailTableRow.SignalType, 
          detailTableRow.ConstructionItems,
          detailTableRow.PlumbingItems,
          detailTableRow.Remark, 
          detailTableRow.WireCrossSectionalArea, 
          detailTableRow.CountCableSamePosition, 
          detailTableModel.RouteName, 
          detailTableRow.IsEcoMode, 
          detailTableRow.IsParentRoute, 
          detailTableRow.IsReadOnly, 
          detailTableRow.PlumbingIdentityInfo + "-" + extendValue, 
          groupId, 
          detailTableRow.IsReadOnlyPlumbingItems,
          detailTableRow.IsMixConstructionItems, 
          detailTableRow.CopyIndex + extendValue, 
          detailTableRow.IsReadOnlyParameters, 
          detailTableRow.IsReadOnlyWireSizeAndWireStrip,
          detailTableRow.IsReadOnlyPlumbingSize,
          detailTableRow.WireSizes, 
          detailTableRow.WireStrips, 
          detailTableRow.EarthSizes, 
          detailTableRow.PlumbingSizes, 
          detailTableRow.PlumbingItemTypes ) ;
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRowsOfGroup = ReferenceDetailTableModelsOrigin.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          foreach ( var detailTableRowOfGroup in detailTableRowsOfGroup ) {
            var newReferenceDetailTableRow = new DetailTableModel( 
              detailTableRowOfGroup.CalculationExclusion, 
              detailTableRowOfGroup.Floor, 
              detailTableRowOfGroup.CeedCode, 
              _isCallFromAddWiringInformationCommand ? AddWiringInformationCommandBase.SpecialSymbol : detailTableModel.DetailSymbol, 
              detailTableModel.DetailSymbolUniqueId,
              detailTableModel.FromConnectorUniqueId, 
              detailTableModel.ToConnectorUniqueId, 
              detailTableRowOfGroup.WireType, 
              detailTableRowOfGroup.WireSize, 
              detailTableRowOfGroup.WireStrip,
              detailTableRowOfGroup.WireBook,
              detailTableRowOfGroup.EarthType, 
              detailTableRowOfGroup.EarthSize, 
              detailTableRowOfGroup.NumberOfGrounds,
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
              detailTableModel.RouteName, 
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
            _detailTableModelsOrigin.Insert( indexForSelectedDetailTableRow, newReferenceDetailTableRow ) ;
          }
        }
        else {
          _detailTableModelsOrigin.Insert( indexForSelectedDetailTableRow, referenceDetailTableRow ) ;
        }

        DetailTableModels.Insert( indexForSelectedDetailTableRowSummary, referenceDetailTableRow ) ;
      }
    }

    private List<DetailTableModel> GroupDetailTableModels( ObservableCollection<DetailTableModel> oldDetailTableModels )
    {
      List<DetailTableModel> newDetailTableModels = new() ;
      List<string> existedGroupIds = new() ;
      foreach ( var detailTableRow in oldDetailTableModels ) {
        if ( string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          newDetailTableModels.Add( detailTableRow ) ;
        }
        else {
          if ( existedGroupIds.Contains( detailTableRow.GroupId ) ) 
            continue ;
          
          var detailTableRowWithSameWiringType = oldDetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
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
              if ( ! string.IsNullOrEmpty( detailTableRowWithSameRemark.NumberOfGrounds ) ) {
                numberOfGrounds += int.Parse( detailTableRowWithSameRemark.NumberOfGrounds ) ;
              }
            }
          }

          var newDetailTableRow = new DetailTableModel( 
            detailTableRow.CalculationExclusion, 
            detailTableRow.Floor, 
            detailTableRow.CeedCode, 
            detailTableRow.DetailSymbol, 
            detailTableRow.DetailSymbolUniqueId,
            detailTableRow.FromConnectorUniqueId,
            detailTableRow.ToConnectorUniqueId,
            detailTableRow.WireType, 
            detailTableRow.WireSize, 
            detailTableRow.WireStrip, 
            wireBook > 0 ? wireBook.ToString() : string.Empty, 
            detailTableRow.EarthType, 
            detailTableRow.EarthSize, 
            numberOfGrounds > 0 ? numberOfGrounds.ToString() : string.Empty, 
            detailTableRow.PlumbingType, 
            detailTableRow.PlumbingSize, 
            detailTableRow.NumberOfPlumbing, 
            detailTableRow.ConstructionClassification, 
            detailTableRow.SignalType, 
            detailTableRow.ConstructionItems, 
            detailTableRow.PlumbingItems, 
            string.Join( ", ", newRemark ), 
            detailTableRow.WireCrossSectionalArea, 
            detailTableRow.CountCableSamePosition, 
            detailTableRow.RouteName, 
            detailTableRow.IsEcoMode, 
            detailTableRow.IsParentRoute, 
            detailTableRow.IsReadOnly, 
            detailTableRow.PlumbingIdentityInfo, 
            detailTableRow.GroupId, 
            detailTableRow.IsReadOnlyPlumbingItems, 
            detailTableRow.IsMixConstructionItems,
            detailTableRow.CopyIndex, 
            detailTableRow.IsReadOnlyParameters, 
            detailTableRow.IsReadOnlyWireSizeAndWireStrip, 
            detailTableRow.IsReadOnlyPlumbingSize, 
            detailTableRow.WireSizes, 
            detailTableRow.WireStrips,
            detailTableRow.EarthSizes, 
            detailTableRow.PlumbingSizes, 
            detailTableRow.PlumbingItemTypes ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          existedGroupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      return newDetailTableModels ;
    }
  }

  public class DetailTableData
  {
    
    #region Fields
    private static readonly object Padlock = new();
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
        
        lock (Padlock) {
          _instance ??= new DetailTableData() ;
        }
        
        return _instance;
      }
    }

    public bool FirstLoaded { get ; set ; }

  }
}