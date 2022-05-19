using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Text ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
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
      string layerSettingsFileName = "Electrical.App.Commands.Initialization.ExportDWGCommand.ArentExportLayersFile".GetDocumentStringByKeyOrDefault( document, "Arent-export-layers.txt" ) ;
      var activeView = document.ActiveView ;
      SaveFileDialog saveFileDialog = new() { Filter = "DWG file (*.dwg)|*.dwg", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;
      if ( saveFileDialog.ShowDialog() != DialogResult.OK ) return ;
      var filePath = Path.GetDirectoryName( saveFileDialog.FileName ) ;
      var fileName = Path.GetFileName( saveFileDialog.FileName ) ;
      string directory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ! ;
      var resourcesPath = Path.Combine( directory.Substring( 0, directory.IndexOf( "bin", StringComparison.Ordinal ) ), "resources" ) ;
      string settingFilePath = Path.Combine( resourcesPath, layerSettingsFileName ) ;
      var names  =  GetLayerNames( settingFilePath ) ;
      var oldNames = names.Select( x => x.Copy() ).ToList() ;
      var viewModel = new LayerNameSettingViewModel( names ) ;
      var dlgLayerNameSettingDialog= new LayerNameSettingDialog( viewModel ) ;
      dlgLayerNameSettingDialog.ShowDialog() ;
      if (  dlgLayerNameSettingDialog.DialogResult == true  ) ReplaceLayerNames( oldNames , names, settingFilePath, GetEncoding(settingFilePath) ) ;
      
      DWGExportOptions options = new() { LayerMapping = settingFilePath } ;
      List<ElementId> viewIds = new() { activeView.Id } ;
      document.Export( filePath, fileName, viewIds, options ) ;
    }

    private List<Layer> GetLayerNames( string filePath )
    {
      const string exceptString = "Ifc" ;
      List<Layer> names = new List<Layer>() ;
      using ( StreamReader reader = File.OpenText(filePath) ) {
        string line ;
        while ( ( line = reader.ReadLine() ) != null ) {
          if ( line[ 0 ] != '#' ) {
            var words = line.Split( '\t' ) ;
            var name = words[ 2 ] ;
            if ( name != "" && !name.Contains( exceptString ) && names.FirstOrDefault( c => c.Name == name ) == null ) {
              names.Add( new Layer( name ) ) ;
            }
          }
        }
      
        reader.Close() ;
      }

      return names ;
    }
    
    private Encoding GetEncoding( string filename )
    {
      var bom = new byte[ 4 ] ;
      using ( var file = new FileStream( filename, FileMode.Open, FileAccess.Read ) ) {
        file.Read( bom, 0, 4 ) ;
      }

      if ( bom[ 0 ] == 0x2b && bom[ 1 ] == 0x2f && bom[ 2 ] == 0x76 ) return Encoding.UTF7 ;
      if ( bom[ 0 ] == 0xef && bom[ 1 ] == 0xbb && bom[ 2 ] == 0xbf ) return Encoding.UTF8 ;
      if ( bom[ 0 ] == 0xff && bom[ 1 ] == 0xfe && bom[ 2 ] == 0 && bom[ 3 ] == 0 ) return Encoding.UTF32 ; //UTF-32LE
      if ( bom[ 0 ] == 0xff && bom[ 1 ] == 0xfe ) return Encoding.Unicode ; //UTF-16LE
      if ( bom[ 0 ] == 0xfe && bom[ 1 ] == 0xff ) return Encoding.BigEndianUnicode ; //UTF-16BE
      if ( bom[ 0 ] == 0 && bom[ 1 ] == 0 && bom[ 2 ] == 0xfe && bom[ 3 ] == 0xff ) return new UTF32Encoding( true, true ) ; //UTF-32BE

      return Encoding.ASCII ;
    }

    private void ReplaceLayerNames( List<Layer> oldLayerNames, List<Layer> newLayerNames, string filePath, Encoding encoding)
    {
      try {
        bool hasChange = false ;
        string text = File.ReadAllText( filePath ) ;
        for ( int i = 0 ; i < newLayerNames.Count() ; i++ ) {
          if ( oldLayerNames[ i ].Name != newLayerNames[ i ].Name ) {
            hasChange = true ;
            text = text.Replace( oldLayerNames[ i ].Name, newLayerNames[ i ].Name ) ;
          }
        }
        if(hasChange) File.WriteAllText( filePath, text, encoding ) ;
      }
      catch ( Exception e ) {
        MessageBox.Show( e.Message, "Error" ) ;
      }
    }
  }
}