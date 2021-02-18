using System.Collections.Generic ;
using Autodesk.Revit.DB ;
using CsvHelper ;
using CsvHelper.Configuration ;
using CsvHelper.TypeConversion ;

namespace Arent3d.Revit.Csv.Converters
{
  public class LengthUnitConverter : ITypeConverter
  {
    private static readonly UnitDictionary UnitDic = new UnitDictionary( new Dictionary<string, ForgeTypeId>
    {
      { "in", UnitTypeId.Inches },
      { "ft", UnitTypeId.Feet },
      { "mm", UnitTypeId.Millimeters },
      { "cm", UnitTypeId.Centimeters },
      { "dm", UnitTypeId.Decimeters },
      { "m", UnitTypeId.Meters },
    } ) ;

    public string ConvertToString( object value, IWriterRow row, MemberMapData memberMapData )
    {
      return UnitDic.GetValueWithUnit( (double) value, GetUsedUnit( CsvDisplayUnit.DisplayUnit ) ) ;
    }

    public object ConvertFromString( string text, IReaderRow row, MemberMapData memberMapData )
    {
      if ( UnitDic.Match( text ) is not { } tuple ) return double.NaN ;

      if ( null == tuple.Unit ) return tuple.Value ;

      return UnitUtils.ConvertToInternalUnits( tuple.Value, tuple.Unit ) ;
    }

    private static ForgeTypeId GetUsedUnit( DisplayUnit displayUnit )
    {
      return displayUnit switch
      {
        DisplayUnit.METRIC => UnitTypeId.Meters,
        _ => UnitTypeId.Feet,
      } ;
    }
  }
}