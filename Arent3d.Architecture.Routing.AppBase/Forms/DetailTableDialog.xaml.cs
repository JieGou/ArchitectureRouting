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
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DetailTableDialog : Window
  {
    private DetailTableViewModel2 ViewModel => (DetailTableViewModel2)DataContext ;
    private const string DefaultChildPlumbingSymbol = "↑" ;
    private const string NoPlumping = "配管なし" ;
    private const string IncorrectDataErrorMessage = "Incorrect data." ;
    private const string CaptionErrorMessage = "Error" ;


    // private static string MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage =
    //   "Construction categories are mixed in the detail symbol {0}. Would you like to proceed to create the detail table?" ;

    public DetailTableDialog(DetailTableViewModel2 viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      HideReferenceDataGrid( ! viewModel.IsAddReference ) ;
      viewModel.IsAddReference = false ;
      ViewModel.DtGrid = DtGrid ;
      ViewModel.DtReferenceGrid = DtReferenceGrid ;
    }
    
    // dt grid below
    
    private void BtnHideReferenceDataGrid_Click( object sender, RoutedEventArgs e )
    {
      if ( BtnReferenceSelectAll.Visibility == Visibility.Visible ) {
        HideReferenceDataGrid( true ) ;
      }
      else if ( BtnReferenceSelectAll.Visibility == Visibility.Hidden ) {
        HideReferenceDataGrid( false ) ;
      }
    }

    private void HideReferenceDataGrid( bool isVisible )
    {
      BtnReferenceSelectAll.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnDeleteReferenceLine.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnReadCtlFile.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnSelectDetailTableRowWithSameDetailSymbolId.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnAddReference.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnAddReferenceRows.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      DtReferenceGrid.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      DtReferenceGrid.Height = isVisible ? 0 : 370 ;
      DetailTableWindow.Height = isVisible ? 590 : 980 ;
      if ( isVisible ) return ;
      DetailTableWindow.WindowStartupLocation = WindowStartupLocation.Manual ;
      DetailTableWindow.Top = 50 ;
      DetailTableWindow.Left = 400 ;
    }
    
    
    private void BtnDeleteReferenceLine_Click( object sender, RoutedEventArgs e )
    {
      // if ( ! _selectedReferenceDetailTableRows.Any() ) return ;
      // ViewModel.DeleteReferenceDetailTableRows( _detailTableViewModel, DetailTableViewModelSummary, _selectedReferenceDetailTableRows ) ;
      // UpdateReferenceDetailTableModels() ;
    }
    
    
    private void BtnAddReference_Click( object sender, RoutedEventArgs e )
    {
      // DialogResult = false ;
      // _detailTableViewModel.IsAddReference = true ;
      // DetailTableViewModelSummary.IsAddReference = true ;
      // Close() ;
    }
    
    private void BtnAddReferenceRows_Click( object sender, RoutedEventArgs e )
    {
      // if ( ! _selectedReferenceDetailTableRows.Any() ) return ;
      // ViewModel.AddReferenceDetailTableRows( _detailTableViewModel, DetailTableViewModelSummary, _selectedReferenceDetailTableRows ) ;
      // DataContext = DetailTableViewModelSummary ;
      // DtGrid.ItemsSource = DetailTableViewModelSummary.DetailTableModels ;
    }
    
    private void UpdateReferenceDetailTableModels()
    {
      // DataContext = DetailTableViewModelSummary ;
      // DtReferenceGrid.ItemsSource = DetailTableViewModelSummary.ReferenceDetailTableModels ;
      // _selectedReferenceDetailTableRows.Clear() ;
      // DtReferenceGrid.SelectedItems.Clear() ;
    }
    
    private void BtnCreateDetailTable_OnClick( object sender, RoutedEventArgs e )
    {
      // var confirmResult = MessageBoxResult.OK ;
      // var mixtureOfMultipleConstructionClassificationsInDetailSymbol = string.Empty ;
      // if ( IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol( _detailTableViewModel.DetailTableModels, ref mixtureOfMultipleConstructionClassificationsInDetailSymbol ) )
      //   confirmResult = MessageBox.Show( string.Format( "Dialog.Electrical.MultipleConstructionCategoriesAreMixedWithSameDetailSymbol.Warning".GetAppStringByKeyOrDefault( MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage ), 
      //       mixtureOfMultipleConstructionClassificationsInDetailSymbol ), "Warning", MessageBoxButton.OKCancel ) ;
      // if ( confirmResult == MessageBoxResult.OK ) {
      //   ViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
      //   ViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
      //   DialogResult = true ;
      //   Close() ;
      // }
      //
      // if ( DataContext is DetailTableViewModel context ) {
      //   context.IsCancelCreateDetailTable = confirmResult == MessageBoxResult.Cancel ;
      // }
    }
    
    // private void BtnCompleted_OnClick( object sender, RoutedEventArgs e )
    // {
    //   // ViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
    //   // ViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
    //   // DialogResult = true ;
    //   // Close() ;
    // }

    private void PlumpingTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var plumbingType = comboBox.SelectedValue ;
      // if ( plumbingType == null ) return ;
      // if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
      //   MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      // }
      // else {
      //   if ( detailTableRow.PlumbingType == plumbingType.ToString() ) return ;
      //   if ( plumbingType.ToString() == DefaultChildPlumbingSymbol ) {
      //     comboBox.SelectedValue = detailTableRow.PlumbingType ;
      //   }
      //   else {
      //     List<DetailTableModel> detailTableModels = _detailTableViewModel.DetailTableModels.Where( c => c.DetailSymbolId == detailTableRow.DetailSymbolId ).ToList() ;
      //
      //     if ( plumbingType.ToString() == NoPlumping ) {
      //       CreateDetailTableCommandBase.SetNoPlumbingDataForOneSymbol( detailTableModels, _isMixConstructionItems ) ;
      //     }
      //     else {
      //       CreateDetailTableCommandBase.SetPlumbingData( _conduitsModelData, ref detailTableModels, plumbingType.ToString(), _isMixConstructionItems ) ;
      //     }
      //
      //     var detailTableRowsHaveGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) ).ToList() ;
      //     if ( detailTableRowsHaveGroupId.Any() ) {
      //       if ( _isMixConstructionItems ) {
      //         ViewModel.SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsHaveGroupId ) ;
      //       }
      //       else {
      //         ViewModel.SetGroupIdForDetailTableRows( detailTableRowsHaveGroupId ) ;
      //       }
      //     }
      //
      //     if ( _isMixConstructionItems ) {
      //       ViewModel.SetPlumbingItemsForDetailTableRowsMixConstructionItems( detailTableModels ) ;
      //     }
      //     else {
      //       ViewModel.SetPlumbingItemsForDetailTableRows( detailTableModels ) ;
      //     }
      //
      //     if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.ContainsKey( detailTableModels.First().DetailSymbolId ) ) {
      //       DetailSymbolIdsWithPlumbingTypeHasChanged.Add( detailTableModels.First().DetailSymbolId, plumbingType!.ToString() ) ;
      //     }
      //     else {
      //       DetailSymbolIdsWithPlumbingTypeHasChanged[ detailTableModels.First().DetailSymbolId ] = plumbingType!.ToString() ;
      //     }
      //
      //     var newDetailTableModelList = _detailTableViewModel.DetailTableModels.ToList() ;
      //     ViewModel.SortDetailTableModel( ref newDetailTableModelList, _isMixConstructionItems ) ;
      //     _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModelList ) ;
      //     CreateDetailTableViewModelByGroupId() ;
      //   }
      // }
    }
    
    private void FloorSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedFloor = comboBox.SelectedValue ;
      // if ( selectedFloor == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.Floor, selectedFloor.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
    //  UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void WireTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var wireType = comboBox.SelectedValue == null ? string.Empty : comboBox.SelectedValue.ToString() ;
      // if ( string.IsNullOrEmpty( wireType ) ) return ;
      //
      // var wireSizesOfWireType = _wiresAndCablesModelData.Where( w => w.WireType == wireType ).Select( w => w.DiameterOrNominal ).Distinct().ToList() ;
      // var wireSizes = wireSizesOfWireType.Any() ? ( from wireSize in wireSizesOfWireType select new DetailTableModel.ComboboxItemType( wireSize, wireSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireType, wireType, wireSizes ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void WireSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedWireSize = comboBox.SelectedValue ;
      // var selectedDetailTableRow = (DetailTableModel) DtGrid.SelectedValue ;
      // if ( string.IsNullOrEmpty( selectedDetailTableRow.WireType ) || selectedWireSize == null || string.IsNullOrEmpty( selectedWireSize.ToString() ) ) return ;
      //
      // var wireStripsOfWireType = _wiresAndCablesModelData.Where( w => w.WireType == selectedDetailTableRow.WireType && w.DiameterOrNominal == selectedWireSize.ToString() ).Select( w => w.NumberOfHeartsOrLogarithm == "0" ? "-" : w.NumberOfHeartsOrLogarithm + w.COrP ).Distinct().ToList() ;
      // var wireStrips = wireStripsOfWireType.Any() ? ( from wireStrip in wireStripsOfWireType select new DetailTableModel.ComboboxItemType( wireStrip, wireStrip ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireSize, selectedWireSize.ToString(), wireStrips ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void WireStripSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedWireStrip = comboBox.SelectedValue ;
      // var selectedDetailTableRow = (DetailTableModel) DtGrid.SelectedValue ;
      // if ( string.IsNullOrEmpty( selectedDetailTableRow.WireType ) || string.IsNullOrEmpty( selectedDetailTableRow.WireSize ) || selectedWireStrip == null || string.IsNullOrEmpty( selectedWireStrip.ToString() ) ) return ;
      //
      // var crossSectionalArea = Convert.ToDouble( _wiresAndCablesModelData.FirstOrDefault( w => w.WireType == selectedDetailTableRow.WireType && w.DiameterOrNominal == selectedDetailTableRow.WireSize && ( w.NumberOfHeartsOrLogarithm + w.COrP == selectedWireStrip.ToString() || ( selectedWireStrip.ToString() == "-" && w.NumberOfHeartsOrLogarithm == "0" ) ) )?.CrossSectionalArea ) ;
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireStrip, selectedWireStrip.ToString(), new List<DetailTableModel.ComboboxItemType>(), crossSectionalArea ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void WireBookSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedWireBook = comboBox.SelectedValue ;
      // if( selectedWireBook == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireBook, comboBox.SelectedValue.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void WireBookLostFocus( object sender, RoutedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var wireBook = comboBox.Text ;
      // if( string.IsNullOrEmpty( wireBook ) ) return ;
      // var isNumberValue = int.TryParse( wireBook, out var selectedWireBookInt ) ;
      // if ( ! isNumberValue || ( isNumberValue && selectedWireBookInt < 1 ) ) {
      //   comboBox.Text = string.Empty ;
      //   return ;
      // }
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.WireBook, wireBook!, new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void EarthTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedEarthType = comboBox.SelectedValue ;
      // if ( selectedEarthType == null ) return ;
      //
      // var earthSizes = _wiresAndCablesModelData.Where( c => c.WireType == selectedEarthType.ToString() ).Select( c => c.DiameterOrNominal ).ToList() ;
      // var earthSizeTypes = earthSizes.Any() ? ( from earthSize in earthSizes select new DetailTableModel.ComboboxItemType( earthSize, earthSize ) ).ToList() : new List<DetailTableModel.ComboboxItemType>() ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.EarthType, selectedEarthType.ToString(), earthSizeTypes ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void EarthSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedEarthSize = comboBox.SelectedValue ;
      // if ( selectedEarthSize == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.EarthSize, selectedEarthSize.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void NumberOfGroundsSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedNumberOfGrounds = comboBox.SelectedValue ;
      // if ( selectedNumberOfGrounds == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.NumberOfGrounds, selectedNumberOfGrounds.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void NumberOfGroundsLostFocus( object sender, RoutedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var numberOfGrounds = comboBox.Text ;
      // if( string.IsNullOrEmpty( numberOfGrounds ) ) return ;
      // var isNumberValue = int.TryParse( numberOfGrounds, out var numberOfGroundsInt ) ;
      // if ( ! isNumberValue || ( isNumberValue && numberOfGroundsInt < 1 ) ) {
      //   comboBox.Text = string.Empty ;
      //   return ;
      // }
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.NumberOfGrounds, numberOfGrounds!, new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void PlumbingSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedPlumbingSize = comboBox.SelectedValue ;
      // if ( selectedPlumbingSize == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.PlumbingSize, selectedPlumbingSize.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void ConstructionClassificationSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedConstructionClassification = comboBox.SelectedValue ;
      // if ( selectedConstructionClassification == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.ConstructionClassification, selectedConstructionClassification.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void SignalTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var selectedSignalType = comboBox.SelectedValue ;
      // if ( selectedSignalType == null ) return ;
      //
      // if ( comboBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.SignalType, selectedSignalType.ToString(), new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void RemarkChanged( object sender, KeyboardFocusChangedEventArgs e )
    {
      // if ( sender is not TextBox textBox ) return ;
      // var remark = textBox.Text ;
      //
      // if ( textBox.DataContext is DetailTableModel editedDetailTableRow ) {
      //   ViewModel.ComboboxSelectionChanged( _detailTableViewModel, DetailTableViewModelSummary, editedDetailTableRow, DetailTableViewModel.EditedColumn.Remark, remark, new List<DetailTableModel.ComboboxItemType>() ) ;
      // }
      //
      // UpdateDataGridAndRemoveSelectedRow() ;
    }
    
    private void ConstructionItemSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var constructionItem = comboBox.SelectedValue ;
      // if ( constructionItem == null ) return ;
      // if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
      //   MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      // }
      // else {
      //   if ( detailTableRow.ConstructionItems == constructionItem.ToString() ) return ;
      //   var detailTableRowsChangeConstructionItems = _detailTableViewModel.DetailTableModels.Where( c => c.RouteName == detailTableRow.RouteName ).ToList() ;
      //   var detailTableRowsWithSameGroupId = _detailTableViewModel.DetailTableModels.Where( c => ! string.IsNullOrEmpty( c.GroupId ) && c.GroupId == detailTableRow.GroupId && c.RouteName != detailTableRow.RouteName ).ToList() ;
      //   if ( detailTableRowsWithSameGroupId.Any() ) {
      //     var routeWithSameGroupId = detailTableRowsWithSameGroupId.Select( d => d.RouteName ).Distinct().ToHashSet() ;
      //     detailTableRowsChangeConstructionItems.AddRange( _detailTableViewModel.DetailTableModels.Where( c => routeWithSameGroupId.Contains( c.RouteName ) ).ToList() ) ;
      //   }
      //
      //   foreach ( var detailTableRowChangeConstructionItems in detailTableRowsChangeConstructionItems ) {
      //     detailTableRowChangeConstructionItems.ConstructionItems = constructionItem.ToString() ;
      //   }
      //
      //   var routesWithConstructionItemHasChanged = detailTableRowsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
      //   ViewModel.UpdatePlumbingItemsAfterChangeConstructionItems( _detailTableViewModel.DetailTableModels, detailTableRow.RouteName, constructionItem.ToString() ) ;
      //   if ( ! detailTableRow.IsMixConstructionItems ) {
      //     #region Update Plumbing Type (Comment out)
      //     // var detailTableRowsWithSameRouteName = newDetailTableModels.Where( c => c.RouteName == detailTableRow.RouteName ).ToList() ;
      //     // foreach ( var detailTableRowWithSameRouteName in detailTableRowsWithSameRouteName ) {
      //     //   var detailTableRowsWithSameDetailSymbolId = newDetailTableModels.Where( c => c.DetailSymbolId == detailTableRowWithSameRouteName.DetailSymbolId ).ToList() ;
      //     //   CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( _conduitsModelData, detailTableRowsWithSameDetailSymbolId, detailTableRow.PlumbingType, false, _isMixConstructionItems ) ;
      //     // }
      //     #endregion
      //     ViewModel.UnGroupDetailTableRowsAfterChangeConstructionItems( _detailTableViewModel.DetailTableModels, routesWithConstructionItemHasChanged, constructionItem.ToString() ) ;
      //   }
      //   foreach ( var routeName in routesWithConstructionItemHasChanged ) {
      //     if ( ! RoutesWithConstructionItemHasChanged.ContainsKey( routeName ) ) {
      //       RoutesWithConstructionItemHasChanged.Add( routeName, constructionItem.ToString() ) ;
      //     }
      //     else {
      //       RoutesWithConstructionItemHasChanged[ routeName ] = constructionItem.ToString() ;
      //     }
      //   }
      //
      //   CreateDetailTableViewModelByGroupId() ;
      // }
    }
    
    private void PlumbingItemsSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      // if ( sender is not ComboBox comboBox ) return ;
      // var plumbingItem = comboBox.SelectedValue ;
      // if ( plumbingItem == null ) return ;
      // if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow ) {
      //   MessageBox.Show( IncorrectDataErrorMessage, CaptionErrorMessage ) ;
      // }
      // else {
      //   if ( detailTableRow.PlumbingItems == plumbingItem.ToString() ) return ;
      //   var detailTableRowsWithSamePlumbing = _detailTableViewModel.DetailTableModels.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
      //   foreach ( var detailTableRowWithSamePlumbing in detailTableRowsWithSamePlumbing ) {
      //     detailTableRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
      //   }
      //
      //   var detailTableRowsSummaryWithSamePlumbing = DetailTableViewModelSummary.DetailTableModels.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
      //   foreach ( var detailTableRowWithSamePlumbing in detailTableRowsSummaryWithSamePlumbing ) {
      //     detailTableRowWithSamePlumbing.PlumbingItems = plumbingItem.ToString() ;
      //   }
      //   
      //   UpdateDataGridAndRemoveSelectedRow() ;
      // }
    }
    
    private void BtnPlumbingSummary_Click( object sender, RoutedEventArgs e )
    {
      // if ( ! _selectedDetailTableRows.Any() ) return ;
      // _isMixConstructionItems = false ;
      // PlumbingSummary() ;
    }
    
    private void BtnPlumbingSummaryMixConstructionItems_Click( object sender, RoutedEventArgs e )
    {
      // if ( ! _selectedDetailTableRows.Any() ) return ;
      // _isMixConstructionItems = true ;
      // PlumbingSummary() ;
    }
    
    private void PlumbingSummary()
    {
      // ViewModel.PlumbingSummary( _conduitsModelData, _detailSymbolStorable, _detailTableViewModel, _selectedDetailTableRows, _isMixConstructionItems, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      // CreateDetailTableViewModelByGroupId() ;
      // ResetSelectedItems() ;
      // DtGrid.SelectedItems.Clear() ;
    }
    
    private void BtnSplitPlumbing_Click( object sender, RoutedEventArgs e )
    {
      // ViewModel.SplitPlumbing( _conduitsModelData, _detailSymbolStorable, _detailTableViewModel, DetailSymbolIdsWithPlumbingTypeHasChanged ) ;
      // CreateDetailTableViewModelByGroupId() ;
      // ResetSelectedItems() ;
      // DtGrid.SelectedItems.Clear() ;
    }
    
    private void CreateDetailTableViewModelByGroupId()
    {
      // List<DetailTableModel> newDetailTableModels = ViewModel.GroupDetailTableModels( _detailTableViewModel.DetailTableModels ) ;
      // List<DetailTableModel> newReferenceDetailTableModels = ViewModel.GroupDetailTableModels( _detailTableViewModel.ReferenceDetailTableModels ) ;
      // DetailTableViewModel newDetailTableViewModel = new( new ObservableCollection<DetailTableModel>( newDetailTableModels ),  new ObservableCollection<DetailTableModel>( newReferenceDetailTableModels ), _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems, 
      //   _detailTableViewModel.Levels, _detailTableViewModel.WireTypes, _detailTableViewModel.EarthTypes, _detailTableViewModel.Numbers, _detailTableViewModel.ConstructionClassificationTypes, _detailTableViewModel.SignalTypes ) ;
      // DataContext = newDetailTableViewModel ;
      // DtGrid.ItemsSource = newDetailTableViewModel.DetailTableModels ;
      // //DetailTableViewModelSummary = newDetailTableViewModel ;
      // if ( _detailTableViewModel.ReferenceDetailTableModels.Any() ) {
      //   DtReferenceGrid.ItemsSource = newDetailTableViewModel.ReferenceDetailTableModels ;
      // }
    }
    
    private void UnGroupDetailTableRows( string groupId )
    {
      // var detailTableModels = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId ).ToList() ;
      // foreach ( var detailTableRow in detailTableModels ) {
      //   detailTableRow.GroupId = string.Empty ;
      // }
    }
    
    private bool IsThereAnyMixtureOfMultipleConstructionClassificationsInDetailSymbol(ObservableCollection<DetailTableModel> detailTableModels, ref string mixtureOfMultipleConstructionClassificationsInDetailSymbol )
    {
      // var detailTableModelsGroupByDetailSymbolId = detailTableModels.GroupBy( d => d.DetailSymbol ) ;
      // var mixSymbolGroup = detailTableModelsGroupByDetailSymbolId.Where( x => x.GroupBy( y => y.ConstructionClassification ).Count() > 1 ).ToList() ;
      // mixtureOfMultipleConstructionClassificationsInDetailSymbol = mixSymbolGroup.Any()
      //   ? string.Join( ", ", mixSymbolGroup.Select( y => y.Key ).Distinct() )
      //   : string.Empty ;
      // return !string.IsNullOrEmpty( mixtureOfMultipleConstructionClassificationsInDetailSymbol ) ;
      return false ;
    }
  }
}