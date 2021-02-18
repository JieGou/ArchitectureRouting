using System ;
using System.Collections.Generic ;
using System.Text.RegularExpressions ;

namespace Arent3d.Revit.Csv.UnitInfos
{
  internal class UnitAbbrList
  {
    private static readonly Regex AbbrRegEx = new Regex( @"^(milli|centi|deci|kilo|)(.+)$", RegexOptions.Compiled | RegexOptions.Singleline ) ;

    private readonly Dictionary<string, List<string>> _abbreviations = new() ;

    public UnitAbbrList( IEnumerable<(string, string)> abbrs )
    {
      foreach ( var (key, value) in abbrs ) {
        if ( false == _abbreviations.TryGetValue( key, out var list ) ) {
          list = new List<string>() ;
          _abbreviations.Add( key, list ) ;
        }

        list.Add( value ) ;
      }
    }

    public IEnumerable<string> GetAbbreviations( string str )
    {
      var match = AbbrRegEx.Match( str ) ;
      if ( false == match.Success ) yield break ;

      if ( false == _abbreviations.TryGetValue( match.Groups[ 2 ].Value, out var list ) ) yield break ;

      var prefix = match.Groups[ 1 ].Value switch
      {
        "milli" => "m",
        "centi" => "c",
        "deci" => "d",
        "kilo" => "k",
        _ => "",
      } ;
      foreach ( var unit in list ) {
        yield return prefix + unit ;
      }
    }
  }
}