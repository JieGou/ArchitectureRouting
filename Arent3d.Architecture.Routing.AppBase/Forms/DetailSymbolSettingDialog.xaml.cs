using System ;
using System.Collections.Generic ;
using System.Drawing.Text ;
using System.Linq ;
using System.Windows ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class DetailSymbolSettingDialog : Window
  {
    public string DetailSymbol ;
    public string Angle ;
    public DetailSymbolSettingDialog()
    {
      InitializeComponent() ;
      LoadComboboxValue() ;
      DetailSymbol = string.Empty ;
      Angle = string.Empty ;
    }
    
    private void Button_OK( object sender, RoutedEventArgs e )
    {
      DetailSymbol = CmbDetailSymbols.SelectedValue.ToString() ;
      Angle = CmbAngle.SelectedValue.ToString() ;
      DialogResult = true ;
      Close() ;
    }

    private void LoadComboboxValue()
    {
      List<char> symbols = new List<char>() ;
      for (var letter = 'A'; letter <= 'Z'; letter++)
      {
        symbols.Add(letter);
      }

      CmbDetailSymbols.ItemsSource = symbols ;
      CmbDetailSymbols.SelectedItem = symbols.First() ;
      
      List<int> width = new List<int>() ;
      List<double> spacing = new List<double>() ;
      List<int> offset = new List<int>() ;
      for ( var i = 1 ; i <= 10 ; i++ ) {
        width.Add( i * 10 );
        spacing.Add( Convert.ToDouble( i ) / 10 );
        offset.Add( i );
      }

      CmbHeight.ItemsSource = spacing ;
      CmbHeight.SelectedItem = spacing[ 1 ] ;
      CmbWidth.ItemsSource = width ;
      CmbWidth.SelectedItem = width[ 4 ] ;
      CmbSpacing.ItemsSource = spacing ;
      CmbSpacing.SelectedItem = spacing[ 2 ] ;
      CmbLineSpacing.ItemsSource = spacing ;
      CmbLineSpacing.SelectedItem = spacing[ 5 ] ;
      CmbOffset.ItemsSource = offset ;
      CmbOffset.SelectedItem = offset[ 4 ] ;
      CmbRedStampSize.ItemsSource = offset ;
      CmbRedStampSize.SelectedItem = offset[ 0 ] ;

      InstalledFontCollection fonts = new InstalledFontCollection() ;
      List<string> fontName = ( from font in fonts.Families select font.Name ).ToList() ;

      CmbFont.ItemsSource = fontName ;
      CmbFont.SelectedItem = fontName.FirstOrDefault( f => f.Contains( "MS" )) ;
      
      List<int> angle = new List<int>() { 0, 90, 180, -90} ;
      CmbAngle.ItemsSource = angle ;
      CmbAngle.SelectedItem = angle[ 2 ] ;

      List<string> hideTextBackground = new List<string>() { "On", "Off" } ;
      CmbHideTextBackground.ItemsSource = hideTextBackground ;
      CmbHideTextBackground.SelectedItem = hideTextBackground[ 1 ] ;
    }
  }
}