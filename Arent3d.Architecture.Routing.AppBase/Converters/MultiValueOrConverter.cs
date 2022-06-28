using System ;
using System.Windows.Data ;
using System.Linq;

namespace Arent3d.Architecture.Routing.AppBase.Converters
{
  [ValueConversion(typeof(string), typeof(bool))]
  public class MultiValueOrConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return value is string s && ((string)parameter).Split(';').Contains(s);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return false ;
    }
  }
}