using CsvHelper ;
using CsvHelper.Configuration ;
using CsvHelper.TypeConversion ;

namespace Arent3d.Revit.Csv.Converters
{
  public class LengthParameterDataConverter : ITypeConverter
  {
    public string ConvertToString( object value, IWriterRow row, MemberMapData memberMapData )
    {
      return value.ToString() ;
    }

    public object ConvertFromString( string text, IReaderRow row, MemberMapData memberMapData )
    {
      var param = new LengthParameterData() ;
      param.SetValueString( text ) ;
      return param ;
    }
  }
}