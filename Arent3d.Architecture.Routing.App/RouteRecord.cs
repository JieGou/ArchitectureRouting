using System ;
using Arent3d.Architecture.Routing.RouteEnd ;
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
    [Index( 0 ), Name( "Route Name" )]
    public string RouteName { get ; set ; } = string.Empty ;

    [Index( 1 ), Name( "From Key" )]
    public string FromKey { get ; set ; } = string.Empty ;

    [Index( 2 ), Name( "From End" ), TypeConverter( typeof( EndPointIndicatorConverter ) )]
    public IEndPointIndicator? FromIndicator { get ; set ; }

    [Index( 3 ), Name( "To Key" )]
    public string ToKey { get ; set ; } = string.Empty ;

    [Index( 4 ), Name( "To End" ), TypeConverter( typeof( EndPointIndicatorConverter ) )]
    public IEndPointIndicator? ToIndicator { get ; set ; }

    [Index( 5 ), Name( "Nominal Diameter" )]
    public double NominalDiameter { get ; set ; } = -1 ;

    [Index( 6 ), Name( "On Pipe Space" )]
    public bool IsRoutingOnPipeSpace { get ; set ; } = false ;
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