using System ;
using System.Globalization ;
using System.Text ;
using Arent3d.Architecture.Routing.RouteEnd ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class RouteSegment
  {
    public double PreferredNominalDiameter { get ; private set ; }

    public IEndPointIndicator FromId { get ; }
    public IEndPointIndicator ToId { get ; }
    public bool IsRoutingOnPipeSpace { get ; internal set ; } = false ;

    public double? GetRealNominalDiameter( Document document )
    {
      if ( 0 < PreferredNominalDiameter ) return PreferredNominalDiameter ;

      return GetRealNominalDiameterFromEndPoints( document ) ;
    }

    private double? GetRealNominalDiameterFromEndPoints( Document document )
    {
      if ( FromId.GetEndPointDiameter( document ) is { } d1 ) {
        return d1 ;
      }
      if ( ToId.GetEndPointDiameter( document ) is { } d2 ) {
        return d2 ;
      }

      return null ;
    }

    public void ChangePreferredNominalDiameter( double d )
    {
      PreferredNominalDiameter = d ;
    }

    public bool ApplyRealNominalDiameter( Document document )
    {
      if ( GetRealNominalDiameterFromEndPoints( document ) is not {} d ) return false ;

      PreferredNominalDiameter = d ;
      return true ;
    }

    public RouteSegment( IEndPointIndicator fromId, IEndPointIndicator toId, double preferredNominalDiameter, bool isRoutingOnPipeSpace )
    {
      PreferredNominalDiameter = preferredNominalDiameter ;
      IsRoutingOnPipeSpace = isRoutingOnPipeSpace ;
      FromId = fromId ;
      ToId = toId ;
    }
  }

  [StorableConverterOf( typeof( RouteSegment ) )]
  internal class RouteInfoConverter : StorableConverterBase<RouteSegment, string>
  {
    private static readonly char[] FieldSplitter = { '|' } ;
    
    protected override RouteSegment NativeToCustom( Element storedElement, string nativeTypeValue )
    {
      var split = nativeTypeValue.Split( FieldSplitter, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( 3 != split.Length && 4 != split.Length ) throw new InvalidOperationException() ;

      if ( false == double.TryParse( split[ 0 ], NumberStyles.Any, CultureInfo.InvariantCulture, out var nominalDiameter ) ) throw new InvalidOperationException() ;
      var fromId = EndPointIndicator.ParseIndicator( split[ 1 ] ) ?? throw new InvalidOperationException() ;
      var toId = EndPointIndicator.ParseIndicator( split[ 2 ] ) ?? throw new InvalidOperationException() ;
      var isRoutingOnPipeSpace = ( 3 < split.Length && ParseBool( split[ 3 ], false ) ) ;

      return new RouteSegment( fromId, toId, nominalDiameter, isRoutingOnPipeSpace ) ;
    }

    private static bool ParseBool( string s, bool bDefault )
    {
      return s.ToLower() switch
      {
        "0" => false,
        "f" => false,
        "" => false,
        "1" => true,
        "t" => true,
        _ => bDefault,
      } ;
    }

    protected override string CustomToNative( Element storedElement, RouteSegment customTypeValue )
    {
      var builder = new StringBuilder() ;

      builder.Append( customTypeValue.PreferredNominalDiameter.ToString( CultureInfo.InvariantCulture ) ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.FromId ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.ToId ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.IsRoutingOnPipeSpace ? 'T' : 'F' ) ;

      return builder.ToString() ;
    }
  }
}