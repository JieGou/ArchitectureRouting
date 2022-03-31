using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
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
  public class CeedViewModel : ViewModelBase
  {
    private const string NotExistConnectorFamilyInFolderModelWarning = "excelで指定したモデルはmodelフォルダーに存在していませんので、既存のモデルを使用します。" ;
    public List<CeedModel> CeedModels { get ; }
    public CeedStorable CeedStorable { get ; }
    public readonly List<string> CeedModelNumbers = new List<string>() ;
    public readonly List<string> ModelNumbers = new List<string>() ;

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

    private void AddModelNumber( List<CeedModel> ceedModels )
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
    }

    private static List<string> LoadConnectorFamily( Document document, List<string> connectorFamilyPaths )
    {
      List<string> connectorFamilyFiles = new() ;

      var existedConnectorFamilies = GetExistedConnectorFamilies( document, connectorFamilyPaths ) ;
      if ( existedConnectorFamilies.Any() ) {
        var confirmMessage = MessageBox.Show( "モデルがすでに存在していますが、上書きしますか。", "Confirm Message", MessageBoxButton.OKCancel ) ;
        if ( confirmMessage == MessageBoxResult.Cancel ) {
          connectorFamilyPaths = connectorFamilyPaths.Where( p => ! existedConnectorFamilies.ContainsValue( p ) ).ToList() ;
          connectorFamilyFiles.AddRange( existedConnectorFamilies.Keys ) ;
        }
      }
      
      using Transaction loadTransaction = new ( document, "Load connector's family" ) ;
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
        if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == connectorFamilyName ) is Family family ) {
          existsConnectorFamilies.Add( connectorFamilyFile, connectorFamilyPath ) ;
        }
      }

      return existsConnectorFamilies ;
    }

    public static void ReplaceMultipleSymbols( Document document, UIApplication uiApplication, ref CeedViewModel? allCeedModels, ref CeedViewModel? usingCeedModel, ref DataGrid dtGrid )
    {
      const string successfullyMess = "Replaced multiple floor plan symbols successfully." ;
      const string failedMess = "Replaced multiple floor plan symbols failed." ;
      var infoPath = string.Empty ;
      List<string> connectorFamilyPaths = new() ;
      MessageBox.Show( "Please select sample model folder.", "Message" ) ;
      FolderBrowserDialog folderBrowserDialog = new() ;
      if ( folderBrowserDialog.ShowDialog() == DialogResult.OK ) {
        string folderPath = folderBrowserDialog.SelectedPath ;
        infoPath = Directory.GetFiles( folderPath ).FirstOrDefault( f => Path.GetExtension( f ) is ".xls" or ".xlsx" ) ;
        if ( string.IsNullOrEmpty( infoPath ) ) {
          MessageBox.Show( "No info file in selected folder.", "Error" ) ;
          return ;
        }

        DirectoryInfo dirInfo = new( folderPath ) ;
        var familyFolder = dirInfo.GetDirectories().FirstOrDefault() ;
        if ( familyFolder != null ) {
          connectorFamilyPaths = Directory.GetFiles( familyFolder.FullName ).ToList() ;
        }
        else {
          MessageBox.Show( "No family folder in selected folder.", "Error" ) ;
          return ;
        }
      }

      if ( connectorFamilyPaths.Any() ) {
        try {
          List<string> connectorFamilyFiles ;
          List<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements ;
          using var progress = ProgressBar.ShowWithNewThread( uiApplication ) ;
          progress.Message = "Processing......." ;
          using ( var progressData = progress.Reserve( 0.3 ) ) {
            connectorFamilyReplacements = ExcelToModelConverter.GetConnectorFamilyReplacements( infoPath! ) ;
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
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarning, "Message" ) ;
      }
    }
    
    private static bool IsUpdateCeedStorableAfterReplaceMultipleSymbolsSuccessfully( Document document, ref CeedViewModel? allCeedModels, ref CeedViewModel? usingCeedModel, IReadOnlyCollection<ExcelToModelConverter.ConnectorFamilyReplacement> connectorFamilyReplacements, ICollection<string> connectorFamilyFileName )
    {
      List<string> deviceSymbolsNotHaveConnectorFamily = new () ;
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
        using Transaction t = new( document, "Save CeeD data" ) ;
        t.Start() ;
        ceedStorable.Save() ;
        t.Commit() ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        MessageBox.Show( "Save CeeD data failed.", "Error" ) ;
        return false;
      }

      if ( deviceSymbolsNotHaveConnectorFamily.Any() ) {
        MessageBox.Show( NotExistConnectorFamilyInFolderModelWarning + "( " + string.Join( ", ", deviceSymbolsNotHaveConnectorFamily ) + " )", "Message" ) ;
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