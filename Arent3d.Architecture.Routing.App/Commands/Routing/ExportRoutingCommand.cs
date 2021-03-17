using System.ComponentModel ;
using System.IO ;
using System.Linq ;
using Arent3d.Revit.Csv ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

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

      using var dlg = new FileSaveDialog( $"{"Dialog.Commands.Routing.FromTo.FileName".GetAppStringByKeyOrDefault( null )} (*.csv)|*.csv" )
      {
        Title = "Dialog.Commands.Routing.FromTo.Title.Export".GetAppStringByKeyOrDefault( null )
      } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return Result.Succeeded ;

      WriteFromTo( doc, ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ) ;

      return Result.Succeeded ;
    }

    private static void WriteFromTo( Document document, string csvFileName )
    {
      var fromToList = document.CollectRoutes().ToSegmentsWithName().ToRouteRecords( document ) ;

      using var writer = new StreamWriter( csvFileName, false ) ;
      writer.WriteCsvFile( fromToList ) ;
    }
  }
}