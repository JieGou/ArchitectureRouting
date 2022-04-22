using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DetailTableDialog : Window
  {
    private const string DefaultChildPlumbingSymbol = "↑" ;
    private const string NoPlumping = "配管なし" ;
    private const string IncorrectDataErrorMessage = "Incorrect data." ;
    private const string CaptionErrorMessage = "Error" ;
    private readonly Document _document ;
    private readonly List<ConduitsModel> _conduitsModelData ;
    private readonly List<WiresAndCablesModel> _wiresAndCablesModelData ;
    private readonly DetailTableViewModel _detailTableViewModel ;
    private readonly DetailSymbolStorable _detailSymbolStorable ;
    private List<DetailTableModel> _selectedDetailTableRows ;
    private List<DetailTableModel> _selectedDetailTableRowsSummary ;
    private DetailTableModel? _copyDetailTableRow ;
    private DetailTableModel? _copyDetailTableRowSummary ;
    public DetailTableViewModel DetailTableViewModelSummary { get ; set ; }
    public Dictionary<string, string> RoutesWithConstructionItemHasChanged { get ; }
    public Dictionary<string, string> DetailSymbolIdsWithPlumbingTypeHasChanged { get ; }
    private bool _isMixConstructionItems ;
    
    private static string MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage =
      "Construction categories are mixed in the detail symbol {0}. Would you like to proceed to create the detail table?" ;

    public DetailTableDialog( Document document, DetailTableViewModel viewModel, List<ConduitsModel> conduitsModelData, List<WiresAndCablesModel> wiresAndCablesModelData, bool mixConstructionItems )
    {
      InitializeComponent() ;
      _document = document ;
      DataContext = viewModel ;
      _detailTableViewModel = viewModel ;
      _detailSymbolStorable = document.GetDetailSymbolStorable() ;
      DetailTableViewModelSummary = viewModel ;
      _conduitsModelData = conduitsModelData ;
      _wiresAndCablesModelData = wiresAndCablesModelData ;
      _isMixConstructionItems = mixConstructionItems ;
      RoutesWithConstructionItemHasChanged = new Dictionary<string, string>() ;
      DetailSymbolIdsWithPlumbingTypeHasChanged = new Dictionary<string, string>() ;
      _selectedDetailTableRows = new List<DetailTableModel>() ;
      _selectedDetailTableRowsSummary = new List<DetailTableModel>() ;
      _copyDetailTableRow = null ;
      _copyDetailTableRowSummary = null ;

      CreateDetailTableViewModelByGroupId() ;
      
      var rowStyle = new Style( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
    }
    
    private void Row_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (DetailTableModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedItem.GroupId ) ) return ;
      UnGroupDetailTableRows( selectedItem.GroupId ) ;
      CreateDetailTableViewModelByGroupId() ;
    }

    private void DtGrid_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not DataGrid dataGrid ) return ;
      var selectedItems = dataGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      ResetSelectedItems() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableModel detailTableRow ) continue ;
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRows = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == detailTableRow.GroupId ).ToList() ;
          _selectedDetailTableRows.AddRange( detailTableRows ) ;
        }
        else {
          _selectedDetailTableRows.Add( detailTableRow ) ;
        }
        _selectedDetailTableRowsSummary.Add( detailTableRow ) ;
      }
    }

    private void BtnDeleteLine_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      DetailTableViewModel.DeleteDetailTableRows( _detailTableViewModel, _selectedDetailTableRows, DetailTableViewModelSummary, _selectedDetailTableRowsSummary, _detailSymbolStorable ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void BtnCopyLine_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      _copyDetailTableRow = _selectedDetailTableRows.First() ;
      _copyDetailTableRowSummary = _selectedDetailTableRowsSummary.First() ;
      ResetSelectedItems() ;
    }

    private void BtnPasteLine_Click( object sender, RoutedEventArgs e )
    {
      if ( _copyDetailTableRow == null || _copyDetailTableRowSummary == null ) {
        MessageBox.Show( "Please choose a row to copy", "Message" ) ;
        return ;
      }

      var pasteDetailTableRow = ! _selectedDetailTableRows.Any() ? _copyDetailTableRow : _selectedDetailTableRows.First() ;
      var pasteDetailTableRowSummary = ! _selectedDetailTableRowsSummary.Any() ? _copyDetailTableRowSummary : _selectedDetailTableRowsSummary.First() ;
      DetailTableViewModel.PasteDetailTableRow( _detailTableViewModel, _copyDetailTableRow, pasteDetailTableRow, DetailTableViewModelSummary, pasteDetailTableRowSummary ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void BtnSelectAll_Click( object sender, RoutedEventArgs e )
    {
      _selectedDetailTableRows = _detailTableViewModel.DetailTableModels.ToList() ;
      _selectedDetailTableRowsSummary = DetailTableViewModelSummary.DetailTableModels.ToList() ;
      DtGrid.SelectAll() ;
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      DetailTableViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
      DetailTableViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
      DialogResult = true ;
      Close() ;
    }
    
    private void BtnCreateDetailTable_OnClick( object sender, RoutedEventArgs e )
    {
      var confirmResult = MessageBoxResult.OK ;
      var mixtureOfMultipleConstructionClassificationsInDetailSymbol = string.Empty ;
      if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableViewModel.DetailTableModels, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) )
        confirmResult = MessageBox.Show( string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), 
            mixtureOfMultipleConstructionClassificationsInDetailSymbol ), "Warning", MessageBoxButton.OKCancel ) ;
      if ( confirmResult == MessageBoxResult.OK ) {
        DetailTableViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
        DetailTableViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
        DialogResult = true ;
        Close() ;
      }

      if ( DataContext is DetailTableViewModel context ) {
        context.IsCancelCreateDetailTable = confirmResult == MessageBoxResult.Cancel ;
      }
    }

    private void BtnCompleted_OnClick( object sender, RoutedEventArgs e )
    {
      DetailTableViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
      DetailTableViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
      DialogResult = true ;
      Close() ;
    }

    private void UpdateDataGridAndRemoveSelectedRow()
    {
      DataContext = DetailTableViewModelSummary ;
      DtGrid.ItemsSource = DetailTableViewModelSummary.DetailTableModels ;
      ResetSelectedItems() ;
    }

    private void ResetSelectedItems()
    {
      _selectedDetailTableRows.Clear() ;
      _selectedDetailTableRowsSummary.Clear() ;
    }

    private void BtnAdd_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      var selectedDetailTableRow = _selectedDetailTableRows.Last() ;
      var selectedDetailTableRowSummary = _selectedDetailTableRowsSummary.Last() ;
      DetailTableViewModel.AddDetailTableRow( _detailTableViewModel, selectedDetailTableRow, DetailTableViewModelSummary, selectedDetailTableRowSummary ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void BtnMoveUp_Click( object sender, RoutedEventArgs e )
    {
      MoveDetailTableRow( true ) ;
    }
    
    private void BtnMoveDown_Click( object sender, RoutedEventArgs e )
    {
      MoveDetailTableRow( false ) ;
    }

    private void MoveDetailTableRow( bool isMoveUp )
    {
      if ( ! _selectedDetailTableRows.Any() || ! _selectedDetailTableRowsSummary.Any() ) return ;
      var selectedDetailTableRow = _selectedDetailTableRows.First() ;
      var selectedDetailTableRowSummary = _selectedDetailTableRowsSummary.First() ;
      DetailTableViewModel.MoveDetailTableRow( _detailTableViewModel, selectedDetailTableRow, DetailTableViewModelSummary, selectedDetailTableRowSummary, isMoveUp ) ;
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void PlumpingTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
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
          List<DetailTableModel> detailTableModels = _detailTableViewModel.DetailTableModels.Where( c => c.DetailSymbolId == detailTableRow.DetailSymbolId ).ToList() ;

          if ( plumbingType.ToString() == NoPlumping ) {
            CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( ref detailTableModels, _isMixConstructionItems ) ;
          }
          else {
            CreateDetailTableCommandBase.SetPlumbingData( _conduitsModelData, ref detailTableModels, plumbingType.ToString(), _isMixConstructionItems ) ;
            DetailTableViewModel.SetPlumbingSizesForDetailTableRows( _conduitsModelData, detailTableModels, plumbingType.ToString() ) ;
          }

          var detailTableRowsHaveGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) ).ToList() ;
          if ( detailTableRowsHaveGroupId.Any() ) {
            if ( _isMixConstructionItems ) {
              DetailTableViewModel.SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsHaveGroupId ) ;
            }
            else {
              DetailTableViewModel.SetGroupIdForDetailTableRows( detailTableRowsHaveGroupId ) ;
            }
          }

          if ( _isMixConstructionItems ) {
            DetailTableViewModel.SetPlumbingItemsForDetailTableRowsMixConstructionItems( detailTableModels ) ;
          }
          else {
            DetailTableViewModel.SetPlumbingItemsForDetailTableRows( detailTableModels ) ;
          }

          if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.ContainsKey( detailTableModels.First().DetailSymbolId ) ) {
            DetailSymbolIdsWithPlumbingTypeHasChanged.Add( detailTableModels.First().DetailSymbolId, plumbingType!.ToString() ) ;
          }
          else {
            DetailSymbolIdsWithPlumbingTypeHasChanged[ detailTableModels.First().DetailSymbolId ] = plumbingType!.ToString() ;
          }

          var newDetailTableModelList = _detailTableViewModel.DetailTableModels.ToList() ;
          DetailTableViewModel.SortDetailTableModel( ref newDetailTableModelList, _isMixConstructionItems ) ;
          _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModelList ) ;
          CreateDetailTableViewModelByGroupId() ;
        }
      }
    }

    private void FloorSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedFloor = comboBox.SelectedValue ;
      if ( selectedFloor == null ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.Floor, selectedFloor.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }
      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void WireTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var wireType = comboBox.SelectedValue == null ? string.Empty : comboBox.SelectedValue.ToString() ;
      if ( string.IsNullOrEmpty( wireType ) ) return ;

      var wireSizesOfWireType = _wiresAndCablesModelData.Where( w => w.WireType == wireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
      var wireSizes = wireSizesOfWireType.Any() ? ( from wireSize in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSize, wireSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireType, wireType, wireSizes ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void WireSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedWireSize = comboBox.SelectedValue ;
      var selectedDetailTableRow = (DetailTableModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedDetailTableRow.WireType ) || selectedWireSize == null || string.IsNullOrEmpty( selectedWireSize.ToString() ) ) return ;

      var wireStripsOfWireType = _wiresAndCablesModelData.Where( w => w.WireType == selectedDetailTableRow.WireType && w.DiameterOrNominal == selectedWireSize.ToString() ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      var wireStrips = wireStripsOfWireType.Any() ? ( from wireStrip in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireSize, selectedWireSize.ToString(), wireStrips ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void WireStripSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedWireStrip = comboBox.SelectedValue ;
      var selectedDetailTableRow = (DetailTableModel) DtGrid.SelectedValue ;
      if ( string.IsNullOrEmpty( selectedDetailTableRow.WireType ) || string.IsNullOrEmpty( selectedDetailTableRow.WireSize )|| selectedWireStrip == null || string.IsNullOrEmpty( selectedWireStrip.ToString() ) ) return ;
      
      var crossSectionalArea = Convert.ToDouble( _wiresAndCablesModelData.FirstOrDefault( w => w.WireType == selectedDetailTableRow.WireType && w.DiameterOrNominal == selectedDetailTableRow.WireSize && w.NumberOfHeartsOrLogarithm + w.COrP == selectedDetailTableRow.WireStrip )?.CrossSectionalArea ) ;
      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireStrip, selectedWireStrip.ToString(), new List<DetailTableModel.ComboboxItemType>(), crossSectionalArea ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void WireBookSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var isNumberValue = int.TryParse( comboBox.SelectedValue.ToString(), out var selectedWireBookText ) ;
      if ( ! isNumberValue || ( isNumberValue && selectedWireBookText is < 1 or > 10 ) ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireBook, comboBox.SelectedValue.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void EarthTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedEarthType = comboBox.SelectedValue ;
      if ( selectedEarthType == null ) return ;

      var earthSizes = _wiresAndCablesModelData.Where( c => c.WireType == selectedEarthType.ToString() ).Select( c => c.DiameterOrNominal ).ToList() ;
      var earthSizeTypes = earthSizes.Any() ? ( from earthSize in earthSizes select new DetailTableModel.ComboboxItemType( earthSize, earthSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.EarthType, selectedEarthType.ToString(), earthSizeTypes ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void EarthSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedEarthSize = comboBox.SelectedValue ;
      if ( selectedEarthSize == null ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.EarthSize, selectedEarthSize.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void NumberOfGroundsSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedNumberOfGrounds = comboBox.SelectedValue ;
      if ( selectedNumberOfGrounds == null ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.NumberOfGrounds, selectedNumberOfGrounds.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void PlumbingSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedPlumbingSize = comboBox.SelectedValue ;
      if ( selectedPlumbingSize == null ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.PlumbingSize, selectedPlumbingSize.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void ConstructionClassificationSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedConstructionClassification = comboBox.SelectedValue ;
      if ( selectedConstructionClassification == null ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.ConstructionClassification, selectedConstructionClassification.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void SignalTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var selectedSignalType = comboBox.SelectedValue ;
      if ( selectedSignalType == null ) return ;

      if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.SignalType, selectedSignalType.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void RemarkChanged( object sender, KeyboardFocusChangedEventArgs e )
    {
      if ( sender is not TextBox textBox ) return ;
      var remark = textBox.Text ;

      if ( textBox.DataContext is DetailTableModel editedDetailTableRow ) {
        DetailTableViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.Remark, remark, new List<DetailTableModel.ComboboxItemType>() ) ;
      }

      UpdateDataGridAndRemoveSelectedRow() ;
    }

    private void ConstructionItemSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var constructionItem = comboBox.SelectedValue ;
      if ( constructionItem == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableRow.ConstructionItems == constructionItem.ToString() ) return ;
        var detailTableRowsChangeConstructionItems = _detailTableViewModel.DetailTableModels.Where( c => c.RouteName == detailTableRow.RouteName ).ToList() ;
        var detailTableRowsWithSameGroupId = _detailTableViewModel.DetailTableModels.Where( c => ! string.IsNullOrEmpty( c.GroupId ) && c.GroupId == detailTableRow.GroupId && c.RouteName != detailTableRow.RouteName ).ToList() ;
        if ( detailTableRowsWithSameGroupId.Any() ) {
          var routeWithSameGroupId = detailTableRowsWithSameGroupId.Select( d => d.RouteName ).Distinct().ToHashSet() ;
          detailTableRowsChangeConstructionItems.AddRange( _detailTableViewModel.DetailTableModels.Where( c => routeWithSameGroupId.Contains( c.RouteName ) ).ToList() ) ;
        }

        foreach ( var detailTableRowChangeConstructionItems in detailTableRowsChangeConstructionItems ) {
          detailTableRowChangeConstructionItems.ConstructionItems = constructionItem.ToString() ;
        }

        var routesWithConstructionItemHasChanged = detailTableRowsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
        DetailTableViewModel.UpdatePlumbingItemsAfterChangeConstructionItems( _detailTableViewModel.DetailTableModels, detailTableRow.RouteName, constructionItem.ToString() ) ;
        if ( ! detailTableRow.IsMixConstructionItems ) {
          #region Update Plumbing Type (Comment out)
          // var detailTableRowsWithSameRouteName = newDetailTableModels.Where( c => c.RouteName == detailTableRow.RouteName ).ToList() ;
          // foreach ( var detailTableRowWithSameRouteName in detailTableRowsWithSameRouteName ) {
          //   var detailTableRowsWithSameDetailSymbolId = newDetailTableModels.Where( c => c.DetailSymbolId == detailTableRowWithSameRouteName.DetailSymbolId ).ToList() ;
          //   CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( _conduitsModelData, detailTableRowsWithSameDetailSymbolId, detailTableRow.PlumbingType, false, _isMixConstructionItems ) ;
          // }
          #endregion
          DetailTableViewModel.UnGroupDetailTableRowsAfterChangeConstructionItems( _detailTableViewModel.DetailTableModels, routesWithConstructionItemHasChanged, constructionItem.ToString() ) ;
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

    private void PlumbingItemsSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var plumbingItem = comboBox.SelectedValue ;
      if ( plumbingItem == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
        MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      }
      else {
        if ( detailTableRow.PlumbingItems == plumbingItem.ToString() ) return ;
        var detailTableRowsWithSamePlumbing = _detailTableViewModel.DetailTableModels.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
        foreach ( var detailTableRowWithSamePlumbing in detailTableRowsWithSamePlumbing ) {
          detailTableRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
        }

        var detailTableRowsSummaryWithSamePlumbing = DetailTableViewModelSummary.DetailTableModels.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
        foreach ( var detailTableRowWithSamePlumbing in detailTableRowsSummaryWithSamePlumbing ) {
          detailTableRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
        }
        
        UpdateDataGridAndRemoveSelectedRow() ;
      }
    }

    private void BtnPlumbingSummary_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() ) return ;
      _isMixConstructionItems = false ;
      PlumbingSummary() ;
    }

    private void BtnPlumbingSummaryMixConstructionItems_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() ) return ;
      _isMixConstructionItems = true ;
      PlumbingSummary() ;
    }

    private void PlumbingSummary()
    {
      DetailTableViewModel.PlumbingSummary( _conduitsModelData, _detailTableViewModel, _selectedDetailTableRows, _isMixConstructionItems ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
    }
    
    private void BtnSplitPlumbing_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() ) return ;
      DetailTableViewModel.SplitPlumbing( _conduitsModelData, _detailTableViewModel, _selectedDetailTableRows ) ;
      CreateDetailTableViewModelByGroupId() ;
      ResetSelectedItems() ;
      DtGrid.SelectedItems.Clear() ;
    }

    private void CreateDetailTableViewModelByGroupId()
    {
      List<DetailTableModel> newDetailTableModels = new() ;
      List<string> existedGroupIds = new() ;
      foreach ( var detailTableRow in _detailTableViewModel.DetailTableModels ) {
        if ( string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          newDetailTableModels.Add( detailTableRow ) ;
        }
        else {
          if ( existedGroupIds.Contains( detailTableRow.GroupId ) ) continue ;
          var detailTableRowWithSameWiringType = _detailTableViewModel.DetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          var detailTableRowsGroupByRemark = detailTableRowWithSameWiringType.GroupBy( d => d.Remark ).ToDictionary( g => g.Key, g => g.ToList() ) ;
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
            detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, detailTableRow.IsParentRoute, 
            detailTableRow.IsReadOnly, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, detailTableRow.IsReadOnlyPlumbingItems, detailTableRow.IsMixConstructionItems,
            detailTableRow.CopyIndex, detailTableRow.IsReadOnlyParameters, detailTableRow.IsReadOnlyWireSizeAndWireStrip, detailTableRow.IsReadOnlyPlumbingSize, detailTableRow.WireSizes, detailTableRow.WireStrips,
            detailTableRow.EarthSizes, detailTableRow.PlumbingSizes, detailTableRow.PlumbingItemTypes ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          existedGroupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      DetailTableViewModel newDetailTableViewModel = new( new ObservableCollection<DetailTableModel>( newDetailTableModels ), _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems, 
        _detailTableViewModel.Levels, _detailTableViewModel.WireTypes, _detailTableViewModel.EarthTypes, _detailTableViewModel.Numbers, _detailTableViewModel.ConstructionClassificationTypes, _detailTableViewModel.SignalTypes ) ;
      DataContext = newDetailTableViewModel ;
      DtGrid.ItemsSource = newDetailTableViewModel.DetailTableModels ;
      DetailTableViewModelSummary = newDetailTableViewModel ;
    }
    
    private void UnGroupDetailTableRows( string groupId )
    {
      var detailTableModels = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId ).ToList() ;
      foreach ( var detailTableRow in detailTableModels ) {
        detailTableRow.GroupId = string.Empty ;
      }
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
  }
}