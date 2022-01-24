using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ConduitInformationDialog : Window
  {
    private readonly List<ConduitsModel> _conduitsModelData ;
    private readonly ConduitInformationViewModel _conduitInformationViewModel ;
    public readonly Dictionary<string, string> RoutesChangedConstructionItem ;

    public ConduitInformationDialog( ConduitInformationViewModel viewModel, List<ConduitsModel> conduitsModelData )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      _conduitInformationViewModel = viewModel ;
      _conduitsModelData = conduitsModelData ;
      RoutesChangedConstructionItem = new Dictionary<string, string>() ;
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      this.Close() ;
    }

    private void BtnCompleted_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      this.Close() ;
    }

    private void ConduitTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var pipingType = comboBox.SelectedValue ;
      if ( pipingType == null ) return ;
      if ( DtGrid.SelectedItem is not ConduitInformationModel conduitInformationModel || conduitInformationModel.PipingType == pipingType!.ToString() ) return ;
      if ( pipingType!.ToString() == "↑" ) {
        comboBox.SelectedValue = conduitInformationModel.PipingType ;
      }
      else {
        Dictionary<string, int> pipingData = CreateDetailTableCommandBase.GetPipingData( _conduitsModelData, pipingType!.ToString(), conduitInformationModel.PipingCrossSectionalArea ) ;

        List<ConduitInformationModel> newConduitInformationModels = new List<ConduitInformationModel>() ;
        foreach ( var (pipingSize, numberOfPipes) in pipingData ) {
          var parentConduitInformationModel = new ConduitInformationModel( conduitInformationModel.CalculationExclusion, conduitInformationModel.Floor, conduitInformationModel.CeeDCode, conduitInformationModel.DetailSymbol, conduitInformationModel.DetailSymbolId, conduitInformationModel.WireType, conduitInformationModel.WireSize, conduitInformationModel.WireStrip, conduitInformationModel.WireBook, conduitInformationModel.EarthType, conduitInformationModel.EarthSize, conduitInformationModel.NumberOfGrounds, pipingType!.ToString(), pipingSize.Replace( "mm", string.Empty ), numberOfPipes.ToString(), conduitInformationModel.ConstructionClassification, conduitInformationModel.Classification, conduitInformationModel.ConstructionItems, conduitInformationModel.ConstructionItems, conduitInformationModel.Remark, conduitInformationModel.PipingCrossSectionalArea, conduitInformationModel.CountCableSamePosition, conduitInformationModel.RouteName, false ) ;
          newConduitInformationModels.Add( parentConduitInformationModel ) ;
        }

        List<ConduitInformationModel> oldConduitInformationModels = _conduitInformationViewModel.ConduitInformationModels.Where( c => c.DetailSymbol == conduitInformationModel.DetailSymbol && c.CeeDCode == conduitInformationModel.CeeDCode && Math.Abs( c.PipingCrossSectionalArea - conduitInformationModel.PipingCrossSectionalArea ) == 0 ).ToList() ;
        foreach ( var oldConduitInformationModel in oldConduitInformationModels ) {
          _conduitInformationViewModel.ConduitInformationModels.Remove( oldConduitInformationModel ) ;
        }

        foreach ( var newConduitInformationModel in newConduitInformationModels ) {
          _conduitInformationViewModel.ConduitInformationModels.Add( newConduitInformationModel ) ;
        }

        newConduitInformationModels = _conduitInformationViewModel.ConduitInformationModels.OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.CountCableSamePosition ).ThenByDescending( x => x.PipingSize ).GroupBy( x => x.DetailSymbolId ).SelectMany( x => x ).ToList() ;
        _conduitInformationViewModel.ConduitInformationModels = new ObservableCollection<ConduitInformationModel>( newConduitInformationModels ) ;
        this.DataContext = _conduitInformationViewModel ;
        DtGrid.ItemsSource = _conduitInformationViewModel.ConduitInformationModels ;
      }
    }

    private void ConstructionItemSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      var constructionItem = comboBox.SelectedValue ;
      if ( constructionItem == null ) return ;
      if ( DtGrid.SelectedItem is not ConduitInformationModel conduitInformationModel || conduitInformationModel.ConstructionItems == constructionItem!.ToString() ) return ;
      var conduitInformationModelsSameRoute = _conduitInformationViewModel.ConduitInformationModels.Where( c => c.RouteName == conduitInformationModel.RouteName ).ToList() ;
      foreach ( var conduitInformationModelSameRoute in conduitInformationModelsSameRoute ) {
        var newConduitInformationModel = new ConduitInformationModel( conduitInformationModelSameRoute.CalculationExclusion, conduitInformationModelSameRoute.Floor, conduitInformationModelSameRoute.CeeDCode, conduitInformationModelSameRoute.DetailSymbol, conduitInformationModelSameRoute.DetailSymbolId, conduitInformationModelSameRoute.WireType, conduitInformationModelSameRoute.WireSize, conduitInformationModelSameRoute.WireStrip, conduitInformationModelSameRoute.WireBook, conduitInformationModelSameRoute.EarthType, conduitInformationModelSameRoute.EarthSize, conduitInformationModelSameRoute.NumberOfGrounds, conduitInformationModelSameRoute.PipingType, conduitInformationModelSameRoute.PipingSize, conduitInformationModelSameRoute.NumberOfPipes, conduitInformationModelSameRoute.ConstructionClassification, conduitInformationModelSameRoute.Classification, constructionItem!.ToString(), constructionItem!.ToString(), conduitInformationModelSameRoute.Remark, conduitInformationModelSameRoute.PipingCrossSectionalArea, conduitInformationModelSameRoute.CountCableSamePosition, conduitInformationModelSameRoute.RouteName, conduitInformationModelSameRoute.IsReadOnly ) ;
        _conduitInformationViewModel.ConduitInformationModels.Add( newConduitInformationModel ) ;
      }

      if ( ! RoutesChangedConstructionItem.ContainsKey( conduitInformationModel.RouteName! ) ) {
        RoutesChangedConstructionItem.Add( conduitInformationModel.RouteName!, constructionItem!.ToString() ) ;
      }
      else {
        RoutesChangedConstructionItem[ conduitInformationModel.RouteName! ] = constructionItem!.ToString() ;
      }

      foreach ( var conduitInformationModelSameRoute in conduitInformationModelsSameRoute ) {
        _conduitInformationViewModel.ConduitInformationModels.Remove( conduitInformationModelSameRoute ) ;
      }

      var newConduitInformationModels = _conduitInformationViewModel.ConduitInformationModels.OrderBy( x => x.DetailSymbol ).ThenByDescending( x => x.CountCableSamePosition ).ThenByDescending( x => x.PipingSize ).GroupBy( x => x.DetailSymbolId ).SelectMany( x => x ).ToList() ;
      _conduitInformationViewModel.ConduitInformationModels = new ObservableCollection<ConduitInformationModel>( newConduitInformationModels ) ;
      this.DataContext = _conduitInformationViewModel ;
      DtGrid.ItemsSource = _conduitInformationViewModel.ConduitInformationModels ;
    }
  }
}