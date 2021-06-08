using System ;
using System.Globalization ;
using System.Text ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Revit ;
using Arent3d.Utility ;
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

    public RouteSegment( MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, IEndPoint fromEndPoint, IEndPoint toEndPoint, double? preferredNominalDiameter, bool isRoutingOnPipeSpace, double? fixedBopHeight, AvoidType avoidType )
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
  internal class RouteSegmentConverter : StorableConverterBase<RouteSegment>
  {
    protected override RouteSegment Parse( Element storedElement, Parser parser )
    {
      var preferredDiameter = parser.GetDouble( 0 ) ;
      var fromId = EndPointExtensions.ParseEndPoint( storedElement.Document, parser.GetString( 1 ) ?? throw new InvalidOperationException() ) ?? throw new InvalidOperationException() ;
      var toId = EndPointExtensions.ParseEndPoint( storedElement.Document, parser.GetString( 2 ) ?? throw new InvalidOperationException() ) ?? throw new InvalidOperationException() ;
      var isRoutingOnPipeSpace = parser.GetBool( 3 ) ?? throw new InvalidOperationException() ;
      var curveType = parser.GetElement<MEPCurveType>( 4, storedElement.Document ) ?? throw new InvalidOperationException() ;
      var fixedBopHeight = parser.GetDouble( 5 ) ;
      var avoidType = parser.GetEnum<AvoidType>( 6 ) ?? throw new InvalidOperationException() ;
      var classificationInfo = MEPSystemClassificationInfo.Deserialize( parser.GetString( 7 ) ?? throw new InvalidOperationException() ) ?? throw new InvalidOperationException() ;
      var systemType = parser.GetElement<MEPSystemType>( 8, storedElement.Document ) ?? throw new InvalidOperationException() ;

      return new RouteSegment( classificationInfo, systemType, curveType, fromId, toId, preferredDiameter, isRoutingOnPipeSpace, fixedBopHeight, avoidType ) ;
    }

    protected override Stringifier Stringify( Element storedElement, RouteSegment customTypeValue )
    {
      var stringifier = new Stringifier() ;

      stringifier.Add( customTypeValue.PreferredNominalDiameter ) ;
      stringifier.Add( customTypeValue.FromEndPoint.ToString() ) ;
      stringifier.Add( customTypeValue.ToEndPoint.ToString() ) ;
      stringifier.Add( customTypeValue.IsRoutingOnPipeSpace ) ;
      stringifier.Add( customTypeValue.CurveType ) ;
      stringifier.Add( customTypeValue.FixedBopHeight ) ;
      stringifier.Add( customTypeValue.AvoidType ) ;
      stringifier.Add( customTypeValue.SystemClassificationInfo.Serialize() ) ;
      stringifier.Add( customTypeValue.SystemType ) ;

      return stringifier ;
    }
  }
}