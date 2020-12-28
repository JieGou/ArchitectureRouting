namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Routing record from from-to CSV files.
  /// </summary>
  public readonly struct RouteRecord
  {
    public string RouteId { get ; }
    public ConnectorIds FromId { get ; }
    public ConnectorIds ToId { get ; }

    public RouteRecord( string routeId, ConnectorIds fromId, ConnectorIds toId )
    {
      RouteId = routeId ;
      FromId = fromId ;
      ToId = toId ;
    }
  }
}