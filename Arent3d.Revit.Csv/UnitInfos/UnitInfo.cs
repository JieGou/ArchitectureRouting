using System ;
using System.Collections.Generic ;
using System.Text.RegularExpressions ;
using Autodesk.Revit.DB ;

#if REVIT2019 || REVIT2020
using SpecTypeProxy = Autodesk.Revit.DB.UnitType ;
using DisplayUnitTypeProxy = Autodesk.Revit.DB.DisplayUnitType ;
#else
using SpecTypeProxy = Autodesk.Revit.DB.ForgeTypeId ;
using DisplayUnitTypeProxy = Autodesk.Revit.DB.ForgeTypeId ;
#endif

namespace Arent3d.Revit.Csv.UnitInfos
{
  internal abstract class UnitInfo
  {
    private static readonly Regex ShortenRegex = new Regex( @"^(milli|centi|deci|kilo|)(meters|feet|inches)$", RegexOptions.Compiled | RegexOptions.Singleline ) ;

    public UnitDictionary CreateUnitDictionary() => new UnitDictionary( GenerateUnitList( GetUnitAbbrList(), GetSpecTypeId() ) ) ;

    protected abstract UnitAbbrList GetUnitAbbrList() ;
    protected abstract SpecTypeProxy GetSpecTypeId() ;

    private IEnumerable<KeyValuePair<string, DisplayUnitTypeProxy>> GenerateUnitList( UnitAbbrList abbrList, SpecTypeProxy specTypeId )
    {
#if REVIT2019 || REVIT2020
      foreach ( var displayUnitTypeId in UnitUtils.GetValidDisplayUnits( specTypeId ) ) {
        var str = UnitUtils.GetTypeCatalogString( displayUnitTypeId ) ;
#else
      foreach ( var displayUnitTypeId in UnitUtils.GetValidUnits( specTypeId ) ) {
        var str = UnitUtils.GetTypeCatalogStringForUnit( displayUnitTypeId ) ;
#endif
        if ( string.IsNullOrEmpty( str ) ) continue ;

        str = str.ToLowerInvariant() ;
        foreach ( var abbr in abbrList.GetAbbreviations( str ) ) {
          yield return new KeyValuePair<string, DisplayUnitTypeProxy>( abbr, displayUnitTypeId ) ;
        }

        yield return new KeyValuePair<string, DisplayUnitTypeProxy>( str, displayUnitTypeId ) ;
      }
    }
  }
}