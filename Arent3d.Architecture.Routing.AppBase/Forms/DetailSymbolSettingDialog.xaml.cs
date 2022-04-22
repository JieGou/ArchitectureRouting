using System ;
using System.Collections.Generic ;
using System.Drawing.Text ;
using System.Linq ;
using System.Windows ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DetailSymbolSettingDialog : Window
  {
    public string DetailSymbol ;
    public double HeightCharacter ;
    public int PercentWidth ;
    public string Angle ;
    public string SymbolFont ;
    public string SymbolStyle ;
    public int Offset ;
    public int BackGround ;

    public DetailSymbolSettingDialog( List<string> symbols, List<int> angle, string defaultSymbol )
    {
      InitializeComponent() ;
      LoadComboboxValue() ;

      CmbDetailSymbols.ItemsSource = symbols ;
      CmbDetailSymbols.SelectedItem = defaultSymbol ;
      DetailSymbol = defaultSymbol ;

      CmbAngle.ItemsSource = angle ;
      CmbAngle.SelectedItem = angle.FirstOrDefault() ;
      Angle = angle.FirstOrDefault().ToString() ;

      SymbolFont = string.Empty ;
      SymbolStyle = string.Empty ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      DetailSymbol = CmbDetailSymbols.SelectedValue.ToString() ;
      Angle = CmbAngle.SelectedValue.ToString() ;
      HeightCharacter = Convert.ToDouble( CmbHeight.SelectedValue.ToString() ) ;
      PercentWidth = Convert.ToInt32( CmbWidth.SelectedValue.ToString() ) ;
      SymbolFont = CmbFont.SelectedValue.ToString() ;
      SymbolStyle = CmbStyle.SelectedValue.ToString() ;
      Offset = Convert.ToInt32( CmbOffset.SelectedValue.ToString() ) ;
      BackGround = CmbHideTextBackground.SelectedValue.ToString() == "On" ? 0 : 1 ;
      DialogResult = true ;
      Close() ;
    }

    private void LoadComboboxValue()
    {
      List<double> height = new List<double>() ;
      List<int> width = new List<int>() ;
      List<double> spacing = new List<double>() ;
      List<int> offset = new List<int>() ;
      for ( var i = 1 ; i <= 10 ; i++ ) {
        width.Add( i * 10 - 5) ;
        width.Add( i * 10 ) ;
        spacing.Add( Convert.ToDouble( i ) / 10 ) ;
        if ( i > 5 ) continue ;
        height.Add( i - 0.5 ) ;
        height.Add( i ) ;
        offset.Add( i ) ;
      }
      height.Add(2.7);
      height = height.OrderBy( x => x ).ToList() ;

      CmbHeight.ItemsSource = height ;
      CmbHeight.SelectedItem = height[ 7 ] ;
      CmbWidth.ItemsSource = width ;
      CmbWidth.SelectedItem = width.Last() ;
      CmbSpacing.ItemsSource = spacing ;
      CmbSpacing.SelectedItem = spacing[ 2 ] ;
      CmbLineSpacing.ItemsSource = spacing ;
      CmbLineSpacing.SelectedItem = spacing[ 5 ] ;
      CmbOffset.ItemsSource = offset ;
      CmbOffset.SelectedItem = offset.Last() ;
      CmbRedStampSize.ItemsSource = offset ;
      CmbRedStampSize.SelectedItem = offset.First() ;

      InstalledFontCollection fonts = new InstalledFontCollection() ;
      List<string> fontName = ( from font in fonts.Families select font.Name ).ToList() ;
      CmbFont.ItemsSource = fontName ;
      CmbFont.SelectedItem = fontName.FirstOrDefault( f => f.Contains( "Arial" ) ) ;

      List<string> fontStyle = new List<string>() { System.Drawing.FontStyle.Regular.GetFieldName(), System.Drawing.FontStyle.Bold.GetFieldName(), System.Drawing.FontStyle.Italic.GetFieldName(), System.Drawing.FontStyle.Underline.GetFieldName() } ;
      CmbStyle.ItemsSource = fontStyle ;
      CmbStyle.SelectedItem = fontStyle.FirstOrDefault() ;

      List<string> hideTextBackground = new List<string>() { "On", "Off" } ;
      CmbHideTextBackground.ItemsSource = hideTextBackground ;
      CmbHideTextBackground.SelectedItem = hideTextBackground[ 1 ] ;
    }
  }
}