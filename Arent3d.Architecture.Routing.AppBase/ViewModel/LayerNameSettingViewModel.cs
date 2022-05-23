using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Text ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class LayerNameSettingViewModel : NotifyPropertyChanged
  {
    private readonly List<Layer> _layerNames ;
    private readonly List<Layer> _oldLayerNames ;
    private readonly Document _document ;
    private readonly string _settingFilePath ;
    
    public ObservableCollection<Layer> Layers { get ; }
    
    public RelayCommand<Window> ExportFileDwgCommand => new ( ExportDWG ) ;
    
    public LayerNameSettingViewModel(Document document)
    {
      _document = document ;
      _settingFilePath = GetSettingPath() ;
      _layerNames = new List<Layer>() ;
      _oldLayerNames = new List<Layer>() ;
      Layers = new ObservableCollection<Layer>() ;
      var layers = GetLayerNames( _settingFilePath ) ;
      if ( layers.Any() ) {
        _layerNames = layers ;
        _oldLayerNames = layers.Select( x => x.Copy() ).ToList() ; ;
        SetDataSource( layers ) ;
      }
    }
    
    private void SetDataSource( List<Layer> layers )
    {
      Layers.Clear();
      foreach ( var layer in layers ) {
        Layers.Add( layer );
      }
    }

    private string GetSettingPath()
    {
      string layerSettingsFileName = "Electrical.App.Commands.Initialization.ExportDWGCommand.ArentExportLayersFile".GetDocumentStringByKeyOrDefault( _document, "Arent-export-layers.txt" ) ;
      string directory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ! ;
      var resourcesPath = Path.Combine( directory.Substring( 0, directory.IndexOf( "bin", StringComparison.Ordinal ) ), "resources" ) ;
      string filePath = Path.Combine( resourcesPath, layerSettingsFileName ) ;

      return filePath ;
    }
    
     private void ExportDWG( Window window)
    {
      var activeView = _document.ActiveView ;
      SaveFileDialog saveFileDialog = new() { Filter = "DWG file (*.dwg)|*.dwg", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;
      if ( saveFileDialog.ShowDialog() != DialogResult.OK ) return ;
      var filePath = Path.GetDirectoryName( saveFileDialog.FileName ) ;
      var fileName = Path.GetFileName( saveFileDialog.FileName ) ;
      DWGExportOptions options = new() { LayerMapping = _settingFilePath } ;
      List<ElementId> viewIds = new() { activeView.Id } ;
      // replace text
      var names = _layerNames ;
      var oldNames = _oldLayerNames ;
      var encoding = GetEncoding( _settingFilePath ) ;
      ReplaceLayerNames( oldNames , names, _settingFilePath, encoding) ;
      
      // export dwg
      _document.Export( filePath, fileName, viewIds, options ) ;
      
      // close window
      window.DialogResult = true ;
      window.Close();
    }

    private List<Layer> GetLayerNames( string filePath )
    {
      const string exceptString = "Ifc" ;
      List<Layer> names = new List<Layer>() ;
      using ( StreamReader reader = File.OpenText(filePath) ) {
        string? line ;
        while ( ( line = reader.ReadLine() ) != null ) {
          if ( line[ 0 ] != '#' ) {
            var words = line.Split( '\t' ) ;
            var familyName = words[ 0 ] ;
            var typeOfLayer = words[ 1 ] ;
            var layerName = words[ 2 ] ;
            string familyType = "" ;
            if ( typeOfLayer != "" ) {
              familyType = $" ({typeOfLayer})" ;
            }
            names.Add( new Layer( layerName , familyName + familyType ) ) ;
          }
        }
        reader.Close() ;
      }

      var filterNames = names.Distinct()
        .Where( x => ! x.LayerName.Contains( exceptString ) && ! String.IsNullOrEmpty( x.LayerName ) )
        .GroupBy( x => x.LayerName ).Select( ng => new Layer()
        {
          LayerName = ng.First().LayerName,
          FamilyName = String.Join("\n", ng.Select(x => x.FamilyName).ToArray())
        }).ToList() ;
      
      return filterNames ;
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
          if ( oldLayerNames[ i ].LayerName != newLayerNames[ i ].LayerName ) {
            hasChange = true ;
            text = text.Replace( oldLayerNames[ i ].LayerName, newLayerNames[ i ].LayerName ) ;
          }
        }
        if(hasChange) File.WriteAllText( filePath, text, encoding ) ;
      }
      catch ( Exception e ) {
        MessageBox.Show( e.Message, "Error" ) ;
      }
    }
  }

  public class Layer
  {
    public string LayerName { get ; set ; } 
    
    public string FamilyName { get ; set ; }
    

    public Layer()
    {
      LayerName = String.Empty ;
      FamilyName = String.Empty ;
    }

    public Layer(string layerName, string familyName)
    {
      LayerName = layerName ;
      FamilyName = familyName ;
    }
  }
}