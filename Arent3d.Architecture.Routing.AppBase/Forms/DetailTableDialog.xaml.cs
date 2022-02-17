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
      UnGroupDetailTableModel( selectedItem.GroupId ) ;
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

        foreach ( var oldDetailTableModel in detailTableModels ) {
          _detailTableViewModel.DetailTableModels.Remove( oldDetailTableModel ) ;
        }

        foreach ( var newDetailSymbolModel in newDetailTableModels ) {
          _detailTableViewModel.DetailTableModels.Add( newDetailSymbolModel ) ;
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
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableModel || detailTableModel.ConstructionItems == constructionItem!.ToString() ) return ;
      var detailTableModelsChangeConstructionItems = _detailTableViewModel.DetailTableModels.Where( c => c.RouteName == detailTableModel.RouteName ).ToList() ;
      var detailTableModelSameGroupId = _detailTableViewModel.DetailTableModels.Where( c => ! string.IsNullOrEmpty( c.GroupId ) && c.GroupId == detailTableModel.GroupId && c.RouteName != detailTableModel.RouteName ).ToList() ;
      if ( detailTableModelSameGroupId.Any() ) {
        var routeSameGroupId = detailTableModelSameGroupId.Select( d => d.RouteName ).Distinct().ToList() ;
        detailTableModelsChangeConstructionItems.AddRange( _detailTableViewModel.DetailTableModels.Where( c => routeSameGroupId.Contains( c.RouteName ) ).ToList() ) ;
      }
      List<DetailTableModel> newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var oldDetailTableModel in _detailTableViewModel.DetailTableModels ) {
        if ( detailTableModelsChangeConstructionItems.Contains( oldDetailTableModel ) ) {
          var newDetailSymbolModel = new DetailTableModel( oldDetailTableModel.CalculationExclusion, oldDetailTableModel.Floor, oldDetailTableModel.CeeDCode, oldDetailTableModel.DetailSymbol, oldDetailTableModel.DetailSymbolId, oldDetailTableModel.WireType, oldDetailTableModel.WireSize, oldDetailTableModel.WireStrip, oldDetailTableModel.WireBook, oldDetailTableModel.EarthType, oldDetailTableModel.EarthSize, oldDetailTableModel.NumberOfGrounds, oldDetailTableModel.PlumbingType, oldDetailTableModel.PlumbingSize, oldDetailTableModel.NumberOfPlumbing, oldDetailTableModel.ConstructionClassification, oldDetailTableModel.SignalType, constructionItem!.ToString(), constructionItem!.ToString(), oldDetailTableModel.Remark, oldDetailTableModel.WireCrossSectionalArea, oldDetailTableModel.CountCableSamePosition, oldDetailTableModel.RouteName, oldDetailTableModel.IsEcoMode, oldDetailTableModel.IsParentRoute, oldDetailTableModel.IsReadOnly, oldDetailTableModel.ParentPlumbingType, oldDetailTableModel.GroupId ) ;
          newDetailTableModels.Add( newDetailSymbolModel ) ;
        }
        else {
          newDetailTableModels.Add( oldDetailTableModel ) ;
        }
      }

      var routeChangeConstructionItem = detailTableModelsChangeConstructionItems.Select( d => d.RouteName ).Distinct().ToList() ;
      UnGroupDetailTableModelAfterChangeConstructionItems( ref newDetailTableModels, routeChangeConstructionItem, constructionItem!.ToString() ) ;
      foreach ( var routeName in routeChangeConstructionItem ) {
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

    private void UnGroupDetailTableModelAfterChangeConstructionItems( ref List<DetailTableModel> detailTableModels, List<string> routeNames, string constructionItems )
    {
      var groupIdOfDetailTableModelsChangeConstructionItems = detailTableModels.Where( d => routeNames.Contains( d.RouteName ) && ! string.IsNullOrEmpty( d.GroupId ) ).Select( d => d.GroupId ).Distinct().ToList() ;
      foreach ( var groupId in groupIdOfDetailTableModelsChangeConstructionItems ) {
        var detailTableModelSameGroupId = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems != constructionItems ).ToList() ;
        var detailTableModelsChangeConstructionItems = detailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId && d.ConstructionItems == constructionItems ).ToList() ;
        if ( detailTableModelSameGroupId.Any() ) {
          if ( detailTableModelSameGroupId.Count == 1 ) 
            detailTableModelSameGroupId.First().GroupId = string.Empty ;
          if ( detailTableModelsChangeConstructionItems.Count == 1 ) 
            detailTableModelsChangeConstructionItems.First().GroupId = string.Empty ;
        }

        if ( detailTableModelsChangeConstructionItems.Count <= 1 ) continue ;
        foreach ( var detailTableModel in detailTableModelsChangeConstructionItems ) {
          var newGroupId = detailTableModel.DetailSymbolId + "-" + detailTableModel.ParentPlumbingType + "-" + detailTableModel.ConstructionItems + "-" + detailTableModel.WireType + detailTableModel.WireSize + detailTableModel.WireStrip ;
          detailTableModel.GroupId = newGroupId ;
        }
      }
    }

    private void SaveData( IReadOnlyCollection<DetailTableModel> detailTableModels )
    {
      try {
        DetailTableStorable detailTableStorable = _document.GetDetailTableStorable() ;
        {
          if ( ! detailTableModels.Any() ) return ;
          var newDetailSymbolId = detailTableModels.ToList().Select( d => d.DetailSymbolId ).ToList() ;
          var oldDetailTableModels = detailTableStorable.DetailTableModelData.Where( d => ! newDetailSymbolId.Contains( d.DetailSymbolId ) ).ToList() ;
          detailTableStorable.DetailTableModelData = detailTableModels.ToList() ;
          detailTableStorable.DetailTableModelData.AddRange( oldDetailTableModels ) ;
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
      foreach ( var (_, detailTableModelsSameDetailSymbolId) in detailTableModelsGroupByDetailSymbolId ) {
        SetGroupId( detailTableModelsSameDetailSymbolId ) ;
      }

      CreateDetailTableViewModelByGroupId() ;
      SaveData( _detailTableViewModel.DetailTableModels ) ;
    }

    private void SetGroupId( List<DetailTableModel> detailTableModelsGroupByDetailSymbolId )
    {
      var detailTableModelsGroupByPlumbingType = detailTableModelsGroupByDetailSymbolId.GroupBy( d => d.ParentPlumbingType ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      foreach ( var (_, detailTableModelsSamePlumbingType) in detailTableModelsGroupByPlumbingType ) {
        var detailTableModelsGroupByConstructionItem = detailTableModelsSamePlumbingType.GroupBy( d => d.ConstructionItems ).ToDictionary( g => g.Key, g => g.ToList() ) ;
        foreach ( var (_, detailTableModelsSameConstructionItem) in detailTableModelsGroupByConstructionItem ) {
          var detailTableModelsGroupByWiringType = detailTableModelsSameConstructionItem.GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) ).ToDictionary( g => g.Key.WireType + g.Key.WireSize + "x" + g.Key.WireStrip, g => g.ToList() ) ;
          foreach ( var (_, detailTableModelsSameWiringType) in detailTableModelsGroupByWiringType ) {
            var oldDetailTableModel = detailTableModelsSameWiringType.FirstOrDefault() ;
            if ( oldDetailTableModel == null ) continue ;
            if ( detailTableModelsSameWiringType.Count == 1 ) {
              oldDetailTableModel.GroupId = string.Empty ;
            }
            else {
              var groupId = oldDetailTableModel.DetailSymbolId + "-" + oldDetailTableModel.ParentPlumbingType + "-" + oldDetailTableModel.ConstructionItems + "-" + oldDetailTableModel.WireType + oldDetailTableModel.WireSize + oldDetailTableModel.WireStrip ;
              foreach ( var detailTableModelSameWiringType in detailTableModelsSameWiringType ) {
                detailTableModelSameWiringType.GroupId = groupId ;
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
      foreach ( var detailTableModel in _detailTableViewModel.DetailTableModels ) {
        if ( string.IsNullOrEmpty( detailTableModel.GroupId ) ) {
          newDetailTableModels.Add( detailTableModel ) ;
        }
        else {
          if ( groupIds.Contains( detailTableModel.GroupId ) ) continue ;
          var detailTableModelsSameGroupId = _detailTableViewModel.DetailTableModels.Where( d => d.GroupId == detailTableModel.GroupId ) ;
          var detailTableModelsGroupByRemark = detailTableModelsSameGroupId.GroupBy( d => d.Remark ).ToDictionary( g => g.Key, g => g.ToList() ) ;
          List<string> newRemark = new List<string>() ;
          var numberOfGrounds = 0 ;
          foreach ( var (remark, detailTableModelsSameRemark) in detailTableModelsGroupByRemark ) {
            newRemark.Add( remark + ( detailTableModelsSameRemark.Count == 1 ? string.Empty : "x" + detailTableModelsSameRemark.Count ) ) ;
            numberOfGrounds += detailTableModelsSameRemark.Count == 1 ? 1 : detailTableModelsSameRemark.Count ;
          }

          var newDetailSymbolModel = new DetailTableModel( detailTableModel.CalculationExclusion, detailTableModel.Floor, detailTableModel.CeeDCode, detailTableModel.DetailSymbol, detailTableModel.DetailSymbolId, detailTableModel.WireType, detailTableModel.WireSize, detailTableModel.WireStrip, numberOfGrounds.ToString(), detailTableModel.EarthType, detailTableModel.EarthSize, detailTableModel.NumberOfGrounds, detailTableModel.PlumbingType, detailTableModel.PlumbingSize, detailTableModel.NumberOfPlumbing, detailTableModel.ConstructionClassification, detailTableModel.SignalType, detailTableModel.ConstructionItems, detailTableModel.PlumbingItems, string.Join( ", ", newRemark ), detailTableModel.WireCrossSectionalArea, detailTableModel.CountCableSamePosition, detailTableModel.RouteName, detailTableModel.IsEcoMode, detailTableModel.IsParentRoute, detailTableModel.IsReadOnly, detailTableModel.ParentPlumbingType, detailTableModel.GroupId ) ;
          newDetailTableModels.Add( newDetailSymbolModel ) ;
          groupIds.Add( detailTableModel.GroupId ) ;
        }
      }

      DetailTableViewModel newDetailTableViewModel = new DetailTableViewModel( new ObservableCollection<DetailTableModel>( newDetailTableModels ), _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems ) ;
      this.DataContext = newDetailTableViewModel ;
      DtGrid.ItemsSource = newDetailTableViewModel.DetailTableModels ;
      DetailTableViewModelSummary = newDetailTableViewModel ;
    }
    
    private void UnGroupDetailTableModel( string groupId )
    {
      var detailTableModels = _detailTableViewModel.DetailTableModels.Where( d => ! string.IsNullOrEmpty( d.GroupId ) && d.GroupId == groupId ).ToList() ;
      foreach ( var detailTableModel in detailTableModels ) {
        detailTableModel.GroupId = string.Empty ;
      }
    }
  }
}