using System ;
using System.Text ;
using Arent3d.Architecture.Routing.EndPoint ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class RouteInfo
  {
    public IEndPointIndicator FromId { get ; }
    public IEndPointIndicator ToId { get ; }

    public int[] PassPoints { get ; }

    public RouteInfo( IEndPointIndicator fromId, IEndPointIndicator toId, params int[] passPoints )
    {
      FromId = fromId ;
      ToId = toId ;
      PassPoints = passPoints ;
    }
  }

  [StorableConverterOf( typeof( RouteInfo ) )]
  internal class RouteInfoConverter : StorableConverterBase<RouteInfo, string>
  {
    private static readonly char[] FieldSplitter = { '|' } ;
    
    protected override RouteInfo NativeToCustom( Element storedElement, string nativeTypeValue )
    {
      var split = nativeTypeValue.Split( FieldSplitter, StringSplitOptions.RemoveEmptyEntries ) ;
      var fromId = EndPointIndicator.ParseIndicator( 0 < split.Length ? split[ 0 ] : string.Empty ) ?? throw new InvalidOperationException() ;
      var toId = EndPointIndicator.ParseIndicator( 1 < split.Length ? split[ 1 ] : string.Empty ) ?? throw new InvalidOperationException() ;
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

      builder.Append( customTypeValue.FromId ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.ToId ) ;
      foreach ( var id in customTypeValue.PassPoints ) {
        builder.Append( '|' ) ;
        builder.Append( id ) ;
      }

      return builder.ToString() ;
    }
  }
}