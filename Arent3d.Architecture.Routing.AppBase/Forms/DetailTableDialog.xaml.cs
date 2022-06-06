using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Visibility = System.Windows.Visibility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DetailTableDialog : Window
  {
    private DetailTableViewModel ViewModel => (DetailTableViewModel)DataContext ;

    public DetailTableDialog(DetailTableViewModel viewModel )
    {
      InitializeComponent() ;
      DataContext = viewModel ;
      HideReferenceDataGrid( ! viewModel.IsAddReference ) ;
      viewModel.IsAddReference = false ;
      ViewModel.DtGrid = DtGrid ;
      ViewModel.DtReferenceGrid = DtReferenceGrid ;
      ViewModel.CreateDetailTableViewModelByGroupId( true ) ;
    }
    
    private void BtnHideReferenceDataGrid_Click( object sender, RoutedEventArgs e )
    {
      if ( BtnReferenceSelectAll.Visibility == Visibility.Visible ) {
        HideReferenceDataGrid( true ) ;
      }
      else if ( BtnReferenceSelectAll.Visibility == Visibility.Hidden ) {
        HideReferenceDataGrid( false ) ;
      }
    }

    private void HideReferenceDataGrid( bool isVisible )
    {
      BtnReferenceSelectAll.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnDeleteReferenceLine.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnReadCtlFile.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnSelectDetailTableRowWithSameDetailSymbolId.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnAddReference.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      BtnAddReferenceRows.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      DtReferenceGrid.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible ;
      DtReferenceGrid.Height = isVisible ? 0 : 370 ;
      DetailTableWindow.Height = isVisible ? 590 : 980 ;
      if ( isVisible ) return ;
      DetailTableWindow.WindowStartupLocation = WindowStartupLocation.Manual ;
      DetailTableWindow.Top = 50 ;
      DetailTableWindow.Left = 400 ;
    }
    
    private void PlumpingTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.PlumingTypeSelection(comboBox) ;
    }
    
    private void FloorSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.FloorSelection( comboBox ) ;
    }
    
    private void WireTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.WireTypeSelection( comboBox );
    }
    
    private void WireSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.WireSizeSelection( comboBox ) ;
    }
    
    private void WireStripSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.WireStripSelection( comboBox ) ;
    }
    
    private void WireBookSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.WireBookSelection( comboBox ) ;
    }
    
    private void WireBookLostFocus( object sender, RoutedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.WireBookLostFocus( comboBox );
    }
    
    private void EarthTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.EarthTypeSelection( comboBox );
    }
    
    private void EarthSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.EarthSizeSelection( comboBox );
    }
    
    private void NumberOfGroundsSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.NumberOfGroundsSelection( comboBox ) ;
    }
    
    private void NumberOfGroundsLostFocus( object sender, RoutedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.NumberOfGroundsLostFocus( comboBox );
    }
    
    private void PlumbingSizeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.PlumbingSizeSelectionChanged( comboBox ) ;
    }
    
    private void ConstructionClassificationSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.ConstructionClassificationSelectionChanged( comboBox ) ;
    }
    
    private void SignalTypeSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.SignalTypeSelection( comboBox );
    }
    
    private void RemarkChanged( object sender, KeyboardFocusChangedEventArgs e )
    {
      if ( sender is not TextBox textBox ) return ;
      ViewModel.Remark( textBox ) ;
    }
    
    private void ConstructionItemSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.ConstructionItemSelection( comboBox ) ;
    }
    
    private void PlumbingItemsSelectionChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( sender is not ComboBox comboBox ) return ;
      ViewModel.PlumbingItemsSelection( comboBox ) ;
    }
  }
}