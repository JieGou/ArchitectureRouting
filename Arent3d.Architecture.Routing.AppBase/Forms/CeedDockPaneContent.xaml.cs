using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using KeyEventArgs = System.Windows.Input.KeyEventArgs ;
using MessageBox = System.Windows.MessageBox ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeedDockPaneContent : UserControl
  {
    private CeedViewModel ViewModel => (CeedViewModel) DataContext ;

    public CeedDockPaneContent( CeedViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      BtnReplaceSymbol.IsEnabled = false ;
      if ( ViewModel.IsExistUsingCode ) {
        CbShowOnlyUsingCode.Visibility = Visibility.Visible ;
      }
    }

    public CeedDockPaneContent()
    {
      InitializeComponent() ;
    }

    private void Button_LoadData( object sender, RoutedEventArgs e )
    {
      ViewModel.Load( CbShowOnlyUsingCode ) ;
      if ( CbShowDiff.IsChecked == false ) {
        CbShowDiff.IsChecked = true ;
      }

      BtnReplaceSymbol.IsEnabled = false ;
    }

    private void CmbKeyDown( object sender, KeyEventArgs e )
    {
      if ( e.Key == Key.Enter ) {
        ViewModel.Search() ;
      }
    }

    private void ShowCeedModelNumberColumn_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.ShowCeedModelNumberColumn( DtGrid, LbCeedModelNumbers, CmbCeedModelNumbers ) ;
    }

    private void ShowCeedModelNumberColumn_UnChecked( object sender, RoutedEventArgs e )
    {
      ViewModel.UnShowCeedModelNumberColumn( DtGrid, LbCeedModelNumbers, CmbCeedModelNumbers ) ;
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

    private void Button_ReplaceSymbol( object sender, RoutedEventArgs e )
    {
      ViewModel.ReplaceSymbol( DtGrid, BtnReplaceSymbol ) ;
    }

    private void Button_ReplaceMultipleSymbols( object sender, RoutedEventArgs e )
    {
      ViewModel.ReplaceMultipleSymbols( DtGrid ) ;
    }

    private void DtGrid_OnMouseDoubleClick( object sender, MouseButtonEventArgs e )
    {
      if ( ( (DataGrid) sender ).SelectedItem is not CeedModel ceedModel ) return ;
      ViewModel.SelectedDeviceSymbol = ceedModel.GeneralDisplayDeviceSymbol ;
      ViewModel.SelectedCondition = ceedModel.Condition ;
      ViewModel.SelectedCeedCode = ceedModel.CeedSetCode ;
      ViewModel.SelectedModelNum = ceedModel.ModelNumber ;
      ViewModel.SelectedFloorPlanType = ceedModel.FloorPlanType ;
      ViewModel.CreateConnector() ;
    }

    private void DtGrid_OnMouseLeftButtonUp( object sender, MouseButtonEventArgs e )
    {
      BtnReplaceSymbol.IsEnabled = false ;
      if ( sender is not DataGrid dataGrid ) return ;
      if ( dataGrid.SelectedItem == null ) return ;
      if ( dataGrid.SelectedItem is not CeedModel ceedModel ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return ;
      }

      ViewModel.SelectedCeedModel = ceedModel ;
      BtnReplaceSymbol.IsEnabled = true ;
    }
  }

  // ReSharper disable once ClassNeverInstantiated.Global
  public class DesignCeedDockPaneContentViewModel : CeedViewModel
  {
    public DesignCeedDockPaneContentViewModel() : base( default!, default!, default! )
    {
    }
  }
}