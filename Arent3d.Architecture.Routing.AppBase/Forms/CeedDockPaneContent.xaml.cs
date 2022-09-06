using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using KeyEventArgs = System.Windows.Input.KeyEventArgs ;
using MessageBox = System.Windows.MessageBox ;

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
    }

    public CeedDockPaneContent()
    {
      InitializeComponent() ;
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

    private void ShowOnlyUsingCode_Checked( object sender, RoutedEventArgs e )
    {
      ViewModel.ShowOnlyUsingCode() ;
      CbShowDiff.IsChecked = false ;
    }

    private void ShowOnlyUsingCode_UnChecked( object sender, RoutedEventArgs e )
    {
      ViewModel.UnShowOnlyUsingCode() ;
      CbShowDiff.IsChecked = false ;
    }

    private void Button_ReplaceSymbol( object sender, RoutedEventArgs e )
    {
      ViewModel.ReplaceSymbol( DtGrid, BtnReplaceSymbol ) ;
    }

    private void Button_ReplaceMultipleSymbols( object sender, RoutedEventArgs e )
    {
      ViewModel.ReplaceMultipleSymbols( DtGrid ) ;
    }

    private void PreviewListMouseDoubleClick( object sender, MouseButtonEventArgs e )
    {
      var listView = ( sender as ListView ) ! ;
      if ( listView.SelectedValue == null ) return ;
      var selectedItem = (CeedViewModel.PreviewListInfo) listView.SelectedValue ;
      ViewModel.SelectedDeviceSymbol = selectedItem.GeneralDisplayDeviceSymbol ;
      ViewModel.SelectedCondition = selectedItem.Condition ;
      ViewModel.SelectedCeedCode = selectedItem.CeedSetCode ;
      ViewModel.SelectedModelNum = selectedItem.ModelNumber ;
      ViewModel.SelectedFloorPlanType = selectedItem.FloorPlanType ;
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
      ViewModel.ShowPreviewList( ceedModel ) ;
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