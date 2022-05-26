using System ;
using System.ComponentModel ;
using System.Runtime.CompilerServices ;
using System.Text.RegularExpressions ;
using System.Windows ;
using System.Windows.Input ;
using Autodesk.Revit.DB ;
using ControlLib ;
using LengthConverter = Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters.LengthConverter ;


namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class PullBoxDialog : Window, INotifyPropertyChanged
  {

    public double HeightConnector { get ; set ; } 
    public double HeightWire { get ; set ; } 
    public PullBoxDialog()
    {
      InitializeComponent() ;
      HeightConnector = 1000 ;
      HeightWire = 0 ;
      TopLevelContainer.DataContext = this;
    }
    
    private void Button_Ok( object sender, RoutedEventArgs e )
    {
      var a = HeightConnector ;
      var b = HeightWire ;
      DialogResult = true ;
      Close() ;
    }
    
    private void FromFixedHeightNumericUpDown_OnValueChanged( object sender, ValueChangedEventArgs e )
    {
      // Manually update FixedHeight because Value binding is not called.
      // FromFixedHeight = GetLengthConverter( DisplayUnitSystem ).ConvertBackUnit( FromFixedHeightNumericUpDown.Value ) ;
      //
      // OnValueChanged( EventArgs.Empty ) ;
    }
    
    private static ValueConverters.LengthConverter GetLengthConverter( DisplayUnit displayUnitSystem )
    {
      return displayUnitSystem switch
      {
        DisplayUnit.METRIC => ValueConverters.LengthConverter.Millimeters,
        DisplayUnit.IMPERIAL => ValueConverters.LengthConverter.Inches,
        _ => LengthConverter.Default,
      } ;
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