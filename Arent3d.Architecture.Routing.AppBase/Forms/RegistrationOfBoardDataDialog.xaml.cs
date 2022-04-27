using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using DataGridCell = System.Windows.Controls.DataGridCell ;
using KeyEventArgs = System.Windows.Input.KeyEventArgs ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class RegistrationOfBoardDataDialog
  {
    private RegistrationOfBoardDataViewModel ViewModel => (RegistrationOfBoardDataViewModel)DataContext ;
    public RegistrationOfBoardDataDialog( RegistrationOfBoardDataViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      Style cellStyle = new( typeof( DataGridCell ) ) ;
      cellStyle.Setters.Add( new EventSetter( MouseDoubleClickEvent, new MouseButtonEventHandler( Cell_DoubleClick ) ) ) ;
      DtGrid.CellStyle = cellStyle ;
    }

    private void CmbAutoControlPanel_KeyDown( object sender, KeyEventArgs e )
    {
      if ( e.Key == Key.Enter ) {
        ViewModel.SearchCommand.Execute( null );
      }
    }

    private void CmbSignalDestination_KeyDown( object sender, KeyEventArgs e )
    {
      if ( e.Key == Key.Enter ) {
        ViewModel.SearchCommand.Execute( null );
      }
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      SaveBoardModelDisplayAndOnlyUsingCodeState() ;
      DialogResult = true ;
      Close() ;
    }

    private void SaveBoardModelDisplayAndOnlyUsingCodeState()
    {
      ViewModel.Save();
    }

    private void Cell_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var dataGridCellTarget = (DataGridCell) sender ;
      var dataContext = (RegistrationOfBoardDataModel) dataGridCellTarget.DataContext ;
      var textBlock = dataGridCellTarget.Content as TextBlock ;
      var cellValue = textBlock?.Text ;
      var column = dataGridCellTarget.Column ;
      var columnIndex = column?.DisplayIndex ;
      
      if ( string.IsNullOrEmpty( cellValue ) ) return ;
      ViewModel.CellSelectedAutoControlPanel = dataContext?.AutoControlPanel ?? string.Empty ;
      ViewModel.CellSelectedSignalDestination = dataContext?.SignalDestination ?? string.Empty ;
      if ( columnIndex == 0 ) ViewModel.IsFromPowerConnector = true ;
      if ( columnIndex == 1 ) ViewModel.IsFromPowerConnector = false ;
      SaveBoardModelDisplayAndOnlyUsingCodeState() ;
      DialogResult = true ;
      Close() ;
    }
  }

  // ReSharper disable once ClassNeverInstantiated.Global
  public class DesignRegistrationOfBoardDataViewModel : RegistrationOfBoardDataViewModel
  {
    public DesignRegistrationOfBoardDataViewModel() : base( default! )
    {
    }
  }
}