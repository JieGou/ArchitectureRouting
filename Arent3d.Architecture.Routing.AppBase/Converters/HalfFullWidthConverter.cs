using System ;
using System.Globalization ;
using System.Windows.Data ;

namespace Arent3d.Architecture.Routing.AppBase.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class HalfFullWidthConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if ( value is not string str || string.IsNullOrEmpty( str ) )
                return string.Empty ;

            return StringWidthUtils.ToFullWidth( str ) ;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            return Binding.DoNothing ;
        }
    }
}