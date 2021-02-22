using System ;
using System.Collections.Generic ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public interface IEndPointIndicator : IEquatable<IEndPointIndicator>
  {
    bool IsInvalid { get ; }

    EndPoint? GetEndPoint( Document document, SubRoute subRoute, bool isFrom ) ;
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
        "c:" => ToIEndPointIndicator( ParseConnectorIndicatorImpl( str.Substring( 2 ) ) ),
        "p:" => ToIEndPointIndicator( ParsePassPointEndIndicatorImpl( str.Substring( 2 ) ) ),
        _ => null,
      } ;
    }

    private static IEndPointIndicator? ToIEndPointIndicator( IEndPointIndicator indicator )
    {
      if ( indicator.IsInvalid ) return null ;
      return indicator ;
    }

    public static ConnectorIndicator ParseConnectorIndicator( string str )
    {
      if ( false == str.StartsWith( "c:" ) ) return ConnectorIndicator.InvalidConnectorIndicator ;

      return ParseConnectorIndicatorImpl( str.Substring( 2 ) ) ;
    }

    public static PassPointEndIndicator ParsePassPointEndIndicator( string str )
    {
      if ( false == str.StartsWith( "p:" ) ) return PassPointEndIndicator.InvalidConnectorIndicator ;

      return ParsePassPointEndIndicatorImpl( str.Substring( 2 ) ) ;
    }

    private static ConnectorIndicator ParseConnectorIndicatorImpl( string substring )
    {
      var array = substring.Split( ConnectPointSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return ConnectorIndicator.InvalidConnectorIndicator ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == int.TryParse( array[ 1 ], out var connId ) ) return ConnectorIndicator.InvalidConnectorIndicator ;

      return new ConnectorIndicator( elmId, connId ) ;
    }

    private static PassPointEndIndicator ParsePassPointEndIndicatorImpl( string substring )
    {
      var array = substring.Split( PassPointEndSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return PassPointEndIndicator.InvalidConnectorIndicator ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == TryParseEndSide( array[ 1 ], out var endSide ) ) return PassPointEndIndicator.InvalidConnectorIndicator ;

      return new PassPointEndIndicator( elmId, endSide ) ;
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

    public static string ToString( ConnectorIndicator ep )
    {
      return $"c:{ep.ElementId}/{ep.ConnectorId}" ;
    }

    public static string ToString( PassPointEndIndicator ep )
    {
      return $"p:{ep.ElementId}/{( ep.SideType == PassPointEndSide.Forward ? '+' : '-' )}" ;
    }

    #endregion
  }
}