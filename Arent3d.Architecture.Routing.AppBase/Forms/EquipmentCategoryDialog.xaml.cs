using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class EquipmentCategoryDialog : Window
  {
    public EquipmentCategoryDialog()
    {
      InitializeComponent() ;
    }
    private void Button_Click( object sender, RoutedEventArgs e )
    {
      EquipmentCategoryViewModel vm = (EquipmentCategoryViewModel) DataContext ;
      var button = (Button) sender ;
      vm.SelectedEquipmentCategory = vm.EquipmentCategories[ button.CommandParameter.ToString() ] ;
      DialogResult = true ;
      Close() ;
    }
  }
}