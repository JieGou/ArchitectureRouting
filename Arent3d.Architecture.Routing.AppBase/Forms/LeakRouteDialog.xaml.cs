using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Text.RegularExpressions ;
using System.Windows.Controls ;
using System.Windows.Input ;
using System.Windows.Interop ;
using System.Windows.Media ;
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

    private bool UseDirectionOptionForRectangleMode { get ; }
    
    // for dialog resize
    private double HiddenHeight { get ; set ; }
    private double ShowHeight { get ; set ; }
    
    private Brush ActiveColor { get ; } = Brushes.LightSkyBlue;
    private Brush DeActiveColor { get ; set ; }
    
    /// Direction of rectangle mode
    public bool IsRecModeClockWise{ get ; private set ; }

    public LeakRouteDialog(bool useDirectionOptionForRectangleMode = false)
    {
      InitializeComponent() ;
      WindowStartupLocation = WindowStartupLocation.CenterScreen ;
      CmbCreationMode.ItemsSource = _listCreationMode ;
      CmbCreationMode.SelectedIndex = 0 ;
      TxtHeight.Text = "1000" ;
      var conduitTypes = ( from conduitType in (ConduitTypes[]) Enum.GetValues( typeof( ConduitTypes ) ) select conduitType.GetFieldName() ).ToList() ;
      CmbConduitType.ItemsSource = conduitTypes ;
      CmbConduitType.SelectedIndex = 0 ;
      
      // set up for direction selection of rectangle mode
      UseDirectionOptionForRectangleMode = useDirectionOptionForRectangleMode ;
      ShowHeight = Height + 25;
      HiddenHeight = ShowHeight - BtnClockWise.Height ;
      DeActiveColor = BtnClockWise.Background ;
      BtnClockWise.Background = ActiveColor ;
      IsRecModeClockWise = true ;
      ShowDirectionOptions( false ) ;
      
      // set this dialog's parent to Revit Window
      var helper = new WindowInteropHelper( this ) { Owner = Autodesk.Windows.ComponentManager.ApplicationWindow } ;
    }

    private void ShowDirectionOptions(bool bShow)
    {
      if ( ! UseDirectionOptionForRectangleMode )
        return ;

      var visibility = bShow? Visibility.Visible : Visibility.Collapsed;
      BtnClockWise.Visibility = visibility;
      BtnCounterClockWise.Visibility = visibility;
      LabelDirection.Visibility = visibility;
      
      if ( bShow ) {
        MaxHeight = ShowHeight ;
        Height = ShowHeight ;
        MinHeight = ShowHeight ;
      }
      else {
        MinHeight = HiddenHeight ;
        Height = HiddenHeight ;
        MaxHeight = HiddenHeight ;
      }
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

    private void OnModeChanged( object sender, SelectionChangedEventArgs e )
    {
      if ( ! UseDirectionOptionForRectangleMode )
        return ;
      // change dialog size and show/hide direction option
      ShowDirectionOptions( CmbCreationMode.SelectedIndex == 1 ) ;
    }

    private void OnBtnDirectionClick( object sender, RoutedEventArgs e )
    {
      if(Equals( sender, BtnClockWise )) {
        IsRecModeClockWise = true ;
        BtnClockWise.Background = ActiveColor;
        BtnCounterClockWise.Background = DeActiveColor;
      }
      else if(Equals( sender, BtnCounterClockWise )) {
        IsRecModeClockWise = false ;
        BtnCounterClockWise.Background = ActiveColor;
        BtnClockWise.Background = DeActiveColor;
      }
    }
  }
}