using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class CoordinateIndicator : IEquatable<CoordinateIndicator>, IEndPointIndicator
  {
    public (Route? Route, SubRoute? SubRoute) ParentBranch( Document document ) => ( null, null ) ;  // CoordinateIndicator has no parent branch.

    public XYZ Origin { get ; }
    public XYZ Direction { get ; }
    public bool IsOneSided => false ;

    public CoordinateIndicator( XYZ origin, XYZ direction )
    {
      Origin = origin ;
      Direction = direction ;
    }

    public EndPointBase? GetEndPoint( Document document, SubRoute subRoute )
    {
      return new CoordinateEndPoint( this, subRoute ) ;
    }

    public double? GetEndPointDiameter( Document document ) => null ;

    public bool IsValid( Document document, bool isFrom )
    {
      return true ;
    }

    public void Accept( IEndPointIndicatorVisitor visitor )
    {
      visitor.Visit( this ) ;
    }

    public T Accept<T>( IEndPointIndicatorVisitor<T> visitor )
    {
      return visitor.Visit( this ) ;
    }

    public bool Equals( CoordinateIndicator other )
    {
      return Equals( Origin, other.Origin ) && Equals( Direction, other.Direction ) ;
    }

    public override bool Equals( object? obj )
    {
      return obj is CoordinateIndicator other && Equals( other ) ;
    }

    public bool Equals( IEndPointIndicator indicator )
    {
      return indicator is ConnectorIndicator other && Equals( other ) ;
    }

    public override int GetHashCode()
    {
      unchecked {
        return ( GetHashCode( Origin ) * ( 397 * 397 * 397 ) ) ^ GetHashCode( Direction ) ;
      }
    }

    private static int GetHashCode( XYZ xyz )
    {
      unchecked {
        var hashCode = xyz.X.GetHashCode() ;
        hashCode = ( hashCode * 397 ) ^ xyz.Y.GetHashCode() ;
        hashCode = ( hashCode * 397 ) ^ xyz.Z.GetHashCode() ;
        return hashCode ;
      }
    }


    public static bool operator ==( CoordinateIndicator left, CoordinateIndicator right )
    {
      return left.Equals( right ) ;
    }

    public static bool operator !=( CoordinateIndicator left, CoordinateIndicator right )
    {
      return ! left.Equals( right ) ;
    }

    public override string ToString()
    {
      return EndPointIndicator.ToString( this ) ;
    }

    public static CoordinateIndicator? Parse( string str )
    {
      return EndPointIndicator.ParseCoordinateIndicator( str ) ;
    }
  }
}