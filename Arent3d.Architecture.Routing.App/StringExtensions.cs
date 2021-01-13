using System.Text.RegularExpressions ;

namespace Arent3d.Architecture.Routing.App
{
  public static class StringExtensions
  {
    private static readonly Regex WordRegex = new Regex( "[A-Za-z0-9]+", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled ) ;
    private static readonly Regex WordSplitterRegex = new Regex( "(?<=.)[A-Z](?=[a-z])|(?<=[a-z])[A-Z]", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled ) ;

    public static string ToSnakeCase( this string str )
    {
      return WordRegex.Replace( str, match => SplitWord( match.Value, '_' ) ).ToLower() ;
    }

    public static string SeparateByWords( this string str )
    {
      return WordRegex.Replace( str.Replace( '_', ' ' ), match => SplitWord( match.Value, ' ' ) ) ;
    }

    private static string SplitWord( string word, char separator )
    {
      return WordSplitterRegex.Replace( word, match => separator + match.Value ) ;
    }
  }
}