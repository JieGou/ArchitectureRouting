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

    public ConduitInformationDialog( ConduitInformationViewModel viewModel, List<ConduitsModel> conduitsModelData )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      _conduitInformationViewModel = viewModel ;
      _conduitsModelData = conduitsModelData ;
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      this.Close() ;
    }

    private void BtnCompleted_OnClick( object sender, RoutedEventArgs e )
    {
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
        Dictionary<string, int> pipingData = ShowConduitInformationCommandBase.GetPipingData( _conduitsModelData, pipingType!.ToString(), double.Parse( conduitInformationModel.PipingCrossSectionalArea! ) ) ;

        List<ConduitInformationModel> newConduitInformationModels = new List<ConduitInformationModel>() ;
        foreach ( var (pipingSize, numberOfPipes) in pipingData ) {
          var parentConduitInformationModel = new ConduitInformationModel( conduitInformationModel.CalculationExclusion, conduitInformationModel.Floor, conduitInformationModel.CeeDCode, conduitInformationModel.DetailSymbol, conduitInformationModel.WireType, conduitInformationModel.WireSize, conduitInformationModel.WireStrip, conduitInformationModel.WireBook, conduitInformationModel.EarthType, conduitInformationModel.EarthSize, conduitInformationModel.NumberOfGrounds, pipingType!.ToString(), pipingSize.Replace( "mm", string.Empty ), numberOfPipes.ToString(), conduitInformationModel.ConstructionClassification, conduitInformationModel.Classification, conduitInformationModel.ConstructionItems, conduitInformationModel.ConstructionItems, conduitInformationModel.Remark, conduitInformationModel.PipingCrossSectionalArea, conduitInformationModel.CountCableSamePosition, false ) ;
          newConduitInformationModels.Add( parentConduitInformationModel ) ;
        }

        List<ConduitInformationModel> oldConduitInformationModels = _conduitInformationViewModel.ConduitInformationModels.Where( c => c.DetailSymbol == conduitInformationModel.DetailSymbol && c.CeeDCode == conduitInformationModel.CeeDCode && c.PipingCrossSectionalArea == conduitInformationModel.PipingCrossSectionalArea ).ToList() ;
        foreach ( var oldConduitInformationModel in oldConduitInformationModels ) {
          _conduitInformationViewModel.ConduitInformationModels.Remove( oldConduitInformationModel ) ;
        }

        foreach ( var newConduitInformationModel in newConduitInformationModels ) {
          _conduitInformationViewModel.ConduitInformationModels.Add( newConduitInformationModel ) ;
        }

        newConduitInformationModels = _conduitInformationViewModel.ConduitInformationModels.OrderBy( x => x.DetailSymbol ).ThenByDescending( y=>y.CountCableSamePosition ).ThenByDescending( y=>y.PipingSize ).ToList();
        _conduitInformationViewModel.ConduitInformationModels = new ObservableCollection<ConduitInformationModel>( newConduitInformationModels ) ;
        this.DataContext = _conduitInformationViewModel ;
        DtGrid.ItemsSource = _conduitInformationViewModel.ConduitInformationModels ;
      }
    }
  }
}