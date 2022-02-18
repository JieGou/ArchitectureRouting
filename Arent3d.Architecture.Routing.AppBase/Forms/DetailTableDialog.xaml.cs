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
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DetailTableDialog : Window
  {
    private readonly Document _document ;
    private readonly List<ConduitsModel> _conduitsModelData ;
    private readonly DetailTableViewModel _detailTableViewModel ;
    public DetailTableViewModel DetailTableViewModelSummary ;
    public readonly Dictionary<string, string> RoutesChangedConstructionItem ;
    public readonly Dictionary<string, string> DetailSymbolsChangedPlumbingType ;

    public DetailTableDialog( Document document, DetailTableViewModel viewModel, List<ConduitsModel> conduitsModelData )
    {
      InitializeComponent() ;
      _document = document ;
      DataContext = viewModel ;
      _detailTableViewModel = viewModel ;
      DetailTableViewModelSummary = viewModel ;
      _conduitsModelData = conduitsModelData ;
      RoutesChangedConstructionItem = new Dictionary<string, string>() ;
      DetailSymbolsChangedPlumbingType = new Dictionary<string, string>() ;
      CreateDetailTableViewModelByGroupId() ;
      
      Style rowStyle = new Style( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( DataGridRow.MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
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

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      SaveData( _detailTableViewModel.DetailTableModels ) ;
      DialogResult = true ;
      this.Close() ;
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
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableModel || detailTableModel.PlumbingType == plumbingType!.ToString() ) return ;
      if ( plumbingType!.ToString() == "↑" ) {
        comboBox.SelectedValue = detailTableModel.PlumbingType ;
      }
      else {
        List<DetailTableModel> detailTableModels = _detailTableViewModel.DetailTableModels.Where( c => c.DetailSymbolId == detailTableModel.DetailSymbolId ).ToList() ;

        List<DetailTableModel> newDetailTableModels = detailTableModels.Select( x => x ).ToList() ;

        CreateDetailTableCommandBase.SetPlumbingDataForOneSymbol( _conduitsModelData, ref newDetailTableModels, plumbingType!.ToString(), true ) ;

        if ( newDetailTableModels.FirstOrDefault( d => ! string.IsNullOrEmpty( d.GroupId ) ) != null )
          SetGroupId( newDetailTableModels ) ;

        foreach ( var oldDetailTableRow in detailTableModels ) {
          _detailTableViewModel.DetailTableModels.Remove( oldDetailTableRow ) ;
        }

        foreach ( var newDetailTableRow in newDetailTableModels ) {
          _detailTableViewModel.DetailTableModels.Add( newDetailTableRow ) ;
        }

        if ( ! DetailSymbolsChangedPlumbingType.ContainsKey( newDetailTableModels.First().DetailSymbolId ) ) {
          DetailSymbolsChangedPlumbingType.Add( newDetailTableModels.First().DetailSymbolId, plumbingType!.ToString() ) ;
        }
        else {
          DetailSymbolsChangedPlumbingType[ newDetailTableModels.First().DetailSymbolId ] = plumbingType!.ToString() ;
        }
        
        newDetailTableModels = _detailTableViewModel.DetailTableModels.OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.DetailSymbolId ).ThenByDescending( x => x.IsParentRoute ).GroupBy( x => x.DetailSymbolId ).SelectMany( x => x ).ToList() ;
        _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
        CreateDetailTableViewModelByGroupId() ;
        SaveData( _detailTableViewModel.DetailTableModels ) ;
      }
    }

    private void ConstructionItemSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var constructionItem = comboBox.SelectedValue ;
      if ( constructionItem == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableRow || detailTableRow.ConstructionItems == constructionItem!.ToString() ) return ;
      var detailTableRowsChangeConstructionItems = _detailTableViewModel.DetailTableModels.Where( c => c.RouteName == detailTableRow.RouteName ).ToList() ;
      var detailTableRowsSameGroupId = _detailTableViewModel.DetailTableModels.Where( c => ! string.IsNullOrEmpty( c.GroupId ) && c.GroupId == detailTableRow.GroupId && c.RouteName != detailTableRow.RouteName ).ToList() ;
      if ( detailTableRowsSameGroupId.Any() ) {
        var routeSameGroupId = detailTableRowsSameGroupId.Select( d => d.RouteName ).Distinct().ToList() ;
        detailTableRowsChangeConstructionItems.AddRange( _detailTableViewModel.DetailTableModels.Where( c => routeSameGroupId.Contains( c.RouteName ) ).ToList() ) ;
      }
      List<DetailTableModel> newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var oldDetailTableRow in _detailTableViewModel.DetailTableModels ) {
        if ( detailTableRowsChangeConstructionItems.Contains( oldDetailTableRow ) ) {
          var newDetailTableRow = new DetailTableModel( oldDetailTableRow.CalculationExclusion, oldDetailTableRow.Floor, oldDetailTableRow.CeeDCode, oldDetailTableRow.DetailSymbol, oldDetailTableRow.DetailSymbolId, oldDetailTableRow.WireType, oldDetailTableRow.WireSize, oldDetailTableRow.WireStrip, oldDetailTableRow.WireBook, oldDetailTableRow.EarthType, oldDetailTableRow.EarthSize, oldDetailTableRow.NumberOfGrounds, oldDetailTableRow.PlumbingType, oldDetailTableRow.PlumbingSize, oldDetailTableRow.NumberOfPlumbing, oldDetailTableRow.ConstructionClassification, oldDetailTableRow.SignalType, constructionItem!.ToString(), constructionItem!.ToString(), oldDetailTableRow.Remark, oldDetailTableRow.WireCrossSectionalArea, oldDetailTableRow.CountCableSamePosition, oldDetailTableRow.RouteName, oldDetailTableRow.IsEcoMode, oldDetailTableRow.IsParentRoute, oldDetailTableRow.IsReadOnly, oldDetailTableRow.ParentPlumbingType, oldDetailTableRow.GroupId ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
        }
        else {
          newDetailTableModels.Add( oldDetailTableRow ) ;
        }
      }

      var routesChangeConstructionItem = detailTableRowsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
      UnGroupDetailTableRowsAfterChangeConstructionItems( ref newDetailTableModels, routesChangeConstructionItem, constructionItem!.ToString() ) ;
      foreach ( var routeName in routesChangeConstructionItem ) {
        if ( ! RoutesChangedConstructionItem.ContainsKey( routeName ) ) {
          RoutesChangedConstructionItem.Add( routeName, constructionItem!.ToString() ) ;
        }
        else {
          RoutesChangedConstructionItem[ routeName ] = constructionItem!.ToString() ;
        }
      }

      _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      CreateDetailTableViewModelByGroupId() ;
      SaveData( _detailTableViewModel.DetailTableModels ) ;
    }

    private void UnGroupDetailTableRowsAfterChangeConstructionItems( ref List<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
    {
      var groupIdOfDetailTableRowsChangeConstructionItems = detailTableModels.Where( d => routeNames.Contains( d.RouteName ) && ! string.IsNullOrEmpty( d.GroupId ) ).Select( d => d.GroupId ).Distinct().ToList() ;
      foreach ( var groupId in groupIdOfDetailTableRowsChangeConstructionItems ) {
        var detailTableRowsSameGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems != constructionItems ).ToList() ;
        var detailTableRowsChangeConstructionItems = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems == constructionItems ).ToList() ;
        if ( detailTableRowsSameGroupId.Any() ) {
          if ( detailTableRowsSameGroupId.Count == 1 ) 
            detailTableRowsSameGroupId.First().GroupId = string.Empty ;
          if ( detailTableRowsChangeConstructionItems.Count == 1 ) 
            detailTableRowsChangeConstructionItems.First().GroupId = string.Empty ;
        }

        if ( detailTableRowsChangeConstructionItems.Count <= 1 ) continue ;
        foreach ( var detailTableRow in detailTableRowsChangeConstructionItems ) {
          var newGroupId = detailTableRow.DetailSymbolId + "-" + detailTableRow.ParentPlumbingType + "-" + detailTableRow.ConstructionItems + "-" + detailTableRow.WireType + detailTableRow.WireSize + detailTableRow.WireStrip ;
          detailTableRow.GroupId = newGroupId ;
        }
      }
    }

    private void SaveData( IReadOnlyCollection<DetailTableModel> detailTableRowsBySelectedDetailSymbols )
    {
      try {
        DetailTableStorable detailTableStorable = _document.GetDetailTableStorable() ;
        {
          if ( ! detailTableRowsBySelectedDetailSymbols.Any() ) return ;
          var selectedDetailSymbolIds = detailTableRowsBySelectedDetailSymbols.ToList().Select( d => d.DetailSymbolId ).Distinct().ToList() ;
          var detailTableRowsByOtherDetailSymbols = detailTableStorable.DetailTableModelData.Where( d => ! selectedDetailSymbolIds.Contains( d.DetailSymbolId ) ).ToList() ;
          detailTableStorable.DetailTableModelData = detailTableRowsBySelectedDetailSymbols.ToList() ;
          if ( detailTableRowsByOtherDetailSymbols.Any() ) detailTableStorable.DetailTableModelData.AddRange( detailTableRowsByOtherDetailSymbols ) ;
        }
        using Transaction t = new Transaction( _document, "Save data" ) ;
        t.Start() ;
        detailTableStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }

    private void BtnPlumbingSummary_Click( object sender, RoutedEventArgs e )
    {
      var detailTableModelsGroupByDetailSymbolId = _detailTableViewModel.DetailTableModels.ToList().GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      foreach ( var (_, detailTableRowsSameDetailSymbolId) in detailTableModelsGroupByDetailSymbolId ) {
        SetGroupId( detailTableRowsSameDetailSymbolId ) ;
      }

      CreateDetailTableViewModelByGroupId() ;
      SaveData( _detailTableViewModel.DetailTableModels ) ;
    }

    private void SetGroupId( List<DetailTableModel> detailTableRowsSameDetailSymbolId )
    {
      var detailTableRowsGroupByPlumbingType = detailTableRowsSameDetailSymbolId.GroupBy( d => d.ParentPlumbingType ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      foreach ( var (_, detailTableRowsSamePlumbingType) in detailTableRowsGroupByPlumbingType ) {
        var detailTableRowsGroupByConstructionItem = detailTableRowsSamePlumbingType.GroupBy( d => d.ConstructionItems ).ToDictionary( g => g.Key, g => g.ToList() ) ;
        foreach ( var (_, detailTableRowsSameConstructionItem) in detailTableRowsGroupByConstructionItem ) {
          var detailTableRowsGroupByWiringType = detailTableRowsSameConstructionItem.GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) ).ToDictionary( g => g.Key.WireType + g.Key.WireSize + "x" + g.Key.WireStrip, g => g.ToList() ) ;
          foreach ( var (_, detailTableRowsSameWiringType) in detailTableRowsGroupByWiringType ) {
            var oldDetailTableRow = detailTableRowsSameWiringType.FirstOrDefault() ;
            if ( oldDetailTableRow == null ) continue ;
            if ( detailTableRowsSameWiringType.Count == 1 ) {
              oldDetailTableRow.GroupId = string.Empty ;
            }
            else {
              var groupId = oldDetailTableRow.DetailSymbolId + "-" + oldDetailTableRow.ParentPlumbingType + "-" + oldDetailTableRow.ConstructionItems + "-" + oldDetailTableRow.WireType + oldDetailTableRow.WireSize + oldDetailTableRow.WireStrip ;
              foreach ( var detailTableRowSameWiringType in detailTableRowsSameWiringType ) {
                detailTableRowSameWiringType.GroupId = groupId ;
              }
            }
          }
        }
      }
    }

    private void CreateDetailTableViewModelByGroupId()
    {
      List<DetailTableModel> newDetailTableModels = new List<DetailTableModel>() ;
      List<string> groupIds = new List<string>() ;
      foreach ( var detailTableRow in _detailTableViewModel.DetailTableModels ) {
        if ( string.IsNullOrEmpty( detailTableRow.GroupId ) ) {
          newDetailTableModels.Add( detailTableRow ) ;
        }
        else {
          if ( groupIds.Contains( detailTableRow.GroupId ) ) continue ;
          var detailTableRowsSameGroupId = _detailTableViewModel.DetailTableModels.Where( d => d.GroupId == detailTableRow.GroupId ) ;
          var detailTableRowsGroupByRemark = detailTableRowsSameGroupId.GroupBy( d => d.Remark ).ToDictionary( g => g.Key, g => g.ToList() ) ;
          List<string> newRemark = new List<string>() ;
          var numberOfGrounds = 0 ;
          foreach ( var (remark, detailTableRowsSameRemark) in detailTableRowsGroupByRemark ) {
            newRemark.Add( remark + ( detailTableRowsSameRemark.Count == 1 ? string.Empty : "x" + detailTableRowsSameRemark.Count ) ) ;
            numberOfGrounds += detailTableRowsSameRemark.Count == 1 ? 1 : detailTableRowsSameRemark.Count ;
          }

          var newDetailTableRow = new DetailTableModel( detailTableRow.CalculationExclusion, detailTableRow.Floor, detailTableRow.CeeDCode, detailTableRow.DetailSymbol, detailTableRow.DetailSymbolId, detailTableRow.WireType, detailTableRow.WireSize, detailTableRow.WireStrip, numberOfGrounds.ToString(), detailTableRow.EarthType, detailTableRow.EarthSize, detailTableRow.NumberOfGrounds, detailTableRow.PlumbingType, detailTableRow.PlumbingSize, detailTableRow.NumberOfPlumbing, detailTableRow.ConstructionClassification, detailTableRow.SignalType, detailTableRow.ConstructionItems, detailTableRow.PlumbingItems, string.Join( ", ", newRemark ), detailTableRow.WireCrossSectionalArea, detailTableRow.CountCableSamePosition, detailTableRow.RouteName, detailTableRow.IsEcoMode, detailTableRow.IsParentRoute, detailTableRow.IsReadOnly, detailTableRow.ParentPlumbingType, detailTableRow.GroupId ) ;
          newDetailTableModels.Add( newDetailTableRow ) ;
          groupIds.Add( detailTableRow.GroupId ) ;
        }
      }

      DetailTableViewModel newDetailTableViewModel = new DetailTableViewModel( new ObservableCollection<DetailTableModel>( newDetailTableModels ), _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems ) ;
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
  }
}