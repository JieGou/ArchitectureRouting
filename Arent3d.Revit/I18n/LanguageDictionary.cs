using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Xml.Linq ;
using Autodesk.Revit.ApplicationServices ;

namespace Arent3d.Revit.I18n
{
  internal class LanguageDictionary
  {
    public CultureInfo Culture { get ; }

    private readonly Dictionary<string, string> _dic = new Dictionary<string, string>() ;

    public LanguageDictionary( LanguageType languageType )
    {
      Culture = GetCulture( languageType ) ;
    }

    public bool GetStringByKey( string keyName, out string result )
    {
      if ( false == _dic.TryGetValue( keyName, out var value ) ) {
        result = keyName ;
        return false ;
      }

      result = value ?? keyName ;
      return true ;
    }

    public void ReadAndAppendLanguageDictionary( string languageDirectoryPath )
    {
      // 指定言語で読み込み
      var culture = Culture ;
      while ( ! culture.IsNeutralCulture && ! culture.Equals( CultureInfo.InvariantCulture ) ) {
        var langFile = Path.Combine( languageDirectoryPath, $"{culture.Name}.plist" ) ;
        AppendFileContents( langFile ) ;
        culture = culture.Parent ;
      }

      // 不足分は英語から読み込み
      culture = CultureInfo.GetCultureInfo( "en-US" ) ;
      while ( ! culture.IsNeutralCulture && ! culture.Equals( CultureInfo.InvariantCulture ) ) {
        var langFile = Path.Combine( languageDirectoryPath, $"{culture.Name}.plist" ) ;
        AppendFileContents( langFile ) ;
        culture = culture.Parent ;
      }
    }

    private void AppendFileContents( string filePath )
    {
      if ( ! File.Exists( filePath ) ) return ;

      var isKey = true ;
      string? key = null ;
      foreach ( var elm in XElement.Load( filePath ).Elements( "dict" ).Elements() ) {
        if ( isKey ) {
          if ( "key" != elm.Name.LocalName ) continue ;
          isKey = false ;
          key = elm.Value ;
        }
        else {
          if ( "string" != elm.Name.LocalName ) continue ;
          isKey = true ;
          if ( null == key || _dic.ContainsKey( key ) ) continue ;

          _dic.Add( key, elm.Value ) ;
        }
      }
    }

    private static CultureInfo GetCulture( LanguageType languageType )
    {
      return languageType switch
      {
        LanguageType.Unknown => CultureInfo.InvariantCulture,
        LanguageType.English_USA => CultureInfo.GetCultureInfo( "en-US" ),
        LanguageType.German => CultureInfo.GetCultureInfo( "de-DE" ),
        LanguageType.Spanish => CultureInfo.GetCultureInfo( "es-ES" ),
        LanguageType.French => CultureInfo.GetCultureInfo( "fr-FR" ),
        LanguageType.Italian => CultureInfo.GetCultureInfo( "it-IT" ),
        LanguageType.Dutch => CultureInfo.GetCultureInfo( "nl-NL" ),
        LanguageType.Chinese_Simplified => CultureInfo.GetCultureInfo( "zh-CN" ),
        LanguageType.Chinese_Traditional => CultureInfo.GetCultureInfo( "zh-TW" ),
        LanguageType.Japanese => CultureInfo.GetCultureInfo( "ja-JP" ),
        LanguageType.Korean => CultureInfo.GetCultureInfo( "ko-KR" ),
        LanguageType.Russian => CultureInfo.GetCultureInfo( "ru-RU" ),
        LanguageType.Czech => CultureInfo.GetCultureInfo( "cs-CZ" ),
        LanguageType.Polish => CultureInfo.GetCultureInfo( "pl-PL" ),
        LanguageType.Hungarian => CultureInfo.GetCultureInfo( "hu-HU" ),
        LanguageType.Brazilian_Portuguese => CultureInfo.GetCultureInfo( "pt-BR" ),
        LanguageType.English_GB => CultureInfo.GetCultureInfo( "en-US" ),
        _ => CultureInfo.InvariantCulture,
      } ;
    }
  }
}