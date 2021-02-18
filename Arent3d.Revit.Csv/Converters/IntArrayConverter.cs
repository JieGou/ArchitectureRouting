using System.Linq ;
using Arent3d.Utility ;
using CsvHelper ;
using CsvHelper.Configuration ;
using CsvHelper.TypeConversion ;

namespace Arent3d.Revit.Csv.Converters
{
  public class IntArrayConverter : ITypeConverter
  {
    public string ConvertToString( object value, IWriterRow row, MemberMapData memberMapData )
    {
      return string.Join( ",", (int[]) value ) ;
    }

    public object ConvertFromString( string text, IReaderRow row, MemberMapData memberMapData )
    {
      return text.Split( ',' ).Select( str => int.TryParse( str, out var num ) ? num : (int?) null ).NonNull().ToArray() ;
    }
  }
}