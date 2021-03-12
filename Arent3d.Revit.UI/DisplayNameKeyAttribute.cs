using System ;
using Arent3d.Revit.I18n ;

namespace Arent3d.Revit.UI
{
  public class DisplayNameKeyAttribute : Attribute
  {
    public string Key { get ; }
    public string? DefaultString { get ; set ; }

    public DisplayNameKeyAttribute( string key )
    {
      Key = key ;
    }

    public string GetApplicationString()
    {
      return LanguageConverter.GetAppStringByKey( Key ) ?? DefaultString ?? LanguageConverter.GetDefaultString( Key ) ;
    }
  }
}