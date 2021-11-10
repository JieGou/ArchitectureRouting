using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class OffsetSettingDialog : Window
  {
    public OffsetSettingDialog()
    {
      InitializeComponent() ;
    }
    
    public OffsetSettingDialog( OffsetSettingViewModel viewModel)
    {
      InitializeComponent();
      DataContext = viewModel;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
    }    

    private void OffsetButtons_OnLeftOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void OffsetButtons_OnRightOnClick( object sender, RoutedEventArgs e )
    {
      DialogResult = false ;
      Close() ;
    }
  }
}