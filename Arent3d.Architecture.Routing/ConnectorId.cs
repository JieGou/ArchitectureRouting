namespace Arent3d.Architecture.Routing
{
  public readonly struct ConnectorIds
  {
    public int ElementId { get ; init ; }
    public int ConnectorId { get ; init ; }

    public ConnectorIds( int elementId, int connectorId )
    {
      ElementId = elementId ;
      ConnectorId = connectorId ;
    }
  }
}