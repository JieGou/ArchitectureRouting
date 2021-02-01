using System.Collections.Generic ;
using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using CsvHelper ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Import Start-End" )]
  [Image( "resources/MEP.ico" )]
  public class FileRoutingCommand : RoutingCommandBase
  {
    /// <summary>
    /// Collects from-to records to be auto-routed.
    /// </summary>
    /// <returns>Routing from-to records.</returns>
    protected override IAsyncEnumerable<RouteRecord>? ReadRouteRecords( UIDocument uiDocument )
    {
      var csvFileName = OpenFromToCsv() ;
      if ( null == csvFileName ) return null ;

      return ReadRouteRecordsFromFile( csvFileName ) ;
    }

    private static async IAsyncEnumerable<RouteRecord> ReadRouteRecordsFromFile( string csvFileName )
    {
      using var reader = new StreamReader( csvFileName, true ) ;
      using var csv = new CsvReader( reader, CultureInfo.CurrentCulture ) ;
      csv.Configuration.BadDataFound = x => { } ;

      if ( false == await csv.ReadAsync() ) yield break ;
      if ( false == csv.ReadHeader() ) yield break ;

      while ( await csv.ReadAsync() ) {
        var routeFields = RouteParser.ParseFields( csv ) ;
        if ( null == routeFields ) continue ;

        yield return routeFields.Value ;
      }
    }

    private static string? OpenFromToCsv()
    {
      using var dlg = new FileOpenDialog( "Routing from-to list (*.csv)|*.csv" ) { Title = "Open from-to list file" } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return null ;

      return ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ;
    }
  }
}