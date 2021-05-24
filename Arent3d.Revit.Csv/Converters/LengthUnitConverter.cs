using System.Collections.Generic ;
using Autodesk.Revit.DB ;
using CsvHelper ;
using CsvHelper.Configuration ;
using CsvHelper.TypeConversion ;

#if REVIT2019 || REVIT2020
using DisplayUnitTypeProxy = Autodesk.Revit.DB.DisplayUnitType ;
#else
using DisplayUnitTypeProxy = Autodesk.Revit.DB.ForgeTypeId ;
#endif

namespace Arent3d.Revit.Csv.Converters
{
  public class LengthUnitConverter : ITypeConverter
  {
    private static readonly UnitDictionary UnitDic = new UnitDictionary( new Dictionary<string, DisplayUnitTypeProxy>
    {
#if REVIT2019 || REVIT2020
      { "in", DisplayUnitType.DUT_DECIMAL_INCHES },
      { "ft", DisplayUnitType.DUT_DECIMAL_FEET },
      { "mm", DisplayUnitType.DUT_MILLIMETERS },
      { "cm", DisplayUnitType.DUT_CENTIMETERS },
      { "dm", DisplayUnitType.DUT_DECIMETERS },
      { "m", DisplayUnitType.DUT_METERS },
#else
      { "in", UnitTypeId.Inches },
      { "ft", UnitTypeId.Feet },
      { "mm", UnitTypeId.Millimeters },
      { "cm", UnitTypeId.Centimeters },
      { "dm", UnitTypeId.Decimeters },
      { "m", UnitTypeId.Meters },
#endif
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

    private static DisplayUnitTypeProxy GetUsedUnit( DisplayUnit displayUnit )
    {
#if REVIT2019 || REVIT2020
      return displayUnit switch
      {
        DisplayUnit.METRIC => DisplayUnitType.DUT_METERS,
        _ => DisplayUnitType.DUT_DECIMAL_FEET,
      } ;
#else
      return displayUnit switch
      {
        DisplayUnit.METRIC => UnitTypeId.Meters,
        _ => UnitTypeId.Feet,
      } ;
#endif
    }
  }
}