using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Text ;
using System.Threading ;
using System.Windows ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms.ValueConverters ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MessageBox = System.Windows.MessageBox ;
using ProgressBar = Arent3d.Revit.UI.Forms.ProgressBar ;

namespace Arent3d.Architecture.Routing.AppBase.Forms
{
  public partial class CsvModelDialog
  {
    private readonly Document _document ;
    private List<WiresAndCablesModel> _allWiresAndCablesModels ;
    private List<ConduitsModel> _allConduitModels ;
    private List<HiroiSetMasterModel> _allHiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel> _allHiroiSetMasterEcoModels ;
    private List<HiroiSetCdMasterModel> _allHiroiSetCdMasterNormalModels ;
    private List<HiroiSetCdMasterModel> _allHiroiSetCdMasterEcoModels ;
    private List<HiroiMasterModel> _allHiroiMasterModels ;
    private List<CeedModel> _ceedModelData ;

    public CsvModelDialog( UIApplication uiApplication ) : base( uiApplication )
    {
      InitializeComponent() ;

      _document = uiApplication.ActiveUIDocument.Document ;
      _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
      _allConduitModels = new List<ConduitsModel>() ;
      _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      _allHiroiMasterModels = new List<HiroiMasterModel>() ;
      _ceedModelData = new List<CeedModel>() ;
    }

    private void Button_Save( object sender, RoutedEventArgs e )
    {
      using var progress = ProgressBar.ShowWithNewThread( UIApplication ) ;
      progress.Message = "Saving data..." ;
      using ( var progressData = progress?.Reserve( 0.5 ) ) {
        CsvStorable csvStorable = _document.GetCsvStorable() ;
        {
          if ( _allWiresAndCablesModels.Any() )
            csvStorable.WiresAndCablesModelData = _allWiresAndCablesModels ;
          if ( _allConduitModels.Any() )
            csvStorable.ConduitsModelData = _allConduitModels ;
          if ( _allHiroiSetMasterNormalModels.Any() )
            csvStorable.HiroiSetMasterNormalModelData = _allHiroiSetMasterNormalModels ;
          if ( _allHiroiSetMasterEcoModels.Any() )
            csvStorable.HiroiSetMasterEcoModelData = _allHiroiSetMasterEcoModels ;
          if ( _allHiroiSetCdMasterNormalModels.Any() )
            csvStorable.HiroiSetCdMasterNormalModelData = _allHiroiSetCdMasterNormalModels ;
          if ( _allHiroiSetCdMasterEcoModels.Any() )
            csvStorable.HiroiSetCdMasterEcoModelData = _allHiroiSetCdMasterEcoModels ;
          if ( _allHiroiMasterModels.Any() )
            csvStorable.HiroiMasterModelData = _allHiroiMasterModels ;

          try {
            using Transaction t = new Transaction( _document, "Save data" ) ;
            t.Start() ;
            csvStorable.Save() ;
            t.Commit() ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            MessageBox.Show( "Save CSV Files Failed.", "Error Message" ) ;
            DialogResult = false ;
          }
        }
        progressData?.ThrowIfCanceled() ;
      }

      using ( var progressData = progress?.Reserve( 0.9 ) ) {
        CeedStorable ceedStorable = _document.GetCeedStorable() ;
        {
          if ( _ceedModelData.Any() ) {
            ceedStorable.CeedModelData = _ceedModelData ;
            try {
              using Transaction t = new Transaction( _document, "Save CeeD data" ) ;
              t.Start() ;
              ceedStorable.Save() ;
              _document.MakeCertainAllConnectorFamilies() ;
              t.Commit() ;
            }
            catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            }
          }
        }
        progressData?.ThrowIfCanceled() ;
      }

      DialogResult = true ;
      Close() ;
    }

    private void Button_LoadWiresAndCablesData( object sender, RoutedEventArgs e )
    {
      _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 2, ModelName.WiresAndCables, true ) ;
    }

    private void Button_LoadConduitsData( object sender, RoutedEventArgs e )
    {
      _allConduitModels = new List<ConduitsModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 2, ModelName.Conduits, true ) ;
    }

    private void Button_LoadHiroiSetMasterNormalData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetMasterNormal, true ) ;
    }

    private void Button_LoadHiroiSetMasterEcoData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetMasterEco, true ) ;
    }

    private void Button_LoadHiroiSetCdMasterNormalData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetCdMasterNormal, true ) ;
    }

    private void Button_LoadHiroiSetCdMasterEcoData( object sender, RoutedEventArgs e )
    {
      _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiSetCdMasterEco, true ) ;
    }

    private void Button_LoadHiroiMasterData( object sender, RoutedEventArgs e )
    {
      _allHiroiMasterModels = new List<HiroiMasterModel>() ;
      string filePath = OpenFileDialog() ;
      if ( string.IsNullOrEmpty( filePath ) ) return ;
      GetData( filePath, 0, ModelName.HiroiMaster, true ) ;
    }

    private string OpenFileDialog()
    {
      OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Csv files (*.csv)|*.csv", Multiselect = false } ;
      string filePath = string.Empty ;
      if ( openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ) {
        filePath = openFileDialog.FileName ;
      }

      return filePath ;
    }
    
    private enum OldConstructionClassificationType
    {
      天井ふところ,
      床隠蔽,
      二重床
    }
    
    private enum NewConstructionClassificationType
    {
      天井コロガシ,
      打ち込み,
      フリーアクセス
    }

    private bool GetData( string path, int startLine, ModelName modelName, bool showMessageFlag )
    {
      var checkFile = true ;
      const int wacColCount = 10 ;
      const int conduitColCount = 5 ;
      const int hsmColCount = 27 ;
      const int hsCdmColCount = 4 ;
      const int hmColCount = 12 ;
      try {
        using StreamReader reader = new StreamReader( path, Encoding.GetEncoding( "shift-jis" ), true ) ;
        List<string> lines = new List<string>() ;
        var startRow = 0 ;
        while ( ! reader.EndOfStream ) {
          var line = reader.ReadLine() ;
          if ( startRow > startLine ) {
            var values = line!.Split( ',' ) ;

            switch ( modelName ) {
              case ModelName.WiresAndCables :
                if ( values.Length < wacColCount ) checkFile = false ;
                else {
                  WiresAndCablesModel wiresAndCablesModel = new WiresAndCablesModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ] ) ;
                  _allWiresAndCablesModels.Add( wiresAndCablesModel ) ;
                }

                break ;
              case ModelName.Conduits :
                if ( values.Length < conduitColCount ) checkFile = false ;
                else {
                  ConduitsModel conduitsModel = new ConduitsModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ] ) ;
                  _allConduitModels.Add( conduitsModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterNormal :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterNormalModel = new HiroiSetMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ], values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], values[ 21 ], values[ 22 ], values[ 23 ], values[ 24 ], values[ 25 ], values[ 26 ] ) ;
                  _allHiroiSetMasterNormalModels.Add( hiroiSetMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetMasterEco :
                if ( values.Length < hsmColCount ) checkFile = false ;
                else {
                  HiroiSetMasterModel hiroiSetMasterEcoModel = new HiroiSetMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ], values[ 12 ], values[ 13 ], values[ 14 ], values[ 15 ], values[ 16 ], values[ 17 ], values[ 18 ], values[ 19 ], values[ 20 ], values[ 21 ], values[ 22 ], values[ 23 ], values[ 24 ], values[ 25 ], values[ 26 ] ) ;
                  _allHiroiSetMasterEcoModels.Add( hiroiSetMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterNormal :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  var constructionClassification = GetConstructionClassification( values[ 3 ] ) ;
                  HiroiSetCdMasterModel hiroiSetCdMasterNormalModel = new HiroiSetCdMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], constructionClassification ) ;
                  _allHiroiSetCdMasterNormalModels.Add( hiroiSetCdMasterNormalModel ) ;
                }

                break ;
              case ModelName.HiroiSetCdMasterEco :
                if ( values.Length < hsCdmColCount ) checkFile = false ;
                else {
                  var constructionClassification = GetConstructionClassification( values[ 3 ] ) ;
                  HiroiSetCdMasterModel hiroiSetCdMasterEcoModel = new HiroiSetCdMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ] ) ;
                  _allHiroiSetCdMasterEcoModels.Add( hiroiSetCdMasterEcoModel ) ;
                }

                break ;
              case ModelName.HiroiMaster :
                if ( values.Length < hmColCount ) checkFile = false ;
                else {
                  HiroiMasterModel hiroiMasterModel = new HiroiMasterModel( values[ 0 ], values[ 1 ], values[ 2 ], values[ 3 ], values[ 4 ], values[ 5 ], values[ 6 ], values[ 7 ], values[ 8 ], values[ 9 ], values[ 10 ], values[ 11 ] ) ;
                  _allHiroiMasterModels.Add( hiroiMasterModel ) ;
                }

                break ;
              default :
                throw new ArgumentOutOfRangeException( nameof( modelName ), modelName, null ) ;
            }
          }

          if ( ! checkFile ) {
            break ;
          }

          startRow++ ;
        }

        reader.Close() ;
        reader.Dispose() ;
        if ( ! checkFile ) {
          if (showMessageFlag ) {
            MessageBox.Show( "Incorrect file format.", "Error Message" ) ;
          }

          return false ;
        }
        else {
          if (showMessageFlag ) {
            MessageBox.Show( "Load file successful.", "Result Message" ) ;
          }

          return true ;
        }
      }
      catch ( Exception ) {
        if (showMessageFlag ) {
          MessageBox.Show( "Load file failed.", "Error Message" ) ;
        }

        return false ;
      }
    }

    private string GetConstructionClassification( string oldConstructionClassification )
    {
      string newConstructionClassification ;
      if ( oldConstructionClassification == OldConstructionClassificationType.天井ふところ.GetFieldName() ) {
        newConstructionClassification = NewConstructionClassificationType.天井コロガシ.GetFieldName() ;
      }
      else if ( oldConstructionClassification == OldConstructionClassificationType.床隠蔽.GetFieldName() ) {
        newConstructionClassification = NewConstructionClassificationType.打ち込み.GetFieldName() ;
      }
      else if ( oldConstructionClassification == OldConstructionClassificationType.二重床.GetFieldName() ) {
        newConstructionClassification = NewConstructionClassificationType.フリーアクセス.GetFieldName() ;
      }
      else {
        newConstructionClassification = oldConstructionClassification ;
      }

      return newConstructionClassification ;
    }

    private enum ModelName
    {
      WiresAndCables,
      Conduits,
      HiroiSetMasterNormal,
      HiroiSetMasterEco,
      HiroiSetCdMasterNormal,
      HiroiSetCdMasterEco,
      HiroiMaster
    }

    private void BtnLoadAll_OnClick( object sender, RoutedEventArgs e )
    {
      var dialog = new FolderBrowserDialog() ;
      dialog.ShowNewFolderButton = false ;
      dialog.ShowDialog() ;
      string[] fileNames = new[]
      {
        "hiroimaster.csv", 
        "hiroisetcdmaster_normal.csv",
        "hiroisetcdmaster_eco.csv", 
        "hiroisetmaster_eco.csv",
        "hiroisetmaster_normal.csv", 
        "電線管一覧.csv", 
        "電線・ケーブル一覧.csv"
      } ;
      bool isLoadedCeedFile = false ;
      var ceedCodeFile = "【CeeD】セットコード一覧表" ;
      string equipmentSymbolsFile = "機器記号一覧表" ;
      StringBuilder correctMessage = new StringBuilder() ;
      StringBuilder errorMessage = new StringBuilder() ;
      string defaultCorrectMessage = "指定されたフォルダから以下のデータを正常にロードできました。" ;
      string defaultErrorMessage = "以下のファイルの読み込みが失敗しました。" ;
      correctMessage.AppendLine( defaultCorrectMessage ) ;
      errorMessage.AppendLine( defaultErrorMessage ) ;
      foreach ( var fileName in fileNames ) {
        var path = Path.Combine( dialog.SelectedPath, fileName ) ;
        if ( File.Exists( path ) ) {
          bool isGetDataWithoutError ;
          switch ( fileName ) {
            case "hiroimaster.csv" :
              _allHiroiMasterModels = new List<HiroiMasterModel>() ; 
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiMaster, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 Hiroi Master" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
            case "hiroisetcdmaster_normal.csv" :
              _allHiroiSetCdMasterNormalModels = new List<HiroiSetCdMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetCdMasterNormal, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 Hiroi Set CD Master Normal" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
            case "hiroisetcdmaster_eco.csv" :
              _allHiroiSetCdMasterEcoModels = new List<HiroiSetCdMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetCdMasterEco, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 Hiroi Set CD Master ECO" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
            case "hiroisetmaster_eco.csv" :
              _allHiroiSetMasterEcoModels = new List<HiroiSetMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetMasterEco, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 Hiroi Set Master ECO" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
            case "hiroisetmaster_normal.csv" :
              _allHiroiSetMasterNormalModels = new List<HiroiSetMasterModel>() ;
              isGetDataWithoutError = GetData( path, 0, ModelName.HiroiSetMasterNormal, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 Hiroi Set Master Normal" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
            case "電線管一覧.csv" :
              _allConduitModels = new List<ConduitsModel>() ;
              isGetDataWithoutError = GetData( path, 2, ModelName.Conduits, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 電線管一覧" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
            case "電線・ケーブル一覧.csv" :
              _allWiresAndCablesModels = new List<WiresAndCablesModel>() ;
              isGetDataWithoutError = GetData( path, 2, ModelName.WiresAndCables, false ) ;
              if(isGetDataWithoutError){
                correctMessage.AppendLine( "\u2022 電線・ケーブル一覧" ) ;
              }
              else {
                errorMessage.AppendLine( $"\u2022 {fileName}" ) ;
              }
              break ;
          }
        }
      }

      // load 【CeeD】セットコード一覧表 and 機器記号一覧表 files
      var ceedCodeXlsxFilePath = Path.Combine( dialog.SelectedPath, ceedCodeFile + ".xlsx" ) ;
      var ceedCodeXlsFilePath = Path.Combine( dialog.SelectedPath, ceedCodeFile + ".xls" ) ;
      var equipmentSymbolsXlsxFilePath = Path.Combine( dialog.SelectedPath, equipmentSymbolsFile + ".xlsx" ) ;
      var equipmentSymbolsXlsFilePath = Path.Combine( dialog.SelectedPath, equipmentSymbolsFile + ".xls" ) ;
      if ( File.Exists( ceedCodeXlsxFilePath ) ) {
        isLoadedCeedFile = LoadCeedCodeFile( correctMessage, errorMessage, ceedCodeFile, equipmentSymbolsFile, ceedCodeXlsxFilePath, equipmentSymbolsXlsxFilePath, equipmentSymbolsXlsFilePath ) ;
      }

      if ( File.Exists( ceedCodeXlsFilePath ) && ! isLoadedCeedFile ) {
        isLoadedCeedFile = LoadCeedCodeFile( correctMessage, errorMessage, ceedCodeFile, equipmentSymbolsFile, ceedCodeXlsFilePath, equipmentSymbolsXlsxFilePath, equipmentSymbolsXlsFilePath ) ;
      }

      string resultMessage = string.Empty ;
      if ( !correctMessage.ToString().Trim().Equals( defaultCorrectMessage ) ) {
        resultMessage += correctMessage +"\r";
      }
      if ( !errorMessage.ToString().Trim().Equals( defaultErrorMessage ) ) {
        resultMessage += errorMessage ;
      }
      if ( string.IsNullOrEmpty( resultMessage.Trim() ) ) {
        resultMessage = "指定されたフォルダに条件に一致するファイルが存在しません。" ;
      }
      MessageBox.Show(
        resultMessage,"Result Message" ) ;
    }

    private bool LoadCeedCodeFile( StringBuilder correctMessage, StringBuilder errorMessage, string ceedCodeFile, string equipmentSymbolsFile, string ceedCodeFilePath, string equipmentSymbolsXlsxFilePath, string equipmentSymbolsXlsFilePath )
    {
      if ( File.Exists( equipmentSymbolsXlsxFilePath ) ) {
        _ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, equipmentSymbolsXlsxFilePath ) ;
        if ( _ceedModelData.Any() ) {
          correctMessage.AppendLine( "\u2022 " + ceedCodeFile ) ;
          correctMessage.AppendLine( "\u2022 " + equipmentSymbolsFile ) ;
          return true ;
        }
      }

      if ( File.Exists( equipmentSymbolsXlsFilePath ) ) {
        _ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, equipmentSymbolsXlsFilePath) ;
        if ( _ceedModelData.Any() ) {
          correctMessage.AppendLine( "\u2022 " + ceedCodeFile ) ;
          correctMessage.AppendLine( "\u2022 " + equipmentSymbolsFile ) ;
          return true ;
        }
      }

      _ceedModelData = ExcelToModelConverter.GetAllCeedModelNumber( ceedCodeFilePath, string.Empty ) ;
      if ( _ceedModelData.Any() ) {
        correctMessage.AppendLine( "\u2022 " + ceedCodeFile ) ;
        return true ;
      }

      errorMessage.AppendLine( $"\u2022 {Path.GetFileName( ceedCodeFilePath )}" ) ;

      if ( File.Exists( equipmentSymbolsXlsxFilePath ) )
        errorMessage.AppendLine( $"\u2022 {Path.GetFileName( equipmentSymbolsXlsxFilePath )}" ) ;
      if ( File.Exists( equipmentSymbolsXlsFilePath ) )
        errorMessage.AppendLine( $"\u2022 {Path.GetFileName( equipmentSymbolsXlsFilePath )}" ) ;
      return false ;
    }
  }
}
