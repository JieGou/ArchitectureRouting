using System ;
using System.Collections.Generic ;
using Arent3d.Utility ;
using System.Linq ;
using CsvHelper ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Parses from-to list CSV files.
  /// </summary>
  public static class RouteParser
  {
    private const string RouteIdColumn = "Route ID" ;
    private const string FromElementIdColumn = "From Family Instance ID" ;
    private const string FromConnectorIdColumn = "From Connector ID" ;
    private const string ToElementIdColumn = "To Family Instance ID" ;
    private const string ToConnectorIdColumn = "To Connector ID" ;
    private const string PassPointIdsColumn = "Pass Point IDs" ;

    /// <summary>
    /// Parses a new route information from a CSV file record.
    /// </summary>
    /// <param name="csv">CSV reader.</param>
    /// <returns>An route information.</returns>
    public static RouteRecord? ParseFields( CsvReader csv )
    {
      if ( false == csv.TryGetField( RouteIdColumn, out string routeId ) ) return null ;

      if ( false == TryGetIntField( csv, FromElementIdColumn, out var fromElementId ) ) return null ;
      if ( false == TryGetIntField( csv, FromConnectorIdColumn, out var fromConnectorId ) ) return null ;

      if ( false == TryGetIntField( csv, ToElementIdColumn, out var toElementId ) ) return null ;
      if ( false == TryGetIntField( csv, ToConnectorIdColumn, out var toConnectorId ) ) return null ;

      if ( false == csv.TryGetField( PassPointIdsColumn, out string passPointIds ) ) return null ;

      var passPoints = passPointIds.Split( ',' ).Select( str => int.TryParse( str, out var num ) ? num : (int?) null ).NonNull().ToArray() ;

      return new RouteRecord( routeId, new ConnectorIndicator( fromElementId, fromConnectorId ), new ConnectorIndicator( toElementId, toConnectorId ), passPoints ) ;
    }

    private static bool TryGetIntField( CsvReader csv, string fieldName, out int value )
    {
      value = default ;

      if ( false == csv.TryGetField( fieldName, out string toIdStr ) ) return false ;
      return int.TryParse( toIdStr, out value ) ;
    }

    public static IEnumerable<string> GetHeaders()
    {
      return new[] { RouteIdColumn, FromElementIdColumn, FromConnectorIdColumn, ToElementIdColumn, ToConnectorIdColumn, PassPointIdsColumn } ;
    }

    public static IEnumerable<object> GetRow( RouteRecord record )
    {
      yield return record.RouteId ;
      yield return record.FromId.ElementId ;
      yield return record.FromId.ConnectorId ;
      yield return record.ToId.ElementId ;
      yield return record.ToId.ConnectorId ;
      yield return string.Join( ",", record.PassPoints ) ;
    }
  }
}