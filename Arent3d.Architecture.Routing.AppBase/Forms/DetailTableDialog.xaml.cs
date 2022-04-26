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
    private readonly DetailTableViewModel _detailTableViewModel ;
    private readonly DetailSymbolStorable _detailSymbolStorable ;
    private List<DetailTableModel> _selectedDetailTableRows ;
    private List<DetailTableModel> _selectedReferenceDetailTableRows ;
    private DetailTableModel? _copyDetailTableRow ;
    public DetailTableViewModel DetailTableViewModelSummary { get ; set ; }
    public Dictionary<string, string> RoutesWithConstructionItemHasChanged { get ; }
    public Dictionary<string, string> DetailSymbolIdsWithPlumbingTypeHasChanged { get ; }
    private bool _isMixConstructionItems ;
    
    private static string MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage =
      "Construction categories are mixed in the detail symbol {0}. Would you like to proceed to create the detail table?" ;

    public DetailTableDialog( Document document, DetailTableViewModel viewModel, List<ConduitsModel> conduitsModelData, bool mixConstructionItems )
    {
      InitializeComponent() ;
      _document = document ;
      DataContext = viewModel ;
      _detailTableViewModel = viewModel ;
      _detailSymbolStorable = document.GetDetailSymbolStorable() ;
      DetailTableViewModelSummary = viewModel ;
      _conduitsModelData = conduitsModelData ;
      _isMixConstructionItems = mixConstructionItems ;
      RoutesWithConstructionItemHasChanged = new Dictionary<string, string>() ;
      DetailSymbolIdsWithPlumbingTypeHasChanged = new Dictionary<string, string>() ;
      _selectedDetailTableRows = new List<DetailTableModel>() ;
      _copyDetailTableRow = null ;
      _selectedReferenceDetailTableRows = new List<DetailTableModel>() ;

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
      _selectedDetailTableRows.Clear() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableModel detailTableRow ) continue ;
        if ( ! string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          var detailTableRows = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == detailTableRow.GroupId ).ToList() ;
          _selectedDetailTableRows.AddRange( detailTableRows ) ;
        }
        else {
          _selectedDetailTableRows.Add( detailTableRow ) ;
        }
      }
    }
    
    private void DtReferenceGrid_SelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not DataGrid dataGrid ) return ;
      var selectedItems = dataGrid.SelectedItems ;
      if ( selectedItems.Count <= 0 ) return ;
      _selectedReferenceDetailTableRows.Clear() ;
      foreach ( var item in selectedItems ) {
        if ( item is not DetailTableModel detailTableRow ) continue ;
        _selectedReferenceDetailTableRows.Add( detailTableRow ) ;
      }
    }

    private void BtnDeleteLine_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() ) return ;
      DetailTableViewModel.DeleteDetailTableRows( _detailTableViewModel, _selectedDetailTableRows, _detailSymbolStorable ) ;
      CreateDetailTableViewModelByGroupId() ;
      _selectedDetailTableRows.Clear() ;
    }
    
    private void BtnCopyLine_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableRows.Any() ) return ;
      _copyDetailTableRow = _selectedDetailTableRows.First() ;
      _selectedDetailTableRows.Clear() ;
    }

    private void BtnPasteLine_Click( object sender, RoutedEventArgs e )
    {
      if ( _copyDetailTableRow == null ) {
        MessageBox.Show( "Please choose a row to copy", "Message" ) ;
        return ;
      }

      var pasteDetailTableRow = ! _selectedDetailTableRows.Any() ? _copyDetailTableRow : _selectedDetailTableRows.First() ;
      var newDetailTableModels = DetailTableViewModel.PasteDetailTableRow( _detailTableViewModel, _copyDetailTableRow, pasteDetailTableRow ) ;
      _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      CreateDetailTableViewModelByGroupId() ;
      _selectedDetailTableRows.Clear() ;
    }
    
    private void BtnSelectAll_Click( object sender, RoutedEventArgs e )
    {
      _selectedDetailTableRows.Clear() ;
      _selectedDetailTableRows = _detailTableViewModel.DetailTableModels.ToList() ;
      DtGrid.SelectAll() ;
    }

    private void BtnReferenceSelectAll_Click( object sender, RoutedEventArgs e )
    {
      _selectedReferenceDetailTableRows = DetailTableViewModelSummary.ReferenceDetailTableModels.ToList() ;
      DtReferenceGrid.SelectAll() ;
    }
    
    private void BtnDeleteReferenceLine_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedReferenceDetailTableRows.Any() ) return ;
      DetailTableViewModel.DeleteReferenceDetailTableRows( DetailTableViewModelSummary, _selectedReferenceDetailTableRows ) ;
      UpdateReferenceDetailTableModels() ;
    }
    
    private void BtnReadCtlFile_Click( object sender, RoutedEventArgs e )
    {
      DetailTableViewModel.ReadCtlFile( DetailTableViewModelSummary ) ;
      UpdateReferenceDetailTableModels() ;
    }
    
    private void BtnAddReference_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      _detailTableViewModel.IsAddReference = true ;
      DetailTableViewModelSummary.IsAddReference = true ;
      Close() ;
    }
    
    private void BtnAddReferenceRows_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedReferenceDetailTableRows.Any() ) return ;
      DetailTableViewModel.AddReferenceDetailTableRows( DetailTableViewModelSummary, _selectedReferenceDetailTableRows ) ;
      DataContext = DetailTableViewModelSummary ;
      DtGrid.ItemsSource = DetailTableViewModelSummary.DetailTableModels ;
      _detailTableViewModel.DetailTableModels = DetailTableViewModelSummary.DetailTableModels ;
      _selectedReferenceDetailTableRows.Clear() ;
    }

    private void UpdateReferenceDetailTableModels()
    {
      DataContext = DetailTableViewModelSummary ;
      DtReferenceGrid.ItemsSource = DetailTableViewModelSummary.ReferenceDetailTableModels ;
      _detailTableViewModel.ReferenceDetailTableModels = DetailTableViewModelSummary.ReferenceDetailTableModels ;
      _selectedReferenceDetailTableRows.Clear() ;
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      DetailTableViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
      DetailTableViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
      DialogResult = true ;
      this.Close() ;
    }
    
    private void BtnSaveAndCreate_OnClick( object sender, RoutedEventArgs e )
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
        this.Close() ;
      }

      if ( this.DataContext is DetailTableViewModel context ) {
        context.IsCancelCreateDetailTable = confirmResult == MessageBoxResult.Cancel ;
      }
    }

    private void BtnCompleted_OnClick( object sender, RoutedEventArgs e )
    {
      DetailTableViewModel.SaveData( _document, _detailTableViewModel.DetailTableModels ) ;
      DetailTableViewModel.SaveDetailSymbolData( _document, _detailSymbolStorable ) ;
      DialogResult = true ;
      this.Close() ;
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
        if ( _isMixConstructionItems )
          DetailTableViewModel.UpdatePlumbingItemsAfterChangeConstructionItems( _detailTableViewModel.DetailTableModels, detailTableRow.RouteName, constructionItem.ToString() ) ;
        else {
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

        CreateDetailTableViewModelByGroupId() ;
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
      _selectedDetailTableRows.Clear() ;
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
            detailTableRow.IsReadOnly, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, detailTableRow.IsReadOnlyPlumbingItems, detailTableRow.IsMixConstructionItems, detailTableRow.CopyIndex ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          existedGroupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      DetailTableViewModel newDetailTableViewModel = new( new ObservableCollection<DetailTableModel>( newDetailTableModels ), _detailTableViewModel.ReferenceDetailTableModels, _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems ) ;
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