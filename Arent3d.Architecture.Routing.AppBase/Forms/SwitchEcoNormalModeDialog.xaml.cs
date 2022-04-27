using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{ 
  public partial class SwitchEcoNormalModeDialog
  { 
    public SwitchEcoNormalModeViewModel ViewModel => (SwitchEcoNormalModeViewModel)DataContext ;
    public SwitchEcoNormalModeDialog( SwitchEcoNormalModeViewModel switchEcoNormalModeViewModel ) : base()
    {
      InitializeComponent() ;
      DataContext = switchEcoNormalModeViewModel ;
    }
 
    private void Button_Cancel_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
    }
    
    private void Button_OK_Click( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
    }
    
  }
  
  public abstract class DesignSwitchEcoNormalModeViewModel : SwitchEcoNormalModeViewModel
  { 
  }
}