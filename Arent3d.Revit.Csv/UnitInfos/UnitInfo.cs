using System ;
using System.Collections.Generic ;
using System.Text.RegularExpressions ;

namespace Arent3d.Revit.Csv.UnitInfos
{
  internal abstract class UnitInfo
  {
    private static readonly Regex ShortenRegex = new Regex( @"^(milli|centi|deci|kilo|)(meters|feet|inches)$", RegexOptions.Compiled | RegexOptions.Singleline ) ;

    public UnitDictionary CreateUnitDictionary() => new UnitDictionary( GenerateUnitList( GetUnitAbbrList(), GetSpecTypeId() ) ) ;

    protected abstract UnitAbbrList GetUnitAbbrList() ;
    protected abstract SpecType GetSpecTypeId() ;

    private static IEnumerable<KeyValuePair<string, DisplayUnitType>> GenerateUnitList( UnitAbbrList abbrList, SpecType specType )
    {
      foreach ( var displayUnitType in specType.GetValidDisplayUnits() ) {
        var str = displayUnitType.GetTypeCatalogString() ;
        if ( string.IsNullOrEmpty( str ) ) continue ;

        str = str.ToLowerInvariant() ;
        foreach ( var abbr in abbrList.GetAbbreviations( str ) ) {
          yield return new KeyValuePair<string, DisplayUnitType>( abbr, displayUnitType ) ;
        }

        yield return new KeyValuePair<string, DisplayUnitType>( str, displayUnitType ) ;
      }
    }
  }
}