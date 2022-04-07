using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ExportDWGCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      try {
        return document.Transaction( "Electrical.App.Commands.Initialization.ExportDWGCommandBase".GetAppStringByKeyOrDefault( "Export DWG" ), _ =>
        {
          ExportDWG( document ) ;
          return Result.Succeeded ;
        } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private void ExportDWG( Document document )
    {
      const string layerSettingsFileName = "Arent-export-layers.txt" ;
      var activeView = document.ActiveView ;
      SaveFileDialog saveFileDialog = new() { Filter = "DWG file (*.dwg)|*.dwg", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;
      if ( saveFileDialog.ShowDialog() != DialogResult.OK ) return ;
      var filePath = Path.GetDirectoryName( saveFileDialog.FileName ) ;
      var fileName = Path.GetFileName( saveFileDialog.FileName ) ;
      string directory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ! ;
      var resourcesPath = Path.Combine( directory.Substring( 0, directory.IndexOf( "bin", StringComparison.Ordinal ) ), "resources" ) ;
      string settingFilePath = Path.Combine( resourcesPath, layerSettingsFileName ) ;
      DWGExportOptions options = new() { LayerMapping = settingFilePath } ;
      List<ElementId> viewIds = new() { activeView.Id } ;
      document.Export( filePath, fileName, viewIds, options ) ;
    }
  }
}