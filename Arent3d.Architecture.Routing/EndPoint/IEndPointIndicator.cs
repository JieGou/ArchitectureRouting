using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.Text.RegularExpressions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoint
{
  public interface IEndPointIndicator : IEquatable<IEndPointIndicator>
  {
    EndPointBase? GetAutoRoutingEndPoint( Document document, SubRoute subRoute, bool isFrom ) ;
  }

  public static class EndPointIndicator
  {
    #region Parser

    private static readonly char[] ConnectPointSplitter = { '/' } ;
    private static readonly char[] PassPointEndSplitter = { '/' } ;
    private static readonly char[] IndicatorListSplitter = { '|' } ;

    public static IEnumerable<IEndPointIndicator> ParseIndicatorList( string str )
    {
      return str.Split( IndicatorListSplitter, StringSplitOptions.RemoveEmptyEntries ).Select( ParseIndicator ).NonNull() ;
    }

    public static IEndPointIndicator? ParseIndicator( string str )
    {
      return str.Substring( 0, 2 ) switch
      {
        "c:" => ParseConnectorIndicatorImpl( str.Substring( 2 ) ),
        "p:" => ParsePassPointEndIndicatorImpl( str.Substring( 2 ) ),
        "o:" => ParseCoordinateIndicatorImpl( str.Substring( 2 ) ),
        _ => null,
      } ;
    }

    public static ConnectorIndicator? ParseConnectorIndicator( string str )
    {
      if ( false == str.StartsWith( "c:" ) ) return null ;

      return ParseConnectorIndicatorImpl( str.Substring( 2 ) ) ;
    }

    public static PassPointEndIndicator? ParsePassPointEndIndicator( string str )
    {
      if ( false == str.StartsWith( "p:" ) ) return null ;

      return ParsePassPointEndIndicatorImpl( str.Substring( 2 ) ) ;
    }

    public static CoordinateIndicator? ParseCoordinateIndicator( string str )
    {
      if ( false == str.StartsWith( "o:" ) ) return null ;

      return ParseCoordinateIndicatorImpl( str.Substring( 2 ) ) ;
    }

    private static ConnectorIndicator? ParseConnectorIndicatorImpl( string substring )
    {
      var array = substring.Split( ConnectPointSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return null ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == int.TryParse( array[ 1 ], out var connId ) ) return null ;

      return new ConnectorIndicator( elmId, connId ) ;
    }

    private static PassPointEndIndicator? ParsePassPointEndIndicatorImpl( string substring )
    {
      var array = substring.Split( PassPointEndSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return null ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == TryParseEndSide( array[ 1 ], out var endSide ) ) return null ;

      return new PassPointEndIndicator( elmId, endSide ) ;
    }

    private static readonly Regex XYZRegex = new Regex( @"^\s*\(\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*\)\s*/\s*\(\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*\)\s*$", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant ) ;

    private static CoordinateIndicator? ParseCoordinateIndicatorImpl( string substring )
    {
      var match = XYZRegex.Match( substring ) ;
      if ( false == match.Success ) return null ;

      if ( false == double.TryParse( match.Groups[ 1 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var x ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 2 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var y ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 3 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var z ) ) return null ;

      if ( false == double.TryParse( match.Groups[ 1 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vx ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 2 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vy ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 3 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vz ) ) return null ;

      return new CoordinateIndicator( new XYZ( x, y, z ), new XYZ( vx, vy, vz ) ) ;
    }

    private static bool TryParseEndSide( string s, out PassPointEndSide endSide )
    {
      endSide = default ;

      if ( 1 != s.Length ) return false ;

      switch ( s[ 0 ] ) {
        case '+' :
          endSide = PassPointEndSide.Forward ;
          return true ;

        case '-' :
          endSide = PassPointEndSide.Reverse ;
          return true ;

        default : return false ;
      }
    }

    #endregion

    #region Stringifier

    public static string ToString( IEnumerable<IEndPointIndicator> indicators )
    {
      return string.Join( "|", indicators ) ;
    }

    public static string ToString( IEndPointIndicator ep )
    {
      return ep.ToString() ;
    }

    public static string ToString( ConnectorIndicator ep )
    {
      return $"c:{ep.ElementId}/{ep.ConnectorId}" ;
    }

    public static string ToString( PassPointEndIndicator ep )
    {
      return $"p:{ep.ElementId}/{( ep.SideType == PassPointEndSide.Forward ? '+' : '-' )}" ;
    }

    public static string ToString( CoordinateIndicator ep )
    {
      return $"o:{ToString( ep.Origin )}/{ToString( ep.Direction )}" ;
    }

    private static string ToString( XYZ xyz )
    {
      return FormattableString.Invariant( $"({xyz.X}, {xyz.Y}, {xyz.Z})" ) ;
    }

    #endregion
  }
}