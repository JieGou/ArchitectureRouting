using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.Forms.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectConnectorFamily : Window
  {
    public ObservableCollection<ConnectorFamilyInfo> ConnectorFamilyList { get ; } = new() ;
    private readonly Document _document ;
    private readonly CeedStorable _ceedStorable ;

    public SelectConnectorFamily( Document document )
    {
      InitializeComponent() ;
      _document = document ;
      _ceedStorable = _document.GetCeedStorable() ;
      LoadConnectorFamilyList() ;
    }

    private void Button_OK( object sender, RoutedEventArgs e )
    {
      DialogResult = true ;
      Close() ;
    }

    private void Button_ImportFamily( object sender, RoutedEventArgs e )
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Family files (*.rfa)|*.rfa", Multiselect = true } ;
      var sourcePaths = new List<string>() ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        sourcePaths = openFileDialog.FileNames.ToList() ;
      }

      if ( ! sourcePaths.Any() ) return ;
      try {
        var connectorFamilyUploadFiles = new List<string>() ;
        foreach ( var sourcePath in sourcePaths ) {
          var fileName = Path.GetFileName( sourcePath ) ;
          var resultLoadConnectorFamily = LoadConnectorFamilyAndExportImage( sourcePath, fileName ) ;
          if ( ! resultLoadConnectorFamily ) continue ;
          var isExistedFileName = ConnectorFamilyList.SingleOrDefault( f => f.ToString() == fileName ) != null ;
          if ( isExistedFileName ) continue ;
          ConnectorFamilyList.Add( new ConnectorFamilyInfo( fileName ) ) ;
          connectorFamilyUploadFiles.Add( fileName ) ;
        }

        var newConnectorFamilyUploadFiles = connectorFamilyUploadFiles.Where( f => ! _ceedStorable.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
        _ceedStorable.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;
        using Transaction t = new Transaction( _document, "Save connector family upload data" ) ;
        t.Start() ;
        _ceedStorable.Save() ;
        t.Commit() ;
      }
      catch {
        MessageBox.Show( "Load connector's family failed.", "Error" ) ;
        DialogResult = false ;
        Close() ;
      }
    }

    private bool LoadConnectorFamilyAndExportImage( string filePath, string connectorFamilyFileName )
    {
      var imagePath = ConnectorFamilyManager.GetFolderPath() ;
      if ( ! Directory.Exists( imagePath ) ) Directory.CreateDirectory( imagePath ) ;
      var connectorFamilyName = connectorFamilyFileName!.Replace( ".rfa", "" ) ;
      using Transaction t = new Transaction( _document, "Load connector's family" ) ;
      t.Start() ;
      var connectorFamily = LoadFamily( filePath, connectorFamilyName ) ;
      t.Commit() ;

      if ( connectorFamily == null ) return false ;
      var floorPlanImage = ImageConverter.GetFloorPlanImageFile( imagePath, connectorFamilyName ) ;
      return ! string.IsNullOrEmpty( floorPlanImage ) || ImageConverter.ExportConnectorFamilyImage( _document, connectorFamily, imagePath, connectorFamilyName ) ;
    }

    private Family? LoadFamily( string filePath, string familyName )
    {
      if ( new FilteredElementCollector( _document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == familyName ) is Family family ) return family ;
      _document.LoadFamily( filePath, out family ) ;
      foreach ( ElementId familySymbolId in (IEnumerable<ElementId>) family.GetFamilySymbolIds() )
        _document.GetElementById<FamilySymbol>( familySymbolId ) ;
      return family ;
    }

    private void LoadConnectorFamilyList()
    {
      foreach ( string fileName in _ceedStorable.ConnectorFamilyUploadData ) {
        ConnectorFamilyList.Add( new ConnectorFamilyInfo( fileName ) ) ;
      }

      if ( ConnectorFamilyList.Any() ) ConnectorFamilyList.First().IsSelected = true ;
    }

    public class ConnectorFamilyInfo
    {
      public bool IsSelected { get ; set ; }
      private readonly string _connectorFamilyName ;

      public ConnectorFamilyInfo( string connectorFamilyName )
      {
        _connectorFamilyName = connectorFamilyName ;
        IsSelected = true ;
      }

      public override string ToString()
      {
        return _connectorFamilyName ;
      }
    }
  }
}