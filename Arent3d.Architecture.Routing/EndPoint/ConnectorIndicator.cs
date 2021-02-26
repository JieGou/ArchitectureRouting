using System ;
using Arent3d.Routing ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.EndPoint
{
  public class ConnectorIndicator : IEquatable<ConnectorIndicator>, IEndPointIndicator
  {
    public Route? ParentBranch( Document document ) => null ;  // ConnectorIndicator has no parent branch.

    public int ElementId { get ; }
    public int ConnectorId { get ; }

    public ConnectorIndicator( int elementId, int connectorId )
    {
      ElementId = elementId ;
      ConnectorId = connectorId ;
    }

    public ConnectorIndicator( Connector connector ) : this( connector.Owner.Id.IntegerValue, connector.Id )
    {
    }

    public Connector? GetConnector( Document document )
    {
      return document.FindConnector( this ) ;
    }

    public EndPointBase? GetAutoRoutingEndPoint( Document document, SubRoute subRoute, bool isFrom )
    {
      var conn = GetConnector( document ) ;
      if ( null == conn ) return null ;

      return new ConnectorEndPoint( subRoute.Route, conn, isFrom ) ;
    }

    public bool Equals( ConnectorIndicator other )
    {
      return ElementId == other.ElementId && ConnectorId == other.ConnectorId ;
    }

    public bool Equals( IEndPointIndicator indicator )
    {
      return indicator is ConnectorIndicator other && Equals( other ) ;
    }

    public override bool Equals( object? obj )
    {
      return obj is ConnectorIndicator other && Equals( other ) ;
    }

    public override int GetHashCode()
    {
      unchecked {
        return ( ElementId * 397 ) ^ ConnectorId ;
      }
    }

    public static bool operator ==( ConnectorIndicator left, ConnectorIndicator right )
    {
      return left.Equals( right ) ;
    }

    public static bool operator !=( ConnectorIndicator left, ConnectorIndicator right )
    {
      return ! left.Equals( right ) ;
    }

    public override string ToString()
    {
      return EndPointIndicator.ToString( this ) ;
    }

    public static ConnectorIndicator? Parse( string str )
    {
      return EndPointIndicator.ParseConnectorIndicator( str ) ;
    }
  }
}