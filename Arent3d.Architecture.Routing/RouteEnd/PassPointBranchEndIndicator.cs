using System ;
using Arent3d.Architecture.Routing.CommandTermCaches ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class PassPointBranchEndIndicator : IEquatable<PassPointBranchEndIndicator>, IEndPointIndicator
  {
    public (Route? Route, SubRoute? SubRoute) ParentBranch( Document document )
    {
      var routeName = GetPassPointElement( document )?.GetRouteName() ;
      if ( null == routeName ) return ( null, null ) ;

      RouteCache.Get( document ).TryGetValue( routeName, out var route ) ;
      return ( route, null ) ;
    }

    public int ElementId { get ; }
    public double AngleDegree { get ; }
    public bool IsOneSided => true ;

    public PassPointBranchEndIndicator( int elementId, double angleDegree )
    {
      ElementId = elementId ;
      AngleDegree = angleDegree ;
    }

    public double? GetEndPointDiameter( Document document ) => null ;

    private FamilyInstance? GetPassPointElement( Document document )
    {
      return document.FindPassPointElement( ElementId ) ;
    }

    public EndPointBase? GetEndPoint( Document document, SubRoute subRoute )
    {
      var familyInstance = GetPassPointElement( document ) ;
      if ( null == familyInstance ) return null ;

      return new PassPointBranchEndPoint( subRoute.Route, familyInstance, AngleDegree, subRoute.GetReferenceConnector() ) ;
    }

    public bool IsValid( Document document, bool isFrom )
    {
      return ( null != GetPassPointElement( document ) ) ;
    }

    public void Accept( IEndPointIndicatorVisitor visitor )
    {
      visitor.Visit( this ) ;
    }

    public T Accept<T>( IEndPointIndicatorVisitor<T> visitor )
    {
      return visitor.Visit( this ) ;
    }

    public bool Equals( PassPointBranchEndIndicator other )
    {
      return ElementId == other.ElementId && AngleDegree == other.AngleDegree ;
    }

    public bool Equals( IEndPointIndicator indicator )
    {
      return indicator is PassPointBranchEndIndicator other && Equals( other ) ;
    }

    public override bool Equals( object? obj )
    {
      return obj is PassPointBranchEndIndicator other && Equals( other ) ;
    }

    public override int GetHashCode()
    {
      unchecked {
        return ( ElementId * 397 ) ^ AngleDegree.GetHashCode() ;
      }
    }

    public static bool operator ==( PassPointBranchEndIndicator left, PassPointBranchEndIndicator right )
    {
      return left.Equals( right ) ;
    }

    public static bool operator !=( PassPointBranchEndIndicator left, PassPointBranchEndIndicator right )
    {
      return ! left.Equals( right ) ;
    }

    public override string ToString()
    {
      return EndPointIndicator.ToString( this ) ;
    }

    public static PassPointBranchEndIndicator? Parse( string str )
    {
      return EndPointIndicator.ParsePassPointBranchEndIndicator( str ) ;
    }
  }
}