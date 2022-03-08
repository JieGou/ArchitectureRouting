using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ImportDwgMappingDialog : Window
  {
    public ImportDwgMappingDialog(ImportDwgMappingViewModel viewModel)
    {
      InitializeComponent() ;
      DataContext = viewModel ;
    }

    private void BtnSave_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
    }

    private void BtnCancel_OnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      this.Close() ;
    }
  }
}