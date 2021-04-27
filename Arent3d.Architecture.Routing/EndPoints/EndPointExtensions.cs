using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Routing ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using MathLib ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public static class EndPointExtensions
  {
    public static XYZ ForEndPointType( this XYZ direction, bool isFrom ) => isFrom ? direction : -direction ;

    public static Vector3d ForEndPointType( this Vector3d direction, bool isFrom ) => isFrom ? direction : -direction ;

    public static IEndPoint? GetEndPoint( this IAutoRoutingEndPoint endPoint )
    {
      return endPoint switch
      {
        AutoRoutingEndPoint ep => ep.EndPoint,
        IPseudoEndPoint pep => pep.Source.GetEndPoint(),
        _ => null,
      } ;
    }


    private static readonly char[] EndPointSplitter = { '|' } ;
    private static readonly char[] NameParamSplitter = { ':' } ;
    private static readonly Regex EscapeChars = new Regex( @"%:\|\+ ", RegexOptions.Singleline | RegexOptions.Compiled ) ;

    public static string Stringify( this IEnumerable<IEndPoint> endPoints )
    {
      return string.Join( "|", endPoints.Select( Stringify ) ) ;
    }

    public static string Stringify( this IEndPoint endPoint )
    {
      return $"{Escape( endPoint.TypeName )}:{Escape( endPoint.ParameterString )}" ;
    }

    public static IEnumerable<IEndPoint> ParseEndPoints( Document document, string str )
    {
      return str.Split( EndPointSplitter, StringSplitOptions.RemoveEmptyEntries ).Select( part => ParseEndPoint( document, part ) ).NonNull() ;
    }

    public static IEndPoint? ParseEndPoint( Document document, string str )
    {
      var array = str.Split( NameParamSplitter, StringSplitOptions.None ) ;
      if ( 2 != array.Length ) return null ;

      return ParseEndPoint( document, Unescape( array[ 0 ] ), Unescape( array[ 1 ] ) ) ;
    }

    public static IEndPoint? ParseEndPoint( Document document, string endPointType, string parameters )
    {
      return endPointType switch
      {
        ConnectorEndPoint.Type => ConnectorEndPoint.ParseParameterString( document, parameters ),
        PassPointEndPoint.Type => PassPointEndPoint.ParseParameterString( document, parameters ),
        RouteEndPoint.Type => RouteEndPoint.ParseParameterString( document, parameters ),
        TerminatePointEndPoint.Type => TerminatePointEndPoint.ParseParameterString( document, parameters ),
        _ => null,
      } ;
    }

    private static string Escape( string part )
    {
      return EscapeChars.Replace( part, match => Uri.HexEscape( match.Value[ 0 ] ) ) ;
    }

    private static string Unescape( string part )
    {
      return Uri.UnescapeDataString( part ) ;
    }
  }
}