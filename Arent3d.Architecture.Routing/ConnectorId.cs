using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public readonly struct ConnectorIds : IEquatable<ConnectorIds>
  {
    public int ElementId { get ; }
    public int ConnectorId { get ; }

    public ConnectorIds( int elementId, int connectorId )
    {
      ElementId = elementId ;
      ConnectorId = connectorId ;
    }

    public ConnectorIds( Connector connector ) : this( connector.Owner.Id.IntegerValue, connector.Id )
    {
    }

    public bool Equals( ConnectorIds other )
    {
      return ElementId == other.ElementId && ConnectorId == other.ConnectorId ;
    }

    public override bool Equals( object? obj )
    {
      return obj is ConnectorIds other && Equals( other ) ;
    }

    public override int GetHashCode()
    {
      unchecked {
        return ( ElementId * 397 ) ^ ConnectorId ;
      }
    }

    public static bool operator ==( ConnectorIds left, ConnectorIds right )
    {
      return left.Equals( right ) ;
    }

    public static bool operator !=( ConnectorIds left, ConnectorIds right )
    {
      return ! left.Equals( right ) ;
    }
  }
}