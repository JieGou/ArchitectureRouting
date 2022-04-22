using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class RegistrationOfBoardDataViewModel : ViewModelBase
  {
    public List<RegistrationOfBoardDataModel> RegistrationOfBoardDataModels { get ; }
    public RegistrationOfBoardDataStorable RegistrationOfBoardDataStorable { get ; }
    public readonly List<string> AutoControlPanels = new() ;
    public readonly List<string> SignalDestinations = new() ;

    public RegistrationOfBoardDataViewModel( RegistrationOfBoardDataStorable registrationOfBoardDataStorable )
    {
      RegistrationOfBoardDataStorable = registrationOfBoardDataStorable ;
      RegistrationOfBoardDataModels = registrationOfBoardDataStorable.RegistrationOfBoardData ;
      AddAutoSignal( RegistrationOfBoardDataModels ) ;
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

    public static string GetFilePath()
    {
      MessageBox.Show( "Please select 盤間配線確認表 file.", "Message" ) ;
      OpenFileDialog openFileDialog = new() { Filter = "Csv files (*.xlsx; *.xls)|*.xlsx;*.xls", Multiselect = false } ;
      string filePath = string.Empty ;

      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
         filePath = openFileDialog.FileName ;
      }
      return filePath ;
    }

    public static List<RegistrationOfBoardDataModel> FilterData(RegistrationOfBoardDataViewModel allRegistrationOfBoardDataModels, string autoControlPanelSearch, string signalDestinationSearch)
    {
      if ( string.IsNullOrEmpty( autoControlPanelSearch ) && string.IsNullOrEmpty( signalDestinationSearch ) ) {
       return allRegistrationOfBoardDataModels.RegistrationOfBoardDataModels ;
      }
      else {
        var registrationOfBoardDataModels = new List<RegistrationOfBoardDataModel>() ;
        if ( ! string.IsNullOrEmpty( autoControlPanelSearch ) && ! string.IsNullOrEmpty( signalDestinationSearch ) )
          registrationOfBoardDataModels = allRegistrationOfBoardDataModels.RegistrationOfBoardDataModels.Where( c => c.AutoControlPanel.Contains( autoControlPanelSearch ) && c.SignalDestination.Contains( signalDestinationSearch ) ).ToList() ;
        else if ( ! string.IsNullOrEmpty( autoControlPanelSearch ) && string.IsNullOrEmpty( signalDestinationSearch ) )
          registrationOfBoardDataModels = allRegistrationOfBoardDataModels.RegistrationOfBoardDataModels.Where( c => c.AutoControlPanel.Contains( autoControlPanelSearch ) ).ToList() ;
        else if ( string.IsNullOrEmpty( autoControlPanelSearch ) && ! string.IsNullOrEmpty( signalDestinationSearch ) )
          registrationOfBoardDataModels = allRegistrationOfBoardDataModels.RegistrationOfBoardDataModels.Where( c => c.SignalDestination.Contains( signalDestinationSearch ) ).ToList() ;
        return registrationOfBoardDataModels ;
      }
    }
    
    public static void Save(Document document, RegistrationOfBoardDataViewModel allRegistrationOfBoardDataModels)
    {
      var registrationOfBoardDataStorable = document.GetRegistrationOfBoardDataStorable() ;
      try {
        using Transaction t = new( document, "Save data" ) ;
        t.Start() ;
        registrationOfBoardDataStorable.RegistrationOfBoardData = allRegistrationOfBoardDataModels.RegistrationOfBoardDataModels ;
        registrationOfBoardDataStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
      }
    }
  } 
}