using System.Collections.Generic ;
using Autodesk.Revit.DB ;
using CsvHelper ;
using CsvHelper.Configuration ;
using CsvHelper.TypeConversion ;

namespace Arent3d.Revit.Csv.Converters
{
  public class LengthUnitConverter : ITypeConverter
  {
    private static readonly UnitDictionary UnitDic = new UnitDictionary( new Dictionary<string, DisplayUnitType>
    {
      { "in", DisplayUnitTypes.Inches },
      { "ft", DisplayUnitTypes.Feet },
      { "mm", DisplayUnitTypes.Millimeters },
      { "cm", DisplayUnitTypes.Centimeters },
      { "dm", DisplayUnitTypes.Decimeters },
      { "m", DisplayUnitTypes.Meters },
    } ) ;

    public string ConvertToString( object value, IWriterRow row, MemberMapData memberMapData )
    {
      return UnitDic.GetValueWithUnit( (double) value, GetUsedUnit( CsvDisplayUnit.DisplayUnit ) ) ;
    }

    public object ConvertFromString( string text, IReaderRow row, MemberMapData memberMapData )
    {
      if ( UnitDic.Match( text ) is not { } tuple ) return double.NaN ;

      if ( tuple.Unit is not {} unitType ) return tuple.Value ;

      return UnitUtils.ConvertToInternalUnits( tuple.Value, unitType ) ;
    }

    private static DisplayUnitType GetUsedUnit( DisplayUnit displayUnit )
    {
      return displayUnit switch
      {
        DisplayUnit.METRIC => DisplayUnitTypes.Meters,
        _ => DisplayUnitTypes.Feet,
      } ;
    }
  }
}