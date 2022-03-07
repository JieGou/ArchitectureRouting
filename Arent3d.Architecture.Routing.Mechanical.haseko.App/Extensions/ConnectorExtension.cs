using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Extensions
{
  public static class ConnectorExtension
  {
    
    public static bool HasConnected(this ConnectorManager connectorManagerFirst, ConnectorManager connectorManagerSecond )
    {
      var connectorFirsts = GetConnectors( connectorManagerFirst ) ;
      if ( connectorFirsts.Count == 0 )
        return false ;

      var connectorSeconds = GetConnectors( connectorManagerSecond ) ;
      if ( connectorSeconds.Count == 0 )
        return false ;

      return connectorFirsts.Any( x => x.HasConnected( connectorSeconds ) ) ;
    }
    
    public static bool HasConnected(this Connector connector, List<Connector> otherConnector )
    {
      return otherConnector.Any( x => x.IsConnectedTo( connector ) ) ;
    }
    
    private static List<Connector> GetConnectors( ConnectorManager connectorManager )
    {
      var connectors = new List<Connector>() ;
      
      var connector = connectorManager.Connectors.GetEnumerator() ;
      while ( connector.MoveNext() ) {
        connectors.Add((Connector)connector.Current!);
      }

      return connectors ;
    }
  }
}