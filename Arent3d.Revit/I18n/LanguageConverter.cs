using System.Collections.Generic ;
using System.Text.RegularExpressions ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit.I18n
{
  public static class LanguageConverter
  {
    private static readonly List<string> _languageDirectoryPaths = new() ;
    private static readonly Dictionary<LanguageType, LanguageDictionary> _dictionaries = new() ;

    private static LanguageType _appLangType = LanguageType.English_USA ;

    public static void SetApplicationLanguage( LanguageType langType )
    {
      _appLangType = langType ;
    }

    public static void AddLanguageDirectoryPath( string languageDirectoryPath )
    {
      _languageDirectoryPaths.Add( languageDirectoryPath ) ;
      foreach ( var dic in _dictionaries.Values ) {
        dic.ReadAndAppendLanguageDictionary( languageDirectoryPath ) ;
      }
    }

    private static LanguageDictionary GetAppDictionary()
    {
      return GetDictionary( _appLangType ) ;
    }
    private static LanguageDictionary GetDocumentDictionary( Document document )
    {
      return GetDictionary( document.Application.Language ) ;
    }

    private static LanguageDictionary GetDictionary( LanguageType languageType )
    {
      if ( _dictionaries.TryGetValue( languageType, out var dic ) ) return dic ;

      dic = new LanguageDictionary( languageType ) ;
      _dictionaries.Add( languageType, dic ) ;
      foreach ( var path in _languageDirectoryPaths ) {
        dic.ReadAndAppendLanguageDictionary( path ) ;
      }

      return dic ;
    }

    public static string? GetAppStringByKey( string keyName )
    {
      if ( string.IsNullOrEmpty( keyName ) ) return keyName ;

      return GetStringByKey( GetAppDictionary(), keyName ) ;
    }

    public static string? GetStringByKey( this Document document, string keyName )
    {
      if ( string.IsNullOrEmpty( keyName ) ) return keyName ;

      return GetStringByKey( GetDocumentDictionary( document ), keyName ) ;
    }

    public static string GetDefaultString( string keyName )
    {
      var lastPart = GetLastPart( keyName ) ;
      if ( null == lastPart ) return keyName ;

      return SeparateByWords( lastPart ) ;
    }

    private static string? GetStringByKey( LanguageDictionary dic, string keyName )
    {
      if ( dic.GetStringByKey( keyName, out var text ) ) return text ;

      return null ;
    }

    private static readonly char[] WhiteSpaces = { ' ', '\t', '\r', '\n', '\f', '\v', '\x85', '\xA0' } ;
    private static readonly Regex WordRegex = new Regex( "[A-Za-z0-9]+", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled ) ;
    private static readonly Regex WordSplitterRegex = new Regex( "(?<=.)[A-Z](?=[a-z])|(?<=[0-9])[A-Za-z]|(?<=[a-z])[A-Z]|(?<=[A-Za-z])[0-9]", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled ) ;

    private static string? GetLastPart( string keyName )
    {
      var index = keyName.LastIndexOf( '.' ) ;

      // When keyName doesn't contain `dot', whole of keyName is used.
      if ( index < 0 ) return keyName ;

      // When the last character is `dot', keyName is a sentence. So keyName is not be converted.
      if ( index == keyName.Length - 1 ) return null ;

      // When some white space is included after the last `dot', keyName is a sentence. So keyName is not be converted.
      if ( 0 <= keyName.IndexOfAny( WhiteSpaces, index + 1 ) ) return null ;

      // `namespace.key' format.
      return keyName.Substring( index + 1 ) ;
    }

    private static string SeparateByWords( string keyPart )
    {
      return WordRegex.Replace( keyPart.Replace( '_', ' ' ), match => SplitWord( match.Value ) ) ;
    }

    private static string SplitWord( string word )
    {
      return WordSplitterRegex.Replace( word, match => ' ' + match.Value ) ;
    }
  }
}