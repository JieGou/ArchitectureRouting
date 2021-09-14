using System ;
using System.Collections.Generic ;
using System.Diagnostics ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility.Serialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoints
{
  [DebuggerDisplay("{Key}")]
  public class RouteEndPoint : IRouteBranchEndPoint
  {
    public const string Type = "Route" ;

    private enum SerializeField
    {
      RouteName,
      SubRouteIndex,
    }

    public static RouteEndPoint? ParseParameterString( Document document, string str )
    {
      var deserializer = new DeserializerObject<SerializeField>( str ) ;

      if ( deserializer.GetString( SerializeField.RouteName ) is not { } routeName ) return null ;
      if ( deserializer.GetInt( SerializeField.SubRouteIndex ) is not { } subRouteIndex ) return null ;

      return new RouteEndPoint( document, routeName, subRouteIndex ) ;
    }
    public string ParameterString
    {
      get
      {
        var stringifier = new SerializerObject<SerializeField>() ;

        stringifier.AddNonNull( SerializeField.RouteName, RouteName ) ;
        stringifier.Add( SerializeField.SubRouteIndex, SubRouteIndex ) ;

        return stringifier.ToString() ;
      }
    }


    public string TypeName => Type ;
    public string DisplayTypeName => "EndPoint.DisplayTypeName.Route".GetAppStringByKeyOrDefault( TypeName ) ;
    public EndPointKey Key => new EndPointKey( TypeName, ParameterString ) ;

    internal static RouteEndPoint? FromKeyParam( Document document, string param ) => ParseParameterString( document, param ) ;

    public bool IsReplaceable => true ;

    public bool IsOneSided => false ;

    private readonly Document _document ;

    public XYZ RoutingStartPosition => throw new InvalidOperationException() ;

    public string RouteName { get ; private set ; } = null! ;
    public int SubRouteIndex { get ; private set ; }

    public SubRoute GetSubRoute()
    {
      if ( false == RouteCache.Get( _document ).TryGetValue( RouteName, out var route ) ) throw new KeyNotFoundException() ;

      return route.GetSubRoute( SubRouteIndex ) ?? throw new KeyNotFoundException() ;
    }

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

    private RouteEndPoint( Document document, string routeName, int subRouteIndex )
    {
      _document = document ;
      UpdateRoute( routeName, subRouteIndex ) ;
    }

    public XYZ GetRoutingDirection( bool isFrom ) => throw new InvalidOperationException() ;
    public bool HasValidElement( bool isFrom ) => true ;

    public Connector? GetReferenceConnector() => GetSubRoute()?.GetReferenceConnector() ;

    public double? GetDiameter() => null ;

    public double GetMinimumStraightLength( double edgeDiameter, bool isFrom ) => 0 ;

    public (Route? Route, SubRoute? SubRoute) ParentBranch()
    {
      var subRoute = GetSubRoute() ;
      return ( subRoute.Route, subRoute ) ;
    }

    public bool GenerateInstance( string routeName ) => false ;
    public bool EraseInstance() => false ;

    public override string ToString() => this.Stringify() ;

    public void Accept( IEndPointVisitor visitor ) => visitor.Visit( this ) ;
    public T Accept<T>( IEndPointVisitor<T> visitor ) => visitor.Visit( this ) ;
  }
}