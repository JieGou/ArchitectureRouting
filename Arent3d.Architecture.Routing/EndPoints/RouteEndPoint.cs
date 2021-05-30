using System ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public class RouteEndPoint : IEndPoint
  {
    public const string Type = "Route" ;

    private static readonly Regex _parameterParser = new Regex( @"^(.*)/(\d+)$", RegexOptions.Singleline | RegexOptions.Compiled ) ;

    public static RouteEndPoint? ParseParameterString( Document document, string str )
    {
      var match = _parameterParser.Match( str ) ;
      if ( false == match.Success ) return null ;

      return new RouteEndPoint( document,  match.Groups[ 1 ].Value, int.Parse( match.Groups[ 2 ].Value ) ) ;
    }



    public string TypeName => Type ;
    public EndPointKey Key => new EndPointKey( TypeName, ParameterString ) ;

    public bool IsReplaceable => true ;

    public bool IsOneSided => false ;

    private readonly Document _document ;

    public string ParameterString => $"{RouteName}/{SubRouteIndex}" ;
    public XYZ RoutingStartPosition => throw new InvalidOperationException() ;

    public string RouteName { get ; private set ; } = null! ;
    public int SubRouteIndex { get ; private set ; }

    public Route? GetRoute() => ParentBranch().Route ;
    public SubRoute? GetSubRoute() => ParentBranch().SubRoute ;

    public void UpdateRoute( string routeName, int subRouteIndex )
    {
      RouteName = routeName ;
      SubRouteIndex = subRouteIndex ;
    }

    public RouteEndPoint( SubRoute subRoute )
    {
      _document = subRoute.Route.Document ;
      UpdateRoute( subRoute.Route.RouteName, subRoute.SubRouteIndex ) ;
    }

    public RouteEndPoint( Document document, string routeName, int subRouteIndex )
    {
      _document = document ;
      UpdateRoute( routeName,subRouteIndex ) ;
    }

    public XYZ GetRoutingDirection( bool isFrom ) => throw new InvalidOperationException() ;
    public bool HasValidElement( bool isFrom ) => true ;

    public Connector? GetReferenceConnector() => GetSubRoute()?.GetReferenceConnector() ;

    public double? GetDiameter() => null ;

    public double GetMinimumStraightLength( RouteMEPSystem routeMepSystem, double edgeDiameter, bool isFrom ) => 0 ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch()
    {
      if ( false == RouteCache.Get( _document ).TryGetValue( RouteName, out var route ) ) return ( null, null ) ;

      return ( route, route.GetSubRoute( SubRouteIndex ) ) ;
    }

    public bool GenerateInstance( string routeName ) => false ;
    public bool EraseInstance() => false ;

    public override string ToString() => this.Stringify() ;

    public void Accept( IEndPointVisitor visitor ) => visitor.Visit( this ) ;
    public T Accept<T>( IEndPointVisitor<T> visitor ) => visitor.Visit( this ) ;
  }
}