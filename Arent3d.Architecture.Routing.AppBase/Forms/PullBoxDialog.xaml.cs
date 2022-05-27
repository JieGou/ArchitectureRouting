using System ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Autodesk.Revit.DB ;
using ControlLib ;
using LengthConverter = Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters.LengthConverter ;
using MessageBox = System.Windows.MessageBox ;


namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PullBoxDialog : Window, INotifyPropertyChanged
  {

    public double HeightConnector { get ; set ; } 
    public double HeightWire { get ; set ; } 
    public PullBoxDialog()
    {
      InitializeComponent() ;
      HeightConnector = 3000 ;
      HeightWire = 1000 ;
      TopLevelContainer.DataContext = this;
    }
    
    private void Button_Ok( object sender, RoutedEventArgs e )
    {
      if ( ( HeightConnector - HeightWire ) < 250 ) {
        HeightWire = HeightConnector - 250 ;
        MessageBox.Show( "Height wire must be smaller than height wire at least 250mm ", "Alert Message" ) ;
      }
      else {
        DialogResult = true ;
        Close() ;
      }
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

    public event PropertyChangedEventHandler? PropertyChanged ;

    protected virtual void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }
  }
}