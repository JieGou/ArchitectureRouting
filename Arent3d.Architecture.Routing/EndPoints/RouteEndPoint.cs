using System ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  public class RouteEndPoint : IEndPoint
  {
    public const string Type = "Route" ;

    private enum SerializeField
    {
      RouteName,
      SubRouteIndex,
      ReferenceEndPoint,
    }

    public static RouteEndPoint? ParseParameterString( Document document, string str )
    {
      var deserializer = new DeserializerObject<SerializeField>( str ) ;

      if ( deserializer.GetString( SerializeField.RouteName ) is not { } routeName ) return null ;
      if ( deserializer.GetInt( SerializeField.SubRouteIndex ) is not { } subRouteIndex ) return null ;
      var referenceEndPointKey = deserializer.GetEndPointKey( SerializeField.ReferenceEndPoint ) ;

      return new RouteEndPoint( document, routeName, subRouteIndex, referenceEndPointKey ) ;
    }
    public string ParameterString
    {
      get
      {
        var stringifier = new SerializerObject<SerializeField>() ;

        stringifier.AddNonNull( SerializeField.RouteName, RouteName ) ;
        stringifier.Add( SerializeField.SubRouteIndex, SubRouteIndex ) ;
        stringifier.AddNullable( SerializeField.ReferenceEndPoint, ReferenceEndPointKey ) ;

        return stringifier.ToString() ;
      }
    }


    public string TypeName => Type ;
    public EndPointKey Key => new EndPointKey( TypeName, ParameterString ) ;

    public EndPointKey? ReferenceEndPointKey { get ; }

    public bool IsReplaceable => true ;

    public bool IsOneSided => false ;

    private readonly Document _document ;

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

    public RouteEndPoint( SubRoute subRoute, EndPointKey? referenceEndPointKey )
    {
      _document = subRoute.Route.Document ;
      UpdateRoute( subRoute.Route.RouteName, subRoute.SubRouteIndex ) ;
      ReferenceEndPointKey = referenceEndPointKey ;
    }

    private RouteEndPoint( Document document, string routeName, int subRouteIndex, EndPointKey? referenceEndPointKey )
    {
      _document = document ;
      UpdateRoute( routeName, subRouteIndex ) ;
      ReferenceEndPointKey = referenceEndPointKey ;
    }

    public XYZ GetRoutingDirection( bool isFrom ) => throw new InvalidOperationException() ;
    public bool HasValidElement( bool isFrom ) => true ;

    public Connector? GetReferenceConnector() => GetSubRoute()?.GetReferenceConnector() ;

    public double? GetDiameter() => null ;

    public double GetMinimumStraightLength( double edgeDiameter, bool isFrom ) => 0 ;

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