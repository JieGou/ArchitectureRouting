using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class ShaftSettingDialog : Window
  {
    public ShaftSettingViewModel ShaftSettingViewModel => (ShaftSettingViewModel) DataContext ;
    public ShaftSettingDialog( ShaftSettingViewModel shaftSettingViewModel )
    {
      InitializeComponent() ;
      DataContext = shaftSettingViewModel ;
    }
    
    private void NumberValidationTextCombobox( object sender, TextCompositionEventArgs e )
    {
      const string pattern = @"[^0-9.]+" ;
      Regex regex = new( pattern ) ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }
  }
  
  public class DesignShaftSettingViewModel : ShaftSettingViewModel
  {
    public DesignShaftSettingViewModel() : base( default! )
    {
    }
  }
}