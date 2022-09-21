using System.Runtime.InteropServices ;
using System.Text ;

namespace Arent3d.Architecture.Routing
{
  public static class StringWidthUtils
  {
    private const uint LOCALE_SYSTEM_DEFAULT = 0x0800 ;
    private const uint LCMAP_HALFWIDTH = 0x00400000 ;
    private const uint LCMAP_FULLWIDTH = 0x00800000 ;

    public static string ToHalfWidth( string fullWidth )
    {
      var sb = new StringBuilder( 256 ) ;
      LCMapString( LOCALE_SYSTEM_DEFAULT, LCMAP_HALFWIDTH, fullWidth, -1, sb, sb.Capacity ) ;
      return sb.ToString() ;
    }

    public static string ToFullWidth( string halfWidth )
    {
      var sb = new StringBuilder( 256 ) ;
      LCMapString( LOCALE_SYSTEM_DEFAULT, LCMAP_FULLWIDTH, halfWidth, -1, sb, sb.Capacity ) ;
      return sb.ToString() ;
    }

    public static bool IsHalfWidth( string str )
    {
      return str.IsNormalized( NormalizationForm.FormKC ) ;
    }

    [DllImport( "kernel32.dll", CharSet = CharSet.Unicode )]
    private static extern int LCMapString( uint Locale, uint dwMapFlags, string lpSrcStr, int cchSrc, StringBuilder lpDestStr, int cchDest ) ;
  }
}