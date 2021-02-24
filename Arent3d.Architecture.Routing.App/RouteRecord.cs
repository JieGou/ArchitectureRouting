using System ;
using Arent3d.Architecture.Routing.EndPoint ;
using Arent3d.Revit.Csv.Converters ;
using CsvHelper ;
using CsvHelper.Configuration ;
using CsvHelper.Configuration.Attributes ;
using CsvHelper.TypeConversion ;

namespace Arent3d.Architecture.Routing.App
{
  /// <summary>
  /// Routing record from from-to CSV files.
  /// </summary>
  public class RouteRecord
  {
    [Index( 0 ), Name( "Route ID" )]
    public string RouteId { get ; set ; }

    [Index( 1 ), Name( "From" ), TypeConverter( typeof( EndPointIndicatorConverter ) )]
    public IEndPointIndicator? FromId { get ; set ; }

    [Index( 2 ), Name( "To" ), TypeConverter( typeof( EndPointIndicatorConverter ) )]
    public IEndPointIndicator? ToId { get ; set ; }

    //[Index( 3 ), Name( "Pass Point IDs" ), TypeConverter( typeof( IntArrayConverter ) )]
    [Ignore]
    public int[] PassPoints { get ; set ; }

    public RouteRecord( string routeId, IEndPointIndicator fromId, IEndPointIndicator toId, params int[] passPoints )
    {
      RouteId = routeId ;
      FromId = fromId ;
      ToId = toId ;
      PassPoints = passPoints ;
    }

    public RouteRecord( string routeId, RouteInfo routeInfo ) : this( routeId, routeInfo.FromId, routeInfo.ToId, routeInfo.PassPoints )
    {
    }

    public RouteRecord()
    {
      RouteId = string.Empty ;
      PassPoints = Array.Empty<int>() ;
      FromId = null ;
      ToId = null ;
    }
  }

  internal class EndPointIndicatorConverter : ITypeConverter
  {
    public string ConvertToString( object? value, IWriterRow row, MemberMapData memberMapData )
    {
      if ( null == value ) return string.Empty ;
      return EndPointIndicator.ToString( (IEndPointIndicator) value ) ;
    }

    public object? ConvertFromString( string text, IReaderRow row, MemberMapData memberMapData )
    {
      return EndPointIndicator.ParseIndicator( text ) ;
    }
  }
}