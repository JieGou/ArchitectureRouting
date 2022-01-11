using System ;
using System.IO ;
using System.Windows.Data ;
using System.Windows.Media.Imaging ;

namespace Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters
{
  public class Base64ImageConverter : IValueConverter
  {
    public object? Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
    {
      string? s = value as string ;

      if ( s == null )
        return null ;

      BitmapImage? bi = new BitmapImage() ;

      bi.BeginInit() ;
      try {
        bi.StreamSource = new MemoryStream( System.Convert.FromBase64String( s ) ) ;
      }
      catch {
        return null ;
      }
      bi.EndInit() ;

      return bi ;
    }

    public object ConvertBack( object value, Type targetType, object parameter,
      System.Globalization.CultureInfo culture )
    {
      throw new NotImplementedException() ;
    }
  }
}