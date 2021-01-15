namespace Arent3d.Architecture.Routing
{
  public readonly struct ConnectorIds
  {
    public int ElementId { get ; }
    public int ConnectorId { get ; }

    public ConnectorIds( int elementId, int connectorId )
    {
      ElementId = elementId ;
      ConnectorId = connectorId ;
    }
  }
}