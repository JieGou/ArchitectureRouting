using System ;
using System.Text.RegularExpressions ;
using Autodesk.Revit.DB ;
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
    
    private void Button_Ok( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void NumberValidationTextBox( object sender, TextCompositionEventArgs e )
    {
      Regex regex = new Regex( "^[0-9]+(\\.[0-9]+)?$") ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }
    
    private void NumberNegativeValidationTextBox( object sender, TextCompositionEventArgs e )
    {
      Regex regex = new Regex( "^-[0-9]+(\\.[0-9]+)?$") ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }
  }
}