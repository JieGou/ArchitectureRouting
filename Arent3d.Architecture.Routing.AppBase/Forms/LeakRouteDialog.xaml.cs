using System ;
using System.Collections.Generic ;
using System.Windows ;
using System.Text.RegularExpressions;
using System.Windows.Input ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class LeakRouteDialog : Window
  {
    private const string Mode1 = "自由モード" ;
    private const string Mode2 = "矩形モード" ;
    private List<string> ListCreationMode = new List<string>{Mode1,Mode2} ;
    public int createMode ;
    public double height ;
    public LeakRouteDialog()
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      CmbCreationMode.ItemsSource = ListCreationMode ;
      CmbCreationMode.SelectedIndex = 0 ;
      TxtHeight.Text = "1000" ;
    }
    
    private void Button_Create( object sender, RoutedEventArgs e )
    {
      height = Double.Parse( TxtHeight.Text) ;
      createMode = CmbCreationMode.SelectedIndex ;
      DialogResult = true ;
      Close() ;
    }
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
     
    }
  }
}