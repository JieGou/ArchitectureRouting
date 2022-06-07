using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using KeyEventArgs = System.Windows.Input.KeyEventArgs ;
using MessageBox = System.Windows.MessageBox ;
using Style = System.Windows.Style ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeedModelDialog
  {
    private CeedViewModel ViewModel => (CeedViewModel) DataContext ;

    public CeedModelDialog( CeedViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      BtnReplaceSymbol.IsEnabled = false ;
      Style rowStyle = new( typeof( DataGridRow ) ) ;
      rowStyle.Setters.Add( new EventSetter( MouseDoubleClickEvent, new MouseButtonEventHandler( Row_DoubleClick ) ) ) ;
      rowStyle.Setters.Add( new EventSetter( MouseLeftButtonUpEvent, new MouseButtonEventHandler( Row_MouseLeftButtonUp ) ) ) ;
      DtGrid.RowStyle = rowStyle ;
      ViewModel.DtGrid = DtGrid ;
      if ( ViewModel.IsExistUsingCode ) {
        CbShowOnlyUsingCode.Visibility = Visibility.Visible ;
      }
    }

    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      ViewModel.Load( CbShowOnlyUsingCode ) ;
      if ( CbShowDiff.IsChecked == false ) {
        CbShowDiff.IsChecked = true ;
      }

      BtnReplaceSymbol.IsEnabled = false ;
    }

    private void CmbKeyUp( object sender, KeyEventArgs e )
    {
      var comboBox = (ComboBox) sender ;
      if ( e.Key == Key.Enter ) {
        ViewModel.Search() ;
      }
      else {
        comboBox.IsDropDownOpen = true ;
      }
    }

    private void ShowCeedModelNumberColumn_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.ShowCeedModelNumberColumn( LbCeedModelNumbers, CmbCeedModelNumbers ) ;
    }

    private void ShowCeedModelNumberColumn_UnChecked( object sender, RoutedEventArgs e )
    {
      ViewModel.UnShowCeedModelNumberColumn( LbCeedModelNumbers, CmbCeedModelNumbers ) ;
    }

    private void Button_SymbolRegistration( object sender, RoutedEventArgs e )
    {
      ViewModel.LoadUsingCeedModel( CbShowOnlyUsingCode ) ;
    }

    private void ShowOnlyUsingCode_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.ShowOnlyUsingCode() ;
    }

    private void ShowOnlyUsingCode_UnChecked( object sender, RoutedEventArgs e )
    {
      ViewModel.UnShowOnlyUsingCode() ;
    }

    private void Row_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
    {
      BtnReplaceSymbol.IsEnabled = false ;
      if ( ( (DataGridRow) sender ).DataContext is not CeedModel ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return ;
      }

      ViewModel.SelectedCeedModel = ( (DataGridRow) sender ).DataContext as CeedModel ;
      BtnReplaceSymbol.IsEnabled = true ;
      // ViewModel.PreviewList.Clear() ;
      // ViewModel.PreviewList.Add( ViewModel.SelectedCeedModel! ) ;
    }

    private void Row_DoubleClick( object sender, MouseButtonEventArgs e )
    {
      var selectedItem = (CeedModel) DtGrid.SelectedValue ;
      ViewModel.SelectedDeviceSymbol = selectedItem.GeneralDisplayDeviceSymbol ;
      ViewModel.SelectedCondition = selectedItem.Condition ;
      ViewModel.SelectedCeedCode = selectedItem.CeedSetCode ;
      ViewModel.SelectedModelNum = selectedItem.ModelNumber ;
      ViewModel.SelectedFloorPlanType = selectedItem.FloorPlanType ;
      if ( string.IsNullOrEmpty( ViewModel.SelectedDeviceSymbol ) ) return ;
      ViewModel.Save() ;
      DialogResult = true ;
      Close() ;
    }

    private void Button_ReplaceSymbol( object sender, RoutedEventArgs e )
    {
      ViewModel.ReplaceSymbol( DtGrid, BtnReplaceSymbol ) ;
    }

    private void Button_ReplaceMultipleSymbols( object sender, RoutedEventArgs e )
    {
      ViewModel.ReplaceMultipleSymbols( DtGrid ) ;
    }
  }

  // ReSharper disable once ClassNeverInstantiated.Global
  public class DesignCeedViewModel : CeedViewModel
  {
    public DesignCeedViewModel() : base( default! )
    {
    }
  }
}