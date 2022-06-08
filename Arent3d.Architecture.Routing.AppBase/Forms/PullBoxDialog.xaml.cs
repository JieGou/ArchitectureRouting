using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Input ;
using Visibility = System.Windows.Visibility ;


namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PullBoxDialog : Window
  {
    public PullBoxDialog()
    {
      InitializeComponent() ;
    }

    private void NumberValidationTextBox( object sender, TextCompositionEventArgs e )
    {
      string pattern = @"[^0-9.]+" ;
      Regex regex = new Regex( pattern) ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }
    
    private void NumberNegativeValidationTextBox( object sender, TextCompositionEventArgs e )
    {
      string pattern = @"[^0-9.-]+" ;
      Regex regex = new Regex( pattern) ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }

    private void CreatePullBoxWithoutSettingHeight_Checked( object sender, RoutedEventArgs e )
    {
      SettingHeightPanel.Visibility = Visibility.Hidden ;
      SettingHeightPanel.Height = 0 ;
      WdPullBoxView.Height = 130 ;
    }

    private void CreatePullBoxWithoutSettingHeight_UnChecked( object sender, RoutedEventArgs e )
    {
      SettingHeightPanel.Visibility = Visibility.Visible ;
      SettingHeightPanel.Height = 90 ;
      WdPullBoxView.Height = 220 ;
    }
  }
}