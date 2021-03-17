using System.Collections.Generic ;
using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using Arent3d.Revit.Csv ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using CsvHelper ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Routing.FileRoutingCommand", DefaultString = "Import From-To" )]
  [Image( "resources/MEP.ico" )]
  public class FileRoutingCommand : RoutingCommandBase
  {
    protected override string GetTransactionNameKey() => "TransactionName.Commands.Routing.RoutingFromFile" ;

    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)>? GetRouteSegments( UIDocument uiDocument )
    {
      var csvFileName = OpenFromToCsv() ;
      if ( null == csvFileName ) return null ;

      return ReadRouteRecordsFromFile( csvFileName ).ToSegmentsWithName( uiDocument.Document ) ;
    }

    private static async IAsyncEnumerable<RouteRecord> ReadRouteRecordsFromFile( string csvFileName )
    {
      using var reader = new StreamReader( csvFileName, true ) ;
      // Cannot use return directly, because `reader` will be closed in that case.
      await foreach ( var item in reader.ReadCsvFileAsync<RouteRecord>() ) {
        yield return item ;
      }
    }

    private static string? OpenFromToCsv()
    {
      using var dlg = new FileSaveDialog( $"{"Dialog.Commands.Routing.FromTo.FileName".GetAppStringByKeyOrDefault( null )} (*.csv)|*.csv" )
      {
        Title = "Dialog.Commands.Routing.FromTo.Title.Import".GetAppStringByKeyOrDefault( null )
      } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return null ;

      return ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ;
    }
  }
}