using System ;
using System.Globalization ;
using System.Windows ;
using System.Windows.Data ;

namespace Arent3d.Architecture.Routing.AppBase.Converters
{
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class InvertBooleanVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return Binding.DoNothing;
    }
  }
}