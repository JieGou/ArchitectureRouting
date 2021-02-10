using System ;
using System.Collections.Generic ;
using System.Text ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class RouteInfo
  {
    public ConnectorIndicator FromId { get ; }
    public ConnectorIndicator ToId { get ; }

    public int[] PassPoints { get ; }

    public RouteInfo( ConnectorIndicator fromId, ConnectorIndicator toId, params int[] passPoints )
    {
      FromId = fromId ;
      ToId = toId ;
      PassPoints = passPoints ;
    }
  }

  [StorableConverterOf( typeof( RouteInfo ) )]
  internal class RouteInfoConverter : StorableConverterBase<RouteInfo, string>
  {
    private static readonly char[] FieldSplitter = { ',' } ;
    
    protected override RouteInfo NativeToCustom( Element storedElement, string nativeTypeValue )
    {
      var split = nativeTypeValue.Split( FieldSplitter, StringSplitOptions.RemoveEmptyEntries ) ;
      var fromId = Parse( 0 < split.Length ? split[ 0 ] : string.Empty ) ;
      var toId = Parse( 1 < split.Length ? split[ 1 ] : string.Empty ) ;
      if ( split.Length <= 2 ) {
        return new RouteInfo( fromId, toId, Array.Empty<int>() ) ;
      }
      else {
        var passPoints = new int[ split.Length - 2 ] ;
        for ( var i = 0 ; i < passPoints.Length ; ++i ) {
          passPoints[ i ] = int.TryParse( split[ i + 2 ], out var id ) ? id : 0 ;
        }
        return new RouteInfo( fromId, toId, Array.FindAll( passPoints, id => 0 != id ) ) ;
      }
    }

    protected override string CustomToNative( Element storedElement, RouteInfo customTypeValue )
    {
      var builder = new StringBuilder() ;

      builder.Append( Stringify( customTypeValue.FromId ) ) ;
      builder.Append( ',' ) ;
      builder.Append( Stringify( customTypeValue.ToId ) ) ;
      foreach ( var id in customTypeValue.PassPoints ) {
        builder.Append( ',' ) ;
        builder.Append( id ) ;
      }

      return builder.ToString() ;
    }

    private static ConnectorIndicator Parse( string str )
    {
      return ConnectorIndicator.Parse( str ) ;
    }

    private static string Stringify( ConnectorIndicator indicator )
    {
      return indicator.ToString() ;
    }
  }
}