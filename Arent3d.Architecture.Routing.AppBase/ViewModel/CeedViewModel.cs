using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using DataGrid = System.Windows.Controls.DataGrid ;
using MessageBox = System.Windows.MessageBox ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : NotifyPropertyChanged
  {
    private const string NotExistConnectorFamilyInFolderModelWarningMessage = "excelで指定したモデルはmodelフォルダーに存在していないため、既存のモデルを使用します。" ;
    public List<CeedModel> CeedModels { get ; }
    public CeedStorable CeedStorable { get ; }
    public readonly List<string> CeedModelNumbers = new() ;
    public readonly List<string> ModelNumbers = new() ;
    public List<string> DeviceSymbols = new() ;
    
    private ObservableCollection<FolderModel>? _folders ;
    
    public ObservableCollection<FolderModel> Folders
    {
      get
      {
        if ( null != _folders )
          return _folders ;
    
        var folderModels = GetFolderModels( ) ;
        _folders = new ObservableCollection<FolderModel>( folderModels ) ;
    
        FolderSelected = FindSelectedFolder( _folders ) ;

        return _folders ;
      }
      set
      {
        _folders = value ;
        FolderSelected = FindSelectedFolder( _folders ) ;
        OnPropertyChanged() ;
      }
    }
    
    private FolderModel? _folderSelected ;
    
    private FolderModel? FolderSelected
    {
      get { return _folderSelected ??= FindSelectedFolder( Folders ) ; }
      set => _folderSelected = value ;
    }
    
    public ICommand SelectedItemCommand
    {
      get
      {
        return new RelayCommand<System.Windows.Controls.TreeView>( tv => null != tv, _ =>
        {
          FolderSelected = FindSelectedFolder( Folders ) ;
        } ) ;
      }
    }

    public CeedViewModel( CeedStorable ceedStorable )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedStorable.CeedModelData ;
      AddModelNumber( CeedModels ) ;
    }

    public CeedViewModel( CeedStorable ceedStorable, List<CeedModel> ceedModels )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedModels ;
      AddModelNumber( ceedModels ) ;
    }

    private void AddModelNumber( IReadOnlyCollection<CeedModel> ceedModels )
    {
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ) ) {
        if ( ! CeedModelNumbers.Contains( ceedModel.CeedModelNumber ) ) CeedModelNumbers.Add( ceedModel.CeedModelNumber ) ;
      }

      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.ModelNumber ) ) ) {
        var modelNumbers = ceedModel.ModelNumber.Split( '\n' ) ;
        foreach ( var modelNumber in modelNumbers ) {
          if ( ! ModelNumbers.Contains( modelNumber ) ) ModelNumbers.Add( modelNumber ) ;
        }
      }

      DeviceSymbols = ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.GeneralDisplayDeviceSymbol ) ).Select( c => c.GeneralDisplayDeviceSymbol ).Distinct().ToList() ;
    }
    
    private List<FolderModel> GetFolderModels()
    {
      List<FolderModel> folderModels = new() ;
      
      var parentFolderModel1 = new FolderModel { Name = "Parent Category 1", ParentName = string.Empty, IsExpanded = false, IsSelected = true } ;
      var childFolderModel11 = new FolderModel { Name = "Category 1", ParentName = parentFolderModel1.Name, IsExpanded = false, IsSelected = true } ;
      var childFolderModel12 = new FolderModel { Name = "Category 2", ParentName = parentFolderModel1.Name, IsExpanded = false, IsSelected = false } ;
      parentFolderModel1.Folders.Add( childFolderModel11 ) ;
      parentFolderModel1.Folders.Add( childFolderModel12 ) ;
      folderModels.Add( parentFolderModel1 ) ;
      
      var parentFolderModel2 = new FolderModel { Name = "Parent Category 2", ParentName = string.Empty, IsExpanded = false, IsSelected = false } ;
      var childFolderModel21 = new FolderModel { Name = "Category 1", ParentName = parentFolderModel2.Name, IsExpanded = false, IsSelected = false } ;
      var childFolderModel22 = new FolderModel { Name = "Category 2", ParentName = parentFolderModel2.Name, IsExpanded = false, IsSelected = false } ;
      parentFolderModel2.Folders.Add( childFolderModel21 ) ;
      parentFolderModel2.Folders.Add( childFolderModel22 ) ;
      folderModels.Add( parentFolderModel2 ) ;
      
      return folderModels ;
    }
    
    private FolderModel? FindSelectedFolder( IEnumerable<FolderModel> folders )
    {
      foreach ( var folder in folders ) {
        if ( folder.IsSelected )
          return folder ;

        if ( ! folder.Folders.Any() )
          continue ;

        var subFolder = FindSelectedFolder( folder.Folders ) ;
        if ( null != subFolder )
          return subFolder ;
      }

      return null ;
    }

    private static List<string> LoadConnectorFamily( Document document, List<string> connectorFamilyPaths )
    {
      List<string> connectorFamilyFiles = new() ;

      var existedConnectorFamilies = GetExistedConnectorFamilies( document, connectorFamilyPaths ) ;
      if ( existedConnectorFamilies.Any() ) {
        var confirmMessage = MessageBox.Show( $"モデルがすでに存在していますが、上書きしますか。\n対象モデル：" + string.Join( ", ", existedConnectorFamilies.Keys ), "Confirm Message", MessageBoxButton.OKCancel ) ;
        if ( confirmMessage == MessageBoxResult.Cancel ) {
          connectorFamilyPaths = connectorFamilyPaths.Where( p => ! existedConnectorFamilies.ContainsValue( p ) ).ToList() ;
          connectorFamilyFiles.AddRange( existedConnectorFamilies.Keys ) ;
        }
      }

      using Transaction loadTransaction = new(document, "Load connector's family") ;
      loadTransaction.Start() ;
      foreach ( var connectorFamilyPath in connectorFamilyPaths ) {
        var isLoadFamilySuccessfully = true ;
        var connectorFamilyFile = Path.GetFileName( connectorFamilyPath ) ;
        var connectorFamily = LoadFamily( document, connectorFamilyPath, ref isLoadFamilySuccessfully ) ;
        if ( connectorFamily != null || ( connectorFamily == null && isLoadFamilySuccessfully && existedConnectorFamilies.ContainsValue( connectorFamilyPath ) ) )
          connectorFamilyFiles.Add( connectorFamilyFile ) ;
      }

      loadTransaction.Commit() ;
      return connectorFamilyFiles ;
    }

    private static Family? LoadFamily( Document document, string filePath, ref bool isLoadFamilySuccessfully )
    {
      try {
        document.LoadFamily( filePath, new FamilyOption( true ), out var family ) ;
        if ( family == null ) return family ;
        foreach ( ElementId familySymbolId in family.GetFamilySymbolIds() ) {
          document.GetElementById<FamilySymbol>( familySymbolId ) ;
        }

        return family ;
      }
      catch {
        isLoadFamilySuccessfully = false ;
        return null ;
      }
    }

    private static Dictionary<string, string> GetExistedConnectorFamilies( Document document, IEnumerable<string> connectorFamilyPaths )
    {
      Dictionary<string, string> existsConnectorFamilies = new() ;
      foreach ( var connectorFamilyPath in connectorFamilyPaths ) {
        var connectorFamilyFile = Path.GetFileName( connectorFamilyPath ) ;
        var connectorFamilyName = connectorFamilyFile.Replace( ".rfa", "" ) ;
        if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == connectorFamilyName ) is Family ) {
          existsConnectorFamilies.Add( connectorFamilyFile, connectorFamilyPath ) ;
        }
      }

      return existsConnectorFamilies ;
    }

    public static void ReplaceMultipleSymbols( Document document, UIApplication uiApplication, ref CeedViewModel? allCeedModels, ref CeedViewModel? usingCeedModel, ref DataGrid dtGrid )
    {
      const string successfullyMess = "モデルを正常に置き換えました。" ;
      const string failedMess = "モデルの置き換えが失敗しました。" ;
      List<string> connectorFamilyPaths ;
      MessageBox.Show( "モデルフォルダーを選択してください。", "Message" ) ;
      FolderBrowserDialog folderBrowserDialog = new() ;
      if ( folderBrowserDialog.ShowDialog() != DialogResult.OK ) return ;
      string folderPath = folderBrowserDialog.SelectedPath ;
      string infoPath = Directory.GetFiles( folderPath ).FirstOrDefault( f => Path.GetExtension( f ) is ".xls" or ".xlsx" ) ?? string.Empty ;
      if ( string.IsNullOrEmpty( infoPath ) ) {
        MessageBox.Show( "指定したフォルダーにはモデル指定情報のエクセルファイルが存在していません。", "Error" ) ;
        return ;
      }

      DirectoryInfo dirInfo = new(folderPath) ;
      var familyFolder = dirInfo.GetDirectories().FirstOrDefault() ;
      if ( familyFolder != null ) {
        connectorFamilyPaths = Directory.GetFiles( familyFolder.FullName ).ToList() ;
      }
      else {
        MessageBox.Show( "指定したフォルダーにはモデルデータが存在していません。モデルデータをmodelフォルダに入れてください。", "Error" ) ;
        return ;
      }
        
      if ( connectorFamilyPaths.Any() ) {
        try {
          List<string> connectorFamilyFiles ;
          List<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements ;
          using var progress = ProgressBar.ShowWithNewThread( uiApplication ) ;
          progress.Message = "Processing......." ;
          using ( var progressData = progress.Reserve( 0.3 ) ) {
            connectorFamilyReplacements = ExcelToModelConverter.GetConnectorFamilyReplacements( infoPath ) ;
            connectorFamilyFiles = LoadConnectorFamily( document, connectorFamilyPaths ) ;
            progressData.ThrowIfCanceled() ;
          }

          if ( connectorFamilyFiles.Any() && connectorFamilyReplacements.Any() ) {
            bool result ;
            using ( var progressData = progress.Reserve( 0.6 ) ) {
              result = IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( document, ref allCeedModels, ref usingCeedModel, connectorFamilyReplacements, connectorFamilyFiles ) ;
              progressData.ThrowIfCanceled() ;
            }

            if ( result ) {
              using var progressData = progress.Reserve( 0.9 ) ;
              result = IsUpdateUpdateDataGridAfterReplaceMultipleSymbolsSuccessfully( connectorFamilyReplacements, connectorFamilyFiles, dtGrid ) ;
              progressData.ThrowIfCanceled() ;
            }

            progress.Finish() ;
            MessageBox.Show( result ? successfullyMess : failedMess, "Message" ) ;
          }
          else {
            progress.Finish() ;
            MessageBox.Show( failedMess, "Message" ) ;
          }
        }
        catch ( Exception exception ) {
          MessageBox.Show( exception.Message, "Error" ) ;
        }
      }
      else {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarningMessage, "Message" ) ;
      }
    }

    private static bool IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( Document document, ref CeedViewModel? allCeedModels, ref CeedViewModel? usingCeedModel, IReadOnlyCollection<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName )
    {
      List<string> deviceSymbolsNotHaveConnectorFamily = new() ;
      var ceedStorable = document.GetAllStorables<CeedStorable>().First() ;
      if ( ceedStorable == null ) return false ;
      if ( allCeedModels != null ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) {
            var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
            foreach ( var deviceSymbol in deviceSymbols ) {
              var generalDisplayDeviceSymbol = deviceSymbol.Normalize( NormalizationForm.FormKC ) ;
              var ceedModels = allCeedModels.CeedModels.Where( c => c.GeneralDisplayDeviceSymbol == generalDisplayDeviceSymbol ).ToList() ;
              if ( ! ceedModels.Any() ) continue ;
              var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
              foreach ( var ceedModel in ceedModels ) {
                ceedModel.FloorPlanType = connectorFamilyName ;
              }
            }
          }
          else {
            deviceSymbolsNotHaveConnectorFamily.AddRange( connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ) ;
          }
        }

        ceedStorable.CeedModelData = allCeedModels.CeedModels ;
      }

      if ( usingCeedModel != null ) {
        foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
          if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
          var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
          foreach ( var deviceSymbol in deviceSymbols ) {
            var ceedModels = usingCeedModel.CeedModels.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
            if ( ! ceedModels.Any() ) continue ;
            var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
            foreach ( var ceedModel in ceedModels ) {
              ceedModel.FloorPlanType = connectorFamilyName ;
            }
          }
        }

        ceedStorable.CeedModelData = usingCeedModel.CeedModels ;
      }

      var newConnectorFamilyUploadFiles = connectorFamilyFileName.Where( f => ! ceedStorable.ConnectorFamilyUploadData.Contains( f ) ).ToList() ;
      ceedStorable.ConnectorFamilyUploadData.AddRange( newConnectorFamilyUploadFiles ) ;

      try {
        using Transaction t = new(document, "Save CeeD data") ;
        t.Start() ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
        return false ;
      }

      if ( deviceSymbolsNotHaveConnectorFamily.Any() ) {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarningMessage + "対象の一般表示用機器記号：" + string.Join( ", ", deviceSymbolsNotHaveConnectorFamily ), "Message" ) ;
      }

      return true ;
    }

    private static bool IsUpdateUpdateDataGridAfterReplaceMultipleSymbolsSuccessfully( IEnumerable<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName, ItemsControl dtGrid )
    {
      if ( dtGrid.ItemsSource is not List<CeedModel> newCeedModels ) {
        MessageBox.Show( "CeeD model data is incorrect.", "Error" ) ;
        return false ;
      }

      foreach ( var connectorFamilyReplacement in connectorFamilyReplacements ) {
        if ( ! connectorFamilyFileName.Contains( connectorFamilyReplacement.ConnectorFamilyFile ) ) continue ;
        var deviceSymbols = connectorFamilyReplacement.DeviceSymbols.Split( '\n' ) ;
        foreach ( var deviceSymbol in deviceSymbols ) {
          var ceedModels = newCeedModels.Where( c => c.GeneralDisplayDeviceSymbol == deviceSymbol ).ToList() ;
          if ( ! ceedModels.Any() ) continue ;
          var connectorFamilyName = connectorFamilyReplacement.ConnectorFamilyFile.Replace( ".rfa", "" ) ;
          foreach ( var ceedModel in ceedModels ) {
            ceedModel.FloorPlanType = connectorFamilyName ;
          }
        }
      }

      dtGrid.ItemsSource = new List<CeedModel>( newCeedModels ) ;
      return true ;
    }

    public class FamilyOption : IFamilyLoadOptions
    {
      private readonly bool _forceUpdate ;

      public FamilyOption( bool forceUpdate ) => _forceUpdate = forceUpdate ;

      public bool OnFamilyFound( bool familyInUse, out bool overwriteParameterValues )
      {
        if ( familyInUse && ! _forceUpdate ) {
          overwriteParameterValues = false ;
          return false ;
        }

        overwriteParameterValues = true ;
        return true ;
      }

      public bool OnSharedFamilyFound( Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues )
      {
        source = FamilySource.Project ;
        return OnFamilyFound( familyInUse, out overwriteParameterValues ) ;
      }
    }
  }
}