using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Text.RegularExpressions ;
using System.Windows.Input ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class LeakRouteDialog : Window
  {
    private enum ConduitTypes
    {
      布,
      発色,
      塩ビ
    }

    private const string Mode1 = "自由モード" ;
    private const string Mode2 = "矩形モード" ;
    private readonly List<string> _listCreationMode = new() { Mode1, Mode2 } ;
    public int CreateMode { get ; private set ; }
    public double RouteHeight { get ; private set ; }
    public int ConduitType { get ; private set ; }

    public LeakRouteDialog()
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      CmbCreationMode.ItemsSource = _listCreationMode ;
      CmbCreationMode.SelectedIndex = 0 ;
      TxtHeight.Text = "1000" ;
      var conduitTypes = ( from conduitType in (ConduitTypes[]) Enum.GetValues( typeof( ConduitTypes ) ) select conduitType.GetFieldName() ).ToList() ;
      CmbConduitType.ItemsSource = conduitTypes ;
      CmbConduitType.SelectedIndex = 0 ;
    }

    private void Button_Create( object sender, RoutedEventArgs e )
    {
      RouteHeight = Double.Parse( TxtHeight.Text ) ;
      CreateMode = CmbCreationMode.SelectedIndex ;
      ConduitType = CmbConduitType.SelectedIndex ;
      DialogResult = true ;
      Close() ;
    }

    private void NumberValidationTextBox( object sender, TextCompositionEventArgs e )
    {
      Regex regex = new Regex( "[^0-9]+" ) ;
      e.Handled = regex.IsMatch( e.Text ) ;
    }
  }
}