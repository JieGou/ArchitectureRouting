using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using System.Linq ;
using Arent3d.Csv ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using CsvHelper ;

namespace Arent3d.Architecture.Routing.App.Commands.Routing
{
  [Transaction( TransactionMode.ReadOnly )]
  [DisplayName( "Export From-To" )]
  [Image( "resources/MEP.ico" )]
  public class ExportRoutingCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      using var dlg = new FileSaveDialog( "Routing from-to list (*.csv)|*.csv" ) { Title = "Export from-to list file" } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return Result.Succeeded ;

      WriteFromTo( doc, ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ) ;

      return Result.Succeeded ;
    }

    private static void WriteFromTo( Document document, string csvFileName )
    {
      var fromToList = document.CollectRoutes().SelectMany( RouteRecordUtils.ToRouteRecords ) ;

      using var writer = new StreamWriter( csvFileName, false ) ;
      writer.WriteCsvFile( fromToList ) ;
    }
  }
}