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
      var activeView = document.ActiveView ;
      OpenFileDialog openFileDialog = new() { Filter = "Layer setting file (*.txt)|*.txt", Multiselect = false } ;
      string settingFilePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == DialogResult.OK ) {
        settingFilePath = openFileDialog.FileName ;
      }

      DWGExportOptions options = new() { LayerMapping = settingFilePath } ;
      var filePath = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) ;
      var fileName = activeView.Name + "-layer.dwg" ;
      List<ElementId> viewIds = new() { activeView.Id } ;
      document.Export( filePath, fileName, viewIds, options ) ;
    }
  }
}