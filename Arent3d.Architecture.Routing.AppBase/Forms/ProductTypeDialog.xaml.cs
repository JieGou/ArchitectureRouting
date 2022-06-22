using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ProductTypeDialog : Window
  {
    public ProductTypeDialog()
    {
      InitializeComponent() ;
    }
    private void Button_Click( object sender, RoutedEventArgs e )
    {
      ProductTypeViewModel vm = (ProductTypeViewModel) DataContext ;
      var button = (Button) sender ;
      vm.SelectedProductType = vm.ProductTypes[ button.CommandParameter.ToString() ] ;
      this.DialogResult = true ;
      this.Close() ;
    }
  }
}