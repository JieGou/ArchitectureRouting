using System ;
using System.Globalization ;
using System.Text ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public class RouteSegment
  {
    public MEPCurveType? CurveType { get ; set ; }

    public double? PreferredNominalDiameter { get ; private set ; }

    public IEndPoint FromEndPoint { get ; }
    public IEndPoint ToEndPoint { get ; }
    public bool IsRoutingOnPipeSpace { get ; internal set ; } = false ;

    public double? GetRealNominalDiameter()
    {
      if ( PreferredNominalDiameter.HasValue ) return PreferredNominalDiameter.Value ;

      return GetRealNominalDiameterFromEndPoints() ;
    }

    private double? GetRealNominalDiameterFromEndPoints()
    {
      if ( FromEndPoint.GetDiameter() is { } d1 ) {
        return d1 ;
      }
      if ( ToEndPoint.GetDiameter() is { } d2 ) {
        return d2 ;
      }

      return null ;
    }

    public void ChangePreferredNominalDiameter( double d )
    {
      PreferredNominalDiameter = d ;
    }

    public bool ApplyRealNominalDiameter()
    {
      if ( GetRealNominalDiameterFromEndPoints() is not {} d ) return false ;

      PreferredNominalDiameter = d ;
      return true ;
    }

    public RouteSegment( IEndPoint fromEndPoint, IEndPoint toEndPoint, double? preferredNominalDiameter, bool isRoutingOnPipeSpace )
    {
      PreferredNominalDiameter = ( 0 < preferredNominalDiameter ? preferredNominalDiameter : null ) ;
      IsRoutingOnPipeSpace = isRoutingOnPipeSpace ;
      FromEndPoint = fromEndPoint ;
      ToEndPoint = toEndPoint ;
    }
  }


  [StorableConverterOf( typeof( RouteSegment ) )]
  internal class RouteInfoConverter : StorableConverterBase<RouteSegment, string>
  {
    private static readonly char[] FieldSplitter = { '|' } ;
    
    protected override RouteSegment NativeToCustom( Element storedElement, string nativeTypeValue )
    {
      var split = nativeTypeValue.Split( FieldSplitter, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( 5 != split.Length ) throw new InvalidOperationException() ;

      var preferredDiameter = ( double.TryParse( split[ 0 ], NumberStyles.Any, CultureInfo.InvariantCulture, out var nominalDiameter ) ? (double?) nominalDiameter : null ) ;

      var fromId = EndPointExtensions.ParseEndPoint( storedElement.Document, split[ 1 ] ) ?? throw new InvalidOperationException() ;
      var toId = EndPointExtensions.ParseEndPoint( storedElement.Document, split[ 2 ] ) ?? throw new InvalidOperationException() ;
      var isRoutingOnPipeSpace =  ParseBool( split[ 3 ] ) ;
      var curveType = storedElement.Document.GetElementById<MEPCurveType>( ParseElementId( split[ 4 ] ) ) ;

      return new RouteSegment( fromId, toId, preferredDiameter, isRoutingOnPipeSpace ) ;
    }

    private static bool ParseBool( string s )
    {
      return s.ToLower() switch
      {
        "0" => false,
        "f" => false,
        "" => false,
        "1" => true,
        "t" => true,
        _ => false,
      } ;
    }

    private static ElementId ParseElementId( string s )
    {
      if ( false == int.TryParse( s, out var id ) ) return ElementId.InvalidElementId ;
      if ( ElementId.InvalidElementId.IntegerValue == id ) return ElementId.InvalidElementId ;

      return new ElementId( id ) ;
    }

    protected override string CustomToNative( Element storedElement, RouteSegment customTypeValue )
    {
      var builder = new StringBuilder() ;

      builder.Append( customTypeValue.PreferredNominalDiameter?.ToString( CultureInfo.InvariantCulture ) ?? "---" ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.FromEndPoint ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.ToEndPoint ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.IsRoutingOnPipeSpace ? 'T' : 'F' ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.CurveType.GetValidId().IntegerValue ) ;

      return builder.ToString() ;
    }
  }
}