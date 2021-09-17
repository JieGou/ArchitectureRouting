using Arent3d.Architecture.Routing.AppBase.Model;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Storable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  /// <summary>
  /// Interaction logic for HeightSettingDialog.xaml
  /// </summary>
  public partial class HeightSettingDialog : Window
  {
    public HeightSettingDialog( HeightSettingViewModel viewModel)
    {
      InitializeComponent();
      this.DataContext = viewModel;
    }

    private void Button_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true;
      Close();
    }

    private void TextBox_GotFocus( object sender, RoutedEventArgs e )
    {
      var textBox = (TextBox)sender;
      textBox.SelectAll();
    }

  }
}
