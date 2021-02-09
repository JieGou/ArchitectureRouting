namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Routing record from from-to CSV files.
  /// </summary>
  public readonly struct RouteRecord
  {
    public string RouteId { get ; }

    public ConnectorIndicator FromId { get ; }
    public ConnectorIndicator ToId { get ; }

    public int[] PassPoints { get ; }

    public RouteRecord( string routeId, ConnectorIndicator fromId, ConnectorIndicator toId, params int[] passPoints )
    {
      RouteId = routeId ;
      FromId = fromId ;
      ToId = toId ;
      PassPoints = passPoints ;
    }

    public RouteRecord( string routeId, RouteInfo routeInfo ) : this( routeId, routeInfo.FromId, routeInfo.ToId, routeInfo.PassPoints )
    {
    }
  }
}