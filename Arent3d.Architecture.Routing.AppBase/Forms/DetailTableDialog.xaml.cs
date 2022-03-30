﻿using System.Collections.Generic ;
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
    private const string DefaultParentPlumbingType = "E" ;
    private const string DefaultChildPlumbingSymbol = "↑" ;
    private const string IncorrectDataErrorMessage = "Incorrect data." ;
    private const string CaptionErrorMessage = "Error" ;
    private readonly Document _document ;
    private readonly List<ConduitsModel> _conduitsModelData ;
    private readonly DetailTableViewModel _detailTableViewModel ;
    private readonly List<DetailTableModel> _selectedDetailTableModels ;
    public DetailTableViewModel DetailTableViewModelSummary ;
    public readonly Dictionary<string, string> RoutesWithConstructionItemHasChanged ;
    public readonly Dictionary<string, string> DetailSymbolIdsWithPlumbingTypeHasChanged ;
    private bool _isMixConstructionItems ;
    
    private static string MultipleConstructionCategoriesMixedWithSameDetailSymbolMessage =
      "Construction categories are mixed in the detail symbol {0}. Would you like to proceed to create the detail table?" ;

    public DetailTableDialog( Document document, DetailTableViewModel viewModel, List<ConduitsModel> conduitsModelData, bool mixConstructionItems )
    {
      InitializeComponent() ;
      _document = document ;
      DataContext = viewModel ;
      _detailTableViewModel = viewModel ;
      DetailTableViewModelSummary = viewModel ;
      _conduitsModelData = conduitsModelData ;
      _isMixConstructionItems = mixConstructionItems ;
      RoutesWithConstructionItemHasChanged = new Dictionary<string, string>() ;
      DetailSymbolIdsWithPlumbingTypeHasChanged = new Dictionary<string, string>() ;
      _selectedDetailTableModels = new List<DetailTableModel>() ;

      var isGrouped = viewModel.DetailTableModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.GroupId ) ) != null ;
      if ( isGrouped ) SetGroupIdForNewDetailTableRows() ;
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
      SaveData( _detailTableViewModel.DetailTableModels ) ;
    }

    private void CbCalculationExclusion_Checked( object sender, RoutedEventArgs e )
    {
      if ( DtGrid.SelectedValue is not DetailTableModel selectedItem ) return ;
      if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
        var selectedItems = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId ).ToList() ;
        foreach ( var item in selectedItems.Where( item => ! _selectedDetailTableModels.Contains( item ) ) ) {
          _selectedDetailTableModels.Add( item ) ;
        }
      }
      else {
        if ( ! _selectedDetailTableModels.Contains( selectedItem ) ) {
          _selectedDetailTableModels.Add( selectedItem ) ;
        }
      }
    }

    private void CbCalculationExclusion_UnChecked( object sender, RoutedEventArgs e )
    {
      if ( DtGrid.SelectedValue is not DetailTableModel selectedItem ) return ;
      if ( ! string.IsNullOrEmpty( selectedItem.GroupId ) ) {
        var selectedItems = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == selectedItem.GroupId ).ToList() ;
        foreach ( var item in selectedItems ) {
          var items = _selectedDetailTableModels.Where( d => d.DetailSymbolId == item.DetailSymbolId && d.RouteName == item.RouteName ).ToList() ;
          if ( items.Any() ) {
            foreach ( var detailTableRow in items ) {
              _selectedDetailTableModels.Remove( detailTableRow ) ;
            }
          }
          item.CalculationExclusion = false ;
        }
      }
      else {
        var items = _selectedDetailTableModels.Where( d => d.DetailSymbolId == selectedItem.DetailSymbolId && d.RouteName == selectedItem.RouteName ).ToList() ;
        if ( items.Any() ) {
          foreach ( var detailTableRow in items ) {
            _selectedDetailTableModels.Remove( detailTableRow ) ;
          }
        }
        selectedItem.CalculationExclusion = false ;
      }
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      SaveData( _detailTableViewModel.DetailTableModels ) ;
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
        SaveData( _detailTableViewModel.DetailTableModels ) ;
        DialogResult = true ;
        this.Close() ;
      }

      if ( this.DataContext is DetailTableViewModel context ) {
        context.IsCancelCreateDetailTable = confirmResult == MessageBoxResult.Cancel ;
      }
    }

    private void BtnCompleted_OnClick( object sender, RoutedEventArgs e )
    {
      SaveData( _detailTableViewModel.DetailTableModels ) ;
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

          var newDetailTableModels = new ObservableCollection<DetailTableModel>( detailTableModels ) ;

          CreateDetailTableCommandBase.SetPlumbingData( _conduitsModelData, ref newDetailTableModels, plumbingType.ToString(), _isMixConstructionItems ) ;

          if ( newDetailTableModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.GroupId ) ) != null ) {
            if ( _isMixConstructionItems ) {
              DetailTableViewModel.SetGroupIdForDetailTableRowsMixConstructionItems( newDetailTableModels ) ;
            }
            else {
              DetailTableViewModel.SetGroupIdForDetailTableRows( newDetailTableModels ) ;
            }
          }

          if ( _isMixConstructionItems ) {
            DetailTableViewModel.SetPlumbingItemsForDetailTableRowsMixConstructionItems( newDetailTableModels ) ;
          }
          else {
            DetailTableViewModel.SetPlumbingItemsForDetailTableRows( newDetailTableModels ) ;
          }

          foreach ( var oldDetailTableRow in detailTableModels ) {
            _detailTableViewModel.DetailTableModels.Remove( oldDetailTableRow ) ;
          }

          foreach ( var newDetailTableRow in newDetailTableModels ) {
            _detailTableViewModel.DetailTableModels.Add( newDetailTableRow ) ;
          }

          if ( ! DetailSymbolIdsWithPlumbingTypeHasChanged.ContainsKey( newDetailTableModels.First().DetailSymbolId ) ) {
            DetailSymbolIdsWithPlumbingTypeHasChanged.Add( newDetailTableModels.First().DetailSymbolId, plumbingType!.ToString() ) ;
          }
          else {
            DetailSymbolIdsWithPlumbingTypeHasChanged[ newDetailTableModels.First().DetailSymbolId ] = plumbingType!.ToString() ;
          }

          var newDetailTableModelList = 
            _detailTableViewModel.DetailTableModels
              .OrderBy( x => x.DetailSymbol )
              .ThenByDescending( x => x.DetailSymbolId )
              .ThenByDescending( x => x.IsParentRoute )
              .GroupBy( x => x.DetailSymbolId )
              .SelectMany( x => x )
              .ToList() ;
          _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModelList ) ;
          CreateDetailTableViewModelByGroupId() ;
          SaveData( _detailTableViewModel.DetailTableModels ) ;
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

        List<DetailTableModel> newDetailTableModels = new() ;
        foreach ( var oldDetailTableRow in _detailTableViewModel.DetailTableModels ) {
          if ( detailTableRowsChangeConstructionItems.Contains( oldDetailTableRow ) ) {
            var plumbingItems = _isMixConstructionItems ? oldDetailTableRow.PlumbingItems : constructionItem.ToString() ;
            var newDetailTableRow = new DetailTableModel( oldDetailTableRow.CalculationExclusion, oldDetailTableRow.Floor, oldDetailTableRow.CeedCode, oldDetailTableRow.DetailSymbol, 
              oldDetailTableRow.DetailSymbolId, oldDetailTableRow.WireType, oldDetailTableRow.WireSize, oldDetailTableRow.WireStrip, oldDetailTableRow.WireBook, oldDetailTableRow.EarthType, 
              oldDetailTableRow.EarthSize, oldDetailTableRow.NumberOfGrounds, oldDetailTableRow.PlumbingType, oldDetailTableRow.PlumbingSize, oldDetailTableRow.NumberOfPlumbing, 
              oldDetailTableRow.ConstructionClassification, oldDetailTableRow.SignalType, constructionItem.ToString(), plumbingItems, oldDetailTableRow.Remark, 
              oldDetailTableRow.WireCrossSectionalArea, oldDetailTableRow.CountCableSamePosition, oldDetailTableRow.RouteName, oldDetailTableRow.IsEcoMode, oldDetailTableRow.IsParentRoute, 
              oldDetailTableRow.IsReadOnly, oldDetailTableRow.PlumbingIdentityInfo, oldDetailTableRow.GroupId, oldDetailTableRow.IsReadOnlyPlumbingItems ) ;
            newDetailTableModels.Add( newDetailTableRow ) ;
          }
          else {
            newDetailTableModels.Add( oldDetailTableRow ) ;
          }
        }

        var routesWithConstructionItemHasChanged = detailTableRowsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
        if ( _isMixConstructionItems )
          DetailTableViewModel.UpdatePlumbingItemsAfterChangeConstructionItems( ref newDetailTableModels, detailTableRow.RouteName, constructionItem.ToString() ) ;
        else
          DetailTableViewModel.UnGroupDetailTableRowsAfterChangeConstructionItems( ref newDetailTableModels, routesWithConstructionItemHasChanged, constructionItem.ToString() ) ;
        foreach ( var routeName in routesWithConstructionItemHasChanged ) {
          if ( ! RoutesWithConstructionItemHasChanged.ContainsKey( routeName ) ) {
            RoutesWithConstructionItemHasChanged.Add( routeName, constructionItem.ToString() ) ;
          }
          else {
            RoutesWithConstructionItemHasChanged[ routeName ] = constructionItem.ToString() ;
          }
        }

        _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
        CreateDetailTableViewModelByGroupId() ;
        SaveData( _detailTableViewModel.DetailTableModels ) ;
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
        List<DetailTableModel> detailTableRowsChangePlumbingItems = new() ;
        var detailTableRowsWithSamePlumbing = _detailTableViewModel.DetailTableModels.Where( c => c.PlumbingIdentityInfo == detailTableRow.PlumbingIdentityInfo ).ToList() ;
        if ( detailTableRowsWithSamePlumbing.Any() ) {
          //change group rows
          detailTableRowsChangePlumbingItems.AddRange( detailTableRowsWithSamePlumbing ) ;
        }
        else {
          //change single row
          var changeDetailTableRow = _detailTableViewModel.DetailTableModels.SingleOrDefault( x => x.Equals( detailTableRow ) ) ;
          if ( changeDetailTableRow != null ) detailTableRowsChangePlumbingItems.Add( changeDetailTableRow ) ;
        }

        var newDetailTableModels = new List<DetailTableModel>() ;
        foreach ( var oldDetailTableRow in _detailTableViewModel.DetailTableModels ) {
          if ( detailTableRowsChangePlumbingItems.Contains( oldDetailTableRow ) ) {
            var newDetailTableRow = new DetailTableModel( oldDetailTableRow.CalculationExclusion, oldDetailTableRow.Floor, oldDetailTableRow.CeedCode, oldDetailTableRow.DetailSymbol, 
              oldDetailTableRow.DetailSymbolId, oldDetailTableRow.WireType, oldDetailTableRow.WireSize, oldDetailTableRow.WireStrip, oldDetailTableRow.WireBook, oldDetailTableRow.EarthType, 
              oldDetailTableRow.EarthSize, oldDetailTableRow.NumberOfGrounds, oldDetailTableRow.PlumbingType, oldDetailTableRow.PlumbingSize, oldDetailTableRow.NumberOfPlumbing, 
              oldDetailTableRow.ConstructionClassification, oldDetailTableRow.SignalType, oldDetailTableRow.ConstructionItems, plumbingItem.ToString(), oldDetailTableRow.Remark, 
              oldDetailTableRow.WireCrossSectionalArea, oldDetailTableRow.CountCableSamePosition, oldDetailTableRow.RouteName, oldDetailTableRow.IsEcoMode, oldDetailTableRow.IsParentRoute, 
              oldDetailTableRow.IsReadOnly, oldDetailTableRow.PlumbingIdentityInfo, oldDetailTableRow.GroupId, oldDetailTableRow.IsReadOnlyPlumbingItems ) ;
            newDetailTableModels.Add( newDetailTableRow ) ;
          }
          else {
            newDetailTableModels.Add( oldDetailTableRow ) ;
          }
        }

        _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
        CreateDetailTableViewModelByGroupId() ;
        SaveData( _detailTableViewModel.DetailTableModels ) ;
      }
    }

    private void SaveData( IReadOnlyCollection<DetailTableModel> detailTableRowsBySelectedDetailSymbols )
    {
      try {
        DetailTableStorable detailTableStorable = _document.GetDetailTableStorable() ;
        {
          if ( ! detailTableRowsBySelectedDetailSymbols.Any() ) return ;
          var selectedDetailSymbolIds = detailTableRowsBySelectedDetailSymbols.Select( d => d.DetailSymbolId ).Distinct().ToHashSet() ;
          var detailTableRowsByOtherDetailSymbols = detailTableStorable.DetailTableModelData.Where( d => ! selectedDetailSymbolIds.Contains( d.DetailSymbolId ) ).ToList() ;
          detailTableStorable.DetailTableModelData = detailTableRowsBySelectedDetailSymbols.ToList() ;
          if ( detailTableRowsByOtherDetailSymbols.Any() ) detailTableStorable.DetailTableModelData.AddRange( detailTableRowsByOtherDetailSymbols ) ;
        }
        using Transaction t = new( _document, "Save data" ) ;
        t.Start() ;
        detailTableStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    private void BtnPlumbingSummary_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableModels.Any() ) return ;
      _isMixConstructionItems = false ;
      var detailTableModelsByDetailSymbolIds = 
        _detailTableViewModel.DetailTableModels
          .Where( c => _selectedDetailTableModels.Contains( c ) )
          .GroupBy( d => d.DetailSymbolId )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableModelsByDetailSymbolId in detailTableModelsByDetailSymbolIds ) {
        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( _conduitsModelData, detailTableModelsByDetailSymbolId, DefaultParentPlumbingType, false, _isMixConstructionItems ) ;
      }

      var detailTableModelsGroupByDetailSymbolId =
        _detailTableViewModel.DetailTableModels
          .GroupBy( d => d.DetailSymbolId )
          .Select( x => x.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        DetailTableViewModel.SetGroupIdForDetailTableRows( detailTableRowsWithSameDetailSymbolId ) ;
      }

      var newDetailTableModels = _detailTableViewModel.DetailTableModels.ToList() ;
      DetailTableViewModel.SortDetailTableModel( ref newDetailTableModels, _isMixConstructionItems ) ;
      _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      CreateDetailTableViewModelByGroupId() ;
      SaveData( _detailTableViewModel.DetailTableModels ) ;
    }
    
    private void BtnPlumbingSummaryMixConstructionItems_Click( object sender, RoutedEventArgs e )
    {
      if ( ! _selectedDetailTableModels.Any() ) return ;
      _isMixConstructionItems = true ;
      var detailTableModelsByDetailSymbolIds = 
        _detailTableViewModel.DetailTableModels
          .Where( c => _selectedDetailTableModels.Contains( c ) )
          .GroupBy( d => d.DetailSymbolId )
          .Select( g => g.ToList() ) ;
      foreach ( var detailTableModelsByDetailSymbolId in detailTableModelsByDetailSymbolIds ) {
        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( _conduitsModelData, detailTableModelsByDetailSymbolId, DefaultParentPlumbingType, false, _isMixConstructionItems ) ;
      }
      
      var detailTableModelsGroupByDetailSymbolId =
        _detailTableViewModel.DetailTableModels
          .GroupBy( d => d.DetailSymbolId )
          .Select( x => x.ToList() ) ;
      foreach ( var detailTableRowsWithSameDetailSymbolId in detailTableModelsGroupByDetailSymbolId ) {
        DetailTableViewModel.SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsWithSameDetailSymbolId ) ;
      }
      
      var newDetailTableModels = _detailTableViewModel.DetailTableModels.ToList() ;
      DetailTableViewModel.SortDetailTableModel( ref newDetailTableModels, _isMixConstructionItems ) ;
      CreateDetailTableViewModelByGroupId() ;
      SaveData( _detailTableViewModel.DetailTableModels ) ;
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
            detailTableRow.IsReadOnly, detailTableRow.PlumbingIdentityInfo, detailTableRow.GroupId, detailTableRow.IsReadOnlyPlumbingItems ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          existedGroupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      DetailTableViewModel newDetailTableViewModel = new( new ObservableCollection<DetailTableModel>( newDetailTableModels ), _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems ) ;
      this.DataContext = newDetailTableViewModel ;
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

    private void SetGroupIdForNewDetailTableRows()
    {
      DetailTableStorable detailTableStorable = _document.GetDetailTableStorable() ;
      var allDetailSymbolIds = detailTableStorable.DetailTableModelData.Select( d => d.DetailSymbolId ).Distinct().ToHashSet() ;
      var newDetailTableRows = _detailTableViewModel.DetailTableModels.Where( d => ! allDetailSymbolIds.Contains( d.DetailSymbolId ) ).ToList() ;
      if ( ! newDetailTableRows.Any() ) return ;
      {
        var newDetailTableRowsGroupByDetailSymbolId = 
          newDetailTableRows
            .GroupBy( d => d.DetailSymbolId )
            .Select( g => g.ToList() ) ;
        foreach ( var detailTableRowsWithSameDetailSymbolId in newDetailTableRowsGroupByDetailSymbolId ) {
          if ( _isMixConstructionItems ) {
            DetailTableViewModel.SetGroupIdForDetailTableRowsMixConstructionItems( detailTableRowsWithSameDetailSymbolId ) ;
          }
          else {
            DetailTableViewModel.SetGroupIdForDetailTableRows( detailTableRowsWithSameDetailSymbolId ) ;
          }
        }
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