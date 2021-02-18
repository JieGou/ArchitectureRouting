using System ;
using System.Collections.Generic ;
using System.Text.RegularExpressions ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit.Csv.UnitInfos
{
  internal abstract class UnitInfo
  {
    private static readonly Regex ShortenRegex = new Regex( @"^(milli|centi|deci|kilo|)(meters|feet|inches)$", RegexOptions.Compiled | RegexOptions.Singleline ) ;

    public UnitDictionary CreateUnitDictionary() => new UnitDictionary( GenerateUnitList( GetUnitAbbrList(), GetSpecTypeId() ) ) ;

    protected abstract UnitAbbrList GetUnitAbbrList() ;
    protected abstract ForgeTypeId GetSpecTypeId() ;

    private IEnumerable<KeyValuePair<string, ForgeTypeId>> GenerateUnitList( UnitAbbrList abbrList, ForgeTypeId specTypeId )
    {
      foreach ( var forgeTypeId in UnitUtils.GetValidUnits( specTypeId ) ) {
        var str = UnitUtils.GetTypeCatalogStringForUnit( forgeTypeId ) ;
        if ( string.IsNullOrEmpty( str ) ) continue ;

        str = str.ToLowerInvariant() ;
        foreach ( var abbr in abbrList.GetAbbreviations( str ) ) {
          yield return new KeyValuePair<string, ForgeTypeId>( abbr, forgeTypeId ) ;
        }

        yield return new KeyValuePair<string, ForgeTypeId>( str, forgeTypeId ) ;
      }
    }
  }
}