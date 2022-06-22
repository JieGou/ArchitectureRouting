using System ;
using System.Globalization ;
using System.Windows.Data ;
using Arent3d.Architecture.Routing.Storable.Model ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters
{
  public class StringColorConverter: IValueConverter
  {
    public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
    { 
      return value.ToString() switch
      {
        "Red" => "#FF0000",
        "Yellow"=> "#FFFF00",
        "Cyan"=> "#00FFFF",
        "Blue"=> "#0000FF",
        "Purple"=> "#FF00FF",
        "White" => "#FFFFFF", 
        _ => "#00FF00",
      } ; 
    }

    public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
    {
      throw new NotImplementedException() ;
    }
  }
}