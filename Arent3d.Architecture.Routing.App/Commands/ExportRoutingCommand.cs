using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using CsvHelper ;

namespace Arent3d.Architecture.Routing.App.Commands
{
  [Transaction( TransactionMode.ReadOnly )]
  [DisplayName( "Export Start-End" )]
  [Image( "resources/MEP.ico" )]
  public class ExportRoutingCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      using var dlg = new FileSaveDialog( "Routing from-to list (*.csv)|*.csv" ) { Title = "Save from-to list file" } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return Result.Succeeded ;

      WriteFromTo( doc, ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ) ;

      return Result.Succeeded ;
    }

    private static void WriteFromTo( Document document, string csvFileName )
    {
      var fromToList = document.CollectRoutes().SelectMany( RouteRecordUtils.ToRouteRecords ) ;

      using var reader = new StreamWriter( csvFileName, false ) ;
      using var csv = new CsvWriter( reader, CultureInfo.CurrentCulture ) ;
      csv.Configuration.HasHeaderRecord = true ;

      foreach ( var header in RouteParser.GetHeaders() ) {
        csv.WriteField( header ) ;
      }
      csv.NextRecord() ;

      foreach ( var record in fromToList ) {
        foreach ( var value in RouteParser.GetRow( record ) ) {
          csv.WriteField( value ) ;
        }
        csv.NextRecord() ;
      }
    }
  }
}