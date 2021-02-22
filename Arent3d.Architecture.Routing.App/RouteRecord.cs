using System ;
using Arent3d.Revit.Csv.Converters ;
using CsvHelper.Configuration.Attributes ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Routing record from from-to CSV files.
  /// </summary>
  public class RouteRecord
  {
    [Index( 0 ), Name( "Route ID" )]
    public string RouteId { get ; set ; }

    [Index( 1 ), Name( "From Element ID", "From Family Instance ID" )]
    public int FromElementId { get ; set ; }

    [Index( 2 ), Name( "From Connector ID" )]
    public int FromConnectorId { get ; set ; }

    [Ignore]
    public ConnectorIndicator FromId => new ConnectorIndicator( FromElementId, FromConnectorId ) ;

    [Index( 3 ), Name( "To Element ID", "To Family Instance ID" )]
    public int ToElementId { get ; set ; }

    [Index( 4 ), Name( "To Connector ID" )]
    public int ToConnectorId { get ; set ; }

    [Ignore]
    public ConnectorIndicator ToId => new ConnectorIndicator( ToElementId, ToConnectorId ) ;

    //[Index( 5 ), Name( "Pass Point IDs" ), TypeConverter( typeof( IntArrayConverter ) )]
    [Ignore]
    public int[] PassPoints { get ; set ; }

    public RouteRecord( string routeId, ConnectorIndicator fromId, ConnectorIndicator toId, params int[] passPoints )
    {
      RouteId = routeId ;
      FromElementId = fromId.ElementId ;
      FromConnectorId = fromId.ConnectorId ;
      ToElementId = toId.ElementId ;
      ToConnectorId = toId.ConnectorId ;
      PassPoints = passPoints ;
    }

    public RouteRecord( string routeId, RouteInfo routeInfo ) : this( routeId, routeInfo.FromId, routeInfo.ToId, routeInfo.PassPoints )
    {
    }

    public RouteRecord()
    {
      RouteId = string.Empty ;
      PassPoints = Array.Empty<int>() ;
    }
  }
}