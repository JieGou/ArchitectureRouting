using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CeeDModelDialog : Window
  {
    private readonly CeedViewModel _allCeeDModels ;
    private string _ceeDModelNumberSearch ;

    public CeeDModelDialog( CeedViewModel viewModel )
    {
      InitializeComponent() ;
      this.DataContext = viewModel ;
      _allCeeDModels = viewModel ;
      _ceeDModelNumberSearch = viewModel.CeeDNumberSearch ;
      CmbCeeDModelNumbers.ItemsSource = viewModel.CeeDModelNumbers ;
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_Reset( object sender, RoutedEventArgs e )
    {
      this.DataContext = _allCeeDModels ;
      CmbCeeDModelNumbers.SelectedIndex = -1 ;
      CmbCeeDModelNumbers.Text = "" ;
    }

    private void CmbCeeDModelNumbers_TextChanged( object sender, TextChangedEventArgs e )
    {
      _ceeDModelNumberSearch = ! string.IsNullOrEmpty( CmbCeeDModelNumbers.Text ) ? CmbCeeDModelNumbers.Text : string.Empty ;
    }

    private void Button_Search( object sender, RoutedEventArgs e )
    {
      if ( ! string.IsNullOrEmpty( _ceeDModelNumberSearch ) ) {
        var ceeDModels = _allCeeDModels.CeedModels.Where( c => c.CeeDModelNumber.Contains( _ceeDModelNumberSearch ) ).ToList() ;
        CeedViewModel ceeDModelsSearch = new CeedViewModel( _allCeeDModels.CeedStorable, ceeDModels, _ceeDModelNumberSearch ) ;
        this.DataContext = ceeDModelsSearch ;
      }
      else {
        this.DataContext = _allCeeDModels ;
      }
    }
  }
}