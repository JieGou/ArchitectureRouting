using System ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public readonly struct ConnectorIndicator : IEquatable<ConnectorIndicator>, IEndPointIndicator
  {
    public static readonly ConnectorIndicator InvalidConnectorIndicator = new ConnectorIndicator( 0, 0 ) ;
    
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

    public EndPoint? GetEndPoint( Document document, SubRoute subRoute, bool isFrom )
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
      return $"c:{ElementId}/{ConnectorId}" ;
    }

    private static readonly char[] ConnectPointSplitter = { '/' } ;
    public static ConnectorIndicator Parse( string str )
    {
      if ( false == str.StartsWith( "c:" ) ) return InvalidConnectorIndicator ;

      var array = str.Substring( 2 ).Split( ConnectPointSplitter, 2, StringSplitOptions.RemoveEmptyEntries ) ;
      if ( array.Length < 2 ) return InvalidConnectorIndicator ;

      if ( false == int.TryParse( array[ 0 ], out var elmId ) || false == int.TryParse( array[ 1 ], out var connId ) ) return InvalidConnectorIndicator ;

      return new ConnectorIndicator( elmId, connId ) ;
    }
  }
}