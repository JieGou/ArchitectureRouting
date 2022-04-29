using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class RegistrationOfBoardDataViewModel : NotifyPropertyChanged
  {
    private readonly Document _document ;
    private List<RegistrationOfBoardDataModel> _registrationOfBoardDataModels ;
    public ObservableCollection<RegistrationOfBoardDataModel> RegistrationOfBoardDataModels { get ; }
    private RegistrationOfBoardDataStorable? RegistrationOfBoardDataStorable { get ; set ; }
    public ObservableCollection<string> AutoControlPanels { get ; } = new() ;

    public int SelectedAutoControlPanelIndex { get ; set ; } = -1 ;
    public string? SelectedAutoControlPanel =>
      0 <= SelectedAutoControlPanelIndex ? AutoControlPanels[ SelectedAutoControlPanelIndex ] : null ;

    public ObservableCollection<string> SignalDestinations { get ; } = new() ;
    public int SelectedSignalDestinationIndex { get ; set ; } = -1 ;

    public string? SelectedSignalDestination => 
      0 <= SelectedSignalDestinationIndex ? SignalDestinations[ SelectedSignalDestinationIndex ] : null ;

    public bool IsFromPowerConnector { get ; set ; }
    
    public string? CellSelectedAutoControlPanel { get ; set ; } 
    
    public string? CellSelectedSignalDestination { get ; set ; } 

    public ICommand SearchCommand => new RelayCommand( Search ) ;
    public ICommand LoadCommand => new RelayCommand( Load ) ;
    public ICommand ResetCommand => new RelayCommand( Reset ) ;

    public RegistrationOfBoardDataViewModel( Document document )
    {
      _document = document ;
      var oldRegistrationOfBoardDataStorable = _document.GetAllStorables<RegistrationOfBoardDataStorable>().FirstOrDefault() ;
      if ( oldRegistrationOfBoardDataStorable is null ) {
        _registrationOfBoardDataModels = new() ;
        RegistrationOfBoardDataModels = new() ;
      }
      else {
        RegistrationOfBoardDataStorable = oldRegistrationOfBoardDataStorable ;
        _registrationOfBoardDataModels = oldRegistrationOfBoardDataStorable.RegistrationOfBoardData ;
        RegistrationOfBoardDataModels = new(_registrationOfBoardDataModels) ;
        AddAutoSignal( RegistrationOfBoardDataModels ) ;
      }
    }

    private void Load()
    {
      MessageBox.Show( "Please select 盤間配線確認表 file.", "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
      if ( openFileDialog.ShowDialog() != DialogResult.OK ) {
        return ;
      }

      RegistrationOfBoardDataStorable = _document.GetRegistrationOfBoardDataStorable() ;
      var registrationOfBoardDataModelData = ExcelToModelConverter.GetAllRegistrationOfBoardDataModel( openFileDialog.FileName ) ;
      if ( ! registrationOfBoardDataModelData.Any() ) return ;

      RegistrationOfBoardDataStorable.RegistrationOfBoardData = registrationOfBoardDataModelData ;
      _registrationOfBoardDataModels = registrationOfBoardDataModelData ;
      RegistrationOfBoardDataModels.Clear();
      foreach ( var dataModel in _registrationOfBoardDataModels ) {
        RegistrationOfBoardDataModels.Add( dataModel );
      }
    }

    private void AddAutoSignal( IReadOnlyCollection<RegistrationOfBoardDataModel> registrationOfBoardDataModels )
    {
      foreach ( var registrationOfBoardDataModel in registrationOfBoardDataModels.Where( registrationOfBoardDataModel => ! string.IsNullOrEmpty( registrationOfBoardDataModel.AutoControlPanel ) ) ) {
        if ( ! AutoControlPanels.Contains( registrationOfBoardDataModel.AutoControlPanel ) ) AutoControlPanels.Add( registrationOfBoardDataModel.AutoControlPanel ) ;
      }

      foreach ( var registrationOfBoardDataModel in registrationOfBoardDataModels.Where( registrationOfBoardDataModel => ! string.IsNullOrEmpty( registrationOfBoardDataModel.SignalDestination ) ) ) {
        if ( ! SignalDestinations.Contains( registrationOfBoardDataModel.SignalDestination ) ) SignalDestinations.Add( registrationOfBoardDataModel.SignalDestination ) ;
      }
    }

    private void Search()
    {
      RegistrationOfBoardDataModels.Clear();
      var dataModels = _registrationOfBoardDataModels
        .Where( x => SelectedAutoControlPanel is null || x.AutoControlPanel.Contains( SelectedAutoControlPanel ) )
        .Where( x => SelectedSignalDestination is null || x.SignalDestination.Contains( SelectedSignalDestination ) ) ;
      foreach ( var dataModel in dataModels ) {
        RegistrationOfBoardDataModels.Add( dataModel );
      }
    }

    private void Reset()
    {
      SelectedAutoControlPanelIndex = -1 ;
      SelectedSignalDestinationIndex = -1 ;
      Search();
    }

    public void Save()
    {
      var registrationOfBoardDataStorable = _document.GetRegistrationOfBoardDataStorable() ;
      try {
        using Transaction t = new( _document, "Save data" ) ;
        t.Start() ;
        registrationOfBoardDataStorable.RegistrationOfBoardData = _registrationOfBoardDataModels ;
        registrationOfBoardDataStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
  } 
}