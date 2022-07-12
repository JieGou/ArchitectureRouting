using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Input ;


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
  }
}