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
    public MEPSystemClassificationInfo SystemClassificationInfo { get ; set ; }
    public MEPSystemType? SystemType { get ; set ; }
    public MEPCurveType? CurveType { get ; set ; }

    public double? PreferredNominalDiameter { get ; private set ; }
    
    public double? FixedBopHeight { get ; set ; }
    
    public AvoidType AvoidType { get ; set ; }

    public IEndPoint FromEndPoint { get ; private set ; }
    public IEndPoint ToEndPoint { get ; private set ; }
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

    public RouteSegment( MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType curveType, IEndPoint fromEndPoint, IEndPoint toEndPoint )
      : this( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, null, false, null, AvoidType.Whichever )
    {
    }

    public static RouteSegment Restore( MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, IEndPoint fromEndPoint, IEndPoint toEndPoint, double? preferredNominalDiameter, bool isRoutingOnPipeSpace, double? fixedBopHeight, AvoidType avoidType )
    {
      return new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, preferredNominalDiameter, isRoutingOnPipeSpace, fixedBopHeight, avoidType ) ;
    }

    private RouteSegment( MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, IEndPoint fromEndPoint, IEndPoint toEndPoint, double? preferredNominalDiameter, bool isRoutingOnPipeSpace, double? fixedBopHeight, AvoidType avoidType )
    {
      SystemClassificationInfo = classificationInfo ;
      SystemType = systemType ;
      CurveType = curveType ;

      PreferredNominalDiameter = ( 0 < preferredNominalDiameter ? preferredNominalDiameter : null ) ;
      IsRoutingOnPipeSpace = isRoutingOnPipeSpace ;
      FixedBopHeight = fixedBopHeight ;
      AvoidType = avoidType ;
      FromEndPoint = fromEndPoint ;
      ToEndPoint = toEndPoint ;
    }

    public void ReplaceEndPoint( IEndPoint oldEndPoint, IEndPoint newEndPoint )
    {
      if ( false == oldEndPoint.IsReplaceable ) throw new InvalidOperationException() ;

      if ( FromEndPoint == oldEndPoint ) {
        FromEndPoint = newEndPoint ;
      }

      if ( ToEndPoint == oldEndPoint ) {
        ToEndPoint = oldEndPoint ;
      }
    }
  }


  [StorableConverterOf( typeof( RouteSegment ) )]
  internal class RouteSegmentConverter : StorableConverterBase<RouteSegment, string>
  {
    private static readonly char[] FieldSplitter = { '|' } ;

    protected override RouteSegment NativeToCustom( Element storedElement, string nativeTypeValue )
    {
      var split = nativeTypeValue.Split( FieldSplitter, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( 9 != split.Length ) throw new InvalidOperationException() ;

      var preferredDiameter = ParseNullableDouble( split[ 0 ] ) ;
      var fromId = EndPointExtensions.ParseEndPoint( storedElement.Document, split[ 1 ] ) ?? throw new InvalidOperationException() ;
      var toId = EndPointExtensions.ParseEndPoint( storedElement.Document, split[ 2 ] ) ?? throw new InvalidOperationException() ;
      var isRoutingOnPipeSpace = ParseBool( split[ 3 ] ) ;
      var curveType = storedElement.Document.GetElementById<MEPCurveType>( ParseElementId( split[ 4 ] ) ) ;

      var fixedBopHeight = ParseNullableDouble( split[ 5 ] ) ;

      if ( false == Enum.TryParse( split[ 6 ], out AvoidType avoidType ) ) throw new InvalidOperationException() ;
      var classificationInfo = MEPSystemClassificationInfo.Deserialize( split[ 7 ] ) ?? throw new InvalidOperationException() ;
      var systemType = storedElement.Document.GetElementById<MEPSystemType>( ParseElementId( split[ 8 ] ) ) ;

      return RouteSegment.Restore( classificationInfo, systemType, curveType, fromId, toId, preferredDiameter, isRoutingOnPipeSpace, fixedBopHeight, avoidType ) ;
    }

    private static double? ParseNullableDouble( string s )
    {
      return ( double.TryParse( s, NumberStyles.Any, CultureInfo.InvariantCulture, out var nominalDiameter ) ? nominalDiameter : null ) ;
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
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.FixedBopHeight?.ToString( CultureInfo.InvariantCulture ) ?? "---" ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.AvoidType.ToString() ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.SystemClassificationInfo.Serialize() ) ;
      builder.Append( '|' ) ;
      builder.Append( customTypeValue.SystemType.GetValidId().IntegerValue ) ;

      return builder.ToString() ;
    }
  }
}