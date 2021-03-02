using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.Text.RegularExpressions ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  /// <summary>
  /// Base interface of an indicator.
  /// </summary>
  public interface IEndPointIndicator : IEquatable<IEndPointIndicator>
  {
    /// <summary>
    /// Gets an end point from the document.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="subRoute"></param>
    /// <returns></returns>
    EndPointBase? GetEndPoint( Document document, SubRoute subRoute ) ;

    /// <summary>
    /// Gets a parent route when the end point is dependent to it.
    /// </summary>
    /// <param name="document"></param>
    /// <returns></returns>
    (Route? Route, SubRoute? SubRoute) ParentBranch( Document document ) ;

    /// <summary>
    /// Gets whether the indicated point exists in the document.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="isFrom"></param>
    /// <returns></returns>
    bool IsValid( Document document, bool isFrom ) ;

    /// <summary>
    /// Gets whether this indicator is only one side of from-to.
    /// </summary>
    /// <returns>True if it can be either of from-end and to-end.</returns>
    bool IsOneSided { get ; }

    void Accept( IEndPointIndicatorVisitor visitor ) ;
    T Accept<T>( IEndPointIndicatorVisitor<T> visitor ) ;
  }

  public static class EndPointIndicator
  {
    #region Parser

    private static readonly char[] IndicatorSplitter = { '/' } ;
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
        "t:" => ParsePassPointBranchEndIndicatorImpl( str.Substring( 2 ) ),
        "r:" => ParseRouteIndicatorImpl( str.Substring( 2 ) ),
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

    public static PassPointBranchEndIndicator? ParsePassPointBranchEndIndicator( string str )
    {
      if ( false == str.StartsWith( "t:" ) ) return null ;

      return ParsePassPointBranchEndIndicatorImpl( str.Substring( 2 ) ) ;
    }

    public static RouteIndicator? ParseRouteIndicator( string str )
    {
      if ( false == str.StartsWith( "r:" ) ) return null ;

      return ParseRouteIndicatorImpl( str.Substring( 2 ) ) ;
    }

    private static ConnectorIndicator? ParseConnectorIndicatorImpl( string substring )
    {
      var array = substring.Split( IndicatorSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return null ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == int.TryParse( array[ 1 ], out var connId ) ) return null ;

      return new ConnectorIndicator( elmId, connId ) ;
    }

    private static PassPointEndIndicator? ParsePassPointEndIndicatorImpl( string substring )
    {
      if ( false == int.TryParse( substring, out var elmId ) ) return null ;

      return new PassPointEndIndicator( elmId ) ;
    }

    private static readonly Regex XYZRegex = new Regex( @"^\s*\(\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*\)\s*/\s*\(\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*,\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[Ee][+-]?\d+)?)\s*\)\s*$", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant ) ;

    private static CoordinateIndicator? ParseCoordinateIndicatorImpl( string substring )
    {
      var match = XYZRegex.Match( substring ) ;
      if ( false == match.Success ) return null ;

      if ( false == double.TryParse( match.Groups[ 1 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var x ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 2 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var y ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 3 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var z ) ) return null ;

      if ( false == double.TryParse( match.Groups[ 4 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vx ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 5 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vy ) ) return null ;
      if ( false == double.TryParse( match.Groups[ 6 ].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var vz ) ) return null ;

      return new CoordinateIndicator( new XYZ( x, y, z ), new XYZ( vx, vy, vz ) ) ;
    }

    private static PassPointBranchEndIndicator? ParsePassPointBranchEndIndicatorImpl( string substring )
    {
      var array = substring.Split( IndicatorSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return null ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == double.TryParse( array[ 1 ], NumberStyles.Any, CultureInfo.InvariantCulture, out var angle ) ) return null ;

      return new PassPointBranchEndIndicator( elmId, angle ) ;
    }

    private static RouteIndicator? ParseRouteIndicatorImpl( string substring )
    {
      var array = substring.Split( IndicatorSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return null ;

      if ( false == int.TryParse( array[ 1 ], out var subRouteIndex ) ) return null ;

      return new RouteIndicator( Unescape( array[ 0 ] ), subRouteIndex ) ;
    }

    private static string Unescape( string str )
    {
      return Uri.UnescapeDataString( str ) ;
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
      return $"p:{ep.ElementId}" ;
    }

    public static string ToString( CoordinateIndicator ep )
    {
      return $"o:{ToString( ep.Origin )}/{ToString( ep.Direction )}" ;
    }

    public static string ToString( PassPointBranchEndIndicator ep )
    {
      return FormattableString.Invariant( $"t:{ep.ElementId}/{ep.AngleDegree}" ) ;
    }

    public static string ToString( RouteIndicator ep )
    {
      return FormattableString.Invariant( $"r:{Escape( ep.RouteName )}/{ep.SubRouteIndex}" ) ;
    }

    private static string ToString( XYZ xyz )
    {
      return FormattableString.Invariant( $"({xyz.X}, {xyz.Y}, {xyz.Z})" ) ;
    }

    private static string Escape( string str )
    {
      return Uri.EscapeDataString( str ) ;
    }

    #endregion
  }
}