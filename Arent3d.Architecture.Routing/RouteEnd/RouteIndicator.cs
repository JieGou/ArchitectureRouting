using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.RouteEnd
{
  public class RouteIndicator : IEquatable<RouteIndicator>, IEndPointIndicator
  {
    public string RouteName { get ; }
    public int SubRouteIndex { get ; }
    public bool IsOneSided => false ;

    public RouteIndicator( string routeName, int subRouteIndex )
    {
      RouteName = routeName ;
      SubRouteIndex = subRouteIndex ;
    }

    public SubRoute? GetSubRoute( Document document )
    {
      if ( false == CommandTermCaches.RouteCache.Get( document ).TryGetValue( RouteName, out var route ) ) return null ;

      return route.GetSubRoute( SubRouteIndex ) ;
    }

    public EndPointBase? GetEndPoint( Document document, SubRoute subRoute )
    {
      return new RouteEndPoint( this, subRoute ) ;
    }

    public double? GetEndPointDiameter( Document document ) => null ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch( Document document )
    {
      if ( false == CommandTermCaches.RouteCache.Get( document ).TryGetValue( RouteName, out var route ) ) return ( null, null ) ;
      return ( route, route.GetSubRoute( SubRouteIndex ) ) ;
    }

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

    public bool Equals( RouteIndicator other )
    {
      return Equals( RouteName, other.RouteName ) ;
    }

    public override bool Equals( object? obj )
    {
      return obj is RouteIndicator other && Equals( other ) ;
    }

    public bool Equals( IEndPointIndicator indicator )
    {
      return indicator is RouteIndicator other && Equals( other ) ;
    }

    public override int GetHashCode()
    {
      return RouteName.GetHashCode() ;
    }

    public override string ToString()
    {
      return EndPointIndicator.ToString( this ) ;
    }

    public static RouteIndicator? Parse( string str )
    {
      return EndPointIndicator.ParseRouteIndicator( str ) ;
    }
  }
}