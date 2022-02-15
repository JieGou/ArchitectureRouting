using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
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
    public readonly Dictionary<string, string> RoutesChangedConstructionItem ;
    public readonly Dictionary<string, string> DetailSymbolsChangedPlumbingType ;

    public DetailTableDialog( Document document, DetailTableViewModel viewModel, List<ConduitsModel> conduitsModelData )
    {
      InitializeComponent() ;
      _document = document ;
      DataContext = viewModel ;
      _detailTableViewModel = viewModel ;
      _conduitsModelData = conduitsModelData ;
      RoutesChangedConstructionItem = new Dictionary<string, string>() ;
      DetailSymbolsChangedPlumbingType = new Dictionary<string, string>() ;
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
        this.DataContext = _detailTableViewModel ;
        DtGrid.ItemsSource = _detailTableViewModel.DetailTableModels ;
        SaveData( _detailTableViewModel.DetailTableModels ) ;
      }
    }

    private void ConstructionItemSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var constructionItem = comboBox.SelectedValue ;
      if ( constructionItem == null ) return ;
      if ( DtGrid.SelectedItem is not DetailTableModel detailTableModel || detailTableModel.ConstructionItems == constructionItem!.ToString() ) return ;
      var detailTableModelsSameRoute = _detailTableViewModel.DetailTableModels.Where( c => c.RouteName == detailTableModel.RouteName ).ToList() ;
      List<DetailTableModel> newDetailTableModels = new List<DetailTableModel>() ;
      foreach ( var oldDetailTableModel in _detailTableViewModel.DetailTableModels ) {
        if ( detailTableModelsSameRoute.Contains( oldDetailTableModel ) ) {
          var newDetailSymbolModel = new DetailTableModel( oldDetailTableModel.CalculationExclusion, oldDetailTableModel.Floor, oldDetailTableModel.CeeDCode, oldDetailTableModel.DetailSymbol, oldDetailTableModel.DetailSymbolId, oldDetailTableModel.WireType, oldDetailTableModel.WireSize, oldDetailTableModel.WireStrip, oldDetailTableModel.WireBook, oldDetailTableModel.EarthType, oldDetailTableModel.EarthSize, oldDetailTableModel.NumberOfGrounds, oldDetailTableModel.PlumbingType, oldDetailTableModel.PlumbingSize, oldDetailTableModel.NumberOfPlumbing, oldDetailTableModel.ConstructionClassification, oldDetailTableModel.SignalType, constructionItem!.ToString(), constructionItem!.ToString(), oldDetailTableModel.Remark, oldDetailTableModel.WireCrossSectionalArea, oldDetailTableModel.CountCableSamePosition, oldDetailTableModel.RouteName, oldDetailTableModel.IsEcoMode, oldDetailTableModel.IsParentRoute, oldDetailTableModel.IsReadOnly, oldDetailTableModel.ParentPlumbingType ) ;
          newDetailTableModels.Add( newDetailSymbolModel ) ;
        }
        else {
          newDetailTableModels.Add( oldDetailTableModel ) ;
        }
      }

      if ( ! RoutesChangedConstructionItem.ContainsKey( detailTableModel.RouteName! ) ) {
        RoutesChangedConstructionItem.Add( detailTableModel.RouteName!, constructionItem!.ToString() ) ;
      }
      else {
        RoutesChangedConstructionItem[ detailTableModel.RouteName! ] = constructionItem!.ToString() ;
      }

      _detailTableViewModel.DetailTableModels = new ObservableCollection<DetailTableModel>( newDetailTableModels ) ;
      this.DataContext = _detailTableViewModel ;
      DtGrid.ItemsSource = _detailTableViewModel.DetailTableModels ;
      SaveData( _detailTableViewModel.DetailTableModels ) ;
    }

    private void SaveData( IReadOnlyCollection<DetailTableModel> detailTableModels )
    {
      try {
        DetailTableStorable detailTableStorable = _document.GetDetailTableStorable() ;
        {
          if ( ! detailTableModels.Any() ) return ;
          detailTableStorable.DetailTableModelData = detailTableModels.ToList() ;
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
      List<DetailTableModel> newDetailTableModels = new List<DetailTableModel>() ;
      var detailTableModelsGroupByDetailSymbolId = _detailTableViewModel.DetailTableModels.ToList().GroupBy( d => d.DetailSymbolId ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      foreach ( var (_, detailTableModelsSameDetailSymbolId) in detailTableModelsGroupByDetailSymbolId ) {
        var detailTableModelsGroupByPlumbingType = detailTableModelsSameDetailSymbolId.GroupBy( d => d.ParentPlumbingType ).ToDictionary( g => g.Key, g => g.ToList() ) ;
        foreach ( var (_, detailTableModelsSamePlumbingType) in detailTableModelsGroupByPlumbingType ) {
          var detailTableModelsGroupByConstructionItem = detailTableModelsSamePlumbingType.GroupBy( d => d.ConstructionItems ).ToDictionary( g => g.Key, g => g.ToList() ) ;
          foreach ( var (_, detailTableModelsSameConstructionItem) in detailTableModelsGroupByConstructionItem ) {
            var detailTableModelsGroupByWiringType = detailTableModelsSameConstructionItem.GroupBy( d => ( d.WireType, d.WireSize, d.WireStrip ) ).ToDictionary( g => g.Key.WireType + g.Key.WireSize + "x" + g.Key.WireStrip, g => g.ToList() ) ;
            foreach ( var (_, detailTableModelsSameWiringType) in detailTableModelsGroupByWiringType ) {
              var oldDetailTableModel = detailTableModelsSameWiringType.FirstOrDefault() ;
              if ( oldDetailTableModel == null ) continue ;
              var detailTableModelsGroupByRemark = detailTableModelsSameWiringType.GroupBy( d => d.Remark ).ToDictionary( g => g.Key, g => g.ToList() ) ;
              List<string> newRemark = new List<string>() ;
              int numberOfGrounds = 0 ;
              foreach ( var (remark, detailTableModelsSameRemark) in detailTableModelsGroupByRemark ) {
                newRemark.Add( remark + ( detailTableModelsSameRemark.Count == 1 ? string.Empty : "x" + detailTableModelsSameRemark.Count ) ) ;
                numberOfGrounds += detailTableModelsSameRemark.Count == 1 ? 1 : detailTableModelsSameRemark.Count ;
              }

              var newDetailSymbolModel = new DetailTableModel( oldDetailTableModel.CalculationExclusion, oldDetailTableModel.Floor, oldDetailTableModel.CeeDCode, oldDetailTableModel.DetailSymbol, oldDetailTableModel.DetailSymbolId, oldDetailTableModel.WireType, oldDetailTableModel.WireSize, oldDetailTableModel.WireStrip, numberOfGrounds.ToString(), oldDetailTableModel.EarthType, 
                oldDetailTableModel.EarthSize, oldDetailTableModel.NumberOfGrounds, oldDetailTableModel.PlumbingType, oldDetailTableModel.PlumbingSize, oldDetailTableModel.NumberOfPlumbing, oldDetailTableModel.ConstructionClassification, oldDetailTableModel.SignalType, oldDetailTableModel.ConstructionItems, oldDetailTableModel.PlumbingItems, string.Join( ", ", newRemark ), oldDetailTableModel.WireCrossSectionalArea, oldDetailTableModel.CountCableSamePosition, oldDetailTableModel.RouteName, oldDetailTableModel.IsEcoMode, oldDetailTableModel.IsParentRoute, oldDetailTableModel.IsReadOnly, oldDetailTableModel.ParentPlumbingType ) ;
              newDetailTableModels.Add( newDetailSymbolModel ) ;
            }
          }
        }
      }

      DetailTableViewModel newDetailTableViewModel = new DetailTableViewModel( new ObservableCollection<DetailTableModel>( newDetailTableModels ), _detailTableViewModel.ConduitTypes, _detailTableViewModel.ConstructionItems ) ;
      this.DataContext = newDetailTableViewModel ;
      DtGrid.ItemsSource = newDetailTableViewModel.DetailTableModels ;
    }
  }
}