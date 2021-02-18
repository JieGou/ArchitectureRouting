using System ;
using System.Collections.Generic ;
using System.Globalization ;
using System.IO ;
using System.Reflection ;
using System.Threading.Tasks ;
using Arent3d.Utility ;
using CsvHelper ;
using CsvHelper.Configuration ;

namespace Arent3d.Csv
{
  public static class CsvExtensions
  {
    #region Read

    public static async IAsyncEnumerable<TRecord> ReadCsvFileAsync<TRecord>( this StreamReader reader ) where TRecord : new()
    {
      using var csv = new CsvReader( reader, CultureInfo.CurrentCulture ) ;
      csv.Configuration.BadDataFound = _ => { } ;

      if ( false == await csv.ReadAsync() ) yield break ;
      if ( false == csv.ReadHeader() ) yield break ;

      await foreach ( var item in csv.GetRecordsAsync<TRecord>() ) {
        yield return item ;
      }
    }

    public static IEnumerable<TRecord> ReadCsvFile<TRecord>( this StreamReader reader ) where TRecord : new()
    {
      using var csv = new CsvReader( reader, CultureInfo.CurrentCulture ) ;
      csv.Configuration.BadDataFound = _ => { } ;

      if ( false == csv.Read() ) return Array.Empty<TRecord>() ;
      if ( false == csv.ReadHeader() ) return Array.Empty<TRecord>() ;

      return csv.GetRecords<TRecord>() ;
    }

    #endregion

    #region Write

    public static async Task WriteCsvFileAsync<TRecord>( this StreamWriter writer, IEnumerable<TRecord> records )
    {
      await using var csv = new CsvWriter( writer, CultureInfo.CurrentCulture ) ;
      csv.Configuration.HasHeaderRecord = true ;

      await csv.WriteRecordsAsync( records ) ;
    }

    public static async Task WriteCsvFileAsync<TRecord>( this StreamWriter writer, IAsyncEnumerable<TRecord> records )
    {
      await using var csv = new CsvWriter( writer, CultureInfo.CurrentCulture ) ;
      csv.Configuration.HasHeaderRecord = true ;

      await foreach ( var record in records ) {
        csv.WriteRecord( record ) ;
      }
    }

    public static void WriteCsvFile<TRecord>( this StreamWriter writer, IEnumerable<TRecord> records )
    {
      using var csv = new CsvWriter( writer, CultureInfo.CurrentCulture ) ;
      csv.Configuration.HasHeaderRecord = true ;

      csv.WriteRecords( records ) ;
    }

    #endregion
  }
}