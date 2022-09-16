using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Text ;
using System.Windows ;
using System.Windows.Forms ;
using System.Windows.Media ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Commands.Shaft ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit.I18n ;
using Autodesk.Revit.DB ;
using Color = Autodesk.Revit.DB.Color ;
using MessageBox = System.Windows.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class LayerNameSettingViewModel : NotifyPropertyChanged
  {
    private readonly List<Layer> _newLayerNames ;
    private readonly List<Layer> _oldLayerNames ;
    private readonly Document _document ;
    private readonly string _settingFilePath ;

    public ObservableCollection<Layer> Layers { get ; }
    private List<Layer> RemovedLayers { get ; set ; }
    public List<AutoCadColorsManager.AutoCadColor> AutoCadColors { get ; }

    public string LayerNames { get; set ; }

    public RelayCommand<Window> ExportFileDwgCommand => new(ExportDwg) ;

    public LayerNameSettingViewModel( Document document )
    {
      _document = document ;
      _settingFilePath = GetSettingPath() ;
      _newLayerNames = new List<Layer>() ;
      _oldLayerNames = new List<Layer>() ;
      AutoCadColors = AutoCadColorsManager.GetAutoCadColorDict() ;
      Layers = new ObservableCollection<Layer>() ;
      RemovedLayers = new List<Layer>() ;
      LayerNames = string.Empty ;
      var layers = GetLayers( _settingFilePath ) ;
      if ( ! layers.Any() ) return ;
      SetDataSource( layers, AutoCadColors ) ;
    }

    private void SetDataSource( IEnumerable<Layer> layers, IReadOnlyCollection<AutoCadColorsManager.AutoCadColor> autoCadColors )
    {
      Layers.Clear() ;
      foreach ( var layer in layers ) {
        if ( string.IsNullOrEmpty( layer.Index ) ) {
          layer.Index = AutoCadColorsManager.NoColor ;
        }
        else {
          var solidColor = autoCadColors.FirstOrDefault( c => c.Index == layer.Index )?.SolidColor ?? new SolidColorBrush() ;
          layer.SolidColor = solidColor ;
        }

        Layers.Add( layer ) ;
        _newLayerNames.Add( layer ) ;
        _oldLayerNames.Add( new Layer( layer.LayerName, layer.FullFamilyName, layer.FamilyName, layer.FamilyType, layer.Index ) ) ;
      }
    }

    private string GetSettingPath( bool isOriginFile = true )
    {
      string resourcesPath = Path.Combine( Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location )!, "resources" )  ;
      var layerSettingsFileName = isOriginFile 
        ? "Electrical.App.Commands.Initialization.ExportDWGCommand.ArentExportLayersFile".GetDocumentStringByKeyOrDefault( _document, "Arent-export-layers.txt" )
        : "Electrical.App.Commands.Initialization.ExportDWGCommand.ModifyArentExportLayersFile".GetDocumentStringByKeyOrDefault( _document, "Modify-Arent-export-layers.txt" ) ;
      var filePath = Path.Combine( resourcesPath, layerSettingsFileName ) ;

      return filePath ;
    }

    private void ExportDwg( Window window )
    {
      var activeView = _document.ActiveView ;
      SaveFileDialog saveFileDialog = new() { Filter = "DWG file (*.dwg)|*.dwg", InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) } ;
      if ( saveFileDialog.ShowDialog() != DialogResult.OK ) return ;
      var filePath = Path.GetDirectoryName( saveFileDialog.FileName ) ;
      var fileName = Path.GetFileName( saveFileDialog.FileName ) ;
      // replace text
      var encoding = GetEncoding( _settingFilePath ) ;
      var settingFilePath = ReplaceLayerNamesAndColors( _oldLayerNames, _newLayerNames, _settingFilePath, encoding ) ;
      
      DWGExportOptions options = new() { LayerMapping = settingFilePath } ;
      List<ElementId> viewIds = new() { activeView.Id } ;
      using var transaction = new Transaction( _document ) ;
      transaction.Start( "Override Element Graphic" ) ;
      var overrideGraphic = new OverrideGraphicSettings() ;
      overrideGraphic.SetProjectionLineColor( new Color( 255, 255, 255 ) ) ;
      var curveElements = _document.GetAllInstances<CurveElement>(_document.ActiveView).Where(x => x.LineStyle.Name == CreateCylindricalShaftCommandBase.SubCategoryForSymbol).ToList() ;
      curveElements.ForEach(x => _document.ActiveView.SetElementOverrides(x.Id, overrideGraphic));
      transaction.Commit() ;
      
      // export dwg
      _document.Export( filePath, fileName, viewIds, options ) ;
      
      transaction.Start( "Reset Element Graphic" ) ;
      var defaultGraphic = new OverrideGraphicSettings() ;
      curveElements.ForEach(x => _document.ActiveView.SetElementOverrides(x.Id, defaultGraphic));
      transaction.Commit() ;

      // close window
      window.DialogResult = true ;
      window.Close() ;
    }

    private List<Layer> GetLayers( string filePath )
    {
      const string exceptString = "Ifc" ;
      var layers = GetLayersFromFile( filePath ) ;
      var modifyFilePath = GetSettingPath( false ) ;
      var modifyLayers = GetLayersFromFile( modifyFilePath ) ;

      RemovedLayers = layers.Distinct()
        .Where( x => string.IsNullOrEmpty( x.LayerName ) )
        .ToList() ;

      var filterLayers = layers.Distinct()
        .Where( x => ! x.LayerName.Contains( exceptString ) && ! string.IsNullOrEmpty( x.LayerName ) )
        .OrderBy( x => x.LayerName )
        .ToList() ;

      if ( ! modifyLayers.Any() ) return filterLayers ;

      var modifyFilterLayers = modifyLayers.Distinct()
        .Where( x => ! x.LayerName.Contains( exceptString ) )
        .ToList() ;
      
      foreach ( var layer in filterLayers ) {
        var modifyLayer = modifyFilterLayers.SingleOrDefault( l => l.FamilyName + l.FamilyType == layer.FamilyName + layer.FamilyType ) ;
        if ( modifyLayer == null ) {
          layer.LayerName = string.Empty ;
          layer.Index = AutoCadColorsManager.NoColor ;
        }
        else {
          layer.LayerName = modifyLayer.LayerName ;
          layer.Index = modifyLayer.Index ;
        }
      }

      return filterLayers ;
    }

    private List<Layer> GetLayersFromFile( string filePath )
    {
      var layers = new List<Layer>() ;
      if ( ! File.Exists( filePath ) ) return layers ;
      using var reader = File.OpenText( filePath ) ;
      while ( reader.ReadLine() is { } line ) {
        if ( line[ 0 ] == '#' ) continue ;
        var words = line.Split( '\t' ) ;
        var familyName = words[ 0 ] ;
        var typeOfLayer = words[ 1 ] ;
        var layerName = words[ 2 ] ;
        var colorIndex = words.Length > 3 ? words[ 3 ] : string.Empty ;
        var familyType = "" ;
        if ( typeOfLayer != "" ) {
          familyType = $" ({typeOfLayer})" ;
        }

        layers.Add( new Layer( layerName, familyName + familyType, familyName, typeOfLayer, colorIndex ) ) ;
      }

      reader.Close() ;
      return layers ;
    }

    private static Encoding GetEncoding( string filename )
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

    private string ReplaceLayerNamesAndColors( IReadOnlyList<Layer> oldLayerNames, IReadOnlyList<Layer> newLayerNames, string filePath, Encoding encoding )
    {
      var modifyFilePath = GetSettingPath( false ) ;
      try {
        var content = File.ReadAllText( filePath ) ;
        if ( RemovedLayers.Any() ) {
          foreach ( var layer in RemovedLayers ) {
            var oldValue = layer.FamilyName + ( string.IsNullOrEmpty( layer.FamilyType ) ? string.Empty : '\t' + layer.FamilyType ) ;
            var index = content.IndexOf( oldValue, StringComparison.Ordinal ) ;
            if ( index < 0 ) continue ;
            var length = content.Substring( index ).IndexOf( '\n' ) ;
            content = content.Substring( 0, index ) + content.Substring( index + length + 1 ) ;
          }
        }
        
        for ( var i = 0 ; i < newLayerNames.Count() ; i++ ) {
          var oldValue = oldLayerNames[ i ].FamilyName + ( string.IsNullOrEmpty( oldLayerNames[ i ].FamilyType ) ? string.Empty : '\t' + oldLayerNames[ i ].FamilyType ) ;
          var index = content.IndexOf( oldValue, StringComparison.Ordinal ) ;
          if ( index < 0 ) continue ;
          var length = content.Substring( index ).IndexOf( '\n' ) ;
          if ( string.IsNullOrEmpty( newLayerNames[ i ].LayerName ) ) {
            content = content.Substring( 0, index ) + content.Substring( index + length + 1 ) ;
          }
          else {
            oldValue = content.Substring( index, length - 1 ) ;
            var newValue = string.Join( "\t", newLayerNames[i].FamilyName, newLayerNames[i].FamilyType, newLayerNames[ i ].LayerName ) + ( newLayerNames[ i ].Index == AutoCadColorsManager.NoColor ? string.Empty : '\t' + newLayerNames[ i ].Index ) ;
            content = content.Replace( oldValue, newValue ) ;
          }
        }

        if ( ! File.Exists( modifyFilePath ) ) {
          using var fileStream = File.Create( modifyFilePath ) ;
          fileStream.Close() ;
        }

        File.WriteAllText( modifyFilePath, content, encoding ) ;
      }
      catch ( Exception e ) {
        MessageBox.Show( e.Message, "Error" ) ;
        return filePath ;
      }

      return modifyFilePath ;
    }
  }

  public class Layer
  {
    public string LayerName { get ; set ; }
    public string FullFamilyName { get ; set ; }
    public string FamilyName { get ; set ; }
    public string FamilyType{ get ; set ; }
    public string Index { get ; set ; }
    public SolidColorBrush SolidColor { get ; set ; }

    public Layer( string layerName, string fullFamilyName, string familyName, string familyType, string colorIndex )
    {
      LayerName = layerName ;
      FullFamilyName = fullFamilyName ;
      FamilyName = familyName ;
      FamilyType = familyType ;
      Index = colorIndex ;
      SolidColor = new SolidColorBrush() ;
    }
  }
}