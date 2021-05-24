using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.Text.RegularExpressions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

#if REVIT2019 || REVIT2020
using DisplayUnitTypeProxy = Autodesk.Revit.DB.DisplayUnitType ;
#else
using DisplayUnitTypeProxy = Autodesk.Revit.DB.ForgeTypeId ;
#endif

namespace Arent3d.Revit.Csv
{
  internal class UnitDictionary
  {
    private static readonly Regex RemoveSpacesRegex = new Regex( @"\s", RegexOptions.Singleline | RegexOptions.Compiled ) ;

    private readonly Dictionary<string, DisplayUnitTypeProxy> _dic = new() ;
    private readonly Dictionary<DisplayUnitTypeProxy, string> _reverseDic = new() ;
    private Regex? _regex = null ;
    private Regex Regex => _regex ??= CreateRegex() ;

    private Regex CreateRegex()
    {
      return new Regex( GenerateRealNumberRegexPattern(), RegexOptions.Singleline | RegexOptions.Compiled ) ;
    }

    private string GenerateRealNumberRegexPattern()
    {
      var unitList = new List<string> { "" } ;
      unitList.AddRange( _dic.Keys ) ;
      unitList.Sort( LengthSorter ) ;

      return $@"^\s*([+-]?(?:\s*\d[\d\s]*(?:[.,][\d\s]*)?|(?:[.,]\s*\d[\d\s]*))(?:e\s*[+-]?\s*\d[\d\s]*)?)\s*({string.Join( "|", unitList.Select( Regex.Escape ) )})\s*$" ;
    }

    private static int LengthSorter( string x, string y )
    {
      // length, descending.
      return y.Length - x.Length ;
    }

    public UnitDictionary()
    {
    }

    public UnitDictionary( IEnumerable<KeyValuePair<string, DisplayUnitTypeProxy>> dic )
    {
      AddUnits( dic ) ;
    }

    public void AddUnit( string unitName, DisplayUnitTypeProxy unitTypeId )
    {
      var unit = unitName.ToLowerInvariant() ;

      if ( _dic.ContainsKey( unit ) ) throw new ArgumentException() ;
      _dic.Add( unit, unitTypeId ) ;

      if ( false == _reverseDic.ContainsKey( unitTypeId ) ) {
        _reverseDic.Add( unitTypeId, unit ) ;
      }

      _regex = null ;
    }

    public void AddUnits( IEnumerable<KeyValuePair<string, DisplayUnitTypeProxy>> dic )
    {
      foreach ( var (unitName, unitTypeId) in dic ) {
        var unit = unitName.ToLowerInvariant() ;

        if ( _dic.ContainsKey( unit ) ) throw new ArgumentException() ;
        _dic.Add( unit, unitTypeId ) ;

        if ( false == _reverseDic.ContainsKey( unitTypeId ) ) {
          _reverseDic.Add( unitTypeId, unit ) ;
        }
      }

      _regex = null ;
    }

    public (double Value, DisplayUnitTypeProxy? Unit)? Match( string text )
    {
      var match = Regex.Match( text.Trim().ToLowerInvariant() ) ;
      if ( false == match.Success ) return null ;

      var normalizedNumber = RemoveSpacesRegex.Replace( match.Groups[ 1 ].Value, string.Empty ).Replace( ',', '.' ) ;
      if ( false == double.TryParse( normalizedNumber, out var d ) ) return null ;

      return ( Value: d, _dic[ match.Groups[ 2 ].Value ] ) ;
    }

    public string GetValueWithUnit( double value, DisplayUnitTypeProxy unitTypeId )
    {
      if ( _reverseDic.TryGetValue( unitTypeId, out var unitName ) ) {
        return UnitUtils.ConvertFromInternalUnits( value, unitTypeId ).ToString( CultureInfo.InvariantCulture ) + unitName ;
      }
      else {
        return value.ToString( CultureInfo.InvariantCulture ) ;
      }
    }
  }
}