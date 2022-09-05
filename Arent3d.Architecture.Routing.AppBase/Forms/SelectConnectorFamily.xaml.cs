using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands.PostCommands ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.Forms.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class SelectConnectorFamily : Window
  {
    public ObservableCollection<ConnectorFamilyInfo> ConnectorFamilyList { get ; } = new() ;
    private readonly Document _document ;
    private readonly StorageService<Level, CeedUserModel> _storageService ;
    private readonly IPostCommandExecutorBase? _postCommandExecutor ;

    public SelectConnectorFamily( Document document, StorageService<Level, CeedUserModel> storageService, IPostCommandExecutorBase? postCommandExecutor )
    {
      InitializeComponent() ;
      _document = document ;
      _storageService = storageService ;
      _postCommandExecutor = postCommandExecutor ;
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
        List<LoadFamilyCommandParameter> familyParameters = new() ;
        foreach ( var sourcePath in sourcePaths ) {
          var fileName = Path.GetFileName( sourcePath ) ;
          familyParameters.Add( new LoadFamilyCommandParameter( sourcePath, fileName ) ) ;
        }

        _postCommandExecutor?.LoadFamilyCommand( familyParameters ) ;
        foreach ( var familyParameter in familyParameters ) {
          var isExistedFileName = ConnectorFamilyList.SingleOrDefault( f => f.ToString() == familyParameter.FamilyFileName ) != null ;
          if ( isExistedFileName ) continue ;
          ConnectorFamilyList.Add( new ConnectorFamilyInfo( familyParameter.FamilyFileName ) ) ;
          connectorFamilyUploadFiles.Add( familyParameter.FamilyFileName ) ;
        }

        var newConnectorFamilyUploadFiles = connectorFamilyUploadFiles.Where( f => ! _storageService.Data.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
        _storageService.Data.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;
        //using Transaction t = new( _document, "Save connector family upload data" ) ;
        //t.Start() ;
        //storageService.SaveChange() ;
        //t.Commit() ;
      }
      catch {
        MessageBox.Show( "Load connector's family failed.", "Error" ) ;
        DialogResult = false ;
        Close() ;
      }
    }

    private bool LoadConnectorFamily( string filePath, string connectorFamilyFileName )
    {
      var imagePath = ConnectorFamilyManager.GetFolderPath() ;
      if ( ! Directory.Exists( imagePath ) ) Directory.CreateDirectory( imagePath ) ;
      var connectorFamilyName = connectorFamilyFileName.Replace( ".rfa", "" ) ;
      // using Transaction t = new( _document, "Load connector's family" ) ;
      // t.Start() ;
      var connectorFamily = LoadFamily( filePath, connectorFamilyName ) ;
      //t.Commit() ;

      return connectorFamily != null ;
    }

    private Family? LoadFamily( string filePath, string familyName )
    {
      try {
        if ( new FilteredElementCollector( _document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == familyName ) is Family family ) {
          var confirmMessage = MessageBox.Show( $"モデル{familyName}がすでに存在していますが、上書きしますか。", "Message", MessageBoxButtons.OKCancel ) ;
          if ( confirmMessage == System.Windows.Forms.DialogResult.Cancel ) {
            return family ;
          }

          _document.LoadFamily( filePath, new CeedViewModel.FamilyOption( true ), out var overwriteFamily ) ;
          if ( overwriteFamily == null ) return family ;
          foreach ( ElementId familySymbolId in overwriteFamily.GetFamilySymbolIds() )
            _document.GetElementById<FamilySymbol>( familySymbolId ) ;
          return overwriteFamily ;
        }

        _document.LoadFamily( filePath, new CeedViewModel.FamilyOption( true ), out var newFamily ) ;
        if ( newFamily == null ) return null ;
        foreach ( ElementId familySymbolId in newFamily.GetFamilySymbolIds() )
          _document.GetElementById<FamilySymbol>( familySymbolId ) ;
        return newFamily ;
      }
      catch {
        return null ;
      }
    }

    private void LoadConnectorFamilyList()
    {
      foreach ( var fileName in  _storageService.Data.ConnectorFamilyUploadData ) {
        ConnectorFamilyList.Add( new ConnectorFamilyInfo( fileName ) ) ;
      }

      if ( ConnectorFamilyList.Any() ) 
        ConnectorFamilyList.First().IsSelected = true ;
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